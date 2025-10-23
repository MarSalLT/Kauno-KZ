using KZ.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers
{
    [RoutePrefix("webhooks")]
    public class RedmineWebhookController : ApiController
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const int REDMINE_GLOBAL_ID_FIELD = 11;
        private static readonly int[] COMPLETED_STATUS_IDS = { 3, 5 };
        private static string _cachedCsrfToken = null;
        private static string _cachedCsrfCookie = null;
        private static string _cachedJSessionId = null;
        private static DateTime _tokenExpiry = DateTime.MinValue;

        // Store last 10 webhook calls for debugging
        private static readonly System.Collections.Concurrent.ConcurrentQueue<object> _webhookLog = new System.Collections.Concurrent.ConcurrentQueue<object>();
        private const int MAX_LOG_ENTRIES = 10;

        private string NormalizeGlobalId(string globalId)
        {
            if (string.IsNullOrEmpty(globalId)) return globalId;
            return globalId.Replace("{", "").Replace("}", "").ToUpper();
        }

        private async Task<string> GetCsrfToken(string camundaUrl)
        {
            if (_cachedCsrfToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedCsrfToken;
            }

            try
            {
                var versionUrl = camundaUrl.TrimEnd('/') + "/version";
                var request = new HttpRequestMessage(HttpMethod.Get, versionUrl);

                var camundaUsername = ConfigurationManager.AppSettings["CamundaUsername"];
                var camundaPassword = ConfigurationManager.AppSettings["CamundaPassword"];

                if (!string.IsNullOrEmpty(camundaUsername) && !string.IsNullOrEmpty(camundaPassword))
                {
                    var authBytes = Encoding.UTF8.GetBytes($"{camundaUsername}:{camundaPassword}");
                    var authHeader = Convert.ToBase64String(authBytes);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                }

                var response = await _httpClient.SendAsync(request);

                if (response.Headers.Contains("X-XSRF-TOKEN"))
                {
                    _cachedCsrfToken = response.Headers.GetValues("X-XSRF-TOKEN").FirstOrDefault();
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(5);

                    if (response.Headers.Contains("Set-Cookie"))
                    {
                        var cookies = response.Headers.GetValues("Set-Cookie");
                        foreach (var cookie in cookies)
                        {
                            if (cookie.Contains("XSRF-TOKEN="))
                            {
                                _cachedCsrfCookie = cookie.Split(';')[0];
                            }
                            else if (cookie.Contains("JSESSIONID="))
                            {
                                _cachedJSessionId = cookie.Split(';')[0];
                            }
                        }
                    }

                    return _cachedCsrfToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch CSRF token: {ex.Message}");
                return null;
            }
        }

        private async Task<HttpRequestMessage> CreateAuthenticatedRequest(HttpMethod method, string url, string camundaUrl)
        {
            var request = new HttpRequestMessage(method, url);

            var camundaUsername = ConfigurationManager.AppSettings["CamundaUsername"];
            var camundaPassword = ConfigurationManager.AppSettings["CamundaPassword"];

            if (!string.IsNullOrEmpty(camundaUsername) && !string.IsNullOrEmpty(camundaPassword))
            {
                var authBytes = Encoding.UTF8.GetBytes($"{camundaUsername}:{camundaPassword}");
                var authHeader = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            }

            var csrfToken = await GetCsrfToken(camundaUrl);
            if (!string.IsNullOrEmpty(csrfToken))
            {
                request.Headers.Add("X-XSRF-TOKEN", csrfToken);

                var cookieParts = new List<string>();
                if (!string.IsNullOrEmpty(_cachedJSessionId))
                {
                    cookieParts.Add(_cachedJSessionId);
                }
                if (!string.IsNullOrEmpty(_cachedCsrfCookie))
                {
                    cookieParts.Add(_cachedCsrfCookie);
                }

                if (cookieParts.Count > 0)
                {
                    request.Headers.Add("Cookie", string.Join("; ", cookieParts));
                }
            }

            return request;
        }

        [Route("redmine/logs")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult GetWebhookLogs()
        {
            return Ok(new
            {
                count = _webhookLog.Count,
                logs = _webhookLog.ToArray(),
                note = "Showing last " + MAX_LOG_ENTRIES + " webhook calls"
            });
        }

        private void LogWebhookCall(object logEntry)
        {
            _webhookLog.Enqueue(logEntry);

            // Keep only last MAX_LOG_ENTRIES
            while (_webhookLog.Count > MAX_LOG_ENTRIES)
            {
                _webhookLog.TryDequeue(out _);
            }
        }

        [Route("redmine")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> ReceiveRedmineWebhook([FromBody] JObject payload)
        {
            var timestamp = DateTime.UtcNow;
            var headers = Request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            try
            {
                Debug.WriteLine("==================== REDMINE WEBHOOK RECEIVED ====================");
                Debug.WriteLine($"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}");
                Debug.WriteLine($"Payload: {payload?.ToString(Formatting.Indented) ?? "NULL"}");
                Debug.WriteLine("===================================================================");

                // Extract issue - Redmine can send it directly or wrapped in "data"
                JObject issue = null;
                if (payload["issue"] != null)
                {
                    issue = payload["issue"] as JObject;
                }
                else if (payload["data"]?["issue"] != null)
                {
                    issue = payload["data"]["issue"] as JObject;
                }

                // Log for debugging
                LogWebhookCall(new
                {
                    timestamp,
                    headers,
                    payload = payload?.ToString(Formatting.None) ?? "NULL",
                    hasPayload = payload != null,
                    hasDirectIssue = payload?["issue"] != null,
                    hasDataIssue = payload?["data"]?["issue"] != null,
                    extractedIssue = issue != null
                });

                if (issue == null)
                {
                    return Ok(new { status = "ignored", reason = "No issue data in payload" });
                }

                var issueId = issue["id"].Value<int>();
                var subject = issue["subject"].Value<string>();

                string globalId = null;
                var customFields = issue["custom_fields"] as JArray;
                if (customFields != null)
                {
                    foreach (var field in customFields)
                    {
                        if (field["id"].Value<int>() == REDMINE_GLOBAL_ID_FIELD)
                        {
                            var valueToken = field["value"];
                            if (valueToken != null && valueToken.Type != JTokenType.Null)
                            {
                                globalId = valueToken.Value<string>();
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(globalId))
                {
                    Debug.WriteLine($"No globalId in issue #{issueId}");
                    return Ok(new { status = "warning", reason = "No globalId", issueId });
                }

                globalId = NormalizeGlobalId(globalId);
                Debug.WriteLine($"Processing issue #{issueId} - GlobalID: {globalId}");

                var statusId = issue["status"]["id"].Value<int>();
                var updateSource = COMPLETED_STATUS_IDS.Contains(statusId) ? "completed" : "redmine";

                var issueData = new JObject
                {
                    ["id"] = issueId,
                    ["subject"] = subject
                };

                var descToken = issue["description"];
                if (descToken != null && descToken.Type != JTokenType.Null)
                {
                    issueData["description"] = descToken;
                }

                issueData["statusId"] = statusId;
                issueData["statusName"] = issue["status"]["name"];

                var priorityToken = issue["priority"];
                if (priorityToken != null && priorityToken.Type != JTokenType.Null)
                {
                    issueData["priorityId"] = priorityToken["id"];
                    issueData["priorityName"] = priorityToken["name"];
                }

                var doneRatioToken = issue["done_ratio"];
                if (doneRatioToken != null && doneRatioToken.Type != JTokenType.Null)
                {
                    issueData["done_ratio"] = doneRatioToken;
                }

                var updatedOnToken = issue["updated_on"];
                if (updatedOnToken != null && updatedOnToken.Type != JTokenType.Null)
                {
                    issueData["updated_on"] = updatedOnToken;
                }

                if (customFields != null)
                {
                    issueData["custom_fields"] = customFields;
                }

                var (success, debugInfo) = await SendToCamundaWithRetry(globalId, updateSource, issueId.ToString(), issueData);

                if (success)
                {
                    return Ok(new
                    {
                        status = "success",
                        globalId,
                        issueId,
                        updateSource,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = "error",
                        reason = "Failed to correlate message",
                        globalId,
                        issueId,
                        timestamp = DateTime.UtcNow,
                        debug = debugInfo
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing webhook: {ex.Message}");

                // Log error
                LogWebhookCall(new
                {
                    timestamp,
                    headers,
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    payload = payload?.ToString(Formatting.None) ?? "NULL"
                });

                return Ok(new
                {
                    status = "error",
                    reason = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        private async Task<(bool success, object debugInfo)> SendToCamundaWithRetry(
            string globalId,
            string updateSource,
            string redmineIssueId,
            JObject issueData,
            int maxRetries = 5)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";
            var debugLog = new List<object>();

            globalId = NormalizeGlobalId(globalId);

            var subject = issueData["subject"]?.Value<string>() ?? "";
            var description = issueData["description"]?.Value<string>() ?? "";
            var statusId = issueData["statusId"]?.Value<int>() ?? 1;

            int arcgisStatus;
            switch (statusId)
            {
                case 1: arcgisStatus = 6; break;
                case 2: arcgisStatus = 1; break;
                case 3: arcgisStatus = 4; break;
                case 5: arcgisStatus = 5; break;
                case 6: arcgisStatus = 5; break;
                default: arcgisStatus = 0; break;
            }

            var normalizedGlobalId = NormalizeGlobalId(globalId);

            var message = new
            {
                messageName = "TaskUpdate",
                businessKey = normalizedGlobalId,
                processVariables = new
                {
                    GlobalID = new { value = normalizedGlobalId, type = "String" },
                    updateSource = new { value = updateSource, type = "String" },
                    redmineIssueId = new { value = redmineIssueId, type = "String" },
                    updateData = new { value = issueData.ToString(Formatting.None), type = "String" },
                    Pavadinimas = new { value = subject, type = "String" },
                    Aprasymas = new { value = description, type = "String" },
                    Statusas = new { value = arcgisStatus.ToString(), type = "String" },
                    redmineStatusId = new { value = statusId.ToString(), type = "String" }
                }
            };

            debugLog.Add(new { step = "configuration", camundaUrl, businessKey = globalId });

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(message);
                    var url = $"{camundaUrl}/message";

                    Debug.WriteLine($"Attempt {attempt}: Sending to Camunda");

                    var request = await CreateAuthenticatedRequest(HttpMethod.Post, url, camundaUrl);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    debugLog.Add(new
                    {
                        attempt,
                        statusCode = (int)response.StatusCode,
                        statusText = response.StatusCode.ToString(),
                        responseBody = responseContent
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        return (true, debugLog);
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (responseContent.Contains("MismatchingMessageCorrelationException") ||
                            responseContent.Contains("No process definition or execution matches"))
                        {
                            if (attempt < maxRetries)
                            {
                                var delayMs = (int)(1000 * Math.Pow(1.5, attempt - 1));
                                debugLog.Add(new { action = "retry", delayMs, reason = "No matching process" });
                                await Task.Delay(delayMs);
                                continue;
                            }
                            else
                            {
                                return (false, debugLog);
                            }
                        }
                    }

                    return (false, debugLog);
                }
                catch (Exception ex)
                {
                    debugLog.Add(new { attempt, error = ex.Message, stackTrace = ex.StackTrace });

                    if (attempt == maxRetries)
                        return (false, debugLog);

                    await Task.Delay(1000 * attempt);
                }
            }

            return (false, debugLog);
        }

        [Route("camunda/update")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult ReceiveCamundaUpdate([FromUri] string globalId, [FromBody] JObject payload)
        {
            try
            {
                if (string.IsNullOrEmpty(globalId))
                {
                    return BadRequest("globalId is required");
                }

                globalId = NormalizeGlobalId(globalId);
                Debug.WriteLine($"ReceiveCamundaUpdate: {globalId}");

                JObject taskData = payload["updateData"] as JObject;
                if (taskData == null)
                {
                    return BadRequest("updateData property is missing");
                }

                JArray editsArray = new JArray();
                JObject layerEdit = new JObject();
                layerEdit["id"] = 0;

                JArray updatesArray = new JArray();
                JObject featureUpdate = new JObject();
                JObject attributes = new JObject();
                attributes["GlobalID"] = "{" + globalId + "}";

                if (taskData.ContainsKey("description") && taskData["description"] != null)
                {
                    string description = taskData["description"].ToString();
                    if (!string.IsNullOrEmpty(description))
                    {
                        attributes["Aprasymas"] = description;
                    }
                }

                if (taskData.ContainsKey("subject") && taskData["subject"] != null)
                {
                    string subject = taskData["subject"].ToString();
                    if (!string.IsNullOrEmpty(subject))
                    {
                        attributes["Pavadinimas"] = subject;
                    }
                }

                if (taskData.ContainsKey("statusId") && taskData["statusId"] != null)
                {
                    int status = Convert.ToInt32(taskData["statusId"]);
                    int arcgisStatus;

                    switch (status)
                    {
                        case 1: arcgisStatus = 6; break;
                        case 2: arcgisStatus = 1; break;
                        case 3: arcgisStatus = 4; break;
                        case 5: arcgisStatus = 5; break;
                        case 6: arcgisStatus = 5; break;
                        default: arcgisStatus = 0; break;
                    }

                    attributes["Statusas"] = arcgisStatus;
                    Debug.WriteLine($"Status mapping: {status} -> {arcgisStatus}");
                }

                featureUpdate["attributes"] = attributes;
                updatesArray.Add(featureUpdate);
                layerEdit["updates"] = updatesArray;
                editsArray.Add(layerEdit);

                var featureRepository = new Data.FeatureRepository();
                JObject result = featureRepository.ApplyEdits("tasks", editsArray);

                bool arcgisSuccess = false;
                if (result != null && result["res"] != null)
                {
                    JArray resArray = result["res"] as JArray;
                    if (resArray != null && resArray.Count > 0)
                    {
                        JObject firstRes = resArray[0] as JObject;
                        if (firstRes != null && firstRes["updateResults"] != null)
                        {
                            JArray updateResults = firstRes["updateResults"] as JArray;
                            if (updateResults != null && updateResults.Count > 0)
                            {
                                JObject updateResult = updateResults[0] as JObject;
                                if (updateResult != null && updateResult["success"] != null)
                                {
                                    arcgisSuccess = (bool)updateResult["success"];
                                }
                            }
                        }
                    }
                }

                if (arcgisSuccess)
                {
                    Debug.WriteLine("ArcGIS update successful, updating SQL...");

                    try
                    {
                        var connectionString = ConfigurationManager.AppSettings["DbConnectionString"];
                        var tablePart1 = ConfigurationManager.AppSettings["TableFirstPart"];

                        if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(tablePart1))
                        {
                            using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                            {
                                connection.Open();

                                var sqlParts = new List<string>();
                                var parameters = new System.Data.SqlClient.SqlCommand();

                                parameters.Parameters.AddWithValue("@globalId", globalId);

                                if (attributes.ContainsKey("Pavadinimas"))
                                {
                                    sqlParts.Add("Pavadinimas = @pavadinimas");
                                    parameters.Parameters.AddWithValue("@pavadinimas", attributes["Pavadinimas"].ToString());
                                }

                                if (attributes.ContainsKey("Aprasymas"))
                                {
                                    sqlParts.Add("Aprasymas = @aprasymas");
                                    parameters.Parameters.AddWithValue("@aprasymas", attributes["Aprasymas"].ToString());
                                }

                                if (attributes.ContainsKey("Statusas"))
                                {
                                    sqlParts.Add("Statusas = @statusas");
                                    parameters.Parameters.AddWithValue("@statusas", attributes["Statusas"]);
                                }

                                if (sqlParts.Count > 0)
                                {
                                    string sql = $"UPDATE {tablePart1}.UZDUOTYS_PROJ SET {string.Join(", ", sqlParts)} WHERE REPLACE(REPLACE(UPPER(GlobalID), '{{', ''), '}}', '') = @globalId";

                                    using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                                    {
                                        foreach (System.Data.SqlClient.SqlParameter param in parameters.Parameters)
                                        {
                                            command.Parameters.Add(new System.Data.SqlClient.SqlParameter(param.ParameterName, param.Value));
                                        }

                                        int rowsAffected = command.ExecuteNonQuery();
                                        Debug.WriteLine($"SQL updated: {rowsAffected} row(s)");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception sqlEx)
                    {
                        Debug.WriteLine($"SQL update error: {sqlEx.Message}");
                    }
                }

                return Ok(new
                {
                    status = "success",
                    globalId,
                    result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ReceiveCamundaUpdate: {ex.Message}");
                return Ok(new
                {
                    status = "error",
                    reason = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [Route("health")]
        [HttpGet]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult Health()
        {
            return Ok(new { status = "ok", service = "redmine-webhook-receiver" });
        }
    }
}
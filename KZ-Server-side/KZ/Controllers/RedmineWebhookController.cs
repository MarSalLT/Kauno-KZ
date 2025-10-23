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
        private static readonly int[] COMPLETED_STATUS_IDS = { 3, 5 }; // Resolved, Closed
        private static string _cachedCsrfToken = null;
        private static string _cachedCsrfCookie = null;
        private static string _cachedJSessionId = null;
        private static DateTime _tokenExpiry = DateTime.MinValue;

        private async Task<string> GetCsrfToken(string camundaUrl)
        {
            // Return cached token if still valid (cache for 5 minutes)
            if (_cachedCsrfToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedCsrfToken;
            }

            try
            {
                // Fetch CSRF token by making a GET request to Camunda /version endpoint
                // Note: Even if endpoint returns 404, Camunda includes X-CSRF-Token in headers
                var versionUrl = camundaUrl.TrimEnd('/') + "/version";
                var request = new HttpRequestMessage(HttpMethod.Get, versionUrl);

                // Check if basic auth credentials are configured
                var camundaUsername = ConfigurationManager.AppSettings["CamundaUsername"];
                var camundaPassword = ConfigurationManager.AppSettings["CamundaPassword"];

                if (!string.IsNullOrEmpty(camundaUsername) && !string.IsNullOrEmpty(camundaPassword))
                {
                    var authBytes = Encoding.UTF8.GetBytes($"{camundaUsername}:{camundaPassword}");
                    var authHeader = Convert.ToBase64String(authBytes);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                }

                var response = await _httpClient.SendAsync(request);
                // Don't throw on non-success - we just need the CSRF token from headers

                // Extract CSRF token from response headers (works even on 404)
                // Camunda returns the token as X-XSRF-TOKEN header
                if (response.Headers.Contains("X-XSRF-TOKEN"))
                {
                    _cachedCsrfToken = response.Headers.GetValues("X-XSRF-TOKEN").FirstOrDefault();
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(5);

                    // Also extract cookies (Double Submit Cookie pattern + session)
                    if (response.Headers.Contains("Set-Cookie"))
                    {
                        var cookies = response.Headers.GetValues("Set-Cookie");
                        foreach (var cookie in cookies)
                        {
                            if (cookie.Contains("XSRF-TOKEN="))
                            {
                                // Extract just the cookie value part (XSRF-TOKEN=value)
                                var cookieValue = cookie.Split(';')[0];
                                _cachedCsrfCookie = cookieValue;
                                Debug.WriteLine($"✓ Fetched CSRF cookie: {_cachedCsrfCookie}");
                            }
                            else if (cookie.Contains("JSESSIONID="))
                            {
                                // Also extract JSESSIONID for session management
                                var cookieValue = cookie.Split(';')[0];
                                _cachedJSessionId = cookieValue;
                                Debug.WriteLine($"✓ Fetched JSESSIONID: {_cachedJSessionId}");
                            }
                        }
                    }

                    Debug.WriteLine($"✓ Fetched CSRF token: {_cachedCsrfToken} (Status: {response.StatusCode})");
                    return _cachedCsrfToken;
                }

                Debug.WriteLine($"Warning: No CSRF token found in X-XSRF-TOKEN header (Status: {response.StatusCode})");
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

            // Add basic auth if configured
            var camundaUsername = ConfigurationManager.AppSettings["CamundaUsername"];
            var camundaPassword = ConfigurationManager.AppSettings["CamundaPassword"];

            if (!string.IsNullOrEmpty(camundaUsername) && !string.IsNullOrEmpty(camundaPassword))
            {
                var authBytes = Encoding.UTF8.GetBytes($"{camundaUsername}:{camundaPassword}");
                var authHeader = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            }

            // Add CSRF token
            var csrfToken = await GetCsrfToken(camundaUrl);
            if (!string.IsNullOrEmpty(csrfToken))
            {
                // Camunda uses Double Submit Cookie pattern - needs both header and cookie
                // Use X-XSRF-TOKEN to match what Camunda sends in response
                request.Headers.Add("X-XSRF-TOKEN", csrfToken);

                // Add cookies (both JSESSIONID and XSRF-TOKEN)
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
                    var cookieHeader = string.Join("; ", cookieParts);
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            return request;
        }

        [Route("redmine")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> ReceiveRedmineWebhook([FromBody] JObject payload)
        {
            try
            {
                Debug.WriteLine("===========================================");
                Debug.WriteLine("🔔 REDMINE WEBHOOK RECEIVED");
                Debug.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                Debug.WriteLine($"Payload: {payload?.ToString(Formatting.Indented)}");
                Debug.WriteLine("===========================================");

                // Extract issue from webhook payload
                var issue = payload["issue"] as JObject;
                if (issue == null)
                {
                    Debug.WriteLine("No issue in webhook payload");
                    return Ok(new { status = "ignored", reason = "No issue data" });
                }

                var issueId = issue["id"].Value<int>();
                var subject = issue["subject"].Value<string>();

                // Extract globalId from custom field
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
                    Debug.WriteLine($"No globalId found in issue #{issueId}");
                    return Ok(new { status = "warning", reason = "No globalId", issueId });
                }

                Debug.WriteLine($"Processing issue #{issueId} ({subject}) - globalId: {globalId}");

                // Determine update source
                var statusId = issue["status"]["id"].Value<int>();
                var updateSource = COMPLETED_STATUS_IDS.Contains(statusId) ? "completed" : "redmine";

                // Prepare issue data for Camunda
                var issueData = new JObject();
                issueData["id"] = issueId;
                issueData["subject"] = subject;

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

                // Send message to Camunda with retry
                var (success, debugInfo) = await SendToCamundaWithRetry(globalId, updateSource, issueId.ToString(), issueData);

                if (success)
                {
                    return Ok(new
                    {
                        status = "success",
                        globalId,
                        issueId,
                        updateSource,
                        timestamp = DateTime.UtcNow,
                        debug = debugInfo
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = "error",
                        reason = "Failed to correlate message after retries",
                        globalId,
                        issueId,
                        timestamp = DateTime.UtcNow,
                        debug = debugInfo
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing Redmine webhook: {ex.Message}");
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

            var message = new
            {
                messageName = "TaskUpdate",
                businessKey = globalId,
                processVariables = new
                {
                    GlobalID = new { value = globalId, type = "String" },  // CRITICAL: Set GlobalID for Task_SyncToNet
                    updateSource = new { value = updateSource, type = "String" },
                    redmineIssueId = new { value = redmineIssueId, type = "String" },
                    updateData = new { value = issueData.ToString(Formatting.None), type = "Json" }
                }
            };

            debugLog.Add(new { step = "configuration", camundaUrl, businessKey = globalId });

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(message);
                    var url = $"{camundaUrl}/message";

                    Debug.WriteLine($"📤 Attempt {attempt}: Sending to Camunda: {url}");
                    Debug.WriteLine($"📦 Payload: {json}");

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

                    // Check if it's a correlation error (no matching process instance)
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

                    // For other errors, don't retry
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
                    Debug.WriteLine("No globalId provided");
                    return BadRequest("globalId is required");
                }

                Debug.WriteLine($"ReceiveCamundaUpdate called for globalId: {globalId}");
                Debug.WriteLine($"Payload: {payload}");

                // Extract the actual task data from the nested structure
                JObject taskData = payload["updateData"] as JObject;

                if (taskData == null)
                {
                    Debug.WriteLine("updateData property is missing or invalid");
                    return BadRequest("updateData property is missing or invalid");
                }

                Debug.WriteLine($"TaskData: {taskData}");

                // Build the update payload for ArcGIS
                JArray editsArray = new JArray();
                JObject layerEdit = new JObject();
                layerEdit["id"] = 0; // Layer ID for tasks

                JArray updatesArray = new JArray();
                JObject featureUpdate = new JObject();
                JObject attributes = new JObject();
                attributes["GlobalID"] = globalId;

                // Map fields from Redmine to ArcGIS attributes
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
                    Debug.WriteLine($"Statusas: {taskData["statusId"]}");
                    int status = Convert.ToInt32(taskData["statusId"]);
                    int arcgisStatus;

                    // Map Redmine status to ArcGIS status
                    switch (status)
                    {
                        case 1: // New
                            arcgisStatus = 6;
                            break;
                        case 2: // In Progress
                            arcgisStatus = 1;
                            break;
                        case 3: // Resolved
                            arcgisStatus = 4;
                            break;
                        case 5: // Closed
                            arcgisStatus = 5;
                            break;
                        case 6: // Rejected
                            arcgisStatus = 5;
                            break;
                        default:
                            arcgisStatus = 0;
                            break;
                    }

                    attributes["Statusas"] = arcgisStatus;
                    Debug.WriteLine($"Mapping Statusas: {status} -> {arcgisStatus}");
                }

                featureUpdate["attributes"] = attributes;
                updatesArray.Add(featureUpdate);
                layerEdit["updates"] = updatesArray;
                editsArray.Add(layerEdit);

                // Call FeatureRepository.ApplyEdits to update ArcGIS
                var featureRepository = new Data.FeatureRepository();
                JObject result = featureRepository.ApplyEdits("tasks", editsArray);

                Debug.WriteLine($"ApplyEdits result: {result}");

                // CRITICAL FIX: Update SQL database after successful ArcGIS update
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
                    Debug.WriteLine("✓ ArcGIS update successful, now updating SQL database...");

                    try
                    {
                        var connectionString = ConfigurationManager.AppSettings["DbConnectionString"];
                        var tablePart1 = ConfigurationManager.AppSettings["TableFirstPart"];

                        if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(tablePart1))
                        {
                            using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                            {
                                connection.Open();

                                var sqlParts = new System.Collections.Generic.List<string>();
                                var parameters = new System.Collections.Generic.Dictionary<string, object>();

                                // Handle GlobalID format - database stores lowercase without braces
                                string cleanGlobalId = globalId.Replace("{", "").Replace("}", "").ToLower();
                                parameters["@cleanGlobalId"] = cleanGlobalId;

                                // Build dynamic SQL UPDATE statement based on what fields were updated
                                if (attributes.ContainsKey("Pavadinimas"))
                                {
                                    sqlParts.Add("Pavadinimas = @pavadinimas");
                                    parameters["@pavadinimas"] = attributes["Pavadinimas"].ToString();
                                }

                                if (attributes.ContainsKey("Aprasymas"))
                                {
                                    sqlParts.Add("Aprasymas = @aprasymas");
                                    parameters["@aprasymas"] = attributes["Aprasymas"].ToString();
                                }

                                if (attributes.ContainsKey("Statusas"))
                                {
                                    sqlParts.Add("Statusas = @statusas");
                                    parameters["@statusas"] = attributes["Statusas"];
                                }

                                if (sqlParts.Count > 0)
                                {
                                    // Database stores GlobalID as lowercase without curly braces
                                    string sql = $"UPDATE {tablePart1}.UZDUOTYS_PROJ SET {string.Join(", ", sqlParts)} WHERE LOWER(GlobalID) = @cleanGlobalId";

                                    using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                                    {
                                        foreach (var param in parameters)
                                        {
                                            command.Parameters.AddWithValue(param.Key, param.Value);
                                        }

                                        int rowsAffected = command.ExecuteNonQuery();
                                        Debug.WriteLine($"✓ SQL database updated: {rowsAffected} row(s) affected for GlobalID {globalId}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine("⚠ WARNING: DbConnectionString or TableFirstPart not configured, skipping SQL update");
                        }
                    }
                    catch (Exception sqlEx)
                    {
                        Debug.WriteLine($"❌ ERROR updating SQL database: {sqlEx.Message}");
                        Debug.WriteLine($"Stack trace: {sqlEx.StackTrace}");
                        // Don't fail the whole request if SQL update fails - ArcGIS is source of truth
                    }
                }
                else
                {
                    Debug.WriteLine("⚠ ArcGIS update was not successful, skipping SQL database update");
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

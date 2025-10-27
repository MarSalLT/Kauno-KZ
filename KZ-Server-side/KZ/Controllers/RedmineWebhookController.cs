using KZ.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
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

        // Store last 20 attachment sync operations for debugging
        private static readonly System.Collections.Concurrent.ConcurrentQueue<object> _attachmentSyncLog = new System.Collections.Concurrent.ConcurrentQueue<object>();
        private const int MAX_ATTACHMENT_LOG_ENTRIES = 20;

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

        [Route("attachments/logs")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult GetAttachmentSyncLogs()
        {
            return Ok(new
            {
                count = _attachmentSyncLog.Count,
                logs = _attachmentSyncLog.ToArray(),
                note = "Showing last " + MAX_ATTACHMENT_LOG_ENTRIES + " attachment sync operations",
                version = "v2-direct-arcgis-access",
                timestamp = DateTime.UtcNow
            });
        }

        private void LogAttachmentSync(object logEntry)
        {
            _attachmentSyncLog.Enqueue(logEntry);

            // Keep only last MAX_ATTACHMENT_LOG_ENTRIES
            while (_attachmentSyncLog.Count > MAX_ATTACHMENT_LOG_ENTRIES)
            {
                _attachmentSyncLog.TryDequeue(out _);
            }
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

                // Check if attachments were added/updated by looking at journal details
                bool attachmentChanged = false;
                var journalToken = payload["data"]?["journal"];
                if (journalToken != null && journalToken.Type != JTokenType.Null)
                {
                    var detailsToken = journalToken["details"];
                    if (detailsToken != null && detailsToken.Type == JTokenType.Array)
                    {
                        var details = detailsToken as JArray;
                        foreach (var detail in details)
                        {
                            if (detail["property"]?.ToString() == "attachment")
                            {
                                attachmentChanged = true;
                                Debug.WriteLine($"✅ Attachment change detected in webhook! Attachment: {detail["value"]}");
                                LogAttachmentSync(new
                                {
                                    timestamp = DateTime.UtcNow,
                                    operation = "RedmineWebhookAttachmentDetected",
                                    globalId,
                                    issueId,
                                    attachmentName = detail["value"]?.ToString(),
                                    attachmentId = detail["prop_key"]?.ToString()
                                });
                                break;
                            }
                        }
                    }
                }

                // Extract attachments if present in webhook, or fetch from API if attachment changed
                var attachmentsToken = issue["attachments"];
                if (attachmentsToken != null && attachmentsToken.Type == JTokenType.Array)
                {
                    var attachmentsArray = attachmentsToken as JArray;
                    if (attachmentsArray != null && attachmentsArray.Count > 0)
                    {
                        issueData["attachments"] = attachmentsArray;
                        Debug.WriteLine($"Found {attachmentsArray.Count} attachments in webhook payload");
                    }
                }
                else if (attachmentChanged)
                {
                    // Fetch full issue from Redmine API to get attachments
                    Debug.WriteLine($"Fetching issue {issueId} from Redmine API to get attachments...");
                    try
                    {
                        var redmineUrl = ConfigurationManager.AppSettings["RedmineApiUrl"];
                        var redmineExternalUrl = ConfigurationManager.AppSettings["RedmineExternalUrl"] ?? redmineUrl;
                        var redmineApiKey = ConfigurationManager.AppSettings["RedmineApiKey"];

                        // Use external URL for API calls from .NET
                        var fetchRequest = new HttpRequestMessage(HttpMethod.Get, $"{redmineExternalUrl}/issues/{issueId}.json?include=attachments");
                        fetchRequest.Headers.Add("X-Redmine-API-Key", redmineApiKey);

                        var fetchResponse = await _httpClient.SendAsync(fetchRequest);
                        if (fetchResponse.IsSuccessStatusCode)
                        {
                            var fetchContent = await fetchResponse.Content.ReadAsStringAsync();
                            var fetchedIssue = JObject.Parse(fetchContent);
                            var fetchedAttachments = fetchedIssue["issue"]?["attachments"];

                            if (fetchedAttachments != null && fetchedAttachments.Type == JTokenType.Array)
                            {
                                var attachmentsArray = fetchedAttachments as JArray;
                                if (attachmentsArray.Count > 0)
                                {
                                    issueData["attachments"] = attachmentsArray;
                                    Debug.WriteLine($"✓ Fetched {attachmentsArray.Count} attachments from Redmine API");
                                    LogAttachmentSync(new
                                    {
                                        timestamp = DateTime.UtcNow,
                                        operation = "RedmineFetchedAttachments",
                                        globalId,
                                        issueId,
                                        attachmentCount = attachmentsArray.Count,
                                        attachments = attachmentsArray.Select(a => new {
                                            id = a["id"],
                                            filename = a["filename"],
                                            filesize = a["filesize"]
                                        }).ToArray()
                                    });
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"✗ Failed to fetch issue from Redmine: {fetchResponse.StatusCode}");
                            LogAttachmentSync(new
                            {
                                timestamp = DateTime.UtcNow,
                                operation = "RedmineFetchFailed",
                                globalId,
                                issueId,
                                error = $"HTTP {fetchResponse.StatusCode}"
                            });
                        }
                    }
                    catch (Exception fetchEx)
                    {
                        Debug.WriteLine($"✗ Exception fetching issue from Redmine: {fetchEx.Message}");
                        LogAttachmentSync(new
                        {
                            timestamp = DateTime.UtcNow,
                            operation = "RedmineFetchException",
                            globalId,
                            issueId,
                            error = fetchEx.Message
                        });
                    }
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

        [Route("attachments/redmine-to-arcgis")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> SyncAttachmentsFromRedmineToArcGIS([FromBody] JObject request)
        {
            Debug.WriteLine("==================== SYNC ATTACHMENTS REDMINE→ARCGIS ====================");
            Debug.WriteLine($"Request: {request?.ToString(Formatting.Indented) ?? "NULL"}");

            try
            {
                var globalId = request["globalId"]?.ToString();
                var redmineIssueId = request["redmineIssueId"]?.ToString();
                var attachments = request["attachments"] as JArray;

                Debug.WriteLine($"GlobalID: {globalId}");
                Debug.WriteLine($"RedmineIssueId: {redmineIssueId}");
                Debug.WriteLine($"Attachments count: {attachments?.Count ?? 0}");

                if (string.IsNullOrEmpty(globalId) || attachments == null || attachments.Count == 0)
                {
                    Debug.WriteLine("No attachments to sync");
                    return Ok(new { status = "success", message = "No attachments to sync" });
                }

                var redmineUrl = ConfigurationManager.AppSettings["RedmineApiUrl"];
                var redmineExternalUrl = ConfigurationManager.AppSettings["RedmineExternalUrl"] ?? redmineUrl;
                var redmineApiKey = ConfigurationManager.AppSettings["RedmineApiKey"];
                var tasksServiceRoot = ConfigurationManager.AppSettings["TasksServiceRoot"];

                Debug.WriteLine($"Redmine URL: {redmineUrl}");
                Debug.WriteLine($"Redmine External URL: {redmineExternalUrl}");
                Debug.WriteLine($"TasksServiceRoot: {tasksServiceRoot}");

                // Step 1: Query ArcGIS to get ObjectID from GlobalID
                Debug.WriteLine($"Querying ArcGIS for ObjectID by GlobalID: {globalId}");
                var queryUrl = tasksServiceRoot + "FeatureServer/0/query";
                var queryClient = Utilities.GetRestClient(queryUrl);
                var queryRequest = Utilities.GetRestRequest(queryClient, "", DataFormat.Json, true);
                queryRequest.AddParameter("where", $"GlobalID = '{{{globalId}}}'");
                queryRequest.AddParameter("outFields", "OBJECTID");
                queryRequest.AddParameter("returnGeometry", "false");

                var queryResponse = queryClient.Get(queryRequest);
                Debug.WriteLine($"Query response: {queryResponse.StatusCode}");

                if (!queryResponse.IsSuccessful)
                {
                    Debug.WriteLine($"✗ Failed to query ObjectID: {queryResponse.ErrorMessage}");
                    return Ok(new { status = "error", message = "Failed to query ObjectID from GlobalID" });
                }

                var queryResult = JObject.Parse(queryResponse.Content);
                var features = queryResult["features"] as JArray;

                if (features == null || features.Count == 0)
                {
                    Debug.WriteLine($"✗ No feature found with GlobalID: {globalId}");
                    return Ok(new { status = "error", message = "Feature not found" });
                }

                var objectId = features[0]["attributes"]["OBJECTID"].ToString();
                Debug.WriteLine($"✓ Found ObjectID: {objectId}");

                // Step 2: Get existing attachments from ArcGIS to avoid duplicates
                Debug.WriteLine($"Checking existing attachments in ArcGIS...");
                var existingAttachmentsUrl = tasksServiceRoot + $"FeatureServer/0/{objectId}/attachments";
                var existingClient = Utilities.GetRestClient(existingAttachmentsUrl);
                var existingRequest = Utilities.GetRestRequest(existingClient, "", DataFormat.Json, true);

                var existingResponse = existingClient.Get(existingRequest);
                var existingAttachmentNames = new HashSet<string>();
                var existingAttachmentsByName = new Dictionary<string, string>(); // name -> attachmentId

                if (existingResponse.IsSuccessful)
                {
                    var existingResult = JObject.Parse(existingResponse.Content);
                    var existingAttachments = existingResult["attachmentInfos"] as JArray;
                    if (existingAttachments != null)
                    {
                        foreach (var existing in existingAttachments)
                        {
                            var name = existing["name"]?.ToString();
                            var arcgisAttachmentId = existing["id"]?.ToString();
                            if (!string.IsNullOrEmpty(name))
                            {
                                existingAttachmentNames.Add(name);
                                if (!string.IsNullOrEmpty(arcgisAttachmentId))
                                {
                                    existingAttachmentsByName[name] = arcgisAttachmentId;
                                }
                            }
                        }
                        Debug.WriteLine($"Found {existingAttachmentNames.Count} existing attachments: {string.Join(", ", existingAttachmentNames)}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Warning: Could not fetch existing attachments: {existingResponse.ErrorMessage}");
                }

                // Step 3: Detect and delete attachments that exist in ArcGIS but not in Redmine
                var redmineAttachmentNames = new HashSet<string>();
                foreach (var attachment in attachments)
                {
                    var filename = attachment["filename"]?.ToString();
                    if (!string.IsNullOrEmpty(filename))
                    {
                        redmineAttachmentNames.Add(filename);
                    }
                }

                var attachmentsToDelete = existingAttachmentNames.Except(redmineAttachmentNames).ToList();
                var deletedAttachments = new List<object>();

                if (attachmentsToDelete.Count > 0)
                {
                    Debug.WriteLine($"Found {attachmentsToDelete.Count} attachments to delete from ArcGIS: {string.Join(", ", attachmentsToDelete)}");

                    foreach (var attachmentName in attachmentsToDelete)
                    {
                        try
                        {
                            var arcgisAttachmentId = existingAttachmentsByName[attachmentName];
                            Debug.WriteLine($"Deleting attachment from ArcGIS: {attachmentName} (ID: {arcgisAttachmentId})");

                            var deleteUrl = tasksServiceRoot + $"FeatureServer/0/{objectId}/deleteAttachments";
                            var deleteClient = Utilities.GetRestClient(deleteUrl);
                            var deleteRequest = Utilities.GetRestRequest(deleteClient, "", DataFormat.Json, true);
                            deleteRequest.AddParameter("attachmentIds", arcgisAttachmentId);

                            var deleteResponse = deleteClient.Post(deleteRequest);

                            if (deleteResponse.IsSuccessful)
                            {
                                var deleteResult = JObject.Parse(deleteResponse.Content);
                                var deleteResults = deleteResult["deleteAttachmentResults"] as JArray;
                                if (deleteResults != null && deleteResults.Count > 0)
                                {
                                    var firstResult = deleteResults[0];
                                    if (firstResult["success"]?.Value<bool>() == true)
                                    {
                                        Debug.WriteLine($"✓ Successfully deleted: {attachmentName}");
                                        deletedAttachments.Add(new { filename = attachmentName, status = "deleted" });
                                    }
                                    else
                                    {
                                        var error = firstResult["error"]?.ToString() ?? "Unknown error";
                                        Debug.WriteLine($"✗ Delete failed: {error}");
                                        deletedAttachments.Add(new { filename = attachmentName, status = "delete_failed", error });
                                    }
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"✗ Delete request failed: {deleteResponse.ErrorMessage}");
                                deletedAttachments.Add(new { filename = attachmentName, status = "delete_failed", error = deleteResponse.ErrorMessage });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"✗ Failed to delete attachment: {ex.Message}");
                            deletedAttachments.Add(new { filename = attachmentName, status = "delete_exception", error = ex.Message });
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("No attachments need to be deleted from ArcGIS");
                }

                var syncedAttachments = new List<object>();

                // Step 3: Download and attach each file
                foreach (var attachment in attachments)
                {
                    try
                    {
                        var attachmentId = attachment["id"]?.ToString();
                        var contentUrl = attachment["content_url"]?.ToString();
                        var filename = attachment["filename"]?.ToString();

                        Debug.WriteLine($"Processing attachment: {filename} (ID: {attachmentId})");
                        Debug.WriteLine($"Content URL: {contentUrl}");

                        if (string.IsNullOrEmpty(contentUrl))
                        {
                            Debug.WriteLine("Skipping: No content URL");
                            continue;
                        }

                        // Check if this attachment already exists in ArcGIS
                        if (existingAttachmentNames.Contains(filename))
                        {
                            Debug.WriteLine($"⊙ Skipping duplicate: {filename} (already exists in ArcGIS)");
                            syncedAttachments.Add(new { filename, status = "skipped_duplicate" });
                            continue;
                        }

                        // Download from Redmine (use external URL for accessibility)
                        // Content URLs from Redmine API are like "/attachments/download/39/kaunas.png"
                        var downloadUrl = contentUrl.StartsWith("http") ? contentUrl : redmineExternalUrl + contentUrl;
                        Debug.WriteLine($"Downloading from Redmine: {downloadUrl}");
                        var downloadRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                        downloadRequest.Headers.Add("X-Redmine-API-Key", redmineApiKey);

                        var downloadResponse = await _httpClient.SendAsync(downloadRequest);
                        Debug.WriteLine($"Download response: {downloadResponse.StatusCode}");

                        if (!downloadResponse.IsSuccessStatusCode)
                        {
                            Debug.WriteLine($"Failed to download: {downloadResponse.StatusCode}");
                            syncedAttachments.Add(new { filename, status = "download_failed", statusCode = (int)downloadResponse.StatusCode });
                            continue;
                        }

                        var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
                        Debug.WriteLine($"Downloaded {fileBytes.Length} bytes");

                        // Upload to ArcGIS using addAttachment endpoint
                        var attachUrl = tasksServiceRoot + $"FeatureServer/0/{objectId}/addAttachment";
                        Debug.WriteLine($"Attaching to ArcGIS: {attachUrl}");

                        var attachClient = Utilities.GetRestClient(attachUrl);
                        var attachRequest = Utilities.GetRestRequest(attachClient, "", DataFormat.Json, true);

                        // Determine content type from filename
                        var contentType = "application/octet-stream";
                        var ext = System.IO.Path.GetExtension(filename)?.ToLower();
                        if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
                        else if (ext == ".png") contentType = "image/png";
                        else if (ext == ".pdf") contentType = "application/pdf";

                        attachRequest.AddFileBytes("attachment", fileBytes, filename, contentType);

                        var attachResponse = attachClient.Post(attachRequest);
                        Debug.WriteLine($"Attach response: {attachResponse.StatusCode}");

                        if (attachResponse.IsSuccessful)
                        {
                            var attachResult = JObject.Parse(attachResponse.Content);
                            Debug.WriteLine($"Attach result: {attachResult}");

                            if (attachResult["addAttachmentResult"]?["success"]?.Value<bool>() == true)
                            {
                                syncedAttachments.Add(new { filename, status = "synced", objectId = attachResult["addAttachmentResult"]["objectId"] });
                                Debug.WriteLine($"✓ Successfully attached: {filename}");
                            }
                            else
                            {
                                var error = attachResult["addAttachmentResult"]?["error"]?.ToString() ?? "Unknown error";
                                Debug.WriteLine($"✗ Attach failed: {error}");
                                syncedAttachments.Add(new { filename, status = "attach_failed", error });
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"✗ Attach request failed: {attachResponse.ErrorMessage}");
                            syncedAttachments.Add(new { filename, status = "attach_failed", error = attachResponse.ErrorMessage });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"✗ Failed to sync attachment: {ex.Message}");
                        Debug.WriteLine($"Stack: {ex.StackTrace}");
                        syncedAttachments.Add(new { filename = attachment["filename"]?.ToString(), status = "exception", error = ex.Message });
                    }
                }

                var syncedCount = syncedAttachments.Count(a => ((dynamic)a).status == "synced");
                var deletedCount = deletedAttachments.Count(a => ((dynamic)a).status == "deleted");
                Debug.WriteLine($"Synced {syncedCount} of {attachments.Count} attachments, deleted {deletedCount} attachments");
                Debug.WriteLine("=========================================================================");

                return Ok(new
                {
                    status = "success",
                    globalId,
                    objectId,
                    syncedCount,
                    deletedCount,
                    attachments = syncedAttachments,
                    deleted = deletedAttachments
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error syncing attachments: {ex.Message}");
                Debug.WriteLine($"Stack: {ex.StackTrace}");
                Debug.WriteLine("=========================================================================");
                return Ok(new { status = "error", message = ex.Message });
            }
        }

        private class DownloadResult
        {
            public byte[] FileBytes { get; set; }
            public int StatusCode { get; set; }
            public string Error { get; set; }
        }

        private async Task<DownloadResult> GetAttachmentFromArcGISAsync(string attachmentUrl)
        {
            try
            {
                Debug.WriteLine($"Fetching attachment via HTTP: {attachmentUrl}");

                // Use the existing _httpClient which works for Redmine uploads
                var response = await _httpClient.GetAsync(attachmentUrl);

                Debug.WriteLine($"HTTP Response Status: {(int)response.StatusCode} {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    Debug.WriteLine($"✓ Downloaded {fileBytes.Length} bytes via HTTP");
                    return new DownloadResult { FileBytes = fileBytes, StatusCode = (int)response.StatusCode };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"✗ Failed to download: {response.StatusCode}");
                    if (!string.IsNullOrEmpty(errorContent))
                    {
                        Debug.WriteLine($"Error response: {errorContent.Substring(0, Math.Min(200, errorContent.Length))}");
                    }
                    return new DownloadResult
                    {
                        StatusCode = (int)response.StatusCode,
                        Error = $"{response.StatusCode}: {errorContent?.Substring(0, Math.Min(100, errorContent?.Length ?? 0))}"
                    };
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "";
                Debug.WriteLine($"✗ Error downloading via HTTP: {ex.Message}{innerMsg}");
                return new DownloadResult { Error = ex.Message + innerMsg };
            }
        }

        private byte[] GetAttachmentFromArcGIS(int taskId, int attachmentId)
        {
            try
            {
                Debug.WriteLine($"Fetching attachment from ArcGIS: taskId={taskId}, attachmentId={attachmentId}");

                // Use TasksRepository.GetAttachment which handles authentication properly
                var tasksRepository = new Data.TasksRepository();
                var httpResponse = tasksRepository.GetAttachment(taskId, attachmentId);

                if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                {
                    // Read the content as bytes
                    var content = httpResponse.Content as StreamContent;
                    if (content != null)
                    {
                        var fileBytes = content.ReadAsByteArrayAsync().Result;
                        Debug.WriteLine($"✓ Downloaded {fileBytes.Length} bytes from ArcGIS");
                        return fileBytes;
                    }
                }

                Debug.WriteLine($"✗ Failed to download from ArcGIS");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error downloading from ArcGIS: {ex.Message}");
                return null;
            }
        }

        private async Task<JArray> UploadAttachmentsToRedmine(string globalId, JArray attachments)
        {
            var uploadedTokens = new JArray();
            var logEntry = new
            {
                timestamp = DateTime.UtcNow,
                operation = "UploadToRedmine",
                globalId = globalId,
                attachmentCount = attachments?.Count ?? 0,
                details = new List<object>()
            };

            if (attachments == null || attachments.Count == 0)
            {
                LogAttachmentSync(logEntry);
                return uploadedTokens;
            }

            var redmineUrl = ConfigurationManager.AppSettings["RedmineApiUrl"];
            var redmineExternalUrl = ConfigurationManager.AppSettings["RedmineExternalUrl"] ?? redmineUrl;
            var redmineApiKey = ConfigurationManager.AppSettings["RedmineApiKey"];

            foreach (var attachment in attachments)
            {
                var originalUrl = attachment["url"]?.ToString();

                try
                {
                    if (string.IsNullOrEmpty(originalUrl))
                    {
                        continue;
                    }

                    // Extract taskId and attachmentId from URL
                    // e.g., "https://zemelapiai.vplanas.lt/kauno_eop_is2/web-services/tasks/attachments/16074/7212"
                    var uri = new Uri(originalUrl);
                    var pathParts = uri.AbsolutePath.Split('/');

                    int taskId = 0;
                    int attachmentId = 0;

                    // Find "attachments" in path and get the two numbers after it
                    for (int i = 0; i < pathParts.Length; i++)
                    {
                        if (pathParts[i] == "attachments" && i + 2 < pathParts.Length)
                        {
                            int.TryParse(pathParts[i + 1], out taskId);
                            int.TryParse(pathParts[i + 2], out attachmentId);
                            break;
                        }
                    }

                    if (taskId == 0 || attachmentId == 0)
                    {
                        Debug.WriteLine($"✗ Could not parse taskId/attachmentId from URL: {originalUrl}");
                        ((List<object>)logEntry.details).Add(new {
                            originalUrl,
                            status = "parse_failed",
                            error = "Could not extract taskId and attachmentId from URL"
                        });
                        continue;
                    }

                    // Use attachment ID as filename so we can track it
                    var contentType = attachment["contentType"]?.ToString() ?? "application/octet-stream";
                    var extension = contentType.Contains("jpeg") || contentType.Contains("jpg") ? ".jpg" :
                                   contentType.Contains("png") ? ".png" :
                                   contentType.Contains("pdf") ? ".pdf" : "";
                    var filename = $"{attachmentId}{extension}";

                    Debug.WriteLine($"Downloading attachment: taskId={taskId}, attachmentId={attachmentId}");

                    byte[] fileBytes;
                    try
                    {
                        // Use GetAttachmentFromArcGIS which has authentication
                        fileBytes = GetAttachmentFromArcGIS(taskId, attachmentId);

                        if (fileBytes == null || fileBytes.Length == 0)
                        {
                            Debug.WriteLine($"✗ Failed to download attachment from ArcGIS");
                            ((List<object>)logEntry.details).Add(new {
                                filename,
                                taskId,
                                attachmentId,
                                originalUrl,
                                status = "download_failed",
                                error = "GetAttachmentFromArcGIS returned no bytes - proxy disabled, check if method works now"
                            });
                            continue;
                        }

                        Debug.WriteLine($"✓ Downloaded {fileBytes.Length} bytes, uploading to Redmine...");
                    }
                    catch (Exception downloadEx)
                    {
                        var downloadError = downloadEx.Message;
                        if (downloadEx.InnerException != null)
                        {
                            downloadError += " | Inner: " + downloadEx.InnerException.Message;
                        }
                        Debug.WriteLine($"✗ Exception downloading from ArcGIS: {downloadError}");
                        ((List<object>)logEntry.details).Add(new {
                            filename,
                            taskId,
                            attachmentId,
                            originalUrl,
                            status = "download_exception",
                            error = downloadError,
                            exceptionType = downloadEx.GetType().Name
                        });
                        continue;
                    }

                    // Upload to Redmine (use external URL)
                    var uploadUrl = $"{redmineExternalUrl}/uploads.json";
                    Debug.WriteLine($"Upload URL: {uploadUrl}");
                    Debug.WriteLine($"Filename: {filename}");

                    var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                    uploadRequest.Headers.Add("X-Redmine-API-Key", redmineApiKey);
                    uploadRequest.Content = new ByteArrayContent(fileBytes);
                    uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    Debug.WriteLine($"Sending upload request to Redmine...");
                    var uploadResponse = await _httpClient.SendAsync(uploadRequest);
                    Debug.WriteLine($"Redmine upload response status: {uploadResponse.StatusCode}");

                    if (uploadResponse.IsSuccessStatusCode)
                    {
                        var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
                        var uploadData = JObject.Parse(uploadResult);
                        var token = uploadData["upload"]?["token"]?.ToString();

                        if (!string.IsNullOrEmpty(token))
                        {
                            uploadedTokens.Add(new JObject
                            {
                                ["token"] = token,
                                ["filename"] = filename,
                                ["content_type"] = attachment["contentType"]?.ToString() ?? "application/octet-stream"
                            });
                            Debug.WriteLine($"✓ Uploaded: {filename}, token: {token}");
                            ((List<object>)logEntry.details).Add(new { filename, token, status = "success" });
                        }
                    }
                    else
                    {
                        var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                        Debug.WriteLine($"✗ Upload failed: {errorContent}");
                        ((List<object>)logEntry.details).Add(new { filename, status = "failed", error = errorContent, statusCode = (int)uploadResponse.StatusCode });
                    }
                }
                catch (Exception ex)
                {
                    var errorDetails = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorDetails += " | Inner: " + ex.InnerException.Message;
                        if (ex.InnerException.InnerException != null)
                        {
                            errorDetails += " | Inner2: " + ex.InnerException.InnerException.Message;
                        }
                    }
                    Debug.WriteLine($"Failed to upload attachment to Redmine: {errorDetails}");
                    ((List<object>)logEntry.details).Add(new {
                        originalUrl,
                        status = "exception",
                        error = errorDetails,
                        exceptionType = ex.GetType().Name,
                        stackTrace = ex.StackTrace?.Split('\n').Take(3).ToArray()
                    });
                }
            }

            ((List<object>)logEntry.details).Add(new { uploadedCount = uploadedTokens.Count });
            LogAttachmentSync(logEntry);
            return uploadedTokens;
        }

        // Public method that can be called from CamundaProcessService
        public async Task<JArray> UploadAttachmentsToRedminePublic(string globalId, JArray attachments)
        {
            return await UploadAttachmentsToRedmine(globalId, attachments);
        }

        [Route("attachments/arcgis-to-redmine")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> SyncAttachmentsFromArcGISToRedmine([FromBody] JObject request)
        {
            try
            {
                var globalId = request["globalId"]?.ToString();
                var attachments = request["attachments"] as JArray;

                if (string.IsNullOrEmpty(globalId) || attachments == null || attachments.Count == 0)
                {
                    return Ok(new { status = "success", message = "No attachments to sync", uploads = new JArray() });
                }

                var uploadedTokens = await UploadAttachmentsToRedmine(globalId, attachments);

                return Ok(new
                {
                    status = "success",
                    globalId,
                    uploadCount = uploadedTokens.Count,
                    uploads = uploadedTokens
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uploading attachments to Redmine: {ex.Message}");
                return Ok(new { status = "error", message = ex.Message, uploads = new JArray() });
            }
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
                Debug.WriteLine($"==================== RECEIVE CAMUNDA UPDATE ====================");
                Debug.WriteLine($"GlobalID: {globalId}");
                Debug.WriteLine($"Payload: {payload?.ToString(Formatting.Indented) ?? "NULL"}");
                Debug.WriteLine($"================================================================");

                JObject taskData = payload["updateData"] as JObject;
                if (taskData == null)
                {
                    return BadRequest("updateData property is missing");
                }

                Debug.WriteLine($"TaskData: {taskData.ToString(Formatting.Indented)}");

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

                // Extract and map custom fields from Redmine to database columns
                if (taskData.ContainsKey("custom_fields") && taskData["custom_fields"] is JArray)
                {
                    var customFields = taskData["custom_fields"] as JArray;
                    Debug.WriteLine($"Processing {customFields.Count} custom fields from Redmine");

                    foreach (var field in customFields)
                    {
                        var fieldId = field["id"]?.Value<int>() ?? 0;
                        var valueToken = field["value"];

                        // Skip if value is null or empty
                        if (valueToken == null || valueToken.Type == JTokenType.Null)
                            continue;

                        var fieldValue = valueToken.ToString();
                        if (string.IsNullOrEmpty(fieldValue))
                            continue;

                        Debug.WriteLine($"Custom field {fieldId}: {fieldValue}");

                        switch (fieldId)
                        {
                            case 2: // uzsakovo_email
                                attributes["uzsakovo_email"] = fieldValue;
                                break;
                            case 5: // Adresas
                                attributes["Adresas"] = fieldValue;
                                break;
                            case 6: // X coordinate
                                if (double.TryParse(fieldValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double xCoord))
                                {
                                    attributes["X"] = xCoord;
                                }
                                break;
                            case 7: // Y coordinate
                                if (double.TryParse(fieldValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double yCoord))
                                {
                                    attributes["Y"] = yCoord;
                                }
                                break;
                            case 8: // URL
                                attributes["URL"] = fieldValue;
                                break;
                            case 9: // Uzduoties_tipas (reverse mapping)
                                // Map back from Redmine format to database code
                                string tipasCode = fieldValue;
                                // Handle both em-dash (–) and hyphen (-) separators
                                if (fieldValue.Contains(" – "))
                                {
                                    tipasCode = fieldValue.Split(new[] { " – " }, StringSplitOptions.None)[0];
                                }
                                else if (fieldValue.Contains(" - "))
                                {
                                    tipasCode = fieldValue.Split(new[] { " - " }, StringSplitOptions.None)[0];
                                }
                                attributes["Uzduoties_tipas"] = tipasCode;
                                Debug.WriteLine($"Uzduoties_tipas: {fieldValue} -> {tipasCode}");
                                break;
                            case 10: // Teritorija (reverse mapping)
                                // Map back from Redmine format to database code
                                string teritorijaCode = fieldValue;
                                if (fieldValue.Contains(" - "))
                                {
                                    teritorijaCode = fieldValue.Split(new[] { " - " }, StringSplitOptions.None)[0];
                                }
                                attributes["Teritorija"] = teritorijaCode;
                                break;
                        }
                    }
                }

                // Map priority from Redmine to database
                if (taskData.ContainsKey("priorityId") && taskData["priorityId"] != null)
                {
                    int priorityId = Convert.ToInt32(taskData["priorityId"]);
                    string svarba = "medium";

                    switch (priorityId)
                    {
                        case 1: svarba = "low"; break;
                        case 2: svarba = "medium"; break;
                        case 3: svarba = "high"; break;
                        case 5: svarba = "emergency"; break;
                    }

                    attributes["Svarba"] = svarba;
                    Debug.WriteLine($"Priority mapping: {priorityId} -> {svarba}");
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

                                // Add custom field mappings to SQL update
                                if (attributes.ContainsKey("uzsakovo_email"))
                                {
                                    sqlParts.Add("uzsakovo_email = @uzsakovo_email");
                                    parameters.Parameters.AddWithValue("@uzsakovo_email", attributes["uzsakovo_email"].ToString());
                                }

                                if (attributes.ContainsKey("Adresas"))
                                {
                                    sqlParts.Add("Adresas = @adresas");
                                    parameters.Parameters.AddWithValue("@adresas", attributes["Adresas"].ToString());
                                }

                                if (attributes.ContainsKey("X"))
                                {
                                    sqlParts.Add("X = @x");
                                    parameters.Parameters.AddWithValue("@x", attributes["X"]);
                                }

                                if (attributes.ContainsKey("Y"))
                                {
                                    sqlParts.Add("Y = @y");
                                    parameters.Parameters.AddWithValue("@y", attributes["Y"]);
                                }

                                if (attributes.ContainsKey("URL"))
                                {
                                    sqlParts.Add("URL = @url");
                                    parameters.Parameters.AddWithValue("@url", attributes["URL"].ToString());
                                }

                                if (attributes.ContainsKey("Uzduoties_tipas"))
                                {
                                    sqlParts.Add("Uzduoties_tipas = @uzduoties_tipas");
                                    parameters.Parameters.AddWithValue("@uzduoties_tipas", attributes["Uzduoties_tipas"].ToString());
                                }

                                if (attributes.ContainsKey("Teritorija"))
                                {
                                    sqlParts.Add("Teritorija = @teritorija");
                                    parameters.Parameters.AddWithValue("@teritorija", attributes["Teritorija"].ToString());
                                }

                                if (attributes.ContainsKey("Svarba"))
                                {
                                    sqlParts.Add("Svarba = @svarba");
                                    parameters.Parameters.AddWithValue("@svarba", attributes["Svarba"].ToString());
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

                    // Sync attachments if present in updateData
                    Debug.WriteLine($"Checking for attachments in taskData...");
                    Debug.WriteLine($"taskData.ContainsKey('attachments'): {taskData.ContainsKey("attachments")}");

                    if (taskData.ContainsKey("attachments"))
                    {
                        Debug.WriteLine($"Attachments key exists, type: {taskData["attachments"]?.Type}");
                        Debug.WriteLine($"Is JArray: {taskData["attachments"] is JArray}");
                    }

                    if (taskData.ContainsKey("attachments") && taskData["attachments"] is JArray)
                    {
                        var attachments = taskData["attachments"] as JArray;
                        Debug.WriteLine($"Attachments array: {attachments?.ToString(Formatting.None) ?? "NULL"}");

                        if (attachments != null && attachments.Count > 0)
                        {
                            Debug.WriteLine($"✓ Found {attachments.Count} attachments, syncing to ArcGIS...");

                            LogAttachmentSync(new
                            {
                                timestamp = DateTime.UtcNow,
                                operation = "CamundaToNetAttachmentsReceived",
                                globalId,
                                attachmentCount = attachments.Count,
                                attachments = attachments.Select(a => new {
                                    id = a["id"],
                                    filename = a["filename"],
                                    content_url = a["content_url"]
                                }).ToArray()
                            });

                            try
                            {
                                var redmineIssueId = taskData["id"]?.ToString();
                                Debug.WriteLine($"Redmine Issue ID: {redmineIssueId}");

                                var attachmentRequest = new JObject
                                {
                                    ["globalId"] = globalId,
                                    ["redmineIssueId"] = redmineIssueId,
                                    ["attachments"] = attachments
                                };

                                Debug.WriteLine($"Attachment sync request: {attachmentRequest.ToString(Formatting.Indented)}");

                                // Call attachment sync asynchronously (fire and forget)
                                System.Threading.Tasks.Task.Run(async () =>
                                {
                                    try
                                    {
                                        Debug.WriteLine($"Starting attachment sync for {globalId}...");
                                        var syncResult = await SyncAttachmentsFromRedmineToArcGIS(attachmentRequest);
                                        Debug.WriteLine($"✓ Attachment sync result: {syncResult}");

                                        LogAttachmentSync(new
                                        {
                                            timestamp = DateTime.UtcNow,
                                            operation = "ArcGISAttachmentSyncCompleted",
                                            globalId,
                                            result = syncResult?.ToString()
                                        });
                                    }
                                    catch (Exception attachEx)
                                    {
                                        Debug.WriteLine($"✗ Attachment sync error: {attachEx.Message}");
                                        Debug.WriteLine($"Stack: {attachEx.StackTrace}");

                                        LogAttachmentSync(new
                                        {
                                            timestamp = DateTime.UtcNow,
                                            operation = "ArcGISAttachmentSyncFailed",
                                            globalId,
                                            error = attachEx.Message,
                                            stackTrace = attachEx.StackTrace?.Split('\n').Take(3).ToArray()
                                        });
                                    }
                                });
                            }
                            catch (Exception attachEx)
                            {
                                Debug.WriteLine($"✗ Failed to initiate attachment sync: {attachEx.Message}");
                                LogAttachmentSync(new
                                {
                                    timestamp = DateTime.UtcNow,
                                    operation = "ArcGISAttachmentSyncInitFailed",
                                    globalId,
                                    error = attachEx.Message
                                });
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Attachments array is null or empty");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"No attachments found in taskData");
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

        [Route("redmine/update")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> UpdateRedmineIssue([FromBody] JObject payload)
        {
            Debug.WriteLine("==================== UPDATE REDMINE FROM CAMUNDA ====================");
            Debug.WriteLine($"Payload: {payload?.ToString(Formatting.Indented) ?? "NULL"}");

            try
            {
                var redmineIssueId = payload["redmineIssueId"]?.ToString();
                var pavadinimas = payload["Pavadinimas"]?.ToString();
                var aprasymas = payload["Aprasymas"]?.ToString();
                var statusas = payload["Statusas"]?.ToString();
                var redmineStatusId = payload["redmineStatusId"]?.ToString();
                var redminePriority = payload["redminePriority"]?.ToString();
                var redmineDueDate = payload["redmineDueDate"]?.ToString();
                var redmineCategoryId = payload["redmineCategoryId"]?.ToString();
                var customField10 = payload["customField_10"]?.ToString();
                var updateDataStr = payload["updateData"]?.ToString();

                if (string.IsNullOrEmpty(redmineIssueId))
                {
                    return BadRequest("redmineIssueId is required");
                }

                var redmineUrl = ConfigurationManager.AppSettings["RedmineApiUrl"];
                var redmineApiKey = ConfigurationManager.AppSettings["RedmineApiKey"];

                Debug.WriteLine($"Redmine Issue ID: {redmineIssueId}");
                Debug.WriteLine($"Subject: {pavadinimas}");

                // Parse updateData to check for attachments
                JObject updateData = null;
                JArray attachments = null;
                if (!string.IsNullOrEmpty(updateDataStr))
                {
                    Debug.WriteLine($"Attempting to parse updateData (length: {updateDataStr.Length})");
                    try
                    {
                        updateData = JObject.Parse(updateDataStr);
                        Debug.WriteLine($"✓ Successfully parsed updateData");
                        Debug.WriteLine($"updateData.ContainsKey('attachments'): {updateData.ContainsKey("attachments")}");

                        if (updateData.ContainsKey("attachments"))
                        {
                            Debug.WriteLine($"Attachments type: {updateData["attachments"]?.Type}");
                            Debug.WriteLine($"Is JArray: {updateData["attachments"] is JArray}");
                        }

                        if (updateData.ContainsKey("attachments") && updateData["attachments"] is JArray)
                        {
                            attachments = updateData["attachments"] as JArray;
                            Debug.WriteLine($"✓ Found {attachments.Count} attachments to sync");
                        }
                        else
                        {
                            Debug.WriteLine($"No attachments found in updateData");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"✗ Failed to parse updateData: {ex.Message}");
                        Debug.WriteLine($"updateDataStr: {updateDataStr}");
                    }
                }
                else
                {
                    Debug.WriteLine("updateData is empty or null");
                }

                // Upload attachments to Redmine first if present
                var uploadTokens = new JArray();
                if (attachments != null && attachments.Count > 0)
                {
                    Debug.WriteLine("Uploading attachments to Redmine...");

                    var globalId = payload["GlobalID"]?.ToString();
                    uploadTokens = await UploadAttachmentsToRedmine(globalId, attachments);
                    Debug.WriteLine($"Got {uploadTokens.Count} upload tokens");
                }

                // Build Redmine update payload
                var issueUpdate = new JObject
                {
                    ["issue"] = new JObject
                    {
                        ["subject"] = pavadinimas,
                        ["description"] = aprasymas,
                        ["status_id"] = !string.IsNullOrEmpty(redmineStatusId) ? int.Parse(redmineStatusId) : 1,
                        ["priority_id"] = !string.IsNullOrEmpty(redminePriority) ? int.Parse(redminePriority) : 2,
                        ["notes"] = $"Updated from .NET at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
                    }
                };

                var issue = issueUpdate["issue"] as JObject;

                // Add due date
                if (!string.IsNullOrEmpty(redmineDueDate))
                {
                    issue["due_date"] = redmineDueDate;
                }

                // Add category
                if (!string.IsNullOrEmpty(redmineCategoryId))
                {
                    issue["category_id"] = int.Parse(redmineCategoryId);
                }

                // Add custom fields
                var customFields = new JArray();
                if (!string.IsNullOrEmpty(customField10))
                {
                    customFields.Add(new JObject { ["id"] = 10, ["value"] = customField10 });
                }

                if (customFields.Count > 0)
                {
                    issue["custom_fields"] = customFields;
                }

                // Add upload tokens if present
                if (uploadTokens.Count > 0)
                {
                    issue["uploads"] = uploadTokens;
                    Debug.WriteLine($"Including {uploadTokens.Count} attachment uploads in Redmine update");
                }

                Debug.WriteLine($"Redmine update payload: {issueUpdate.ToString(Formatting.Indented)}");

                // Update Redmine issue
                var updateUrl = $"{redmineUrl}/issues/{redmineIssueId}.json";
                var request = new HttpRequestMessage(HttpMethod.Put, updateUrl);
                request.Headers.Add("X-Redmine-API-Key", redmineApiKey);
                request.Content = new StringContent(issueUpdate.ToString(Formatting.None), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Redmine response: {response.StatusCode}");
                Debug.WriteLine($"Response body: {responseContent}");
                Debug.WriteLine("=====================================================================");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        status = "success",
                        redmineIssueId,
                        attachmentsSynced = uploadTokens.Count
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = "error",
                        message = responseContent,
                        statusCode = (int)response.StatusCode
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error updating Redmine: {ex.Message}");
                Debug.WriteLine($"Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Debug.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
                }
                Debug.WriteLine("=====================================================================");
                return Ok(new { status = "error", message = ex.Message, innerMessage = ex.InnerException?.Message });
            }
        }

        [Route("test-camunda")]
        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult TestCamundaConnection([FromBody] JObject payload)
        {
            var timestamp = DateTime.UtcNow;
            Debug.WriteLine("==================== TEST CAMUNDA CONNECTION ====================");
            Debug.WriteLine($"Timestamp: {timestamp}");
            Debug.WriteLine($"Payload: {payload?.ToString(Formatting.Indented) ?? "NULL"}");
            Debug.WriteLine("==================================================================");

            // Also log to the webhook log so we can see it remotely
            LogWebhookCall(new
            {
                endpoint = "test-camunda",
                timestamp,
                payload = payload?.ToString(Formatting.None) ?? "NULL",
                message = "Camunda successfully connected to .NET endpoint!"
            });

            return Ok(new
            {
                status = "success",
                message = "Connection successful!",
                timestamp,
                receivedData = payload
            });
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KZ.Services
{
    public class CamundaProcessService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _cachedCsrfToken = null;
        private static string _cachedCsrfCookie = null;
        private static string _cachedJSessionId = null;
        private static DateTime _tokenExpiry = DateTime.MinValue;

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

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

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
                                var cookieValue = cookie.Split(';')[0];
                                _cachedCsrfCookie = cookieValue;
                            }
                            else if (cookie.Contains("JSESSIONID="))
                            {
                                var cookieValue = cookie.Split(';')[0];
                                _cachedJSessionId = cookieValue;
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

            var csrfToken = await GetCsrfToken(camundaUrl).ConfigureAwait(false);
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
                    var cookieHeader = string.Join("; ", cookieParts);
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            return request;
        }

        public async Task<string> StartTaskSyncProcess(string globalId, string title, string description, int? statusId = null)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";
            var redmineUrl = ConfigurationManager.AppSettings["RedmineApiUrl"] ?? "http://localhost:8088";
            var redmineApiKey = ConfigurationManager.AppSettings["RedmineApiKey"] ?? "";
            var redmineProjectId = ConfigurationManager.AppSettings["RedmineProjectId"] ?? "5";
            var dotnetApiUrl = ConfigurationManager.AppSettings["DotNetApiUrl"] ?? "http://localhost:3001";

            var normalizedGlobalId = NormalizeGlobalId(globalId);

            var variables = new Dictionary<string, object>
            {
                ["GlobalID"] = new { value = normalizedGlobalId, type = "String" },
                ["Pavadinimas"] = new { value = title, type = "String" },
                ["Aprasymas"] = new { value = description, type = "String" },
                ["redmineUrl"] = new { value = redmineUrl, type = "String" },
                ["redmineApiKey"] = new { value = redmineApiKey, type = "String" },
                ["redmineProjectId"] = new { value = redmineProjectId, type = "String" },
                ["dotnetApiUrl"] = new { value = dotnetApiUrl, type = "String" }
            };

            if (statusId.HasValue)
            {
                variables["Statusas"] = new { value = statusId.Value, type = "Integer" };
            }

            var payload = new
            {
                businessKey = normalizedGlobalId,
                variables
            };

            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var url = $"{camundaUrl}/process-definition/key/task-redmine-sync/start";

                var request = await CreateAuthenticatedRequest(HttpMethod.Post, url, camundaUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(resultJson);
                var processInstanceId = result["id"].ToString();

                Debug.WriteLine($"Started process {processInstanceId} for {normalizedGlobalId}");

                return processInstanceId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start process: {ex.Message}");
                throw;
            }
        }

        public async Task SendTaskUpdateMessage(string globalId, string updateSource, JObject taskData)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";
            var normalizedGlobalId = NormalizeGlobalId(globalId);

            JObject attributes = taskData["attributes"] as JObject;
            if (attributes == null)
            {
                throw new ArgumentException("taskData must contain 'attributes' object");
            }

            var pavadinimas = attributes["Pavadinimas"]?.ToString() ?? "";
            var aprasymas = attributes["Aprasymas"]?.ToString() ?? "";
            var statusas = attributes["Statusas"]?.ToString() ?? "";

            var updateDataObj = new JObject
            {
                ["subject"] = pavadinimas,
                ["description"] = aprasymas,
                ["statusId"] = statusas,
                ["attributes"] = attributes
            };

            if (taskData.ContainsKey("attachments"))
            {
                updateDataObj["attachments"] = taskData["attachments"];
            }

            if (taskData.ContainsKey("related-features"))
            {
                updateDataObj["related-features"] = taskData["related-features"];
            }

            var processVariables = new Dictionary<string, object>
            {
                ["GlobalID"] = new { value = normalizedGlobalId, type = "String" },
                ["updateSource"] = new { value = updateSource, type = "String" },
                ["updateData"] = new { value = updateDataObj.ToString(Formatting.None), type = "String" },
                ["Pavadinimas"] = new { value = pavadinimas, type = "String" },
                ["Aprasymas"] = new { value = aprasymas, type = "String" },
                ["Statusas"] = new { value = statusas, type = "String" }
            };

            // Add Svarba (Priority) mapping to redminePriority
            if (attributes.ContainsKey("Svarba") && attributes["Svarba"] != null)
            {
                string svarba = attributes["Svarba"].ToString().ToLower().Trim();
                string priorityId = "2";
                switch (svarba)
                {
                    case "low": priorityId = "1"; break;
                    case "medium": priorityId = "2"; break;
                    case "high": priorityId = "3"; break;
                    case "emergency": priorityId = "5"; break;
                }
                processVariables["redminePriority"] = new { value = priorityId, type = "String" };
            }

            // Add Teritorija mapping to customField_10
            if (attributes.ContainsKey("Teritorija") && attributes["Teritorija"] != null)
            {
                string teritorija = attributes["Teritorija"].ToString().Trim();
                string mappedTeritorija = teritorija;
                switch (teritorija)
                {
                    case "0": mappedTeritorija = "0 - Nenurodyta"; break;
                    case "1": mappedTeritorija = "1 - Vilijampolė"; break;
                    case "2": mappedTeritorija = "2 - Žaliakalnis"; break;
                    case "3": mappedTeritorija = "3 - Šilainiai"; break;
                    case "4": mappedTeritorija = "4 - Šančiai"; break;
                    case "5": mappedTeritorija = "5 - Petrašiūnai"; break;
                    case "6": mappedTeritorija = "6 - Panemunė"; break;
                    case "7": mappedTeritorija = "7 - Gričiupis"; break;
                    case "8": mappedTeritorija = "8 - Eiguliai"; break;
                    case "9": mappedTeritorija = "9 - Dainava"; break;
                    case "10": mappedTeritorija = "10 - Centras"; break;
                    case "11": mappedTeritorija = "11 - Aleksotas"; break;
                }
                processVariables["customField_10"] = new { value = mappedTeritorija, type = "String" };
            }

            // Add Pabaigos_data (Due Date) mapping to redmineDueDate
            if (attributes.ContainsKey("Pabaigos_data") && attributes["Pabaigos_data"] != null)
            {
                try
                {
                    DateTime dueDate;
                    if (DateTime.TryParse(attributes["Pabaigos_data"].ToString(), out dueDate))
                    {
                        dueDate = dueDate.AddHours(12);
                        processVariables["redmineDueDate"] = new { value = dueDate.ToString("yyyy-MM-dd"), type = "String" };
                    }
                }
                catch { }
            }

            // Add Category based on Imone (company)
            if (attributes.ContainsKey("Imone") && attributes["Imone"] != null)
            {
                string imone = attributes["Imone"].ToString().Trim();
                if (imone == "2") // Gatas
                {
                    processVariables["redmineCategoryId"] = new { value = "1", type = "String" };
                }
            }

            // Handle attachments if present (for .NET -> Redmine sync)
            Debug.WriteLine("==================== CHECKING FOR ATTACHMENTS ====================");
            Debug.WriteLine($"taskData.ContainsKey('attachments'): {taskData.ContainsKey("attachments")}");

            if (taskData.ContainsKey("attachments"))
            {
                Debug.WriteLine($"Attachments key found, type: {taskData["attachments"]?.GetType().Name}");
                Debug.WriteLine($"Attachments value: {taskData["attachments"]}");
            }

            if (taskData.ContainsKey("attachments") && taskData["attachments"] is JArray)
            {
                var attachments = taskData["attachments"] as JArray;
                Debug.WriteLine($"Found {attachments?.Count ?? 0} attachments to upload to Redmine");

                if (attachments != null && attachments.Count > 0)
                {
                    Debug.WriteLine($"Attachment details: {attachments.ToString(Formatting.Indented)}");
                    try
                    {
                        // Upload attachments and get tokens
                        Debug.WriteLine("Creating RedmineWebhookController instance...");
                        var controller = new Controllers.RedmineWebhookController();

                        Debug.WriteLine("Calling UploadAttachmentsToRedminePublic...");
                        var uploadedTokens = await controller.UploadAttachmentsToRedminePublic(normalizedGlobalId, attachments);

                        Debug.WriteLine($"Upload returned {uploadedTokens?.Count ?? 0} tokens");

                        if (uploadedTokens != null && uploadedTokens.Count > 0)
                        {
                            Debug.WriteLine($"✓ Successfully uploaded {uploadedTokens.Count} attachments");
                            Debug.WriteLine($"Tokens: {uploadedTokens.ToString(Formatting.Indented)}");
                            processVariables["attachmentTokens"] = new { value = uploadedTokens.ToString(Formatting.None), type = "String" };
                        }
                        else
                        {
                            Debug.WriteLine("✗ No attachment tokens returned from upload");
                        }
                    }
                    catch (Exception attachEx)
                    {
                        Debug.WriteLine($"✗ Exception during attachment upload: {attachEx.Message}");
                        Debug.WriteLine($"Stack trace: {attachEx.StackTrace}");
                        if (attachEx.InnerException != null)
                        {
                            Debug.WriteLine($"Inner exception: {attachEx.InnerException.Message}");
                        }
                        // Don't throw - let the main update continue even if attachments fail
                    }
                }
                else
                {
                    Debug.WriteLine("Attachments array is null or empty");
                }
            }
            else
            {
                Debug.WriteLine("No attachments array found in taskData");
            }
            Debug.WriteLine("===============================================================");

            var message = new
            {
                messageName = "TaskUpdate",
                businessKey = normalizedGlobalId,
                processVariables = processVariables
            };

            try
            {
                var json = JsonConvert.SerializeObject(message);
                var url = $"{camundaUrl}/message";

                Debug.WriteLine("==================== SEND TASK UPDATE ====================");
                Debug.WriteLine($"URL: {url}");
                Debug.WriteLine($"Business Key: {normalizedGlobalId}");
                Debug.WriteLine($"Update Source: {updateSource}");
                Debug.WriteLine($"Payload:");
                Debug.WriteLine(json);
                Debug.WriteLine("==========================================================");

                var request = await CreateAuthenticatedRequest(HttpMethod.Post, url, camundaUrl).ConfigureAwait(false);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Sending HTTP request to Camunda...");
                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                Debug.WriteLine($"Response Status: {(int)response.StatusCode} {response.StatusCode}");
                Debug.WriteLine($"Response Body: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ FAILED: {response.StatusCode} - {responseContent}");
                    throw new HttpRequestException($"Camunda returned {response.StatusCode}: {responseContent}");
                }

                Debug.WriteLine($"✅ TaskUpdate sent successfully for {normalizedGlobalId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ EXCEPTION in SendTaskUpdateMessage:");
                Debug.WriteLine($"   Message: {ex.Message}");
                Debug.WriteLine($"   Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task SendValidationMessage(string globalId, string validationResult, string rejectionReason = null)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";
            var normalizedGlobalId = NormalizeGlobalId(globalId);

            var variables = new Dictionary<string, object>
            {
                ["validationResult"] = new { value = validationResult, type = "String" }
            };

            if (!string.IsNullOrEmpty(rejectionReason))
            {
                variables["rejectionReason"] = new { value = rejectionReason, type = "String" };
            }

            var message = new
            {
                messageName = "ValidationUpdate",
                businessKey = normalizedGlobalId,
                processVariables = variables
            };

            try
            {
                var json = JsonConvert.SerializeObject(message);
                var url = $"{camundaUrl}/message";

                var request = await CreateAuthenticatedRequest(HttpMethod.Post, url, camundaUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                Debug.WriteLine($"Sent ValidationUpdate for {normalizedGlobalId}: {validationResult}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send ValidationUpdate: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ProcessInstanceExists(string globalId)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";
            var normalizedGlobalId = NormalizeGlobalId(globalId);

            try
            {
                var url = $"{camundaUrl}/process-instance?businessKey={Uri.EscapeDataString(normalizedGlobalId)}";
                var request = await CreateAuthenticatedRequest(HttpMethod.Get, url, camundaUrl);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var instances = JArray.Parse(responseContent);
                    return instances.Count > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking process instance: {ex.Message}");
                return false;
            }
        }
    }
}

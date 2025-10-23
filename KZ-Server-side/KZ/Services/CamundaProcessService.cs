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

        public async Task<string> StartTaskSyncProcess(string globalId, string title, string description, int? statusId = null)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"]
                ?? Environment.GetEnvironmentVariable("CAMUNDA__URL")
                ?? "http://localhost:8080/engine-rest";

            var redmineUrl = ConfigurationManager.AppSettings["RedmineApiUrl"]
                ?? Environment.GetEnvironmentVariable("REDMINE__URL")
                ?? "http://localhost:8088";

            var redmineApiKey = ConfigurationManager.AppSettings["RedmineApiKey"]
                ?? Environment.GetEnvironmentVariable("REDMINE__APIKEY")
                ?? "0efee588d931db2267d22124525730c2acdc3218";

            var redmineProjectId = ConfigurationManager.AppSettings["RedmineProjectId"]
                ?? Environment.GetEnvironmentVariable("REDMINE__PROJECTID")
                ?? "5";

            var dotnetApiUrl = ConfigurationManager.AppSettings["DotNetApiUrl"]
                ?? Environment.GetEnvironmentVariable("DOTNET__APIURL")
                ?? "http://localhost:3001";

            var variables = new Dictionary<string, object>
            {
                // Task data
                ["GlobalID"] = new { value = globalId, type = "String" },
                ["Pavadinimas"] = new { value = title, type = "String" },
                ["Aprasymas"] = new { value = description, type = "String" },

                // Configuration
                ["redmineUrl"] = new { value = redmineUrl, type = "String" },
                ["redmineApiKey"] = new { value = redmineApiKey, type = "String" },
                ["redmineProjectId"] = new { value = redmineProjectId, type = "String" },
                ["dotnetApiUrl"] = new { value = dotnetApiUrl, type = "String" }
            };

            // Optionally include status
            if (statusId.HasValue)
            {
                variables["Statusas"] = new { value = statusId.Value, type = "Integer" };
            }

            var payload = new
            {
                businessKey = globalId,
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

                Debug.WriteLine($"Started process instance {processInstanceId} for globalId {globalId}");

                return processInstanceId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start Camunda process for globalId {globalId}: {ex.Message}");
                throw;
            }
        }

        public async Task SendTaskUpdateMessage(string globalId, string updateSource, object updateData)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";

            var message = new
            {
                messageName = "TaskUpdate",
                businessKey = globalId,
                processVariables = new Dictionary<string, object>
                {
                    ["updateSource"] = new { value = updateSource, type = "String" },
                    ["updateData"] = new { value = JsonConvert.SerializeObject(updateData), type = "Json" }
                }
            };

            try
            {
                var json = JsonConvert.SerializeObject(message);
                var url = $"{camundaUrl}/message";

                var request = await CreateAuthenticatedRequest(HttpMethod.Post, url, camundaUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                Debug.WriteLine($"Sent TaskUpdate message for globalId {globalId} (source: {updateSource})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send TaskUpdate message for globalId {globalId}: {ex.Message}");
                throw;
            }
        }

        public async Task SendValidationMessage(string globalId, string validationResult, string rejectionReason = null)
        {
            var camundaUrl = ConfigurationManager.AppSettings["CamundaRestUrl"] ?? "http://localhost:8080/engine-rest";

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
                businessKey = globalId,
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

                Debug.WriteLine($"Sent ValidationUpdate message for globalId {globalId} (result: {validationResult})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send ValidationUpdate message for globalId {globalId}: {ex.Message}");
                throw;
            }
        }
    }
}

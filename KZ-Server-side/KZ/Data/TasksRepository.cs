using Dapper;
using KZ.Models;
using Newtonsoft.Json.Linq;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.WebPages;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.PdfSharp;



namespace KZ.Data
{
    public class TasksRepository
    {
        private readonly string tablePart1 = WebConfigurationManager.AppSettings["TableFirstPart"];
        private static string _cachedCsrfToken = null;
        private static string _cachedCsrfCookie = null;
        private static string _cachedJSessionId = null;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly HttpClient _httpClient = new HttpClient();

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
                var camundaUsername = WebConfigurationManager.AppSettings["CamundaUsername"];
                var camundaPassword = WebConfigurationManager.AppSettings["CamundaPassword"];

                if (!string.IsNullOrEmpty(camundaUsername) && !string.IsNullOrEmpty(camundaPassword))
                {
                    var authBytes = System.Text.Encoding.UTF8.GetBytes($"{camundaUsername}:{camundaPassword}");
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

        private void AddCamundaAuthHeaders(RestRequest request, string camundaUrl)
        {
            // Add basic auth if configured
            var camundaUsername = WebConfigurationManager.AppSettings["CamundaUsername"];
            var camundaPassword = WebConfigurationManager.AppSettings["CamundaPassword"];

            if (!string.IsNullOrEmpty(camundaUsername) && !string.IsNullOrEmpty(camundaPassword))
            {
                var authBytes = System.Text.Encoding.UTF8.GetBytes($"{camundaUsername}:{camundaPassword}");
                var authHeader = Convert.ToBase64String(authBytes);
                request.AddHeader("Authorization", $"Basic {authHeader}");
            }

            // Add CSRF token (get it synchronously using ConfigureAwait to avoid deadlock)
            try
            {
                // Use ConfigureAwait(false) to avoid deadlock in ASP.NET synchronization context
                var csrfToken = Task.Run(async () => await GetCsrfToken(camundaUrl).ConfigureAwait(false)).Result;

                if (!string.IsNullOrEmpty(csrfToken))
                {
                    // Camunda uses Double Submit Cookie pattern - needs both header and cookie
                    // Use X-XSRF-TOKEN to match what Camunda sends in response
                    request.AddHeader("X-XSRF-TOKEN", csrfToken);
                    Debug.WriteLine($"✓ Added CSRF token header: X-XSRF-TOKEN={csrfToken}");

                    // Add cookies using RestSharp's Cookie API instead of manual header
                    // Parse and add JSESSIONID cookie
                    if (!string.IsNullOrEmpty(_cachedJSessionId))
                    {
                        var jSessionParts = _cachedJSessionId.Split('=');
                        if (jSessionParts.Length == 2)
                        {
                            request.AddCookie(jSessionParts[0], jSessionParts[1]);
                            Debug.WriteLine($"✓ Added cookie: {jSessionParts[0]}={jSessionParts[1]}");
                        }
                    }

                    // Parse and add XSRF-TOKEN cookie
                    if (!string.IsNullOrEmpty(_cachedCsrfCookie))
                    {
                        var xsrfParts = _cachedCsrfCookie.Split('=');
                        if (xsrfParts.Length == 2)
                        {
                            request.AddCookie(xsrfParts[0], xsrfParts[1]);
                            Debug.WriteLine($"✓ Added cookie: {xsrfParts[0]}={xsrfParts[1]}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"⚠ No CSRF token available - proceeding without CSRF protection");
                }

                // Debug: Log all parameters being sent
                Debug.WriteLine($"Request parameters for {request.Resource}:");
                foreach (var param in request.Parameters)
                {
                    Debug.WriteLine($"  [{param.Type}] {param.Name}: {param.Value}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to add CSRF token: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"⚠ Proceeding without CSRF token due to error");
            }
        }

        public JArray GetTasksList(int? statusCode, int? territoryCode)
        {
            JArray list = null;
            RestClient client = GetRestClient();
            RestRequest request = Utilities.GetRestRequest(client, "query", null, true);
            List<string> clauses = new List<string>();
            if (statusCode != null)
            {
                clauses.Add("Statusas = " + statusCode);
            }
            if (territoryCode != null)
            {
                clauses.Add("Teritorija = " + territoryCode);
            }
            string whereClause = "1=1";
            if (clauses.Count > 0)
            {
                whereClause = string.Join(" AND ", clauses.ToArray());
            }
            request.AddParameter("where", whereClause);
            request.AddParameter("outFields", "GlobalID");
            request.AddParameter("returnGeometry", false);
            IRestResponse response = client.Post(request);
            try
            {
                JObject r = JObject.Parse(response.Content);
                JArray featuresArray = (JArray)r.GetValue("features");
                if (featuresArray != null)
                {
                    foreach (JObject feature in featuresArray.Children())
                    {
                        JObject attributes = (JObject)feature["attributes"];
                        if (attributes != null)
                        {
                            if (attributes.ContainsKey("GlobalID"))
                            {
                                feature.Add("id", attributes["GlobalID"]);
                                feature.Remove("attributes");
                            }
                        }
                    }
                }
                list = featuresArray;
            }
            catch
            {
                // ...
            }
            return list;
        }
        public JObject GetTaskData(string id, bool returnAttachments, bool returnRelatedFeatures = false, int? territoryCode = null, bool modAttachmentsData = false, bool modAttributes = false)
        {
            JObject result = new JObject();
            if (!string.IsNullOrEmpty(id))
            {
                RestClient client = GetRestClient();
                RestRequest request = Utilities.GetRestRequest(client, "query", null, true);
                List<string> clauses = new List<string>
                {
                    "GlobalID='" + id + "'"
                };
                if (territoryCode != null)
                {
                    clauses.Add("Teritorija = " + territoryCode);
                }
                request.AddParameter("where", string.Join(" AND ", clauses.ToArray()));
                request.AddParameter("outFields", "*");
                request.AddParameter("returnGeometry", true);
                IRestResponse response = client.Post(request);
                try
                {
                    JObject r = JObject.Parse(response.Content);
                    JArray featuresJArray = (JArray)r.GetValue("features");
                    if (featuresJArray != null)
                    {
                        if (featuresJArray.Count == 1)
                        {
                            JObject feature = (JObject)featuresJArray[0];
                            if (feature != null)
                            {
                                JObject attributes = (JObject)feature["attributes"];
                                if (attributes != null)
                                {
                                    if (modAttributes) {
                                        List<string> attributesToRemove = new List<string> { "editor_app", "created_user", "last_edited_user", "SHAPE.STArea()", "SHAPE.STLength()", "OBJECTID", "URL", "Shape__Area", "Shape__Length", "Uzsakovo_ID", "Uzsakovo_vardas", "Rangovo_ID", "Rangovo_vardas", "lastCommentTime", "Patvirtinimas" };
                                        foreach (string attributeName in attributesToRemove)
                                        {
                                            if (attributes.ContainsKey(attributeName))
                                            {
                                                attributes.Remove(attributeName);
                                            }
                                        }
                                        List<string> dateAttributes = new List<string> { "Pabaigos_data", "created_date", "last_edited_date" }; // TODO... Šituos laukus reiktų atsekti iš serviso aprašomosios info?..
                                        foreach (string attributeName in dateAttributes)
                                        {
                                            if (attributes.ContainsKey(attributeName))
                                            {
                                                try
                                                {
                                                    string dateVal = attributes[attributeName].ToString();
                                                    if (!string.IsNullOrEmpty(dateVal))
                                                    {
                                                        int offset = 0; // Nes "editFieldsInfo" laukai -> "timeZone": "UTC", "respectsDaylightSaving": false
                                                        if (attributeName.Equals("Pabaigos_data"))
                                                        {
                                                            // https://en.wikipedia.org/wiki/UTC%2B02:00
                                                            // https://www.timeanddate.com/time/zone/lithuania/vilnius
                                                            offset = 0; // FIXME!!! Ar ok??? Ar čia turi būti 2 žiemos laiku, 3 vasaros laiku?..
                                                        }
                                                        attributes[attributeName] = GetFormattedDate(long.Parse(dateVal) / 1000, offset);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    // ...
                                                }
                                            }
                                        }
                                        if (attributes.ContainsKey("GlobalID")) // TODO! Šitą `GlobalID` reikia išlupti iš serviso aprašomosios info?..
                                        {
                                            string featureId = attributes["GlobalID"].ToString();
                                            featureId = featureId.Replace("{", "");
                                            featureId = featureId.Replace("}", "");
                                            attributes.Add("URL", string.Format("{0}://{1}/{2}?t=task&id={3}", HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Authority, WebConfigurationManager.AppSettings["AppPath"], featureId));
                                        }
                                        // Dar reikia "Patvirtinimas" lauką pakoreguoti?..
                                        result.Add("attributes", attributes);
                                    }
                                    else
                                    {
                                        result = feature;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // ...
                }
                if (result.ContainsKey("attributes"))
                {
                    if (returnAttachments)
                    {
                        request = Utilities.GetRestRequest(client, "queryAttachments", null, true);
                        request.AddParameter("globalIds", id);
                        request.AddParameter("attachmentsDefinitionExpression", "keywords <> 'temp'"); // Gan svarbu!? Negrąžinti `laikinųjų` (žemėlapio, panoramos screenshot'ų) paveikslėlių
                        response = client.Post(request);
                        JArray attachmentInfosJArray = new JArray();
                        try
                        {
                            JObject r = JObject.Parse(response.Content);
                            JArray attachmentGroupsJArray = (JArray)r.GetValue("attachmentGroups");
                            if (attachmentGroupsJArray != null)
                            {
                                foreach (JObject attachmentGroup in attachmentGroupsJArray.Children())
                                {
                                    string parentGlobalId = (string)attachmentGroup["parentGlobalId"];
                                    if (parentGlobalId.Equals(id) || parentGlobalId.Equals("{" + id + "}")) // 2022.05.17 A. Ražo pastebėtas bug'as...
                                    {
                                        string parentObjectId = (string)attachmentGroup["parentObjectId"];
                                        if (!string.IsNullOrEmpty(parentObjectId))
                                        {
                                            JArray attachmentInfosJArrayTemp = (JArray)attachmentGroup.GetValue("attachmentInfos");
                                            if (attachmentInfosJArrayTemp != null)
                                            {
                                                foreach (JObject attachmentInfo in attachmentInfosJArrayTemp.Children())
                                                {
                                                    if (modAttachmentsData)
                                                    {
                                                        // attachmentInfo["raw-url"] = GetTaskAttachmentUrl(parentObjectId, attachmentInfo["id"].ToString()); // Testavimui aktualu?..
                                                        attachmentInfo["url"] = string.Format("{0}://{1}/{2}/web-services/tasks/attachments/{3}/{4}", HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Authority, WebConfigurationManager.AppSettings["AppPath"], parentObjectId, attachmentInfo["id"]); // FIXME! Nelabai gerai, kad čia reikia hardcode'inti APP URL...
                                                        attachmentInfo.Remove("ATTACHMENTID");
                                                        attachmentInfo.Remove("ATT_NAME");
                                                        attachmentInfo.Remove("CONTENT_TYPE");
                                                        attachmentInfo.Remove("DATA_SIZE");
                                                        attachmentInfo.Remove("exifInfo");
                                                        attachmentInfo.Remove("id");
                                                        // attachmentInfo.Remove("globalId");
                                                        attachmentInfo.Remove("GLOBALID");
                                                        attachmentInfo.Remove("KEYWORDS");
                                                        attachmentInfo.Remove("name");
                                                        attachmentInfo.Remove("parentGlobalId");
                                                    }
                                                    else
                                                    {
                                                        attachmentInfo["src"] = GetTaskAttachmentUrl(parentObjectId.ToString(), attachmentInfo["id"].ToString());
                                                    }
                                                    attachmentInfosJArray.Add(attachmentInfo);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // ...
                        }
                        result.Add("attachments", attachmentInfosJArray);
                    }
                    if (returnRelatedFeatures)
                    {
                        result.Add("related-features", GetTaskRelatedFeatures(id));
                    }
                }
            }
            return result;
        }
        private JArray GetTaskRelatedFeatures(string taskId)
        {
            RestClient client = Utilities.GetRestClient(WebConfigurationManager.AppSettings["TasksServiceRoot"] + "MapServer");
            RestRequest request = Utilities.GetRestRequest(client, "find", null, true);
            request.AddParameter("searchFields", "UzduotiesGUID");
            request.AddParameter("searchText", taskId);
            request.AddParameter("contains", false);
            request.AddParameter("layers", "2,1,3,4,5,6,7,8");
            request.AddParameter("returnFieldName", true);
            request.AddParameter("returnUnformattedValues", true);
            IRestResponse response = client.Post(request);
            JArray relatedFeaturesJArray = null;
            try
            {
                JObject r = JObject.Parse(response.Content);
                if (r.ContainsKey("results"))
                {
                    relatedFeaturesJArray = (JArray)r["results"];
                }
            }
            catch
            {
                // ...
            }
            return relatedFeaturesJArray;
        }
        public HttpResponseMessage GetAttachment(int taskId, int attachmentId)
        {
            string url = GetTaskAttachmentUrl(taskId.ToString(), attachmentId.ToString());
            RestClient client = Utilities.GetRestClient(url);
            RestRequest request = Utilities.GetRestRequest(client);
            IRestResponse response = client.Post(request);
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            MemoryStream stream = new MemoryStream(response.RawBytes);
            r.Content = new StreamContent(stream);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue(response.ContentType);
            r.Content.Headers.ContentLength = stream.Length;
            return r;
        }
        public int? GetStatusCodeFromKey(string statusKey)
        {
            int? statusCode = null;
            if (!string.IsNullOrEmpty(statusKey))
            {
                statusCode = 1001; // Tokio niekada nebus :)
                if (statusKey.Equals("new"))
                {
                    statusCode = 0;
                }
            }
            return statusCode;
        }
        private string GetTaskUrl()
        {
            string url = WebConfigurationManager.AppSettings["TasksServiceRoot"] + "FeatureServer/" + 0 + "/";
            return url;
        }
        private string GetTaskAttachmentUrl(string taskId, string attachmentId)
        {
            string url = GetTaskUrl() + taskId + "/attachments/" + attachmentId;
            return url;
        }
        private RestClient GetRestClient()
        {
            string url = GetTaskUrl();
            RestClient client = Utilities.GetRestClient(url);
            return client;
        }
        private string GetFormattedDate(long seconds, int offset)
        {
            // Converts a Unix time expressed as the number of seconds that have elapsed since 1970-01-01T00:00:00Z to a DateTimeOffset value.
            // https://stackoverflow.com/questions/12305826/what-does-0000-mean-in-the-context-of-a-date-returned-by-the-twitter-api#:~:text=%2B0000%20is%20an%20HHMM%20offset,winter%20and%20%2B0200%20in%20summer.
            // The Offset property value of the returned DateTimeOffset instance is TimeSpan.Zero, which represents Coordinated Universal Time. You can convert it to the time in a specific time zone by calling the TimeZoneInfo.ConvertTime(DateTimeOffset, TimeZoneInfo) method
            /*
                C# -> "2022-03-30T21:00:00", kliente -> 31 00:00 [+3]
                C# -> "2022-01-01T14:00:00", kliente -> 01 16:00 [+2]
                C# -> "2022-04-23T13:00:00", kliente -> 23 16:00 [+3]
                C# -> "2022-03-15T22:00:00", kliente -> 16 00:00 [+2]
            */
            string date = DateTimeOffset.FromUnixTimeSeconds(seconds).ToString(string.Format("yyyy-MM-ddTHH:mm:ss+0{0}00", offset)); // FIXME...
            return date;
        }
        public JObject NotifyAboutChangeToTasksSystem(NotifyAboutChangeToTasksSystem model)
        {
            System.Diagnostics.Debug.WriteLine($"Redmine PART START");

            JObject result = new JObject();
            bool status = false;

            JObject taskData = GetTaskData(model.Id, true, false, null, true, true);

            System.Diagnostics.Debug.WriteLine("- - -");
            System.Diagnostics.Debug.WriteLine(model.Id);
            System.Diagnostics.Debug.WriteLine(model.ActionType);
            System.Diagnostics.Debug.WriteLine("- - -");
            System.Diagnostics.Debug.WriteLine(taskData);

            if (taskData == null || !taskData.ContainsKey("attributes"))
            {
                result.Add("status", "FAILED");
                result.Add("ERR", "Task data or attributes not found");
                return result;
            }

            JObject attributes = (JObject)taskData["attributes"];
            string globalId = attributes["GlobalID"]?.ToString();

            if (string.IsNullOrEmpty(globalId))
            {
                result.Add("status", "FAILED");
                result.Add("ERR", "GlobalID not found in attributes");
                return result;
            }

            try
            {

                if (model.ActionType == "delegation") {
                    System.Diagnostics.Debug.WriteLine($"No process found for GlobalID {globalId}. Starting new process.");
                    status = StartCamundaProcess(taskData);
                }
                else if (model.ActionType == "update")
                {
                    // Check if a Camunda process already exists for this GlobalID
                    string processInstanceId = GetCamundaProcessInstanceByBusinessKey(globalId);
                    if (!string.IsNullOrEmpty(processInstanceId))
                    {
                        // Process exists, trigger the sync-to-redmine worker with updated task data
                        System.Diagnostics.Debug.WriteLine($"Process exists for GlobalID {globalId}. Triggering sync-to-redmine worker.");
                        status = TriggerSyncToRedmineWorker(processInstanceId, taskData);
                    }
                }
                else if (model.ActionType == "cancel_task")
                {
                    // Check if a Camunda process already exists for this GlobalID
                    string processInstanceId = GetCamundaProcessInstanceByBusinessKey(globalId);
                    if (!string.IsNullOrEmpty(processInstanceId))
                    {

                        taskData["attributes"]["Statusas"] = 5;
                        // Process exists, trigger the sync-to-redmine worker with updated task data
                        System.Diagnostics.Debug.WriteLine($"Process exists for GlobalID {globalId}. Triggering sync-to-redmine worker.");
                        status = TriggerSyncToRedmineWorker(processInstanceId, taskData);
                    }
                }

            }
            catch (Exception e)
            {
                result.Add("ERR", e.ToString());
                System.Diagnostics.Debug.WriteLine($"Error in NotifyAboutChangeToTasksSystem: {e.Message}");
            }

            if (status)
            {
                result.Add("status", "OK");
            }
            else
            {
                result.Add("status", "FAILED");
            }
            return result;
        }

        private bool StartCamundaProcess(JObject taskData)
        {
            try
            {
                if (taskData == null || !taskData.ContainsKey("attributes"))
                {
                    System.Diagnostics.Debug.WriteLine("StartCamundaProcess: taskData or attributes is null");
                    return false;
                }

                JObject attributes = (JObject)taskData["attributes"];

                if (attributes == null || !attributes.ContainsKey("GlobalID"))
                {
                    System.Diagnostics.Debug.WriteLine("StartCamundaProcess: attributes or GlobalID is null");
                    return false;
                }

                var camundaUrl = WebConfigurationManager.AppSettings["CamundaRestUrl"];
                var client = new RestClient(camundaUrl);
                client.Timeout = 300000; // Set timeout to 5 minutes (300 seconds)
                var request = new RestRequest("process-definition/key/task-redmine-sync/start", Method.POST);

                Debug.WriteLine($"=== Camunda Request Details ===");
                Debug.WriteLine($"Base URL: {camundaUrl}");
                Debug.WriteLine($"Resource: {request.Resource}");
                Debug.WriteLine($"Full URL: {client.BuildUri(request)}");
                Debug.WriteLine($"Method: {request.Method}");

                AddCamundaAuthHeaders(request, camundaUrl);

                // Build variables dynamically from all attributes
                var variables = new Dictionary<string, object>();

                // Add configuration variables required by Camunda process
                var redmineUrl = WebConfigurationManager.AppSettings["RedmineApiUrl"] ?? "http://localhost:8088";
                var redmineApiKey = WebConfigurationManager.AppSettings["RedmineApiKey"] ?? "";
                var redmineProjectId = WebConfigurationManager.AppSettings["RedmineProjectId"] ?? "5";
                var dotnetApiUrl = WebConfigurationManager.AppSettings["DotNetApiUrl"] ?? "http://localhost:3001";

                // Use lowercase type names as per Camunda REST API specification
                variables["redmineUrl"] = new { value = redmineUrl, type = "string" };
                variables["redmineApiKey"] = new { value = redmineApiKey, type = "string" };
                variables["redmineProjectId"] = new { value = redmineProjectId, type = "string" };
                variables["dotnetApiUrl"] = new { value = dotnetApiUrl, type = "string" };

                Debug.WriteLine($"Added configuration variables:");
                Debug.WriteLine($"  redmineUrl: {redmineUrl}");
                Debug.WriteLine($"  redmineProjectId: {redmineProjectId}");
                Debug.WriteLine($"  dotnetApiUrl: {dotnetApiUrl}");

                Debug.WriteLine($"=== Processing Attributes ===");
                Debug.WriteLine($"Total attributes to process: {attributes.Count}");

                // Add all individual attributes as separate variables
                foreach (var attribute in attributes)
                {
                    string key = attribute.Key;
                    var value = attribute.Value;

                    if (value != null && value.Type != JTokenType.Null)
                    {
                        string stringValue;

                        // Handle different JToken types to avoid Spin JSON parsing issues
                        switch (value.Type)
                        {
                            case JTokenType.String:
                            case JTokenType.Integer:
                            case JTokenType.Float:
                            case JTokenType.Boolean:
                            case JTokenType.Date:
                            case JTokenType.Guid:
                                // Simple types - get the actual value
                                stringValue = ((JValue)value).Value?.ToString() ?? "";
                                variables[key] = new { value = stringValue, type = "string" };

                                // Debug key variables
                                if (key == "Pavadinimas" || key == "Aprasymas" || key == "GlobalID")
                                {
                                    Debug.WriteLine($"✓ Added key variable '{key}' = '{stringValue.Substring(0, Math.Min(50, stringValue.Length))}...' (type: {value.Type})");
                                }
                                break;
                            case JTokenType.Object:
                            case JTokenType.Array:
                                // Complex types - skip them to avoid Spin parsing issues
                                Debug.WriteLine($"⚠ Skipping complex attribute '{key}' (type: {value.Type})");
                                break;
                            default:
                                // Fallback for other types
                                stringValue = value.ToString();
                                variables[key] = new { value = stringValue, type = "string" };
                                break;
                        }
                    }
                }

                // Add attachments array as JSON string if present
                if (taskData.ContainsKey("attachments") && taskData["attachments"] != null)
                {
                    var attachmentsToken = taskData["attachments"];
                    if (attachmentsToken.Type == JTokenType.Array)
                    {
                        JArray attachments = (JArray)attachmentsToken;
                        if (attachments.Count > 0)
                        {
                            variables["attachments"] = new { value = attachments.ToString(Newtonsoft.Json.Formatting.None), type = "string" };
                            variables["attachmentsCount"] = new { value = attachments.Count.ToString(), type = "string" };
                        }
                    }
                }

                // Add related-features array as JSON string if present
                if (taskData.ContainsKey("related-features") && taskData["related-features"] != null)
                {
                    var relatedFeaturesToken = taskData["related-features"];
                    if (relatedFeaturesToken.Type == JTokenType.Array)
                    {
                        JArray relatedFeatures = (JArray)relatedFeaturesToken;
                        if (relatedFeatures.Count > 0)
                        {
                            variables["relatedFeatures"] = new { value = relatedFeatures.ToString(Newtonsoft.Json.Formatting.None), type = "string" };
                            variables["relatedFeaturesCount"] = new { value = relatedFeatures.Count.ToString(), type = "string" };
                        }
                    }
                }

                // CRITICAL: Add GlobalID as explicit variable for BPMN Task_SyncToNet
                if (attributes.ContainsKey("GlobalID") && attributes["GlobalID"] != null)
                {
                    string globalIdValue = attributes["GlobalID"].ToString();
                    variables["GlobalID"] = new { value = globalIdValue, type = "string" };
                    Debug.WriteLine($"✓ EXPLICIT GlobalID variable set: {globalIdValue}");
                }

                // Add custom field variables for BPMN to use
                // Custom field 1: GlobalID (without braces for Redmine)
                if (attributes.ContainsKey("GlobalID") && attributes["GlobalID"] != null)
                {
                    variables["customField_1"] = new { value = attributes["GlobalID"].ToString().Replace("{", "").Replace("}", ""), type = "string" };
                }

                // Custom field 2: uzsakovo_email
                if (attributes.ContainsKey("uzsakovo_email") && attributes["uzsakovo_email"] != null)
                {
                    variables["customField_2"] = new { value = attributes["uzsakovo_email"].ToString(), type = "string" };
                }

                // Custom field 5: Adresas
                if (attributes.ContainsKey("Adresas") && attributes["Adresas"] != null)
                {
                    variables["customField_5"] = new { value = attributes["Adresas"].ToString(), type = "string" };
                }

                // Custom field 6: X
                if (attributes.ContainsKey("X") && attributes["X"] != null)
                {
                    variables["customField_6"] = new { value = attributes["X"].ToString(), type = "string" };
                }

                // Custom field 7: Y
                if (attributes.ContainsKey("Y") && attributes["Y"] != null)
                {
                    variables["customField_7"] = new { value = attributes["Y"].ToString(), type = "string" };
                }

                // Custom field 8: URL
                if (attributes.ContainsKey("URL") && attributes["URL"] != null)
                {
                    variables["customField_8"] = new { value = attributes["URL"].ToString(), type = "string" };
                }

                // Custom field 9: Uzduoties_tipas (with mapping)
                if (attributes.ContainsKey("Uzduoties_tipas") && attributes["Uzduoties_tipas"] != null)
                {
                    string tipas = attributes["Uzduoties_tipas"].ToString().Trim();
                    string mappedTipas = tipas;
                    switch (tipas)
                    {
                        case "0": mappedTipas = "0 – schemų užsakymai"; break;
                        case "1": mappedTipas = "1 – UAB \"Kauno gatvių apšvietimas\""; break;
                        case "2": mappedTipas = "2 – UAB \"Gatas\""; break;
                    }
                    variables["customField_9"] = new { value = mappedTipas, type = "string" };
                }

                // Custom field 10: Teritorija (with mapping - MUST match Redmine list exactly)
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
                    variables["customField_10"] = new { value = mappedTeritorija, type = "string" };
                }

                // Dynamic Project ID based on Imone field
                string dynamicProjectId = redmineProjectId; // Default
                if (attributes.ContainsKey("Imone") && attributes["Imone"] != null)
                {
                    string imone = attributes["Imone"].ToString().Trim();
                    switch (imone)
                    {
                        case "1": dynamicProjectId = "10"; break;
                        case "2": dynamicProjectId = "4"; break;
                        case "3": dynamicProjectId = "11"; break;
                    }
                }
                variables["redmineProjectId"] = new { value = dynamicProjectId, type = "string" };

                // Priority mapping based on Svarba
                if (attributes.ContainsKey("Svarba") && attributes["Svarba"] != null)
                {
                    string svarba = attributes["Svarba"].ToString().ToLower().Trim();
                    string priorityId = "2"; // Default: medium
                    switch (svarba)
                    {
                        case "low": priorityId = "1"; break;
                        case "medium": priorityId = "2"; break;
                        case "high": priorityId = "3"; break;
                        case "emergency": priorityId = "5"; break;
                    }
                    variables["redminePriority"] = new { value = priorityId, type = "string" };
                }

                // Due date with timezone fix
                if (attributes.ContainsKey("Pabaigos_data") && attributes["Pabaigos_data"] != null)
                {
                    try
                    {
                        DateTime dueDate;
                        if (DateTime.TryParse(attributes["Pabaigos_data"].ToString(), out dueDate))
                        {
                            dueDate = dueDate.AddHours(12); // Timezone fix
                            variables["redmineDueDate"] = new { value = dueDate.ToString("yyyy-MM-dd"), type = "string" };
                        }
                    }
                    catch { }
                }

                // Category ID (only for project 4)
                if (dynamicProjectId == "4")
                {
                    variables["redmineCategoryId"] = new { value = "1", type = "string" };
                }

                // Add complete taskData as JSON for reference (as string, not json to avoid Spin parsing)
                variables["taskDataJson"] = new { value = taskData.ToString(Newtonsoft.Json.Formatting.None), type = "string" };

                var camundaPayload = new
                {
                    variables = variables,
                    businessKey = attributes["GlobalID"].ToString()
                };

                Debug.WriteLine($"=== Final Payload ===");
                Debug.WriteLine($"Total variables: {variables.Count}");
                Debug.WriteLine($"Business key: {attributes["GlobalID"].ToString()}");

                // Serialize payload to see exact structure
                var payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(camundaPayload, Newtonsoft.Json.Formatting.Indented);
                Debug.WriteLine($"Payload preview (first 1000 chars):");
                Debug.WriteLine(payloadJson.Substring(0, Math.Min(1000, payloadJson.Length)));

                request.AddJsonBody(camundaPayload);

                Debug.WriteLine($"=== Executing Camunda Request ===");
                Debug.WriteLine($"Sending POST request to start process instance...");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                IRestResponse response = client.Execute(request);
                stopwatch.Stop();

                Debug.WriteLine($"⏱ Request completed in {stopwatch.ElapsedMilliseconds}ms");
                Debug.WriteLine($"Response status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Response status description: {response.StatusDescription}");
                Debug.WriteLine($"Response error message: {response.ErrorMessage ?? "None"}");
                Debug.WriteLine($"Response content length: {response.Content?.Length ?? 0} characters");

                if (!string.IsNullOrEmpty(response.Content))
                {
                    Debug.WriteLine($"Response content: {response.Content}");
                }

                System.Diagnostics.Debug.WriteLine($"Camunda response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Camunda response content: {response.Content}");

                if (response.IsSuccessful)
                {
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Camunda request failed: {response.StatusCode} - {response.ErrorMessage}");
                    return false;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error in StartCamundaProcess: {e.Message}");
                return false;
            }
        }

        private string GetCamundaProcessInstanceByBusinessKey(string businessKey)
        {
            try
            {
                var camundaUrl = WebConfigurationManager.AppSettings["CamundaRestUrl"];
                var client = new RestClient(camundaUrl);
                var request = new RestRequest("process-instance", Method.GET);
                AddCamundaAuthHeaders(request, camundaUrl);
                request.AddParameter("businessKey", businessKey);

                IRestResponse response = client.Execute(request);

                System.Diagnostics.Debug.WriteLine($"GetCamundaProcessInstanceByBusinessKey response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"GetCamundaProcessInstanceByBusinessKey response content: {response.Content}");

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    JArray processInstances = JArray.Parse(response.Content);
                    if (processInstances != null && processInstances.Count > 0)
                    {
                        JObject firstInstance = (JObject)processInstances[0];
                        string processInstanceId = firstInstance["id"]?.ToString();
                        System.Diagnostics.Debug.WriteLine($"Found process instance: {processInstanceId}");
                        return processInstanceId;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"No process instance found for businessKey: {businessKey}");
                return null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCamundaProcessInstanceByBusinessKey: {e.Message}");
                return null;
            }
        }

        private bool TriggerSyncToRedmineWorker(string processInstanceId, JObject taskData)
        {
            try
            {
                if (taskData == null || !taskData.ContainsKey("attributes"))
                {
                    System.Diagnostics.Debug.WriteLine("TriggerSyncToRedmineWorker: taskData or attributes is null");
                    return false;
                }

                JObject attributes = (JObject)taskData["attributes"];
                string globalId = attributes["GlobalID"]?.ToString();

                if (string.IsNullOrEmpty(globalId))
                {
                    System.Diagnostics.Debug.WriteLine("TriggerSyncToRedmineWorker: GlobalID not found");
                    return false;
                }

                var camundaUrl = WebConfigurationManager.AppSettings["CamundaRestUrl"];
                var client = new RestClient(camundaUrl);

                // Step 1: Update all process variables with latest task data
                var updateRequest = new RestRequest($"process-instance/{processInstanceId}/variables", Method.POST);
                AddCamundaAuthHeaders(updateRequest, camundaUrl);

                var modifications = new Dictionary<string, object>();

                // Update all individual attributes
                foreach (var attribute in attributes)
                {
                    string key = attribute.Key;
                    var value = attribute.Value;

                    if (value != null && value.Type != JTokenType.Null)
                    {
                        modifications[key] = new { value = value.ToString(), type = "String" };
                    }
                }

                // Update attachments if present
                if (taskData.ContainsKey("attachments") && taskData["attachments"] != null)
                {
                    var attachmentsToken = taskData["attachments"];
                    if (attachmentsToken.Type == JTokenType.Array)
                    {
                        JArray attachments = (JArray)attachmentsToken;
                        modifications["attachments"] = new { value = attachments.ToString(Newtonsoft.Json.Formatting.None), type = "Json" };
                        modifications["attachmentsCount"] = new { value = attachments.Count.ToString(), type = "Integer" };
                    }
                }

                // Update related-features if present
                if (taskData.ContainsKey("related-features") && taskData["related-features"] != null)
                {
                    var relatedFeaturesToken = taskData["related-features"];
                    if (relatedFeaturesToken.Type == JTokenType.Array)
                    {
                        JArray relatedFeatures = (JArray)relatedFeaturesToken;
                        modifications["relatedFeatures"] = new { value = relatedFeatures.ToString(Newtonsoft.Json.Formatting.None), type = "Json" };
                        modifications["relatedFeaturesCount"] = new { value = relatedFeatures.Count.ToString(), type = "Integer" };
                    }
                }

                // Update complete taskData
                modifications["taskDataJson"] = new { value = taskData.ToString(Newtonsoft.Json.Formatting.None), type = "Json" };

                updateRequest.AddJsonBody(new { modifications = modifications });
                IRestResponse updateResponse = client.Execute(updateRequest);

                System.Diagnostics.Debug.WriteLine($"Update variables response status: {updateResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Update variables response content: {updateResponse.Content}");

                if (!updateResponse.IsSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to update variables: {updateResponse.StatusCode} - {updateResponse.ErrorMessage}");
                    return false;
                }

                // Step 2: Send message to trigger the sync-to-redmine worker
                var messageRequest = new RestRequest("message", Method.POST);
                AddCamundaAuthHeaders(messageRequest, camundaUrl);

                var messagePayload = new
                {
                    messageName = "TaskUpdate",
                    businessKey = globalId,
                    processVariables = new
                    {
                        updateSource = new { value = "dotnet", type = "String" }
                    }
                };

                messageRequest.AddJsonBody(messagePayload);
                IRestResponse messageResponse = client.Execute(messageRequest);

                System.Diagnostics.Debug.WriteLine($"Send message response status: {messageResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Send message response content: {messageResponse.Content}");

                if (messageResponse.IsSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully triggered sync-to-redmine worker for process: {processInstanceId}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send message: {messageResponse.StatusCode} - {messageResponse.ErrorMessage}");
                    return false;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TriggerSyncToRedmineWorker: {e.Message}");
                return false;
            }
        }

        public JObject ApproveOrRejectTask(TaskApprovalOrRejectionModel model)
        {
            System.Diagnostics.Debug.WriteLine($"ApproveOrRejectTask called for task: {model.Id}, status: {model.Status}");

            JObject result = new JObject();

            try
            {
                // Get task data with all attributes
                JObject taskData = GetTaskData(model.Id, true, false, null, true, true);

                if (taskData == null || !taskData.ContainsKey("attributes"))
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "Task data or attributes not found");
                    System.Diagnostics.Debug.WriteLine("ApproveOrRejectTask: Task data not found");
                    return result;
                }

                JObject attributes = (JObject)taskData["attributes"];
                string globalId = attributes["GlobalID"]?.ToString();

                if (string.IsNullOrEmpty(globalId))
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "GlobalID not found in attributes");
                    return result;
                }

                // Find the Camunda process instance for this task
                string processInstanceId = GetCamundaProcessInstanceByBusinessKey(globalId);

                if (string.IsNullOrEmpty(processInstanceId))
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "No Camunda process found for this task");
                    System.Diagnostics.Debug.WriteLine($"ApproveOrRejectTask: No process found for GlobalID {globalId}");
                    return result;
                }

                // Update task status based on approval/rejection
                // Approved -> Status 3 (Closed) in Redmine
                // Rejected -> Status 6 (Rejected) in Redmine
                int redmineStatusId;
                string statusName;

                if (model.Status == "approved")
                {
                    statusName = "approved";
                    // Update Patvirtinimas (approval) field to 1 in ArcGIS Feature Service
                    RestClient arcgisClient = GetRestClient();
                    RestRequest arcgisRequest = Utilities.GetRestRequest(arcgisClient, "applyEdits", null, true);

                    JArray updatesArray = new JArray();
                    JObject featureUpdate = new JObject();
                    JObject updateAttributes = new JObject();
                    updateAttributes["GlobalID"] = globalId;
                    updateAttributes["Patvirtinimas"] = 1;
                    updateAttributes["Statusas"] = 4;
                    featureUpdate["attributes"] = updateAttributes;
                    updatesArray.Add(featureUpdate);

                    arcgisRequest.AddParameter("updates", updatesArray.ToString(Newtonsoft.Json.Formatting.None));
                    arcgisRequest.AddParameter("useGlobalIds", "true"); // Critical: tells ArcGIS to use GlobalID instead of OBJECTID
                    IRestResponse arcgisResponse = arcgisClient.Post(arcgisRequest);

                    System.Diagnostics.Debug.WriteLine($"Update Patvirtinimas response status: {arcgisResponse.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Update Patvirtinimas response content: {arcgisResponse.Content}");

                    if (!arcgisResponse.IsSuccessful)
                    {
                        result.Add("status", "FAILED");
                        result.Add("error", $"Failed to update Patvirtinimas field: {arcgisResponse.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"Failed to update Patvirtinimas: {arcgisResponse.ErrorMessage}");
                        return result;
                    }

                    // Verify the update was successful
                    try
                    {
                        JObject arcgisResponseObj = JObject.Parse(arcgisResponse.Content);
                        if (arcgisResponseObj.ContainsKey("updateResults"))
                        {
                            JArray updateResults = (JArray)arcgisResponseObj["updateResults"];
                            if (updateResults.Count > 0)
                            {
                                JObject updateResult = (JObject)updateResults[0];
                                if (!updateResult.ContainsKey("success") || !(bool)updateResult["success"])
                                {
                                    result.Add("status", "FAILED");
                                    string errorMsg = updateResult["error"]?.ToString() ?? "Unknown error updating Patvirtinimas";
                                    result.Add("error", errorMsg);
                                    System.Diagnostics.Debug.WriteLine($"Failed to update Patvirtinimas: {errorMsg}");
                                    return result;
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing applyEdits response: {parseEx.Message}");
                    }
                }
                else if (model.Status == "reject")
                {
                    statusName = "rejected";
                }
                else
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "Invalid status. Must be 'approved' or 'reject'");
                    return result;
                }

                var camundaUrl = WebConfigurationManager.AppSettings["CamundaRestUrl"];
                var client = new RestClient(camundaUrl);

                //System.Diagnostics.Debug.WriteLine($"Setting task status to: {statusName} (Redmine ID: {redmineStatusId})");

                //// Trigger sync to Redmine via Camunda


                //// Step 1: Update process variables with new status
                //var updateRequest = new RestRequest($"process-instance/{processInstanceId}/variables", Method.POST);

                //var modifications = new Dictionary<string, object>
                //{
                //    ["statusId"] = new { value = redmineStatusId.ToString(), type = "String" },
                //    ["approvalStatus"] = new { value = model.Status, type = "String" },
                //    ["updateSource"] = new { value = "approval", type = "String" }
                //};

                //// Add rejection reason if provided
                //if (!string.IsNullOrEmpty(model.Reason))
                //{
                //    modifications["rejectionReason"] = new { value = model.Reason, type = "String" };
                //}

                //updateRequest.AddJsonBody(new { modifications = modifications });
                //IRestResponse updateResponse = client.Execute(updateRequest);

                //System.Diagnostics.Debug.WriteLine($"Update variables response status: {updateResponse.StatusCode}");
                //System.Diagnostics.Debug.WriteLine($"Update variables response content: {updateResponse.Content}");

                //if (!updateResponse.IsSuccessful)
                //{
                //    result.Add("status", "FAILED");
                //    result.Add("error", $"Failed to update Camunda variables: {updateResponse.StatusCode}");
                //    System.Diagnostics.Debug.WriteLine($"Failed to update variables: {updateResponse.ErrorMessage}");
                //    return result;
                //}

                // Step 2: Send message to trigger the sync-to-redmine worker
                var messageRequest = new RestRequest("message", Method.POST);
                AddCamundaAuthHeaders(messageRequest, camundaUrl);

                var messagePayload = new
                {
                    messageName = "ValidationUpdate",
                    businessKey = globalId,
                    processVariables = new{
                        validationResult = new { value = statusName, type = "String" },
                    }
                };

                messageRequest.AddJsonBody(messagePayload);
                IRestResponse messageResponse = client.Execute(messageRequest);

                System.Diagnostics.Debug.WriteLine($"Send message response status: {messageResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Send message response content: {messageResponse.Content}");

                if (messageResponse.IsSuccessful)
                {
                    result.Add("status", "Ok");
                    result.Add("message", $"Task {model.Status} successfully and synced to Redmine");
                    System.Diagnostics.Debug.WriteLine($"Successfully triggered sync-to-redmine for {model.Status} action");
                }
                else
                {
                    result.Add("status", "FAILED");
                    result.Add("error", $"Failed to send message to Camunda: {messageResponse.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Failed to send message: {messageResponse.ErrorMessage}");
                }
            }
            catch (Exception e)
            {
                result.Add("status", "FAILED");
                result.Add("error", e.Message);
                result.Add("stackTrace", e.StackTrace);
                System.Diagnostics.Debug.WriteLine($"Error in ApproveOrRejectTask: {e.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {e.StackTrace}");
            }

            return result;
        }

        public List<Dictionary<string, object>> GetTasksListForUser()
        {
            List<Dictionary<string, object>> list = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "SELECT GlobalID, Pavadinimas, Aprasymas, Uzduoties_tipas, Svarba, Teritorija, DATEDIFF(s, '1970-01-01 00:00:00', t.Pabaigos_data) AS Pabaigos_data, Adresas, uzsakovo_email, Uzsakovo_vardas, Uzsakovo_imone, Imone, rangovo_email, Statusas, Patvirtinimas, lastCommentTime, Objekto_GUID, DATEDIFF(s, '1970-01-01 00:00:00', t.created_date) AS created_date, DATEDIFF(s, '1970-01-01 00:00:00', t.last_edited_date) AS last_edited_date, DATEDIFF(s, '1970-01-01 00:00:00', ut.last_time_opened) AS last_time_opened, URL FROM " + tablePart1 + ".UZDUOTYS_PROJ AS t LEFT JOIN (SELECT * FROM " + tablePart1 + ".KZ_UZDUOTYS WHERE vartotojo_id = @user) AS ut ON t.GlobalID = ut.uzduoties_id WHERE (GDB_TO_DATE > '9999' OR GDB_TO_DATE IS NULL) AND Aplinka = @env";
                    int? env = 1;
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        env = 2;
                    }
                    string user = "";
                    string email = "";
                    if (HttpContext.Current.Session["id"] != null)
                    {
                        user = HttpContext.Current.Session["id"].ToString();
                    }
                    if (HttpContext.Current.Session["email"] != null && HttpContext.Current.Session["role"] != null)
                    {
                        // APP admin'ui viską rodyti??... Taip pat laikinai ir rangovo rolei?.. O ateityje filtruoti pagal rangovo įmonę??...
                        string role = HttpContext.Current.Session["role"].ToString();
                        if (!role.Equals("admin")) {
                            email = HttpContext.Current.Session["email"].ToString();
                            sql += " AND uzsakovo_email = @email";
                        }
                    }
                    // ...
                    var res = connection.Query(sql, new { user, env, email }).ToList();
                    if (res != null)
                    {
                        list = new List<Dictionary<string, object>>();
                        foreach (object row in res)
                        {
                            IDictionary<string, object> r = (IDictionary<string, object>)row;
                            list.Add(new Dictionary<string, object>(r));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // ...
            }
            return list;
        }
        public JObject LogTaskViewAction(string taskId, string userId)
        {
            JObject result = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = @"
                    IF EXISTS (SELECT 1 FROM {TABLE_PART_1}.KZ_UZDUOTYS WHERE vartotojo_id = @userId AND uzduoties_id = @taskId)
                        BEGIN
                            UPDATE {TABLE_PART_1}.KZ_UZDUOTYS SET last_time_opened = GETUTCDATE() WHERE vartotojo_id = @userId AND uzduoties_id = @taskId;
                    END
                    ELSE
                        BEGIN
                            DECLARE @id AS INTEGER;
                            EXEC dbo.next_rowid '{TABLE_PART_1}', 'KZ_UZDUOTYS', @id OUTPUT;
                            INSERT INTO {TABLE_PART_1}.KZ_UZDUOTYS(vartotojo_id, uzduoties_id, OBJECTID, last_time_opened) OUTPUT INSERTED.* VALUES(@userId, @taskId, @id, GETUTCDATE());
                    END".Replace("{TABLE_PART_1}", tablePart1);
                    var res = connection.Query(sql, new { taskId, userId });
                    result = new JObject
                    {
                        // { "result", res.ToString() }
                    };
                }
            }
            catch (Exception e)
            {
                // ...
            }
            return result;
        }
        public PdfDocument GetReport(string id) {
            string[] taskMetaFields4Output = { "Pavadinimas", "Aprasymas", "Uzduoties_tipas", "Svarba", "Teritorija", "Pabaigos_data", "Adresas", "uzsakovo_email", "Uzsakovo_vardas", "Uzsakovo_imone", "Imone", "rangovo_email", "Statusas", "Patvirtinimas" };
            TaskModel taskModel = new TaskModel
            {
                Id = id
            };
            JObject fields = GetFields();
            JObject taskData = GetTaskData(id, false, false, null, false);
            string htmlString = @"
                    <head>
                        <style>
                            table {
                                border-collapse: collapse;
                                margin-bottom: 20px;
                            }
                            td, th {
                                border: 1px solid #cccccc;
                                padding: 5px;
                                text-align: left;
                            }
                            .comment {
                                margin-top: 10px;
                            }
                            .comment-meta {
                                margin-bottom: 2px;
                            }
                            .author {
                                padding-right: 10px;
                                font-weight: bold;
                            }
                            .created {
                                color: #808080;
                            }
                            img {
                                max-width: 100%;
                                padding-top: 10px;
                            }
                        </style>
                    </head>
                ";
            htmlString += "<body>";
            string title = id;
            string taskAttributesHtmlString = "";
            if (taskData != null && taskData.ContainsKey("attributes"))
            {
                JObject taskAttributes = (JObject)taskData["attributes"];
                if (taskAttributes != null)
                {
                    taskAttributesHtmlString += "<table>";
                    foreach (string fieldName in taskMetaFields4Output)
                    {
                        if (taskAttributes.ContainsKey(fieldName))
                        {
                            taskAttributesHtmlString += "<tr><th>" + GetFieldName(fieldName, fields) + "</th><td>" + GetFieldValue(fieldName, taskAttributes, fields) + "</td></tr>";
                        }
                    }
                    taskAttributesHtmlString += "</table>";
                    if (taskAttributes.ContainsKey("Pavadinimas"))
                    {
                        title = taskAttributes["Pavadinimas"].ToString();
                    }
                }
            }
            htmlString += "<h1>" + title + "</h1>";
            htmlString += taskAttributesHtmlString;
            htmlString += "</body>";
            PdfDocument document = PdfGenerator.GeneratePdf(htmlString, PageSize.A4, imageLoad: OnImageLoad);
            return document;
            // http://localhost:3001/kz/web-services/tasks/get-report?id=9FB256F1-D118-497E-BE5A-35BE56C197CC
        }
        private JObject GetFields()
        {
            RestClient client = GetRestClient();
            RestRequest request = Utilities.GetRestRequest(client, null, null, true);
            IRestResponse response = client.Post(request);
            JObject fieldsDict = null;
            try
            {
                JObject queryResult = JObject.Parse(response.Content);
                if (queryResult.ContainsKey("fields"))
                {
                    fieldsDict = new JObject();
                    JArray fields = (JArray)queryResult["fields"];
                    for (int i = 0; i < fields.Count; i++)
                    {
                        JObject field = (JObject)fields[i];
                        if (field.ContainsKey("name"))
                        {
                            string fieldName = (string)field["name"];
                            fieldsDict.Add(fieldName, field);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // ...
            }
            return fieldsDict;
        }
        private string GetFieldName(string name, JObject fields)
        {
            if (fields != null)
            {
                if (fields.ContainsKey(name))
                {
                    JObject field = (JObject) fields[name];
                    if (field != null && field.ContainsKey("alias"))
                    {
                        name = field["alias"].ToString();
                    }
                }
            }
            return name;
        }
        private string GetFieldValue(string name, JObject attributes, JObject fields)
        {
            string value = attributes[name].ToString();
            if (fields != null)
            {
                if (fields.ContainsKey(name))
                {
                    JObject field = (JObject)fields[name];
                    try
                    {
                        if (field != null)
                        {
                            if (field.ContainsKey("type") && field["type"].ToString().Equals("esriFieldTypeDate"))
                            {
                                try
                                {
                                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                                    dateTime = dateTime.AddSeconds(float.Parse(value) / 1000).ToLocalTime();
                                    value = dateTime.ToString();
                                }
                                catch (Exception e)
                                {
                                    // value = e.ToString();
                                }
                            }
                            if (field.ContainsKey("domain"))
                            {
                                JObject domain = (JObject)field["domain"];
                                if (domain != null && domain.ContainsKey("codedValues"))
                                {
                                    JArray codedValues = (JArray)domain["codedValues"];
                                    for (int j = 0; j < codedValues.Count; j++)
                                    {
                                        JObject codedValue = (JObject)codedValues[j];
                                        if (codedValue.ContainsKey("code") && codedValue.ContainsKey("name"))
                                        {
                                            if (value.Equals(codedValue["code"].ToString()))
                                            {
                                                value = codedValue["name"].ToString();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // ...
                    }
                }
            }
            return value;
        }
        private void OnImageLoad(object sender, HtmlImageLoadEventArgs e)
        {
            // FIXME... Nežinau ar čia viskas ok...
            Uri myUri = new Uri(e.Src);
            string url = HttpUtility.ParseQueryString(myUri.Query).Get("url");
            string token = HttpUtility.ParseQueryString(myUri.Query).Get("token");
            IRestResponse response = GetTaskCommentAttachmentFromEinpixResponse(url, token);
            MemoryStream stream = new MemoryStream(response.RawBytes)
            {
                Position = 0
            };
            System.Drawing.Image sysImg = System.Drawing.Image.FromStream(stream, false, true);
            var ximg = XImage.FromGdiPlusImage(sysImg);
            e.Callback(ximg);
        }
        public IRestResponse GetTaskCommentAttachmentFromEinpixResponse(string url, string token)
        {
            RestClient client = Utilities.GetRestClient(url);
            RestRequest request = Utilities.GetRestRequest(client);
            request.AddHeader("authorization", "Bearer " + token);
            IRestResponse response = client.Get(request);
            return response;
        }

        public JObject UpdateTaskFromCamunda(JObject updateData)
        {
            JObject result = new JObject();

            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTaskFromCamunda called with data: {updateData}");

                if (updateData == null || !updateData.ContainsKey("globalId"))
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "globalId is required");
                    System.Diagnostics.Debug.WriteLine("UpdateTaskFromCamunda: globalId is missing");
                    return result;
                }

                string globalId = updateData["globalId"]?.ToString();

                if (string.IsNullOrEmpty(globalId))
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "globalId cannot be empty");
                    return result;
                }

                // Build the update payload for ArcGIS Feature Service
                RestClient client = GetRestClient();
                RestRequest request = Utilities.GetRestRequest(client, "applyEdits", null, true);

                JArray updatesArray = new JArray();
                JObject featureUpdate = new JObject
                {
                    ["attributes"] = new JObject()
                };

                // Add GlobalID to identify the feature to update
                featureUpdate["attributes"]["GlobalID"] = globalId;

                int fieldsUpdated = 0;

                // Map Camunda/Redmine fields to ArcGIS attributes with null handling

                // Handle description field
                if (updateData.ContainsKey("description") && updateData["description"] != null)
                {
                    string description = updateData["description"].ToString();
                    if (!string.IsNullOrEmpty(description))
                    {
                        featureUpdate["attributes"]["Aprasymas"] = description;
                        fieldsUpdated++;
                        System.Diagnostics.Debug.WriteLine($"Mapping Aprasymas: {description.Substring(0, Math.Min(50, description.Length))}...");
                    }
                }

                // Handle status field (string from Redmine webhook)
                if (updateData.ContainsKey("status") && updateData["status"] != null)
                {
                    int status = Convert.ToInt32(updateData["status"]);
                    int arcgisStatus;

                    switch (status)
                    {
                        case 1:
                            arcgisStatus = 6;
                            break;
                        // Add more cases as needed
                        default:
                            arcgisStatus = 6; // Default: New
                            break;
                    }
                    // Map Redmine status names to ArcGIS Statusas codes

                    featureUpdate["attributes"]["Statusas"] = arcgisStatus;
                    fieldsUpdated++;
                    System.Diagnostics.Debug.WriteLine($"Mapping Statusas: {status} -> {arcgisStatus}");
                    
                }

                // Handle progress field (done_ratio from Redmine)
                if (updateData.ContainsKey("progress") && updateData["progress"] != null)
                {
                    string progress = updateData["progress"].ToString();
                    if (!string.IsNullOrEmpty(progress))
                    {
                        int progressValue;
                        if (int.TryParse(progress, out progressValue))
                        {
                            // Store progress if you have a field for it
                            // featureUpdate["attributes"]["Progress"] = progressValue;
                            fieldsUpdated++;
                            System.Diagnostics.Debug.WriteLine($"Mapping Progress: {progressValue}%");
                        }
                    }
                }

                // Handle updatedFrom field
                if (updateData.ContainsKey("updatedFrom") && updateData["updatedFrom"] != null)
                {
                    string updatedFrom = updateData["updatedFrom"].ToString();
                    System.Diagnostics.Debug.WriteLine($"Update source: {updatedFrom}");
                }

                if (fieldsUpdated == 0)
                {
                    result.Add("status", "FAILED");
                    result.Add("error", "No valid fields to update");
                    System.Diagnostics.Debug.WriteLine("UpdateTaskFromCamunda: No fields to update");
                    return result;
                }

                System.Diagnostics.Debug.WriteLine($"Updating {fieldsUpdated} fields for task {globalId}");

                updatesArray.Add(featureUpdate);
                request.AddParameter("updates", updatesArray.ToString(Newtonsoft.Json.Formatting.None));

                IRestResponse response = client.Post(request);

                System.Diagnostics.Debug.WriteLine($"ArcGIS applyEdits response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"ArcGIS applyEdits response content: {response.Content}");

                if (response.IsSuccessful)
                {
                    JObject responseObj = JObject.Parse(response.Content);

                    if (responseObj.ContainsKey("updateResults"))
                    {
                        JArray updateResults = (JArray)responseObj["updateResults"];
                        if (updateResults.Count > 0)
                        {
                            JObject updateResult = (JObject)updateResults[0];
                            if (updateResult.ContainsKey("success") && (bool)updateResult["success"])
                            {
                                result.Add("status", "OK");
                                result.Add("message", $"Task updated successfully ({fieldsUpdated} fields)");
                                result.Add("globalId", globalId);
                                result.Add("fieldsUpdated", fieldsUpdated);
                                System.Diagnostics.Debug.WriteLine($"Task {globalId} updated successfully with {fieldsUpdated} fields");
                            }
                            else
                            {
                                result.Add("status", "FAILED");
                                string errorMsg = updateResult["error"]?.ToString() ?? "Unknown error";
                                result.Add("error", errorMsg);
                                System.Diagnostics.Debug.WriteLine($"Failed to update task {globalId}: {errorMsg}");
                            }
                        }
                        else
                        {
                            result.Add("status", "FAILED");
                            result.Add("error", "Empty update results");
                        }
                    }
                    else
                    {
                        result.Add("status", "FAILED");
                        result.Add("error", "No update results in response");
                    }
                }
                else
                {
                    result.Add("status", "FAILED");
                    result.Add("error", $"HTTP {response.StatusCode}: {response.ErrorMessage}");
                    result.Add("responseContent", response.Content);
                    System.Diagnostics.Debug.WriteLine($"ArcGIS request failed: {response.StatusCode} - {response.ErrorMessage}");
                }
            }
            catch (Exception e)
            {
                result.Add("status", "FAILED");
                result.Add("error", e.Message);
                result.Add("stackTrace", e.StackTrace);
                System.Diagnostics.Debug.WriteLine($"Error in UpdateTaskFromCamunda: {e.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {e.StackTrace}");
            }

            return result;
        }
    }
}
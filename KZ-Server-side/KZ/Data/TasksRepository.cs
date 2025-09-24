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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
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
            JObject result = new JObject();
            bool status = false;
            JObject taskData = GetTaskData(model.Id, true, false, null, true, true);
            try
            {
                // https://stackoverflow.com/questions/5420656/unable-to-read-data-from-the-transport-connection-an-existing-connection-was-f
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                SmtpClient SmtpServer = new SmtpClient(WebConfigurationManager.AppSettings["SMTPClientServerName"], int.Parse(WebConfigurationManager.AppSettings["SMTPClientServerPort"]))
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(WebConfigurationManager.AppSettings["SMTPNetworkCredentialsEmail"], WebConfigurationManager.AppSettings["SMTPNetworkCredentialsPassword"]),
                    EnableSsl = true
                };
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(WebConfigurationManager.AppSettings["SMTPFrom"])
                };
                string to = null;
                mail.IsBodyHtml = true;
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.Subject = "Kauno EOP IS pranešimas";
                string message = "<table style='border-collapse: collapse;'>";
                string tdStyle = "style='border: 1px solid #eeeeee; padding: 5px;'";
                if (taskData.ContainsKey("attributes"))
                {
                    JObject fields = GetFields();
                    JObject attributes = (JObject)taskData["attributes"];
                    foreach (var attribute in attributes)
                    {
                        message += "<tr><td " + tdStyle + ">" + GetFieldName(attribute.Key, fields) + "</td><td " + tdStyle + ">" + GetFieldValue(attribute.Key, attributes, fields) + "</td></tr>";
                        if (attribute.Key.Equals("Imone"))
                        {
                            to = WebConfigurationManager.AppSettings["SMTPTo" + attribute.Value.ToString()];
                        }
                    }
                }
                if (taskData.ContainsKey("attachments"))
                {
                    JArray attachments = (JArray)taskData["attachments"];
                    if (attachments.Count > 0)
                    {
                        message += "<div style='margin-top: 20px;'>" + "Nuorodos į susijusius failus:" + "</div>";
                        foreach (JObject attachment in attachments)
                        {
                            if (attachment.ContainsKey("url"))
                            {
                                message += "<div style='margin-top: 20px;'><a href='" + attachment["url"] + "'>" + attachment["url"] + "</a></div>";
                            }
                        }
                    }
                }
                message += "</table>";
                mail.Body = message;
                if (to == null || to.IsEmpty())
                {
                    to = WebConfigurationManager.AppSettings["SMTPToGeneral"];
                }
                mail.To.Add(to);
                SmtpServer.Send(mail);
                status = true;
            }
            catch (Exception e)
            {
                result.Add("ERR", e.ToString()); // For testing problems...
            }
            if (status)
            {
                result.Add("status", "OK");
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
    }
}
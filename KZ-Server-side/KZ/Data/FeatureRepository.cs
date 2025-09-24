using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.IO;
using System.Web;
using System.Web.Configuration;

namespace KZ.Data
{
    public class FeatureRepository
    {
        private RestClient GetRestClient(string serviceId, string layerId = null)
        {
            string url;
            if (serviceId.Equals("street-signs-vertical")) {
                url = WebConfigurationManager.AppSettings["VerticalStreetSignsServiceRoot"];
            } else if (serviceId.Equals("tasks")) {
                url = WebConfigurationManager.AppSettings["TasksServiceRoot"];
            } else if (serviceId.Equals("user-points")) {
                url = WebConfigurationManager.AppSettings["StreetSignsServiceRoot"];
            } else {
                url = WebConfigurationManager.AppSettings["StreetSignsServiceRoot"];
            }
            url += "FeatureServer/";
            if (layerId != null)
            {
                url += layerId + "/";
            }
            RestClient client = Utilities.GetRestClient(url);
            return client;
        }
        private RestClient GetRestClientByFeatureType(string featureType)
        {
            RestClient client = null;
            if (featureType.Equals("verticalStreetSignsSupports"))
            {
                client = GetRestClient("street-signs-vertical", WebConfigurationManager.AppSettings["StreetSignsSupportsLayerId"]);
            }
            else if (featureType.Equals("verticalStreetSigns"))
            {
                // client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsLayerId"]); // Taip niekada neturėtų būti?..
            }
            else if (featureType.Equals("horizontalPoints"))
            {
                client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsHorizontalPointsLayerId"]);
            }
            else if (featureType.Equals("horizontalPolylines"))
            {
                client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsHorizontalPolylinesLayerId"]);
            }
            else if (featureType.Equals("horizontalPolygons"))
            {
                client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsHorizontalPolygonsLayerId"]);
            }
            else if (featureType.Equals("otherPoints"))
            {
                client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsOtherPointsLayerId"]);
            }
            else if (featureType.Equals("otherPolylines"))
            {
                client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsOtherPolylinesLayerId"]);
            }
            else if (featureType.Equals("otherPolygons"))
            {
                client = GetRestClient("street-signs", WebConfigurationManager.AppSettings["StreetSignsOtherPolygonsLayerId"]);
            }
            else if (featureType.Equals("tasks"))
            {
                client = GetRestClient("tasks", "0");
            }
            return client;
        }
        public JObject ApplyEdits(string serviceId, JArray edits, bool useGlobalIds = false)
        {
            bool valid = false;
            bool onlyUpdates = false;
            if (edits != null)
            {
                valid = true;
                JArray featuresArray;
                foreach (JObject item in edits.Children())
                {
                    featuresArray = (JArray)item["updates"];
                    if (featuresArray != null)
                    {
                        foreach (JObject feature in featuresArray.Children())
                        {
                            onlyUpdates = true;
                            JObject attributes = (JObject)feature["attributes"];
                            if (attributes != null)
                            {
                                // attributes.Remove("OBJECTID"); // Testavimui galima naudoti...
                                attributes["editor_app"] = HttpContext.Current.User.Identity.Name;
                                if (serviceId.Equals("tasks"))
                                {
                                    // Hmm... Reiktų pakoreguoti last_time_opened ???
                                }
                            }
                            else
                            {
                                valid = false;
                            }
                        }
                    }
                    featuresArray = (JArray)item["adds"];
                    if (featuresArray != null)
                    {
                        foreach (JObject feature in featuresArray.Children())
                        {
                            onlyUpdates = false;
                            JObject attributes = (JObject)feature["attributes"];
                            if (attributes != null)
                            {
                                attributes["editor_app"] = HttpContext.Current.User.Identity.Name;
                            }
                            else
                            {
                                valid = false;
                            }
                        }
                    }
                    featuresArray = (JArray)item["deletes"];
                    if (featuresArray != null)
                    {
                        onlyUpdates = false;
                    }
                }
            }
            JObject result = null;
            if (valid)
            {
                RestClient client = GetRestClient(serviceId);
                RestRequest request = Utilities.GetRestRequest(client, "applyEdits", null, true);
                request.AddParameter("edits", edits.ToString());
                request.AddParameter("rollbackOnFailure", true);
                if ((serviceId.Equals("tasks") && onlyUpdates) || useGlobalIds)
                {
                    request.AddParameter("useGlobalIds", "true");
                }
                IRestResponse response = client.Post(request);
                try
                {
                    result = new JObject
                    {
                        { "res", JArray.Parse(response.Content) }
                    };
                }
                catch (Exception e)
                {
                    result = new JObject
                    {
                        { "err", response.Content }
                    };
                }
            }
            return result;
        }
        public JObject AddPhoto(string objectId, string featureType, HttpPostedFile file, string keyword, string exifInfo)
        {
            JObject result = null;
            RestClient client = GetRestClientByFeatureType(featureType);
            RestRequest request = Utilities.GetRestRequest(client, objectId + "/addAttachment", null, true);
            byte[] fileData = null;
            using (var binaryReader = new BinaryReader(file.InputStream))
            {
                fileData = binaryReader.ReadBytes(file.ContentLength);
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                request.AddParameter("keywords", keyword); // Kad tai veiktų, 2022.04.22 buvo kreiptasi į HNIT...
            }
            if (!string.IsNullOrEmpty(exifInfo))
            {
                request.AddParameter("exifInfo", exifInfo);
            }
            request.AddFileBytes("attachment", fileData, file.FileName, file.ContentType);
            IRestResponse response = client.Post(request);
            try
            {
                result = JObject.Parse(response.Content);
            }
            catch
            {
                // ...
            }
            return result;
        }
        public JObject RemovePhotos(string objectId, string featureType, string attachmentIds)
        {
            JObject result = null;
            RestClient client = GetRestClientByFeatureType(featureType);
            RestRequest request = Utilities.GetRestRequest(client, objectId + "/deleteAttachments", null, true);
            request.AddParameter("attachmentIds", attachmentIds);
            IRestResponse response = client.Post(request);
            try
            {
                result = JObject.Parse(response.Content);
            }
            catch
            {
                // ...
            }
            return result;
        }
        public JObject ReplacePhoto(string objectId, string featureType, string attachmentId, HttpPostedFile file, string keyword, string exifInfo)
        {
            JObject result = null;
            RestClient client = GetRestClientByFeatureType(featureType);
            RestRequest request = Utilities.GetRestRequest(client, objectId + "/updateAttachment", null, true);
            byte[] fileData = null;
            using (var binaryReader = new BinaryReader(file.InputStream))
            {
                fileData = binaryReader.ReadBytes(file.ContentLength);
            }
            request.AddParameter("attachmentId", attachmentId);
            if (!string.IsNullOrEmpty(keyword))
            {
                request.AddParameter("keywords", keyword); // Kad tai veiktų, 2022.04.22 buvo kreiptasi į HNIT...
            }
            if (!string.IsNullOrEmpty(exifInfo))
            {
                request.AddParameter("exifInfo", exifInfo);
            }
            request.AddFileBytes("attachment", fileData, file.FileName, file.ContentType);
            IRestResponse response = client.Post(request);
            try
            {
                result = JObject.Parse(response.Content);
            }
            catch
            {
                // ...
            }
            return result;
        }
    }
}
using KZ.Data;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/feature")]
    [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,approve,manage-tasks-test")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class FeatureController : ApiController
    {
        [Route("apply-edits")]
        [HttpPost]
        [Authorize]
        public IHttpActionResult ApplyEdits(ApplyEditsModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                JArray edits = null;
                try
                {
                    edits = JArray.Parse(model.Edits);
                }
                catch
                {
                    // ...
                }
                if (edits != null)
                {
                    if (HttpContext.Current.User.IsInRole("manage-tasks-test") && !model.ServiceId.Equals("tasks"))
                    {
                        // Po to 12.15 Justinas pastebėjo, kad rolė "manage-tasks-test" negali kurti bookmarks'ų...
                        bool onlyBookmarksLayer = false;
                        if (model.ServiceId.Equals("user-points"))
                        {
                            if (edits.Count == 1)
                            {
                                foreach (JObject item in edits.Children())
                                {
                                    if (item.ContainsKey("id"))
                                    {
                                        if (item["id"].ToString().Equals(WebConfigurationManager.AppSettings["BookmarksLayerId"]))
                                        {
                                            onlyBookmarksLayer = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (!onlyBookmarksLayer)
                        {
                            return BadRequest();
                        }
                    }
                    FeatureRepository featureRepository = new FeatureRepository();
                    JObject result = featureRepository.ApplyEdits(model.ServiceId, edits);
                    return Ok(result);
                }
            }
            return BadRequest();
        }

        [Route("add-photo")]
        [HttpPost]
        [Authorize]
        public IHttpActionResult AddPhoto()
        {
            try
            {
                HttpFileCollection files = HttpContext.Current.Request.Files;
                if (files != null && files.Count == 1)
                {
                    string objectId = null;
                    string featureType = null;
                    string keywords = null;
                    string exifInfo = null;
                    if (HttpContext.Current.Request.Form != null)
                    {
                        foreach (string key in HttpContext.Current.Request.Form.Keys)
                        {
                            if (key == "objectId")
                            {
                                objectId = HttpContext.Current.Request.Form[key];
                            }
                            else if (key == "featureType")
                            {
                                featureType = HttpContext.Current.Request.Form[key];
                            }
                            else if (key == "keywords")
                            {
                                keywords = HttpContext.Current.Request.Form[key];
                            }
                            else if (key == "exifInfo")
                            {
                                exifInfo = HttpContext.Current.Request.Form[key];
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(objectId))
                    {
                        FeatureRepository featureRepository = new FeatureRepository();
                        JObject result = featureRepository.AddPhoto(objectId, featureType, files[0], keywords, exifInfo);
                        return Ok(result);
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.Log("ADD PHOTO ERR" + e.Message);
            }
            return BadRequest();
        }

        [Route("remove-photo")]
        [HttpPost]
        [Authorize]
        public IHttpActionResult RemovePhoto()
        {
            string objectId = null;
            string featureType = null;
            string attachmentIds = null;
            foreach (string key in HttpContext.Current.Request.Form.Keys)
            {
                if (key == "objectId")
                {
                    objectId = HttpContext.Current.Request.Form[key];
                }
                else if (key == "featureType")
                {
                    featureType = HttpContext.Current.Request.Form[key];
                }
                else if (key == "attachmentIds")
                {
                    attachmentIds = HttpContext.Current.Request.Form[key];
                }
            }
            if (!string.IsNullOrEmpty(objectId) && !string.IsNullOrEmpty(attachmentIds))
            {
                FeatureRepository featureRepository = new FeatureRepository();
                JObject result = featureRepository.RemovePhotos(objectId, featureType, attachmentIds);
                return Ok(result);
            }
            return BadRequest();
        }

        [Route("replace-photo")]
        [HttpPost]
        [Authorize]
        public IHttpActionResult ReplacePhoto()
        {
            HttpFileCollection files = HttpContext.Current.Request.Files;
            if (files.Count == 1)
            {
                string objectId = null;
                string featureType = null;
                string keywords = null;
                string exifInfo = null;
                string attachmentId = null;
                foreach (string key in HttpContext.Current.Request.Form.Keys)
                {
                    if (key == "objectId")
                    {
                        objectId = HttpContext.Current.Request.Form[key];
                    }
                    else if (key == "attachmentId")
                    {
                        attachmentId = HttpContext.Current.Request.Form[key];
                    }
                    else if (key == "featureType")
                    {
                        featureType = HttpContext.Current.Request.Form[key];
                    }
                    else if (key == "keywords")
                    {
                        keywords = HttpContext.Current.Request.Form[key];
                    }
                    else if (key == "exifInfo")
                    {
                        exifInfo = HttpContext.Current.Request.Form[key];
                    }
                }
                if (!string.IsNullOrEmpty(objectId) && !string.IsNullOrEmpty(attachmentId))
                {
                    FeatureRepository featureRepository = new FeatureRepository();
                    JObject result = featureRepository.ReplacePhoto(objectId, featureType, attachmentId, files[0], keywords, exifInfo);
                    return Ok(result);
                }
            }
            return BadRequest();
        }
    }
}
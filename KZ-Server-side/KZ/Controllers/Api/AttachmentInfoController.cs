using KZ.Data;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/attachment")]
    [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AttachmentInfoController : ApiController
    {
        [Route("get-data")]
        [HttpGet]
        public IHttpActionResult GetData(string id = null)
        {
            if (id != null)
            {
                AttachmentInfoRepository attachmentInfoRepository = new AttachmentInfoRepository();
                IDictionary<string, object> result = attachmentInfoRepository.GetItem(id);
                return Ok(result);
            }
            return BadRequest();
        }
        [Route("save-data")]
        [HttpPost]
        public IHttpActionResult SaveData(AttachmentInfoSaveDataModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                AttachmentInfoRepository attachmentInfoRepository = new AttachmentInfoRepository();
                JObject result = attachmentInfoRepository.SaveData(model);
                return Ok(result);
            }
            return BadRequest();
        }
    }
}
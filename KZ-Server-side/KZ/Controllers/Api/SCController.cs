using KZ.Data;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/sc")]
    [Authorize(Roles = "sc")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SCController : ApiController
    {
        [Route("get-data")]
        [HttpGet]
        public IHttpActionResult GetData(string category, string type = null, string subtype = null, string id = null, bool withCount = false)
        {
            if (category != null)
            {
                SymbolsRepository symbolsRepository = new SymbolsRepository();
                if (id != null)
                {
                    IDictionary<string, object> result = symbolsRepository.GetItem(category, id);
                    return Ok(result);
                }
                else
                {
                    List<Dictionary<string, object>> list = symbolsRepository.GetList(category, type, subtype, withCount);
                    return Ok(list);
                }
            }
            return BadRequest();
        }

        [Route("save-data")]
        [HttpPost]
        public IHttpActionResult SaveData(SCSaveDataModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                SymbolsRepository symbolsRepository = new SymbolsRepository();
                JObject result = symbolsRepository.SaveData(model, HttpContext.Current.User.Identity.Name);
                return Ok(result);
            }
            return BadRequest();
        }

        [Route("delete-item")]
        [HttpPost]
        public IHttpActionResult DeleteItem(SCDeleteItemModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                SymbolsRepository symbolsRepository = new SymbolsRepository();
                bool success = symbolsRepository.DeleteItem(model);
                return Ok(
                    new
                    {
                        success
                    }
                );
            }
            return BadRequest();
        }

        [Route("get-count")]
        [HttpGet]
        public IHttpActionResult GetCount(string id)
        {
            if (id != null)
            {
                SymbolsRepository symbolsRepository = new SymbolsRepository();
                int? count = symbolsRepository.GetCount(id);
                return Ok(count);
            }
            return BadRequest();
        }

        [Route("get-data-by-symbol-id")]
        [HttpGet]
        public IHttpActionResult GetDataBySymbolId(string id)
        {
            if (id != null)
            {
                SymbolsRepository symbolsRepository = new SymbolsRepository();
                List<Dictionary<string, object>> list = symbolsRepository.GetListBySymbolId(id);
                return Ok(list);
            }
            return BadRequest();
        }
    }
}
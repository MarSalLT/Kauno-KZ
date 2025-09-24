using Newtonsoft.Json.Linq;
using RestSharp;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ProxyController : ApiController
    {
        [Route("web-services/proxy")]
        [HttpGet]
        [HttpPost]
        public IHttpActionResult Proxy()
        {
            try
            {
                HttpContext context = HttpContext.Current;
                if (context.Request.Url.Query.Length < 1)
                {
                    return BadRequest();
                }
                string url = context.Request.Url.Query.Substring(1);
                bool canBeProxied = false;
                if (HttpContext.Current.Request.IsAuthenticated)
                {
                    string regex = "^(?:" + WebConfigurationManager.AppSettings["StreetSignsServiceRoot"] + "|" + WebConfigurationManager.AppSettings["VerticalStreetSignsServiceRoot"] + "|" + WebConfigurationManager.AppSettings["TasksServiceRoot"] + ")(?:Feature|Map)Server(.*)";
                    var match = Regex.Match(url, regex);
                    if (match.Groups.Count == 2)
                    {
                        Regex innerRegex = new Regex(@"^((/\d+)?\?f=json$|(/\d+)?/query(RelatedRecords)?(/|$)|/layers\?f=json$|/find|/(\d+)/(\d+)/attachments)");
                        if (innerRegex.IsMatch(match.Groups[1].ToString()))
                        {
                            canBeProxied = true;
                        }
                        else
                        {
                            // Kitokios užklausos rizikingos (ypač applyEdits ar lygiavertės)
                        }
                    }
                    else
                    {
                        // Gal tai papildomas sluoksnis, kuris nėra jautrus ir galima jį lengvai praleisti...
                        if (new Regex("^https://kp.kaunas.lt/image/rest/services/Kelio_zenklai/Papildomi_sluoksniai/MapServer").IsMatch(url)) // TODO: iškelti į config'ą...
                        {
                            canBeProxied = true;
                        }
                    }
                }
                if (canBeProxied)
                {
                    RestClient client = Utilities.GetRestClient(url);
                    RestRequest request = Utilities.GetRestRequest(client);
                    string[] keys = context.Request.Form.AllKeys;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        request.AddParameter(keys[i], context.Request.Form[keys[i]]);
                    }
                    IRestResponse response = client.Post(request);
                    // TODO: galima būtų pergalvoti logiką dėl response.ContentType, bet kol kas esamus poreikius tenkina
                    if (response.ContentType != null && (response.ContentType.StartsWith("image/") || response.ContentType.Equals("application/pdf") || response.ContentType.Equals("application/octet-stream")))
                    {
                        var r = new HttpResponseMessage(HttpStatusCode.OK);
                        MemoryStream stream = new MemoryStream(response.RawBytes);
                        r.Content = new StreamContent(stream);
                        r.Content.Headers.ContentType = new MediaTypeHeaderValue(response.ContentType);
                        r.Content.Headers.ContentLength = stream.Length;
                        return ResponseMessage(r);
                    }
                    else
                    {
                        JObject result = JObject.Parse(response.Content);
                        return Ok(result);
                    }
                }
                else
                {
                    return BadRequest();
                }
            }
            catch
            {
                return Ok();
            }
        }
    }
}
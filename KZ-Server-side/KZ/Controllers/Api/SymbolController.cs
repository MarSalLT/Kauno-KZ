using Dapper;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;

namespace KZ.Controllers
{
    [RoutePrefix("web-services/symbols")]
    public class SymbolController : ApiController
    {
        [Route("get-symbol")]
        [HttpGet]
        public IHttpActionResult GetData(string id)
        {
            // https://stackoverflow.com/questions/10143323/display-image-from-a-datatable-in-aspimage-in-code-behind
            // https://kp.kaunas.lt/kauno_eop_is/web-services/symbols/get-symbol?id=E058E842-8C7D-45EB-955D-E6A5D0FE07FB
            // https://localhost:44397/kauno_eop_is/web-services/symbols/get-symbol?id=56D93310-DEB4-430F-A2A6-65063F0E0771
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "SELECT img_data FROM " + WebConfigurationManager.AppSettings["TableFirstPart"] + "." + "KZ_SYMBOLS WHERE GlobalID = @id;";
                    var res = connection.QuerySingleOrDefault<byte[]>(sql,
                    new
                    {
                        id
                    });
                    if (res != null)
                    {
                        var r = new HttpResponseMessage(HttpStatusCode.OK);
                        MemoryStream stream = new MemoryStream(res);
                        r.Content = new StreamContent(stream);
                        r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        r.Content.Headers.ContentLength = stream.Length;
                        return ResponseMessage(r);
                    }
                }
            }
            catch (SqlException e)
            {
                // ...
            }
            return BadRequest();
        }
    }
}

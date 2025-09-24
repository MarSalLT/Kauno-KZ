using KZ.Data;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/users-data")]
    [Authorize(Roles = "manage-tasks,manage-tasks-test")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UsersDataController : ApiController
    {
        [Route("get-approvers")]
        [HttpGet]
        public IHttpActionResult GetApprovers()
        {
            UsersRepository usersRepository = new UsersRepository();
            List<Dictionary<string, object>> users = usersRepository.GetApprovers();
            return Ok(users);
        }
    }
}

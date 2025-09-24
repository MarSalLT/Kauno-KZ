using KZ.Data;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/users")]
    [Authorize(Roles = "manage-users")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserManagementController : ApiController
    {
        [Route("get-users")]
        [HttpGet]
        public IHttpActionResult GetUsers()
        {
            UsersRepository usersRepository = new UsersRepository();
            List<Dictionary<string, object>> users = usersRepository.GetUsers();
            return Ok(users);
        }

        [Route("delete")]
        [HttpPost]
        public IHttpActionResult DeleteUser(DeleteUserModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                UsersRepository usersRepository = new UsersRepository();
                bool success = usersRepository.DeleteUser(model);
                return this.Ok(
                    new
                    {
                        success
                    }
                );
            }
            return BadRequest();
        }

        [Route("create-or-update")]
        [HttpPost]
        public IHttpActionResult CreateOrUpdateUser(UserModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                UsersRepository usersRepository = new UsersRepository();
                JObject result;
                if (string.IsNullOrEmpty(model.Id))
                {
                    result = usersRepository.CreateUser(model);
                }
                else
                {
                    bool success = usersRepository.UpdateUser(model);
                    result = new JObject
                    {
                        { "success", success }
                    };
                }
                return Ok(
                    result
                );
            }
            return BadRequest();
        }
    }
}
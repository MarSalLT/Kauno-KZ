using KZ.Data;
using KZ.Models;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/user")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserController : ApiController
    {
        [Route("get-data")]
        [HttpGet]
        public IHttpActionResult GetData()
        {
            if (HttpContext.Current.Items["userRepository"] is IUserRepository userRepository)
            {
                return Ok(userRepository.GetUserData(HttpContext.Current.User.Identity.Name));
            }
            return InternalServerError();
        }

        [Route("login")]
        [HttpPost]
        public IHttpActionResult Login(LoginModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                if (HttpContext.Current.Items["userRepository"] is IUserRepository userRepository)
                {
                    if (userRepository.IsUserValid(model))
                    {
                        string username = model.Username.ToString();
                        Dictionary<string, object> userData = userRepository.GetUserData(username);
                        HttpContext.Current.Session.Add("name", userData["username"]);
                        HttpContext.Current.Session.Add("role", userData["role"]);
                        HttpContext.Current.Session.Add("permissions", userData["permissions"]);
                        HttpContext.Current.Session.Add("email", userData["email"]);
                        HttpContext.Current.Session.Add("id", userData["id"]);
                        return Ok(userData);
                    }
                }
                else
                {
                    return InternalServerError();
                }
            }
            return BadRequest();
        }

        [Route("logout")]
        [HttpPost]
        public IHttpActionResult Logout()
        {
            HttpContext.Current.Session.Abandon();
            return this.Ok(
                new
                {
                    success = true
                }
            );
        }

        [Route("check-if-valid-session")]
        [HttpPost]
        public IHttpActionResult CheckIfValidSession()
        {
            bool valid = false;
            if (HttpContext.Current.Session["name"] != null)
            {
                valid = true;
            }
            return this.Ok(
                new
                {
                    valid
                }
            );
        }
    }
}
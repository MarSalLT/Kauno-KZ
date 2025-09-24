using Dapper;
using KZ.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.WebPages;

namespace KZ.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string tablePart1 = WebConfigurationManager.AppSettings["TableFirstPart"];
        private readonly string tablePart2 = "KZ_USERS";
        public Dictionary<string, object> GetUserData(string username)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            if (username != null && !username.IsEmpty())
            {
                try
                {
                    SqlConnection connection = null;
                    using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                    {
                        string sql = "SELECT name, role, email, uzsakovo_vardas, uzsakovo_imone, GlobalID AS id FROM " + tablePart1 + "." + tablePart2 + " WHERE name = @name";
                        var res = connection.QuerySingle(sql, new { name = username });
                        if (res != null)
                        {
                            data["username"] = res.name.ToString();
                            data["role"] = res.role.ToString();
                            data["email"] = null;
                            if (res.email != null)
                            {
                                data["email"] = res.email.ToString();
                            }
                            data["id"] = null;
                            if (res.id != null)
                            {
                                data["id"] = res.id.ToString();
                            }
                            data["permissions"] = GetUserPermissions(res.role.ToString());
                            data["street-signs-service-root"] = WebConfigurationManager.AppSettings["StreetSignsServiceRoot"];
                            data["vertical-street-signs-service-root"] = WebConfigurationManager.AppSettings["VerticalStreetSignsServiceRoot"];
                            data["tasks-service-root"] = WebConfigurationManager.AppSettings["TasksServiceRoot"];
                            // data["secured-services-url-match"] = WebConfigurationManager.AppSettings["SecuredServicesUrlMatch"];
                            data["mobile-users"] = WebConfigurationManager.AppSettings["MobileUsers"];
                        }
                    }
                }
                catch (Exception e)
                {
                    // ...
                }
                // */
            }
            return data;
        }
        private List<string> GetUserPermissions(string role)
        {
            List<string> permissions = new List<string>();
            switch (role)
            {
                case "admin":
                    permissions.Add("approve");
                    permissions.Add("kz-horizontal-edit");
                    permissions.Add("kz-infra-edit");
                    permissions.Add("kz-vertical-edit");
                    permissions.Add("manage-tasks");
                    permissions.Add("manage-users");
                    permissions.Add("pano-full");
                    permissions.Add("sc");
                    break;
                case "approver":
                    permissions.Add("approve");
                    permissions.Add("kz-horizontal-edit");
                    permissions.Add("kz-infra-edit");
                    permissions.Add("kz-vertical-edit");
                    permissions.Add("manage-tasks");
                    permissions.Add("pano-full");
                    permissions.Add("sc");
                    break;
                case "kz-edit":
                    permissions.Add("kz-horizontal-edit");
                    permissions.Add("kz-infra-edit");
                    permissions.Add("kz-vertical-edit");
                    permissions.Add("pano-full");
                    permissions.Add("sc");
                    break;
                case "kz-horizontal-edit":
                    permissions.Add("kz-horizontal-edit");
                    permissions.Add("pano-full");
                    break;
                case "kz-infra-edit":
                    permissions.Add("kz-infra-edit");
                    permissions.Add("pano-full");
                    break;
                case "kz-vertical-edit":
                    permissions.Add("kz-vertical-edit");
                    permissions.Add("pano-full");
                    permissions.Add("sc");
                    break;
                case "street-viewer":
                    // ...
                    break;
                case "symbols-manager":
                    permissions.Add("sc");
                    break;
                case "tasks-tester":
                    permissions.Add("manage-tasks-test");
                    permissions.Add("pano-full");
                    permissions.Add("sc");
                    break;
                case "viewer-history":
                    permissions.Add("view-history");
                    break;
            }
            return permissions;
        }
        public bool IsUserValid(LoginModel model)
        {
            bool isValid = false;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "SELECT name FROM " + tablePart1 + "." + tablePart2 + " WHERE name = @name AND password = @password";
                    var res = connection.QuerySingle(sql, new { name = model.Username, password = Utilities.ComputeSha256Hash(model.Password) });
                    if (res != null)
                    {
                        isValid = true;
                    }
                }
            }
            catch (Exception e)
            {
                // ...
            }
            // */
            return isValid;
        }
        public void RegisterUser(RegisterModel model)
        {
            throw new NotImplementedException();
        }
    }
}
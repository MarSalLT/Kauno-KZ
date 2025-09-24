using Dapper;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Configuration;

namespace KZ.Data
{
    public class UsersRepository
    {
        private readonly string tablePart1 = WebConfigurationManager.AppSettings["TableFirstPart"];
        private readonly string tablePart2 = "KZ_USERS";
        public List<Dictionary<string, object>> GetUsers()
        {
            List<Dictionary<string, object>> users = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    // FIXME: ar geras sprendimas? Info šaltiniai:
                    // https://stackoverflow.com/questions/24210168/how-to-use-group-by-to-concatenate-strings-while-joining-multiple-tables
                    // https://dba.stackexchange.com/questions/157380/query-with-join-and-concatenation-of-one-joined-tables-column
                    string sql = "";
                    sql += "SELECT u.name, u.role, CONCAT('{', u.GlobalID, '}') AS id, u.email, u.uzsakovo_vardas, u.uzsakovo_imone";
                    sql += " FROM " + tablePart1 + "." + tablePart2 + " AS u ORDER BY name";
                    var res = connection.Query(sql).ToList();
                    if (res != null)
                    {
                        users = new List<Dictionary<string, object>>();
                        foreach (object row in res)
                        {
                            IDictionary<string, object> r = (IDictionary<string, object>)row;
                            users.Add(new Dictionary<string, object>(r));
                        }
                    }
                }
            }
            catch
            {
                // ...
            }
            return users;
        }

        public bool DeleteUser(DeleteUserModel model)
        {
            SqlConnection connection;
            bool success = false;
            using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
            {
                string sql = "DELETE FROM " + tablePart1 + "." + tablePart2 + " WHERE GlobalID = @id;";
                int count = connection.Execute(sql,
                new
                {
                    id = model.Id
                });
                if (count == 1)
                {
                    success = true;
                }
            }
            return success;
        }

        public JObject CreateUser(UserModel model)
        {
            SqlConnection connection;
            JObject result = new JObject
            {
                { "success", false }
            };
            try
            {
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "DECLARE @id AS INTEGER;";
                    sql += " EXEC dbo.next_rowid '" + tablePart1 + "', '" + tablePart2 + "', @id OUTPUT;";
                    sql += " INSERT INTO " + tablePart1 + "." + tablePart2 + "(name, password, role, email, uzsakovo_vardas, uzsakovo_imone, OBJECTID, GlobalID) OUTPUT INSERTED.* VALUES(@name, @password, @role, @email, @clientName, @clientEnterprise, @id, NEWID());";
                    var res = connection.QuerySingle(sql,
                    new
                    {
                        name = model.Name,
                        password = Utilities.ComputeSha256Hash(model.Password),
                        role = model.Role,
                        email = model.Email,
                        clientName = model.ClientName,
                        clientEnterprise = model.ClientEnterprise,
                    });
                    if (res != null)
                    {
                        string email = null;
                        if (res.email != null) {
                            email = res.email.ToString();
                        }
                        result = new JObject
                        {
                            { "success", true },
                            { "id", res.GlobalID.ToString() },
                            { "name", res.name.ToString() },
                            { "role", res.role.ToString() },
                            { "email", email }
                        };
                    }
                }
            }
            catch (SqlException exception)
            {
                // Tikrai klaida bus, jei bandysime užregistruoti vartotoją su jau egzistuojančiu vardu!
                if (exception.Number == 2601)
                {
                    result.Add("reason", "Vartotojas tokiu vardu jau egzistuoja!");
                }
            }
            return result;
        }

        public bool UpdateUser(UserModel model)
        {
            SqlConnection connection;
            bool success = false;
            string password = model.Password;
            if (!string.IsNullOrEmpty(password))
            {
                password = Utilities.ComputeSha256Hash(password);
            }
            using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
            {
                string sql = "UPDATE " + tablePart1 + "." + tablePart2 + " SET role = @role, email = @email, uzsakovo_vardas = @clientName, uzsakovo_imone = @clientEnterprise, password = ISNULL(@password, password) WHERE GlobalID = @id;";
                int count = connection.Execute(sql,
                new
                {
                    role = model.Role,
                    email = model.Email,
                    clientName = model.ClientName,
                    clientEnterprise = model.ClientEnterprise,
                    password,
                    id = model.Id
                });
                if (count == 1)
                {
                    success = true;
                }
            }
            return success;
        }

        public List<Dictionary<string, object>> GetApprovers()
        {
            List<Dictionary<string, object>> users = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "SELECT name, email FROM " + tablePart1 + "." + tablePart2 + " WHERE role = @role AND email IS NOT NULL ORDER BY name";
                    var res = connection.Query(sql,
                    new
                    {
                        role = "approver"
                    }).ToList();
                    if (res != null)
                    {
                        users = new List<Dictionary<string, object>>();
                        foreach (object row in res)
                        {
                            IDictionary<string, object> r = (IDictionary<string, object>)row;
                            users.Add(new Dictionary<string, object>(r));
                        }
                    }
                }
            }
            catch
            {
                // ...
            }
            return users;
        }
    }
}
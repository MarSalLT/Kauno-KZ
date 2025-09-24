using Dapper;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Configuration;

namespace KZ.Data
{
    public class SymbolsRepository
    {
        private readonly string tablePart1 = WebConfigurationManager.AppSettings["TableFirstPart"];
        private readonly string tablePart2 = "KZ_SYMBOLS";
        public List<Dictionary<string, object>> GetList(string category, string type, string subtype, bool withCount)
        {
            List<Dictionary<string, object>> list = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "";
                    if (withCount)
                    {
                        sql += "With r AS (SELECT s.GlobalID, COUNT(v.UnikSimbolID) AS signs_count FROM " + tablePart1 + "." + tablePart2 + " AS s LEFT JOIN " + tablePart1 + ".KZ AS v ON s.GlobalID = v.UnikSimbolID WHERE (v.GDB_TO_DATE > '9999' OR v.GDB_TO_DATE IS NULL) GROUP BY s.GlobalID)";
                        sql += " SELECT s.GlobalID AS id, s.author, s.category, s.type, s.subtype, s.img_width, s.img_height, r.signs_count FROM r LEFT JOIN " + tablePart1 + "." + tablePart2 + " AS s ON r.GlobalID = s.GlobalID";
                    }
                    else
                    {
                        sql += "SELECT *, GlobalID AS id";
                        sql += " FROM " + tablePart1 + "." + tablePart2;
                    }
                    sql += " WHERE category = @category";
                    if (type != null)
                    {
                        sql += " AND type = @type";
                    }
                    string[] subtypes = null;
                    if (subtype != null)
                    {
                        sql += " AND subtype IN @subtypes";
                        subtypes = subtype.Split(',');
                    }
                    if (withCount) {
                        // ... Prikabinti kažkaip?..
                    }
                    var res = connection.Query(sql,
                    new
                    {
                        category,
                        type,
                        subtypes
                    }).ToList();
                    if (res != null)
                    {
                        list = new List<Dictionary<string, object>>();
                        foreach (object row in res)
                        {
                            IDictionary<string, object> r = (IDictionary<string, object>)row;
                            list.Add(new Dictionary<string, object>(r));
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                // ...
            }
            return list;
        }
        public IDictionary<string, object> GetItem(string category, string id)
        {
            IDictionary<string, object> result = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "";
                    sql += "SELECT *, GlobalID AS id";
                    sql += " FROM " + tablePart1 + "." + tablePart2;
                    sql += " WHERE category = @category AND GlobalID = @id";
                    var res = connection.QuerySingle(sql,
                    new
                    {
                        category,
                        id
                    });
                    if (res != null)
                    {
                        result = (IDictionary<string, object>)res;
                    }
                }
            }
            catch (SqlException e)
            {
                // ...
            }
            return result;
        }
        public JObject SaveData(SCSaveDataModel model, string author)
        {
            JObject result = new JObject
            {
                { "success", false }
            };
            string img = model.DataUrl.Replace("data:image/png;base64,", "").Replace(" ", "+");
            byte[] imgData = Convert.FromBase64String(img);
            int imgWidth = 0;
            int imgHeight = 0;
            using (var ms = new MemoryStream(imgData))
            {
                Image image = Image.FromStream(ms);
                imgWidth = image.Width;
                imgHeight = image.Height;
            }
            _ = Encoding.Default.GetString(imgData);
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string id = model.Id;
                    bool success = false;
                    if (id == null)
                    {
                        string sql = "DECLARE @id AS INTEGER;";
                        sql += " EXEC dbo.next_rowid '" + tablePart1 + "', '" + tablePart2 + "', @id OUTPUT;";
                        sql += " INSERT INTO " + tablePart1 + "." + tablePart2 + "(category, type, subtype, data, author, img_width, img_height, OBJECTID, GlobalID, img_data) OUTPUT INSERTED.* VALUES(@category, @type, @subtype, @data, @author, @imgWidth, @imgHeight, @id, NEWID(), @imgData);";
                        var res = connection.QuerySingle(sql,
                        new
                        {
                            category = model.Category,
                            type = model.Type,
                            subtype = model.Subtype,
                            data = model.Data,
                            author,
                            imgWidth,
                            imgHeight,
                            imgData
                        });
                        if (res != null)
                        {
                            id = res.GlobalID.ToString();
                            success = true;
                        }
                    }
                    else
                    {
                        string sql = "UPDATE " + tablePart1 + "." + tablePart2 + " SET type = @type, subtype = @subtype, data = @data, author = @author, img_width = @imgWidth, img_height = @imgHeight, img_data = @imgData WHERE GlobalID = @id;";
                        int count = connection.Execute(sql,
                        new
                        {
                            type = model.Type,
                            subtype = model.Subtype,
                            data = model.Data,
                            author,
                            imgWidth,
                            imgHeight,
                            imgData,
                            id
                        });
                        if (count == 1)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        // SaveImage(model.Category, id, imgData); // Kaunui tai labai nepatiko :)
                        result = new JObject
                        {
                            { "success", true },
                            { "id", id }
                        };
                    }
                }
            }
            catch (Exception exception)
            {
                result = new JObject
                {
                    { "error", exception.ToString() }
                };
            }
            return result;
        }
        public bool DeleteItem(SCDeleteItemModel model)
        {
            bool success = false;
            try
            {
                SqlConnection connection;
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
                        // TODO: dar reiktų šalinti piešinuką iš folder'io?..
                        success = true;
                    }
                }
            }
            catch
            {
                // ...
            }
            return success;
        }
        public int? GetCount(string id)
        {
            int? count = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "SELECT COUNT(GlobalID) FROM " + tablePart1 + ".KZ WHERE UnikSimbolID = @id";
                    count = connection.ExecuteScalar<int>(sql,
                    new
                    {
                        id
                    });
                }
            }
            catch (SqlException e)
            {
                // ...
            }
            return count;
        }
        public List<Dictionary<string, object>> GetListBySymbolId(string id)
        {
            List<Dictionary<string, object>> list = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "SELECT GlobalID FROM " + tablePart1 + ".KZ WHERE UnikSimbolID = @id AND (GDB_TO_DATE > '9999' OR GDB_TO_DATE IS NULL)";
                    var res = connection.Query(sql,
                    new
                    {
                        id
                    }).ToList();
                    if (res != null)
                    {
                        list = new List<Dictionary<string, object>>();
                        foreach (object row in res)
                        {
                            IDictionary<string, object> r = (IDictionary<string, object>)row;
                            list.Add(new Dictionary<string, object>(r));
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                // ...
            }
            return list;
        }
    }
}
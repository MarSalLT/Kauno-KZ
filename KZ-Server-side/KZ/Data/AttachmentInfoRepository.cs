using Dapper;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace KZ.Data
{
    public class AttachmentInfoRepository
    {
        private readonly string tablePart1 = WebConfigurationManager.AppSettings["TableFirstPart"];
        private readonly string tablePart2 = "KZ_ATTACH_CONTENT";
        public IDictionary<string, object> GetItem(string id)
        {
            IDictionary<string, object> result = null;
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    string sql = "";
                    sql += "SELECT data";
                    sql += " FROM " + tablePart1 + "." + tablePart2;
                    sql += " WHERE attachment_guid = @id";
                    var res = connection.QuerySingleOrDefault(sql,
                    new
                    {
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
        public JObject SaveData(AttachmentInfoSaveDataModel model)
        {
            JObject result = new JObject
            {
                { "success", false }
            };
            try
            {
                SqlConnection connection = null;
                using (connection = new SqlConnection(WebConfigurationManager.AppSettings["DbConnectionString"]))
                {
                    bool success = false;
                    // Reikia daryti ale `UPSERT`??... Ale `ON DUPLICATE KEY UPDATE`...
                    // Dabar toks gan kvailas sprendimas...
                    IDictionary<string, object> r = GetItem(model.AttachmentId);
                    if (r == null) {
                        string sql = "DECLARE @id AS INTEGER;";
                        sql += " EXEC dbo.next_rowid '" + tablePart1 + "', '" + tablePart2 + "', @id OUTPUT;";
                        sql += " INSERT INTO " + tablePart1 + "." + tablePart2 + "(attachment_guid, data, OBJECTID) OUTPUT INSERTED.* VALUES(@attachmentId, @data, @id);";
                        var res = connection.QuerySingle(sql,
                        new
                        {
                            attachmentId = model.AttachmentId,
                            data = model.Data,
                        });
                        if (res != null)
                        {
                            success = true;
                        }
                    } else {
                        string sql = "UPDATE " + tablePart1 + "." + tablePart2 + " SET data = @data WHERE attachment_guid = @attachmentId;";
                        int count = connection.Execute(sql,
                        new
                        {
                            attachmentId = model.AttachmentId,
                            data = model.Data,
                        });
                        if (count == 1)
                        {
                            success = true;
                        }
                    }
                    if (success)
                    {
                        result = new JObject
                        {
                            { "success", true }
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
    }
}
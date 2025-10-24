using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Http.Results;

namespace KZ
{
    public class Utilities
    {
        static public string ComputeSha256Hash(string rawData)
        {
            // https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        static public RestClient GetRestClient(string url)
        {
            RestClient client = new RestClient(url);

            // Disable proxy to avoid 10.9.9.50:3000 routing issues on server
            client.Proxy = null;

            if (WebConfigurationManager.AppSettings["SecuredServicesUrlMatchNTLM"] != null && url.Contains(WebConfigurationManager.AppSettings["SecuredServicesUrlMatchNTLM"]))
            {
                // Taip buvo apsaugoti servisai Vilniaus m.
                // Kauno m. neaktualu, bet gali būti kada nors naudinga
                client.Authenticator = new NtlmAuthenticator(WebConfigurationManager.AppSettings["SecuredServicesUsername"], WebConfigurationManager.AppSettings["SecuredServicesPassword"]);
            }
            return client;
        }
        static public RestRequest GetRestRequest(RestClient client, string url = null, DataFormat? dataFormat = DataFormat.Json, bool addFJson = false)
        {
            string baseUrl = client.BaseUrl.ToString();
            RestRequest request = new RestRequest(url);
            if (WebConfigurationManager.AppSettings["SecuredServicesUrlMatch"] != null && baseUrl.Contains(WebConfigurationManager.AppSettings["SecuredServicesUrlMatch"]))
            {
                if (WebConfigurationManager.AppSettings["SecuredServicesLoginUrl"] != null) {
                    bool doGetSecuredServiceLoginData = true;
                    if (HttpContext.Current.Session["agstoken"] != null && HttpContext.Current.Session["secured-service-token-created"] != null)
                    {
                        DateTime tokenCreated = DateTime.Parse(HttpContext.Current.Session["secured-service-token-created"].ToString());
                        DateTime currentTime = DateTime.UtcNow;
                        TimeSpan dif = currentTime.Subtract(tokenCreated);
                        if (dif.TotalSeconds < 1800)
                        { // 30 min... TODO: iškelti į config'ą?..
                            doGetSecuredServiceLoginData = false;
                        }
                    }
                    if (doGetSecuredServiceLoginData)
                    {
                        GetSecuredServiceLoginData(WebConfigurationManager.AppSettings["SecuredServicesLoginUrl"].ToString());
                    }
                    if (HttpContext.Current.Session["agstoken"] != null)
                    {
                        request.AddCookie("agstoken", HttpContext.Current.Session["agstoken"].ToString());
                    }
                    if (HttpContext.Current.Session["AGS_ROLES"] != null)
                    {
                        request.AddCookie("AGS_ROLES", HttpContext.Current.Session["AGS_ROLES"].ToString());
                    }
                }
            }
            if (addFJson) {
                request.AddParameter("f", "json");
            }
            if (dataFormat != null)
            {
                request.RequestFormat = (DataFormat)dataFormat;
            }
            return request;
        }
        static public void GetSecuredServiceLoginData(string url)
        {
            RestClient client = new RestClient(url)
            {
                CookieContainer = new CookieContainer()
            };
            RestRequest request = new RestRequest();
            request.AddParameter("username", WebConfigurationManager.AppSettings["SecuredServicesUsername"]);
            request.AddParameter("password", WebConfigurationManager.AppSettings["SecuredServicesPasswordEncrypted"]);
            client.Post(request);
            // Esmė, kad sėkmingas prisijungimas prie rest/login nustato agstoken, bet po to vėksta redirect'as į rest/services ir ten nusistato AGS_ROLES...
            try
            {
                // https://stackoverflow.com/questions/15983166/how-can-i-get-all-cookies-of-a-cookiecontainer
                Hashtable k = (Hashtable)client.CookieContainer.GetType().GetField("m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client.CookieContainer);
                foreach (DictionaryEntry element in k)
                {
                    SortedList l = (SortedList)element.Value.GetType().GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(element.Value);
                    foreach (var e in l)
                    {
                        var cl = (CookieCollection)((DictionaryEntry)e).Value;
                        foreach (Cookie fc in cl)
                        {
                            if (fc.Name == "agstoken" || fc.Name == "AGS_ROLES")
                            {
                                HttpContext.Current.Session.Add(fc.Name, fc.Value);
                            }
                            // HttpContext.Current.Session.Add("secured-service-token-expires", fc.Expires);
                            HttpContext.Current.Session.Add("secured-service-token-created", DateTime.UtcNow);
                        }
                    }
                }
            }
            catch
            {
                // ...
            }
        }
        static public string Log(string message)
        {
            string error = null;
            string path = WebConfigurationManager.AppSettings["LogDir"];
            path = Path.Combine(path, "log.txt");
            if (!File.Exists(path))
            {
                try
                {
                    var myFile = File.Create(path);
                    myFile.Close();
                }
                catch (Exception e)
                {
                    error = e.ToString();
                }
            }
            if (File.Exists(path))
            {
                try
                {
                    using (StreamWriter w = File.AppendText(path))
                    {
                        w.WriteLine("{0}, {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                        w.WriteLine(message);
                        w.WriteLine("-------------------------------");
                    }
                }
                catch (Exception e)
                {
                    error = e.ToString();
                }
            }
            return error;
        }
    }
}
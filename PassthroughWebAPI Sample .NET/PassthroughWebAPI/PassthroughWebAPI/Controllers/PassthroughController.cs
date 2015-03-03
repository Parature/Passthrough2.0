using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PassthroughWebAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Web.Hosting;
using System.Web.Http;

namespace PassthroughWebAPI.Controllers
{
    public class PassthroughController : ApiController
    {
        // GET: api/Passthrough
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Passthrough/5
        public string Get(int id)
        {
            return "value";
        }

        const string instanceIdParamName = "ping.instanceId";


        // POST: api/Passthrough
        public PassthroughResponse Post([FromBody]PassthroughRequest passthroughRequest)
        {
            var dropOffURL = "https://sso-mutual-auth.parature.com/ext/ref/dropoff";
            //there are many ways of restricting access to this service. simplest would be to reject calls that donot have the mathcing GUID in the SecretGUIDPhrase. And you can possibly restrict calls from a single IP address.


            var token = string.Empty;

            var collection = new X509Certificate2Collection();
            //add your cert to appData then mark it Type : None, copy : Always.
            collection.Import(HostingEnvironment.MapPath("~/bin/App_Data/YourCert.pfx"), "p", X509KeyStorageFlags.PersistKeySet);


            var requestToPing = (HttpWebRequest)WebRequest.Create(dropOffURL);
            requestToPing.Method = "POST";
            requestToPing.PreAuthenticate = true;
            requestToPing.ClientCertificates.Add(collection[0]);
            requestToPing.Headers.Add(instanceIdParamName, passthroughRequest.subject);

            var message = JsonConvert.SerializeObject(passthroughRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            using (var sr = new StreamWriter(requestToPing.GetRequestStream()))
            {
                sr.Write(message);
            }

            // read response from Parature and grab the REF parameter
            var responseFromPing = requestToPing.GetResponse();
            JToken refId = "0";
            if (responseFromPing != null)
            {
                using (var reader = new StreamReader(responseFromPing.GetResponseStream()))
                {
                    JObject.Parse(reader.ReadToEnd()).TryGetValue("REF", out refId);
                }
            }

            if (refId != null)
            {
                token = refId.ToString();
            }
            return new PassthroughResponse(token);
        }


    }
}

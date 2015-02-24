using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;

namespace PassThrough
{
    public partial class ParatureNewPassThrough : System.Web.UI.Page
    {
        /// <summary>
        /// parameter name to denote the ping fed instance id configured for your instance.
        /// </summary>
        private const string instanceIdParamName = "ping.instanceId";

        /// <summary>
        /// instance ID that is configured to accept data from you.
        /// </summary>
        private const string securePassThroughPingFedInstanceID = "YOURSecurePassthroughInstanceIDWhichWeGaveYou";


        /// <summary>
        /// CSR or system email address used for pass through
        /// </summary>
        public string SessEmail { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {

            //NOTE if the authorization fails using my self signed certificate. you might need to add it to your local certificate store.

            //valid API related CSR user account to your department.
            SessEmail = "api@parature.com";
            //send required user informations. you could pass custom information about the user like phone number in the json object inline with your previous passthrough implementation.
            //in GetSecurePassThroughRefID currently i am using my self signed certificate to authenticate the request. you can replace to your signed certificate, i would need details about your public certificate to update on my end.
            JToken refId = GetSecurePassThroughRefID(SessEmail, "Parature", "Tester", "userspassword", "parature_tester@parature.com", "parature_tester", "Tester Account", 99999);


            //Secure PassThrough URL
            //you can test in your sandbox <yourinstance-sandbox>.parature.com before you switch to your production <yourInstance>.parature.com. NOTE: sandbox might render portal with CSS or JS issues. for example, if my instance is s6 then below will be my sandbox URL
            var portalURL = "https://<yourInstance>.parature.com/ics/support/security2.asp";
            //this page adds details to its DOM to do a client side POST to security2.asp, which would get the user authenticated to the support portal based on the refID and instance ID.
            //refID is short lived. so you cannot cache it.

            //we will trigger a client form POST with refID and instanceID, to the security2.asp
            this.Form.Action = portalURL;
            var sb = new StringBuilder();
            sb.Append(string.Format(@"<input type=""hidden"" name=""refID"" value=""{0}"" />", refId));
            sb.Append(string.Format(@"<input type=""hidden"" name=""instanceID"" value=""{0}"" />", securePassThroughPingFedInstanceID));
            sb.Append("<script type='text/javascript'>document.forms[0].submit();</script>");
            securePassThroughDetails.InnerHtml = sb.ToString();

        }

        /// <summary>
        /// method which communicates to the sso-mutual-auth.parature.com
        /// </summary>
        /// <param name="sessEmail">system user id</param>
        /// <param name="firstName">first name</param>
        /// <param name="lastName">last name</param>
        /// <param name="password">user password, send empty if you plan not to update</param>
        /// <param name="emailID">users email id</param>
        /// <param name="userID">userid</param>
        /// <param name="accountName">account user is associated with</param>
        /// <param name="departmentID">your parature instance department id</param>
        /// <returns></returns>
        public string GetSecurePassThroughRefID(string sessEmail, string firstName, string lastName, string password, string emailID, string userID, string accountName, int departmentID)
        {
            //send  user data to the Parature pass through DropOff Url where you will drop off information about the user to be authenticated.
            var dropOffURL = "https://sso-mutual-auth.parature.com/ext/ref/dropoff";

            var token = string.Empty;
            var collection = new X509Certificate2Collection();
            //TODO Update the code below to use your signed certificate. Make sure the certificate with private key is installed in your server machine/developer box the server side logic is deployed.
            //You can perform a variety of methods to pick the certificate from your certificate store and to pass it along the request you make. 
            //here i am importing the certificate using the .cer I have with in my website. You can export your certificate from the store into a .cer file for this purposes.
            collection.Import(Server.MapPath("ExportedSecurePassThroughCertFromCertStore.cer"));

            var requestToPing = (HttpWebRequest)WebRequest.Create(dropOffURL);
            requestToPing.Method = "POST";
            requestToPing.PreAuthenticate = true;
            requestToPing.ClientCertificates.Add(collection[0]);
            requestToPing.Headers.Add(instanceIdParamName, securePassThroughPingFedInstanceID);

            //am constructing the JSON payload with required information. you can take a look at the sample payload here samplePayloadThatsSentToSS0-Mutual-Auth.txt
            var payloadObj = new {
                subject = securePassThroughPingFedInstanceID,
                payload = new {
                    sessEmail = sessEmail,
                    cFname = firstName,
                    cLname = lastName,
                    cEmail = emailID,
                    cPassword = password,
                    cStatus = "REGISTERED",
                    cUname = userID,
                    cTou = "1", // 1 is accepting terms and conditions
                    amName = accountName,
                    deptID = departmentID.ToString(),
                    customKeyValuesAllowed = "CustomFieldValue" // Note: you can pass custom values to update Customer fields after configuring them in the back end. you can ignore if you dont need these custom values. 
                }
            };
            
            using (var sr = new StreamWriter(requestToPing.GetRequestStream()))
            {
                sr.Write(JsonConvert.SerializeObject(payloadObj));
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
            return token;
        }

    }
}

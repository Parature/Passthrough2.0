using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecurePassthroughPlayground
{
    public partial class MainForm : Form
    {
        private const string instanceIdParamName = "ping.instanceId";

        public MainForm()
        {
            InitializeComponent();
        
        }

        private void button1_Click(object sender, EventArgs e)
        {
            log.Text = "";

            try
            {
                var collection = new X509Certificate2Collection();
                //collection.Import(certPath.Text, pass.Text, X509KeyStorageFlags.PersistKeySet);
                collection.Import(certPath.Text);

                //send this user data to the Parature DropOff Url (this is a server-side request)
                var requestToPing = (HttpWebRequest)WebRequest.Create(url.Text);
                requestToPing.Method = "POST";
                requestToPing.PreAuthenticate = true;
                requestToPing.ClientCertificates.Add(collection[0]);
                requestToPing.Headers.Add(instanceIdParamName, instanceId.Text);

                using (var sr = new StreamWriter(requestToPing.GetRequestStream()))
                {
                    sr.Write(payload.Text);
                }

                // read response from Parature and grab the REF parameter
                WebResponse responseFromPing = requestToPing.GetResponse();
                JToken refId;
                using (var reader = new StreamReader(responseFromPing.GetResponseStream()))
                {
                    JObject.Parse(reader.ReadToEnd()).TryGetValue("REF", out refId);
                }

                Log("Drop Off was successful. Ref Id = " + refId);
                Log("");

                string PostDataStr = string.Format("refID={0}&instanceID={1}", refId, instanceId.Text);
                byte[] PostDataByte = Encoding.UTF8.GetBytes(PostDataStr);
                string AdditionalHeaders = "Content-Type: application/x-www-form-urlencoded" + Environment.NewLine;

                webBrowser1.Navigate(passThru.Text, "", PostDataByte, AdditionalHeaders);
                webUrl.Text = passThru.Text;

                Log(string.Format("Post Data sent to {0}:", passThru.Text));
                Log("");
                Log(PostDataStr);

                splitContainer2.Panel2.Enabled = true;
            }
            catch (Exception e1)
            {
                log.Text = "";
                Log("Error: " + e1);
            }
        }

        private void Log(string message)
        {
            log.AppendText(message);
            log.AppendText(Environment.NewLine);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            certPath.Text = openFileDialog1.FileName;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            splitContainer2.Panel2.Enabled = false;
        }

        public string GetPrettyPrintedJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            payload.Text = GetPrettyPrintedJson(payload.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            webBrowser1.Navigate(webUrl.Text);
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            webUrl.Text = webBrowser1.Url.ToString();
        }
    }
}
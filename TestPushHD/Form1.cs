using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace TestPushHD
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TB_key.Text = "https://hk2.notify.windows.com/?token=AwYAAAAfWK0vcSzKaXFpcRVOhi5IjJnjv5fz63OWN8v7CwgjcaMWKh7%2fbkAF2q8hE4%2fmy2vvw3CI3TbmRkJ3KtPmwENP1GSKrHST6QhdXdBbod3MEop5ELXb%2fzKLeUIlEZUWEMA%3d";
            TB_img.Text = "http://p.img.nct.nixcdn.com/playlist/2015/04/27/0/d/5/e/1430100842168.jpg";
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }



        private static string XmlToastTemplate = @"<toast launch=""{0}"">
                                                <visual lang=""en-US"">
                                                <binding template=""{1}"">
                                                <image id=""1"" src=""{2}"" alt=""red graphic""/>
                                                <text id=""1"">{3}</text>
                                                <text id=""2"">{4}</text>
                                                </binding>
                                              
                                                </visual>
                                                </toast>";


        private void PostToWns(string secret, string sid, string uri, string xml, string notificationType, string contentType)
        {
            

            try
            {
                // You should cache this access token.
                var accessToken = GetAccessToken(secret, sid);

                byte[] contentInBytes = Encoding.UTF8.GetBytes(xml);

                HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;
                request.Method = "POST";
                request.Headers.Add("X-WNS-Type", notificationType);
                request.ContentType = contentType;
                request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken.AccessToken));

                using (Stream requestStream = request.GetRequestStream())
                    requestStream.Write(contentInBytes, 0, contentInBytes.Length);

                using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                   TB_result.Text= webResponse.StatusCode.ToString();
            }

            catch (WebException webException)
            {
                HttpStatusCode status = ((HttpWebResponse)webException.Response).StatusCode;

                if (status == HttpStatusCode.Unauthorized)
                {
                    // The access token you presented has expired. Get a new one and then try sending
                    // your notification again.

                    // Because your cached access token expires after 24 hours, you can expect to get 
                    // this response from WNS at least once a day.

                    GetAccessToken(secret, sid);

                    // We recommend that you implement a maximum retry policy.
                    PostToWns(uri, xml, secret, sid, notificationType, contentType);
                }
                else if (status == HttpStatusCode.Gone || status == HttpStatusCode.NotFound)
                {
                    // The channel URI is no longer valid.

                    // Remove this channel from your database to prevent further attempts
                    // to send notifications to it.

                    // The next time that this user launches your app, request a new WNS channel.
                    // Your app should detect that its channel has changed, which should trigger
                    // the app to send the new channel URI to your app server.

                    TB_result.Text = "";
                }
                else if (status == HttpStatusCode.NotAcceptable)
                {
                    // This channel is being throttled by WNS.

                    // Implement a retry strategy that exponentially reduces the amount of
                    // notifications being sent in order to prevent being throttled again.

                    // Also, consider the scenarios that are causing your notifications to be throttled. 
                    // You will provide a richer user experience by limiting the notifications you send 
                    // to those that add true value.

                    TB_result.Text = "";
                }
                else
                {
                    // WNS responded with a less common error. Log this error to assist in debugging.

                    // You can see a full list of WNS response codes here:
                    // http://msdn.microsoft.com/en-us/library/windows/apps/hh868245.aspx#wnsresponsecodes

                    string[] debugOutput = {
                                       status.ToString(),
                                       webException.Response.Headers["X-WNS-Debug-Trace"],
                                       webException.Response.Headers["X-WNS-Error-Description"],
                                       webException.Response.Headers["X-WNS-Msg-ID"],
                                       webException.Response.Headers["X-WNS-Status"]
                                   };
                    TB_result.Text = string.Join(" | ", debugOutput);
                }
            }

            catch (Exception ex)
            {
               TB_result.Text = "EXCEPTION: " + ex.Message;
            }
        }


        // Authorization
        [DataContract]
        public class OAuthToken
        {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
        }

        private OAuthToken GetOAuthTokenFromJson(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(OAuthToken));
                var oAuthToken = (OAuthToken)ser.ReadObject(ms);
                return oAuthToken;
            }
        }

        protected OAuthToken GetAccessToken(string secret, string sid)
        {
            var urlEncodedSecret = HttpUtility.UrlEncode(secret);
            var urlEncodedSid = HttpUtility.UrlEncode(sid);

            var body = String.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com",
                                     urlEncodedSid,
                                     urlEncodedSecret);

            string response;
            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                response = client.UploadString("https://login.live.com/accesstoken.srf", body);
            }
            return GetOAuthTokenFromJson(response);
        }



        private void button1_Click(object sender, EventArgs e)
        {
            TB_result.Text = "";

            string temp = string.Format("\"id\":\"{0}\",\"type\":\"{1}\"", TB_ID.Text, TYPE);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes("{"+temp+"}");
           string data = System.Convert.ToBase64String(plainTextBytes);


            PostToWns("Apv1Opi59wuAfj14oi6/N9J9MKPFuwrZ", "ms-app://s-1-15-2-3312989200-2431252905-3663799122-3080579439-3200542185-2360468662-3777342509", TB_key.Text,
              string.Format(XmlToastTemplate, data, TYPE_SHOW, TB_img.Text, TB_title.Text, TB_content.Text), "wns/toast", "text/xml");
     
        }


        string TYPE_SHOW = "ToastImageAndText02";
        string TYPE = "1";
 

        private void TB_result_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            TYPE_SHOW = "ToastText01";
     
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            TYPE_SHOW = "ToastText02";

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            TYPE_SHOW = "ToastImageAndText02";
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            TYPE_SHOW = "ToastImageAndText01";
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            TYPE = "1";
            TB_ID.Text = "hjPjyrVGfaGm";
            TB_title.Text = "Cuối Cùng";
            TB_content.Text = "Khắc Việt";
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            TYPE = "2";
            TB_ID.Text = "CJx00Q66TsAt";
            TB_img.Text = "http://avatar.nct.nixcdn.com/playlist/2015/05/13/3/4/4/d/1431488620202.jpg";
            TB_title.Text = "Hot Now No.14";
            TB_content.Text = "Album mới";
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            TYPE = "3";
            TB_ID.Text = "jsGlVtZCTXD0M";
            TB_img.Text = "http://avatar.nct.nixcdn.com/mv/2015/05/20/e/d/b/2/1432115357443.jpg";
            TB_title.Text = "Yêu Em Vất Vả Kết Quả Chia Tay";
            TB_content.Text = "Triều Hải, Yuki Huy Nam";
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            TYPE = "4";
            TB_ID.Text = "182443";
            TB_img.Text = "http://avatar.nct.nixcdn.com/singer/avatar/2015/04/01/2/3/a/e/1427864987367.jpg";
            TB_title.Text = "Tiên Tiên";
            TB_content.Text = "ca sĩ";
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            TYPE = "5";
            TB_ID.Text = "http://news.zing.vn/";
            TB_img.Text = "http://stc.live.zdn.vn/zme-sdk-sso3/images/logo_zing.png";
            TB_title.Text = "Zing.vn";
            TB_content.Text = "web";
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            TYPE = "6";
            TB_ID.Text = "588f5a00-48cd-4fc5-b133-a2f4128753f6";
            TB_img.Text = "http://taiphanmem.mobi/wp-content/uploads/2013/10/icon-nhaccuatui.jpg";
            TB_title.Text = "NCT HD";
            TB_content.Text = "ứng dụng";
        }
    }
}

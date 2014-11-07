using DotNetOpenAuth.OAuth2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebApiTest.ApiModel;
using WebApiTest.Helper;

namespace WebApiTest
{
    public partial class FormTest : Form
    {
        private readonly string AuthorizationServerBaseAddress = ConfigurationManager.AppSettings["AuthorizationServerBaseAddress"];
        private readonly string AuthorizePath = ConfigurationManager.AppSettings["AuthorizePath"]; //"/OAuth/Authorize";
        private readonly string TokenPath = ConfigurationManager.AppSettings["TokenPath"]; //"/OAuth/Token";
        private readonly string ClientId = ConfigurationManager.AppSettings["ClientId"]; //"123456";
        private readonly string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"]; //"abcdef";

        private WebServerClient oAuthClient = null;

        private string accessToken = "mockstring";
        private string refreshToken = "mockstring";
        public FormTest()
        {
            InitializeComponent();
        }

        private void InitializeWebServerClient()
        {
            var authorizationServerUri = new Uri(AuthorizationServerBaseAddress);
            var authorizationServer = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(authorizationServerUri, AuthorizePath),
                TokenEndpoint = new Uri(authorizationServerUri, TokenPath)
            };
            oAuthClient = new WebServerClient(authorizationServer, ClientId, ClientSecret);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string userName = txtUsername.Text;
            string passWord = txtPassword.Text;
            IAuthorizationState state = null;
            try
            {
                state = oAuthClient.ExchangeUserCredentialForToken(userName, passWord, scopes: new string[] { "common" });
                if (state != null)
                {
                    accessToken = state.AccessToken;
                    refreshToken = state.RefreshToken;
                    MessageBox.Show(string.Format("登录成功。\r\naccessToken:{0}\r\nrefreshToken:{1}", accessToken, refreshToken));
                }
                else
                {
                    MessageBox.Show("登录失败");
                }
            }
            catch
            {
                MessageBox.Show("登录失败");
            }
        }

        private void FormTest_Load(object sender, EventArgs e)
        {
            try
            {
                var loadedApis = (List<Api>)XmlUtil.Deserialize(typeof(List<Api>), @"XML/ApiRoute.xml");
                var loadedhost = (List<ApiHost>)XmlUtil.Deserialize(typeof(List<ApiHost>), @"XML/ApiHost.xml");
                dgvApi.DataSource = loadedApis;
                cmbHost.DataSource = loadedhost;
                cmbHost.DisplayMember = "Host";
                cmbHost.ValueMember = "Host";

                InitializeWebServerClient();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgvApi_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int index = e.RowIndex;
            try
            {
                txtUrl.Text = dgvApi.Rows[index].Cells["Url"].Value.ToString();
                cmbMethod.Text = dgvApi.Rows[index].Cells["Method"].Value.ToString();
                txtParameter.Text = dgvApi.Rows[index].Cells["DataParameters"].Value.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text;
            string parameter = txtParameter.Text;
            string method = cmbMethod.Text;
            MethodType type;
            switch (method)
            {
                case "POST":
                    type = MethodType.POST; break;
                case "PUT":
                    type = MethodType.PUT; break;
                case "DELETE":
                    type = MethodType.DELETE; break;
                default:
                    type = MethodType.GET; break;
            }
            CallApi(type, url, parameter);
        }



        private void CallApi(MethodType method, string url, string parameter)
        {
            try
            {
                string host = AuthorizationServerBaseAddress;
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("api地址不能为空");
                    return;
                }
                if (!string.IsNullOrEmpty(cmbHost.Text))
                {
                    host = cmbHost.Text;
                }
                if (!url.Contains("http://"))
                {
                    url = host + url;
                }

                HttpContent content = new StringContent(parameter);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpClient client = new HttpClient(this.oAuthClient.CreateAuthorizingHandler(accessToken));
                // 为JSON格式添加一个Accept报头
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = null;
                switch (method)
                {
                    case MethodType.POST:
                        {
                            response = client.PostAsync(url, content).Result;
                            break;
                        }
                    case MethodType.PUT:
                        {
                            response = client.PutAsync(url, content).Result;
                            break;
                        }
                    case MethodType.DELETE:
                        {
                            response = client.DeleteAsync(url).Result;
                            break;
                        }
                    default:
                        {
                            response = client.GetAsync(url).Result;
                            break;
                        }
                }
                if (response != null)
                {
                    txtStatusCode.Text = response.StatusCode.ToString();
                    var resultValue = response.Content.ReadAsStringAsync().Result;
                    txtResult.Text = resultValue;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
    public enum MethodType
    {
        GET = 1,
        POST = 2,
        PUT = 3,
        DELETE = 4,
    }
}



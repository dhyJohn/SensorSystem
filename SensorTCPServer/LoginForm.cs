using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace SensorTCPServer
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            this.Text = "로그인 화면"; // ✅ 타이틀바 텍스트 한글로 변경
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            var client = new HttpClient();
            var loginData = new { username, password };

            var content = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://127.0.0.1:8000/api/token/", content);

            if (response.IsSuccessStatusCode)
            {
                
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync());
                string accessToken = result["access"];
                string refreshToken = result["refresh"];

                Debug.WriteLine($"✅ Access Token: {accessToken}");
                Debug.WriteLine($"✅ Refresh Token: {refreshToken}");


                lblStatus.Text = "로그인 성공";
                await Task.Delay(1000); // 1초간 메시지 보여주기
                var dashboard = new DashboardForm(accessToken, refreshToken);
                
                dashboard.Show();
                this.Hide();
            }
            else
            {
                lblStatus.Text = "로그인 실패. 다시 확인해주세요.";
            }
        }


     
    }
}

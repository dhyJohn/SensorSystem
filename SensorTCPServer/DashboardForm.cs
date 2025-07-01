// ✅ C# 실시간 대시보드 + 텔레그램 경고 기능 추가 (중복 방지 포함, C# 7.3 호환)
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveCharts.WinForms;
using LiveCharts.Wpf;
using System.Drawing;

namespace SensorTCPServer
{
    public partial class DashboardForm : Form
    {
        private string accessToken;
        private string refreshToken;
        private SolidGauge[] gauges = new SolidGauge[6];
        private Label[] labelsFuel = new Label[6];
        private Label[] labelsTemp = new Label[6];
        private Label[] labelsWater = new Label[6];

        private Dictionary<int, bool> volumeAlertSent = new Dictionary<int, bool>();
        private Dictionary<int, bool> tempAlertSent = new Dictionary<int, bool>();
        private Dictionary<int, bool> waterAlertSent = new Dictionary<int, bool>();

        public DashboardForm(string accessToken, string refreshToken)
        {
            InitializeComponent();
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;

            for (int i = 1; i <= 6; i++)
            {
                volumeAlertSent.Add(i, false);
                tempAlertSent.Add(i, false);
                waterAlertSent.Add(i, false);
            }

            InitLayout();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            StartTCPServer();
        }

        private void InitLayout()
        {
            this.Text = "Tank Monitor";
            this.Width = 1100;
            this.Height = 750;

            var table = new TableLayoutPanel
            {
                RowCount = 2,
                ColumnCount = 3,
                Dock = DockStyle.Top,
                Height = 700,
                Padding = new Padding(10)
            };

            for (int i = 0; i < 3; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            for (int i = 0; i < 2; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            for (int i = 0; i < 6; i++)
            {
                var gauge = new SolidGauge
                {
                    Dock = DockStyle.Fill,
                    From = 0,
                    To = 100,
                    Value = 0,
                    Uses360Mode = false,
                    LabelFormatter = val => string.Format("{0:0.0}%", val),
                    ForeColor = Color.Black
                };

                var labelFuel = CreateLabel();
                var labelTemp = CreateLabel();
                var labelWater = CreateLabel();

                var labelPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 1
                };
                labelPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                labelPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                labelPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                labelPanel.Controls.Add(labelFuel, 0, 0);
                labelPanel.Controls.Add(labelTemp, 0, 1);
                labelPanel.Controls.Add(labelWater, 0, 2);

                var innerLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1
                };
                innerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 80F));
                innerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
                innerLayout.Controls.Add(gauge, 0, 0);
                innerLayout.Controls.Add(labelPanel, 0, 1);

                var outerPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10)
                };
                outerPanel.Controls.Add(innerLayout);
                table.Controls.Add(outerPanel, i % 3, i / 3);

                gauges[i] = gauge;
                labelsFuel[i] = labelFuel;
                labelsTemp[i] = labelTemp;
                labelsWater[i] = labelWater;
            }

            this.Controls.Add(table);
        }

        private Label CreateLabel()
        {
            return new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                MaximumSize = new Size(320, 0),
                Dock = DockStyle.Top
            };
        }

        private void StartTCPServer()
        {
            Thread serverThread = new Thread(() =>
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 9000);
                listener.Start();

                while (true)
                {
                    try
                    {
                        using (TcpClient client = listener.AcceptTcpClient())
                        using (NetworkStream stream = client.GetStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string json;
                            while ((json = reader.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(json))
                                {
                                    PostToDjango(json);
                                    var data = JsonConvert.DeserializeObject<List<TankData>>(json);
                                    this.Invoke(new MethodInvoker(async () =>
                                    {
                                        foreach (var tank in data)
                                        {
                                            int idx = tank.tank - 1;
                                            if (idx >= 0 && idx < 6 && gauges[idx] != null)
                                            {
                                                float percent = tank.capacity > 0 ? (tank.volume / tank.capacity) * 100f : 0;
                                                gauges[idx].Value = percent;

                                                labelsFuel[idx].Text = string.Format("{0}: {1:0.0}/{2} ({3:0.0}%)", tank.name, tank.volume, tank.capacity, percent);
                                                Color fuelColor;

                                                if (percent < 20)
                                                {
                                                    fuelColor = Color.Red;
                                                    if (!volumeAlertSent[tank.tank])
                                                    {
                                                        await SendTelegramAlertAsync($"🚨 탱크 {tank.tank} ({tank.name}) 연료 부족: {tank.volume:F1}L / {tank.capacity}L ({percent:F1}%)");
                                                        volumeAlertSent[tank.tank] = true;
                                                    }
                                                }
                                                else if (percent < 40)
                                                {
                                                    fuelColor = Color.Orange;
                                                }
                                                else
                                                {
                                                    fuelColor = Color.Black;
                                                    if (volumeAlertSent[tank.tank])
                                                    {
                                                        await SendTelegramAlertAsync($"✅ 탱크 {tank.tank} ({tank.name}) 연료 정상 복귀: {tank.volume:F1}L / {tank.capacity}L ({percent:F1}%)");
                                                        volumeAlertSent[tank.tank] = false;
                                                    }
                                                }

                                                labelsFuel[idx].ForeColor = fuelColor;

                                                labelsTemp[idx].Text = string.Format("온도: {0:0.0}℃", tank.temp);
                                                if (tank.temp >= 35)
                                                {
                                                    labelsTemp[idx].ForeColor = Color.Red;
                                                    if (!tempAlertSent[tank.tank])
                                                    {
                                                        await SendTelegramAlertAsync($"🔥 탱크 {tank.tank} ({tank.name}) 온도 이상: {tank.temp:F1}℃");
                                                        tempAlertSent[tank.tank] = true;
                                                    }
                                                }
                                                else
                                                {
                                                    labelsTemp[idx].ForeColor = tank.temp >= 30 ? Color.Orange : Color.Black;
                                                    if (tempAlertSent[tank.tank])
                                                    {
                                                        await SendTelegramAlertAsync($"✅ 탱크 {tank.tank} ({tank.name}) 온도 정상 복귀: {tank.temp:F1}℃");
                                                        tempAlertSent[tank.tank] = false;
                                                    }
                                                }

                                                labelsWater[idx].Text = string.Format("물수위: {0:0.0}cm", tank.water);
                                                if (tank.water < 1)
                                                {
                                                    labelsWater[idx].ForeColor = Color.Red;
                                                    if (!waterAlertSent[tank.tank])
                                                    {
                                                        await SendTelegramAlertAsync($"💧 탱크 {tank.tank} ({tank.name}) 물 수위 너무 낮음: {tank.water:F1}cm");
                                                        waterAlertSent[tank.tank] = true;
                                                    }
                                                }
                                                else
                                                {
                                                    labelsWater[idx].ForeColor = tank.water < 2 ? Color.Orange : Color.Black;
                                                    if (waterAlertSent[tank.tank])
                                                    {
                                                        await SendTelegramAlertAsync($"✅ 탱크 {tank.tank} ({tank.name}) 물 수위 정상 복귀: {tank.water:F1}cm");
                                                        waterAlertSent[tank.tank] = false;
                                                    }
                                                }
                                            }
                                        }
                                    }));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("서버 오류: " + ex.Message);
                    }
                }
            });

            serverThread.IsBackground = true;
            serverThread.Start();
        }

        private async Task SendTelegramAlertAsync(string message)
        {
            string botToken = "7937797671:AAFDlGzUOQEyaR-Bif6Ax0U2VYdjsVYkCiE";
            string chatId = "6318453385";
            string url = $"https://api.telegram.org/bot{botToken}/sendMessage";

            using (HttpClient client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "chat_id", chatId },
                    { "text", message }
                };
                var content = new FormUrlEncodedContent(values);
                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("🔄 텔레그램 응답: " + result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("❌ 텔레그램 전송 실패: " + ex.Message);
                }
            }
        }

        private async void PostToDjango(string json)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    var response = await client.PostAsync("http://127.0.0.1:8000/tanks/", content);
                    string result = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("📡 Django 응답: " + result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("❌ Django 전송 실패: " + ex.Message);
                }
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static site_backend_admin.Form1;

namespace site_backend_admin
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        private static readonly HttpClient client = new HttpClient();

        private List<Question> allQuestions = new List<Question>();

        private Question selectedQuestion;
        private string adminEmail;
        private string authCode;
        public class Question
        {
            public string date { get; set; }
            public string time { get; set; }
            public string surname { get; set; }
            public string address { get; set; }
            public string status { get; set; }
            public List<string> files { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public string message { get; set; }
        }

        public static class ApiClient
        {
            private static readonly CookieContainer _cookieContainer = new CookieContainer();

            public static readonly HttpClient Client = new HttpClient(
                new HttpClientHandler
                {
                    CookieContainer = _cookieContainer,
                    UseCookies = true,  // автоматически сохраняет и отправляет cookies
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                }
            )
            {
                BaseAddress = new Uri("http://localhost:8000/")
            };
            public static async Task<HttpResponseMessage> SendWithRefreshAsync(Func<Task<HttpResponseMessage>> request)
            {
                var response = await request();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // пробуем рефрешнуть
                    bool refreshed = await AuthService.RefreshAsync();

                    if (refreshed)
                    {
                        // повторяем запрос с новыми cookies
                        response = await request();
                    }
                    else
                    {
                        // рефреш тоже не помог — разлогиниваем
                        MessageBox.Show("Ошшибка с авторизацией - перезапустите приложение");
                    }
                }

                return response;
            }
        }

        public static class AuthService
        {
            public static async Task<bool> LoginAsync(string username, string code)
            {
                var payload = new { email = username, auth_code = code };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await ApiClient.Client.PostAsync("/auth/login", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Status: {response.StatusCode}\nBody: {responseBody}");
                if (response.IsSuccessStatusCode)
                {
                    // cookies (access_token, refresh_token) сохранятся автоматически
                    // в CookieContainer и будут отправляться при следующих запросах
                    return true;
                }

                return false;
            }
            public static async Task<bool> RefreshAsync()
            {
                var response = await ApiClient.Client.PostAsync("auth/refresh", null);
                return response.IsSuccessStatusCode;
                // refresh_token cookie уйдёт автоматически, новые cookies сохранятся
            }
        }

        public async Task<List<Question>> GetQuestionsAsync()
        {
            try
            {
                string url = "http://localhost:8000/handle_questions/all_questions";
                HttpResponseMessage response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                allQuestions = JsonConvert.DeserializeObject<List<Question>>(jsonResponse);
               
                return allQuestions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
                throw;
            }
        }

        public async void SendAuthCode(string email)
        {
            try
            {
                string url = "http://localhost:8000/auth/get_auth_code";
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("email", email)
                };

                var content = new FormUrlEncodedContent(formData);

                HttpResponseMessage response = await client.PostAsync(url, content);

                response.EnsureSuccessStatusCode();

                string responseData = await response.Content.ReadAsStringAsync();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
                throw;
            }
        }

        public Form1()
        {
            InitializeComponent();
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.ItemHeight = 70;
            listBox1.DrawItem += ListBox1_DrawItem;
            groupBox1.Hide();
            groupBox2.Hide();
            label4.Hide();
            AllocConsole();
        }

        private void ListBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            string text = listBox1.Items[e.Index].ToString();

            using (var brush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
            }

            e.DrawFocusRectangle();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            richTextBox1.Clear();
            richTextBox1.Text = "Загрузка данных...";
            List<Question> questions = await GetQuestionsAsync();
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                listBox1.Items.Add($"Имя: {q.name}\nФамилия: {q.surname}\nАдрес: {q.address}\nEmail: {q.email}");
            }
            richTextBox1.Clear();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            Question selectedQuestion = allQuestions[listBox1.SelectedIndex];
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            groupBox1.Show();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            adminEmail = textBox1.Text;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (adminEmail != null)
            {
                SendAuthCode(adminEmail);
                groupBox2.Show();
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            bool success = await AuthService.LoginAsync(adminEmail, authCode);
            if (success)
            {
                MessageBox.Show("Успешная авторизация");
                groupBox1.Hide();
                groupBox2.Hide();
                button3.Hide();
            }
            else
            {
                MessageBox.Show("Ошибка авторизации");
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            authCode = textBox2.Text.ToString();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}

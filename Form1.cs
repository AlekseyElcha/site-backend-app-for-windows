using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace site_backend_admin
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        // Удаляем этот client, так как будем использовать ApiClient.Client
        // private static readonly HttpClient client = new HttpClient();

        private List<Question> allStatusQuestions = new List<Question>();
        private List<Question> allActiveQuestions = new List<Question>();

        private Question selectedQuestion;
        private string adminEmail;
        private string authCode;
        private string newStatus;
        private string newAnswerMessage;

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
                    UseCookies = true,
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
                    bool refreshed = await AuthService.RefreshAsync();

                    if (refreshed)
                    {
                        response = await request();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка с авторизацией - перезапустите приложение");
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

                var response = await ApiClient.Client.PostAsync("auth/login", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Status: {response.StatusCode}\nBody: {responseBody}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }

            public static async Task<bool> RefreshAsync()
            {
                var response = await ApiClient.Client.PostAsync("auth/refresh", null);
                return response.IsSuccessStatusCode;
            }
        }

        // ✅ Исправлено: используем ApiClient.Client вместо client
        public async Task<List<Question>> GetQuestionsAsync()
        {
            try
            {
                string url = "handle_questions/all_questions";
                HttpResponseMessage response = await ApiClient.Client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                allStatusQuestions = JsonConvert.DeserializeObject<List<Question>>(jsonResponse);

                return allStatusQuestions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
                throw;
            }
        }

        // ✅ Исправлено: используем ApiClient.Client вместо client
        public async void SendAuthCode(string email)
        {
            try
            {
                string url = "auth/get_auth_code";
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("email", email)
                };

                var content = new FormUrlEncodedContent(formData);

                HttpResponseMessage response = await ApiClient.Client.PostAsync(url, content);

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

        // ✅ Исправлено: используем ApiClient.Client вместо client
        public async Task<string> GetFileLinksForQuestion(string questionId)
        {
            try
            {
                string url = $"files/download_all_files/{questionId}";
                HttpResponseMessage response = await ApiClient.Client.PutAsync(url, null);
                response.EnsureSuccessStatusCode();
                string responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
                throw new Exception("Ошибка при получении ссылок.", ex);
            }
        }

        // ✅ Исправлено: используем ApiClient.Client вместо client
        public async Task<string> ChangeQuestionsStatus(string questionId, string newStatus)
        {
            try
            {
                string url = "handle_questions/change_question_status";

                var data = new
                {
                    question_id = questionId,
                    new_status = newStatus
                };

                string jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                Console.WriteLine($"Отправляем JSON: {jsonData}");
                Console.WriteLine($"question_id: {questionId}");
                Console.WriteLine($"new_status: {newStatus}");
                HttpResponseMessage response = await ApiClient.Client.PutAsync(url, content);

                response.EnsureSuccessStatusCode();

                string responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                throw new Exception(ex.Message, ex);
            }
        }
        public async Task<bool> AnswerQuestionAsync(string questionId, string answer)
        {
            try
            {
                string url = "handle_questions/answer_question";

                // ✅ Отправляем правильные поля: message и question_id
                var answerObject = new
                {
                    message = answer,           // поле должно называться message
                    question_id = questionId    // и question_id
                };

                string jsonAnswer = JsonConvert.SerializeObject(answerObject);

                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(new StringContent(jsonAnswer, Encoding.UTF8), "answer");

                    Console.WriteLine($"Отправляем JSON: {jsonAnswer}");

                    HttpResponseMessage response = await ApiClient.Client.PostAsync(url, formData);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Статус: {response.StatusCode}");
                    Console.WriteLine($"Ответ: {responseBody}");

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение: {ex.Message}");
                return false;
            }
        }

        private async Task<List<Question>> UpdateActiveQuestionsData()
        {
            allActiveQuestions.Clear();
            List<Question> data = await GetQuestionsAsync();
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].status == "active")
                {
                    var q = data[i];
                    string files;
                    if (q.files != null)
                    {
                        files = q.files.Count.ToString();
                    }
                    else
                    {
                        files = "нет";
                    }
                    allActiveQuestions.Add(new Question
                    {
                        date = q.date,
                        time = q.time,
                        id = q.id,
                        status = q.status,
                        name = q.name,
                        surname = q.surname,
                        address = q.address,
                        email = q.email,
                        message = q.message,
                        files = q.files
                    });
                }
            }
            return allActiveQuestions;
        }

        public Form1()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.ItemHeight = 150;
            listBox1.DrawItem += ListBox1_DrawItem;
            richTextBox2.Hide();
            groupBox1.Hide();
            groupBox2.Hide();
            label4.Hide();
            label6.Hide();
            groupBox3.Hide();
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

        private async void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            button1.Enabled = false;
            allActiveQuestions.Clear();
            allActiveQuestions = await UpdateActiveQuestionsData();
            Console.WriteLine($"Загружено вопросов: {allActiveQuestions.Count}");
            foreach (var q in allActiveQuestions)
            {
                string files;
                if (q.files != null && q.files.Count > 0)
                {
                    files = q.files.Count.ToString();
                }
                else
                {
                    files = "нет";
                }
                listBox1.Items.Add(
                    $"Дата: {q.date}\n" +
                    $"Время: {q.time}\n" +
                    $"ID Обращения: {q.id}\n" +
                    $"Статус: {q.status}\n" +
                    $"Email: {q.email}\n" +
                    $"Файлы: {files}");
            }
            button1.Enabled = true;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            listBox1.Items.Clear();
            button1.Text = "Получение данных с сервера...";
            allActiveQuestions.Clear();
            allActiveQuestions = await UpdateActiveQuestionsData();
            foreach (var q in allActiveQuestions)
            {
                string files;
                if (q.files != null)
                {
                    files = q.files.Count.ToString();
                }
                else
                {
                    files = "нет";
                }
                listBox1.Items.Add(
                $"Дата: {q.date}\n" +
                $"Время: {q.time}\n" +
                $"ID Обращения: {q.id}\n" +
                $"Статус: {q.status}\n" +
                $"Email: {q.email}\n" +
                $"Файлы: {files}");
            }
            richTextBox2.Clear();
            richTextBox2.Hide();
            richTextBox2.Text = "Загрузка данных...";

            button1.Text = "Обновить данные с сервера";
            button1.Enabled = true;
            richTextBox1.Clear();
        }

        private async void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1) return;
            selectedQuestion = allActiveQuestions[listBox1.SelectedIndex];
            richTextBox1.Text = (
                $"Дата: {selectedQuestion.date}\n" +
                $"Время: {selectedQuestion.time}\n" +
                $"ID Обращения: {selectedQuestion.id}\n" +
                $"Статус: {selectedQuestion.status}\n" +
                $"Имя: {selectedQuestion.name}\n" +
                $"Фамилия: {selectedQuestion.surname}\n" +
                $"Адрес: {selectedQuestion.address}\n" +
                $"Email: {selectedQuestion.email}\n" +
                $"Текст обращения:\n {selectedQuestion.message}");
            if (selectedQuestion.files != null)
            {
                richTextBox2.Text = "Загрузка ссылок на файлы...";
                richTextBox2.Show();
                string links = await GetFileLinksForQuestion(selectedQuestion.id);
                links = links.Substring(1, links.Length - 2);
                richTextBox2.Clear();
                foreach (var link in links.Split(','))
                {
                    richTextBox2.AppendText($"{link.Substring(1, link.Length - 2)}\n\n");
                }
            }
            else
            {
                richTextBox2.Hide();
            }
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

        private async void button4_Click(object sender, EventArgs e)
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
                groupBox3.Show();
                button3.Hide();
                label4.Show();
                label6.Text = adminEmail;
                label6.Show();
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

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                newStatus = comboBox1.SelectedItem.ToString();
            }
            else
            {
                newStatus = null;
            }
        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {
            newAnswerMessage = richTextBox3.Text;
        }

        private async void button5_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (selectedQuestion == null)
                {
                    MessageBox.Show("Сначала выберите вопрос из списка", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                button5.Enabled = false;
                button5.Text = "Отправка данных на сервер...";

                DialogResult result = MessageBox.Show(
                    "Подтверждаете действие по изменению данных обращения?",
                    "Подтверждение действий",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    string question_id = selectedQuestion.id;
                    Console.WriteLine($"question_id: {question_id}");

                    if (!string.IsNullOrEmpty(newStatus))
                    {
                        var response = await ChangeQuestionsStatus(question_id, newStatus);
                        MessageBox.Show($"Ответ сервера: {response}");
                    }

                    if (!string.IsNullOrEmpty(newAnswerMessage))
                    {
                        bool response = await AnswerQuestionAsync(question_id, newAnswerMessage);
                        MessageBox.Show($"Ответ сервера: {response}");
                    }
                }
                else
                {
                    MessageBox.Show("Действия отменены");
                }

                newStatus = "";
                newAnswerMessage = "";
                comboBox1.SelectedIndex = -1;
                richTextBox3.Clear();
                button5.Text = "Сохранить изменения";
                button5.Enabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в button5_Click_1: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                button5.Enabled = true;
                button5.Text = "Сохранить изменения";
            }
        }
    }
}
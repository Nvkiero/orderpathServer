using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace orderpath_server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }

        // ==================== MODEL USER ====================
        public class User
        {
            public string fullName { get; set; }
            public DateTime? dob { get; set; }
            public string gender { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
            public string username { get; set; }
            public string pass { get; set; }
            public bool isSignUp { get; set; }
        }

        // ==================== FIELDS ====================
        List<User> users = new List<User>();
        bool isListening = false;
        Thread serverThread;
        Socket serverSocket;

        class SessionToken
        {
            public string Token { get; set; }
            public DateTime Expiry { get; set; }
            public string Username { get; set; }
        }

        private static Dictionary<string, SessionToken> activeTokens = new Dictionary<string, SessionToken>();

        // ==================== TOKEN FUNCTIONS ====================
        private string GenerateSecureToken(string username)
        {
            string payload = $"{username}:{DateTime.Now.Ticks}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hash);
            }
        }

        private bool IsTokenValid(string token)
        {
            lock (activeTokens)
            {
                if (!activeTokens.ContainsKey(token))
                    return false;

                var session = activeTokens[token];
                if (session.Expiry < DateTime.Now)
                {
                    activeTokens.Remove(token);
                    return false;
                }
                return true;
            }
        }

        // ==================== SERVER THREAD ====================
        private void StartThread()
        {
            try
            {
                int port = 8081;
                IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(ipendpoint);
                serverSocket.Listen(10);

                Invoke(new Action(() => MessageBox.Show($"Server started on port {port}")));

                while (isListening)
                {
                    Socket client = serverSocket.Accept();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => MessageBox.Show($"Lỗi server: {ex.Message}")));
            }
        }

        // ==================== HANDLE CLIENT ====================
        private void HandleClient(Socket client)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = client.Receive(buffer)) > 0)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // 1) Xử lý GET_USERINFO|token
                    if (json.StartsWith("GET_USERINFO|"))
                    {
                        string token = json.Split('|')[1].Trim();
                        if (IsTokenValid(token))
                        {
                            var session = activeTokens[token];
                            string username = session.Username;
                            string info = LayThongTinNguoiDung(username);
                            SendToClient(client, info);
                        }
                        else
                        {
                            SendToClient(client, "TOKEN_INVALID");
                        }
                        continue;
                    }

                    // 2) Xử lý JSON user (signup/login)
                    User user = null;
                    try
                    {
                        user = JsonSerializer.Deserialize<User>(json);
                    }
                    catch
                    {
                        // nếu không parse được json, bỏ qua hoặc trả lỗi
                        SendToClient(client, "SERVER_ERROR|INVALID_JSON");
                        continue;
                    }

                    if (user == null) continue;

                    if (user.isSignUp)
                        ConnectionDatabase(user, client);
                    else
                        ConnectionSQLlogin(user, client);

                    lock (users)
                        users.Add(user);

                    // Cập nhật giao diện
                    Invoke(new Action(() =>
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var u in users)
                            sb.AppendLine(u.username);
                        tb_data.Text = sb.ToString();
                    }));
                }

                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi client: {ex.Message}");
            }
        }

        // ==================== BUTTONS ====================
        private void bt_chay_Click(object sender, EventArgs e)
        {
            if (isListening)
            {
                MessageBox.Show("Server đã chạy rồi!");
                return;
            }

            isListening = true;
            serverThread = new Thread(StartThread) { IsBackground = true };
            serverThread.Start();
            tb_status.Text = "Server đang mở";
        }

        private void bt_KetThuc_Click(object sender, EventArgs e)
        {
            if (!isListening)
            {
                MessageBox.Show("Server chưa chạy");
                return;
            }

            isListening = false;
            serverSocket?.Close();
            serverSocket = null;

            if (serverThread != null && serverThread.IsAlive)
                serverThread.Interrupt();

            MessageBox.Show("Server đã dừng!");
            tb_status.Text = "Server đang đóng";
        }

        private void Server_Load(object sender, EventArgs e)
        {
            tb_status.Text = "Server đang đóng";
        }

        // ==================== SQL UTILITIES ====================
        private string HashSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private void ConnectionDatabase(User user, Socket client)
        {
            string connectionString = "Server=localhost;Database=QLNguoiDung;Integrated Security=True;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string check = "SELECT COUNT(*) FROM Users WHERE username = @username";
                    using (var cmdCheck = new SqlCommand(check, connection))
                    {
                        cmdCheck.Parameters.AddWithValue("@username", user.username);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            SendToClient(client, "SIGNUP_FAIL|USERNAME_EXISTS");
                            return;
                        }
                    }

                    string sql = @"INSERT INTO Users(username, matKhau, hoTen, email, soDienThoai, ngaySinh, gioiTinh, diaChi)
                                   VALUES(@username, @matkhau, @hoTen, @email, @soDienThoai, @ngaySinh, @gioiTinh, @diaChi)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", user.username);
                        cmd.Parameters.AddWithValue("@matkhau", HashSHA256(user.pass));
                        cmd.Parameters.AddWithValue("@hoTen", user.fullName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", user.email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@soDienThoai", user.phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ngaySinh", user.dob ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@gioiTinh", user.gender ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@diaChi", user.address ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    SendToClient(client, "SIGNUP_SUCCESS");
                }
                catch (Exception ex)
                {
                    SendToClient(client, "SERVER_ERROR|" + ex.Message);
                }
            }
        }

        private bool KiemTraDangNhap(string username, string pass)
        {
            string connectionString = "Server=localhost;Database=QLNguoiDung;Integrated Security=True;";
            string query = "SELECT COUNT(*) FROM Users WHERE username = @username AND matKhau = @matKhau";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@matKhau", HashSHA256(pass));
                try
                {
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private void ConnectionSQLlogin(User user, Socket socket)
        {
            try
            {
                if (KiemTraDangNhap(user.username, user.pass))
                {
                    string token = GenerateSecureToken(user.username);
                    DateTime expiry = DateTime.Now.AddMinutes(30);

                    lock (activeTokens)
                    {
                        activeTokens[token] = new SessionToken
                        {
                            Token = token,
                            Expiry = expiry,
                            Username = user.username
                        };
                    }

                    // 1) Gửi thông báo đăng nhập thành công trước
                    SendToClient(socket, "LOGIN_SUCCESS");

                    // Tùy chọn: chờ 1 khoảng nhỏ để giảm khả năng Nagle/OS gộp 2 send thành 1 packet
                    // (không bắt buộc nhưng giúp client dễ tách message hơn)
                    Thread.Sleep(50);

                    // 2) Gửi token dưới dạng riêng biệt
                    string tokenMsg = $"TOKEN|{token}|{expiry:o}";
                    SendToClient(socket, tokenMsg);
                }
                else
                {
                    SendToClient(socket, "LOGIN_FAIL");
                }
            }
            catch (Exception ex)
            {
                SendToClient(socket, "SERVER_ERROR|" + ex.Message);
            }
        }


        // ==================== LẤY THÔNG TIN NGƯỜI DÙNG ====================
        private string LayThongTinNguoiDung(string username)
        {
            string connectionString = "Server=localhost;Database=QLNguoiDung;Integrated Security=True;";
            string query = "SELECT hoTen, email, soDienThoai, ngaySinh, gioiTinh, diaChi FROM Users WHERE username = @username";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var info = new
                            {
                                fullName = reader["hoTen"]?.ToString(),
                                email = reader["email"]?.ToString(),
                                phone = reader["soDienThoai"]?.ToString(),
                                dob = reader["ngaySinh"]?.ToString(),
                                gender = reader["gioiTinh"]?.ToString(),
                                address = reader["diaChi"]?.ToString()
                            };
                            return JsonSerializer.Serialize(info);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return "SERVER_ERROR|" + ex.Message;
                }
            }
            return "USER_NOT_FOUND";
        }

        // ==================== GỬI DỮ LIỆU VỀ CLIENT ====================
        private void SendToClient(Socket socket, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                socket.Send(data);
            }
            catch
            {
                // ignore lỗi khi client ngắt kết nối
            }
        }
    }
}

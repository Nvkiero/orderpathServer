using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace orderpath_server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }

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

        List<User> users = new List<User>();
        bool isListening = false;
        Thread serverThread;
        Socket serverSocket;

        // Token lưu trên server
        class SessionToken
        {
            public string Token { get; set; }
            public DateTime Expiry { get; set; }
        }

        private static Dictionary<string, SessionToken> activeTokens = new Dictionary<string, SessionToken>();

        // ==================== SERVER ====================
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

        private void HandleClient(Socket client)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = client.Receive(buffer)) > 0)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    User user = JsonSerializer.Deserialize<User>(json);

                    if (user == null) continue;

                    if (user.isSignUp)
                        ConnectionDatabase(user, client);
                    else
                        ConnectionSQLlogin(user, client);

                    lock (users)
                        users.Add(user);

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
                // Không dùng MessageBox trong thread phụ
                Console.WriteLine($"Lỗi client: {ex.Message}");
            }
        }

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

        // ==================== XỬ LÝ SQL ====================
        private string HashSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
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

                    string sql = "INSERT INTO Users(username, matKhau, hoTen, email, soDienThoai, ngaySinh, gioitinh) " +
                                 "VALUES(@username, @matkhau, @hoTen, @email, @soDienThoai, @ngaySinh, @gioitinh)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", user.username);
                        cmd.Parameters.AddWithValue("@matkhau", HashSHA256(user.pass));
                        cmd.Parameters.AddWithValue("@hoTen", user.fullName);
                        cmd.Parameters.AddWithValue("@email", user.email);
                        cmd.Parameters.AddWithValue("@soDienThoai", user.phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ngaySinh", user.dob ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@gioitinh", user.gender ?? (object)DBNull.Value);
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
                    string token = Guid.NewGuid().ToString();
                    DateTime expiry = DateTime.Now.AddMinutes(30);
                    
                    lock (activeTokens)
                    {
                        activeTokens[user.username] = new SessionToken
                        {
                            Token = token,
                            Expiry = expiry
                        };
                    }

                    SendToClient(socket, $"LOGIN_SUCCESS");
                }
                else
                {
                    SendToClient(socket, "LOGIN_FAIL");
                }
            }
            catch (Exception ex)
            {
                SendToClient(socket, "SERVER_ERROR" + ex.Message);
            }
        }

        private void SendToClient(Socket socket, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                socket.Send(data);
            }
            catch {
                MessageBox.Show($"haha");
            }
        }
    }
    // qwe123
}

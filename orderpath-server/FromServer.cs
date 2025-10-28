﻿using System;
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
            public string fullName { get; set; }   // Họ tên
            public DateTime? dob { get; set; }     // Ngày sinh (có thể null)
            public string gender { get; set; }     // Giới tính
            public string email { get; set; }      // Email
            public string phone { get; set; }      // Số điện thoại
            public string address { get; set; }    // Địa chỉ
            public string username { get; set; }   // Tên đăng nhập
            public string pass { get; set; }       // Mật khẩu

            public bool isSignUp { get; set; }    // dấu hiệu đăng nhập hay đăng kí
        }

        //Danh sách chứa tất cả người dùng nhận được từ các client
        List<User> users = new List<User>();

        // Cờ kiểm tra server có đang lắng nghe không
        bool isListening = false;

        // Tạo luồng riêng để chạy server
        Thread serverThread;

        Socket serverSocket;

        //Hàm chạy trong luồng riêng để lắng nghe client
        private void StartThread()
        {
            try
            {
                //Tạo endpoint (địa chỉ IP và cổng server)
                int port = 8081;
                IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8081);

                //Tạo socket theo IPv4, kiểu stream (TCP), giao thức TCP
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //Gắn socket vào endpoint (địa chỉ và cổng)
                serverSocket.Bind(ipendpoint);

                //Bắt đầu lắng nghe, cho phép tối đa 10 kết nối chờ
                serverSocket.Listen(10);

                //Hiển thị thông báo server đã chạy 
                Invoke(new Action(() => MessageBox.Show($"Server started on port {port}")));

                //Vòng lặp luôn chờ client kết nối
                while (isListening)
                {
                    // dừng chương trình đến khi
                    // client kết nối, sau đó Accept() trả về socket riêng của client đó
                    Socket client = serverSocket.Accept();

                    // Tạo luồng riêng để xử lý client (để nhiều client chạy song song)
                    Thread clientThread = new Thread(() => HandleClient(client));
                    // dùng để đóng luồng phụ khi chương trình chính tắt
                    clientThread.IsBackground = true;
                    // bắt đầu chạy luồng
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => MessageBox.Show($"Lỗi server: {ex.Message}")));
            }
        }

        // Hàm xử lý từng client riêng biệt
        private void HandleClient(Socket client)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                // Lấy địa chỉ IP của client
                string clientKey = client.RemoteEndPoint.ToString();

                // Vòng lặp nhận dữ liệu từ client
                while ((bytesRead = client.Receive(buffer)) > 0)
                {
                    // Giải mã chuỗi nhận được từ mảng byte -> chuỗi JSON
                    string json = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Dùng JsonSerializer để chuyển JSON thành đối tượng User
                    User user = JsonSerializer.Deserialize<User>(json);

                    // Thử dữ liệu trong SQL nếu trùng gửi lại cho client... không thể tạo username.
                    // Code ở đây...
                    if (user.isSignUp == true)
                    {
                        ConnectionDatabase(user, client);
                    }

                    else { 
                        ConnectionSQLlogin(user, client);
                    }

                    // Dùng lock để tránh xung đột khi nhiều luồng cùng thêm user
                    // lock đảm bảo không cho nhiều luồng cùng truy cập cùng 1 thời điểm
                    lock (users)
                        users.Add(user);

                    // invoke dùng để cập nhật giao diện từ luồng phụ
                    Invoke(new Action(() =>
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var u in users)
                        {
                            sb.AppendLine(u.username);
                        }
                        tb_data.Text = sb.ToString();
                    }));

                }
             
                // ngắt kết nối 
                client.Close();
            }
            catch (Exception ex)
            {
                // Nếu client bị lỗi trong khi gửi dữ liệu
                Invoke(new Action(() => MessageBox.Show($"Lỗi client: {ex.Message}")));
            }
        }

        // Sự kiện khi bấm nút “Chạy Server”
        private void bt_chay_Click(object sender, EventArgs e)
        {
            try
            {
                // Nếu server đang chạy rồi thì không khởi động lại
                if (isListening)
                {
                    MessageBox.Show("Server đã chạy rồi!");
                    return;
                }

                // Đặt trạng thái server đang lắng nghe
                isListening = true;

                // Tạo và khởi động luồng server
                serverThread = new Thread(StartThread);
                serverThread.IsBackground = true; // Kết thúc khi form tắt
                serverThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
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

            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Interrupt();   // yêu cầu ngắt luồng
            }

            MessageBox.Show("Server đã dừng!");
        }

        private void Server_Load(object sender, EventArgs e)
        {
            tb_status.Text = "Server đang đóng";
        }
        private string HashSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        // kết nối Database đăng kí
        private void ConnectionDatabase(User user, Socket client)
        {
            string connectionString = "Server=localhost;Database=QLNguoiDung;Integrated Security=true;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "insert into Users(username, matKhau, hoTen, email, soDienThoai, ngaySinh, gioitinh) " +
                                  "values(@username, @matkhau, @hoTen, @email, @soDienThoai, @ngaySinh, @gioitinh);";

                    string check = "select count(*) from Users where username = @username;";
                    using (var cmdCheck = new SqlCommand(check, connection))
                    {
                        cmdCheck.Parameters.AddWithValue("@username", user.username);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("Username đã tồn tại, vui lòng chọn tên khác.");
                            return;
                        }
                    }
                    string hashedPass = HashSHA256(user.pass);
                    using (var cmd = new SqlCommand(sql, connection))
                    {

                        cmd.Parameters.AddWithValue("@username", user.username);
                        cmd.Parameters.AddWithValue("@matkhau", hashedPass);
                        cmd.Parameters.AddWithValue("@hoTen", user.fullName);
                        cmd.Parameters.AddWithValue("@email", user.email);
                        cmd.Parameters.AddWithValue("@soDienThoai", user.phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ngaySinh", user.dob ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@gioitinh", user.gender ?? (object)DBNull.Value);
                        int affected = cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message);
                    return;
                }
                string response = "SIGNUP_SUCCESS";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                client.Send(responseBytes);

            }
        }
        private bool KiemTraDangNhap(string TenDangNhap, string matKhau)
        {
            // tao chuoi la 1 ket noi
            string connectionString = "Server=localhost;Database=QUANLYKHACHHANG;Integrated Security=True;";
            // tao chuoi la 1 querry
            string query = "SELECT COUNT(*) FROM khachhang WHERE username = @TenDangNhap AND matKhau = @matKhau";
            // tao ket noi dong voi sql
            using (SqlConnection connection = new SqlConnection(connectionString))
            // goi ham de ket noi voi sql va thuc hien querry
            using (SqlCommand command = new SqlCommand(query, connection))
            {

                command.Parameters.AddWithValue("@TenDangNhap", TenDangNhap);
                command.Parameters.AddWithValue("@matKhau", HashSHA256(matKhau));

                try
                {
                    // kiem tra xem co tai khoan nao khop trong sql khong
                    connection.Open();
                    // thuc hien querry dem va gan vao bien count
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối cơ sở dữ liệu: " + ex.Message);
                    return false;
                }
            }
        }
        private void ConnectionSQLlogin(User user, Socket socket)
        {
            // neu co tai khoan khop o SQL thi tra ve
            if (KiemTraDangNhap(user.fullName, user.pass))
            {
                string response = "LOGIN_SUCCESS";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);
                return;
            }
            // khong thi xuat loi 
            else
            {
                string response = "LOGIN_FAIL";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);
                return;
            }
        }
    }
}

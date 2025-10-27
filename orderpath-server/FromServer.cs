using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Text.Json;


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
        }

        List<User> users = new List<User>();

        public int port = 8080;

        IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

        Thread serverthread;
        Socket serverlistener;
        private void StartThread()
        {
            try
            {
                int byteReceived = 0;
                byte[] recv = new byte[0x1000];
                

                serverlistener = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);

                serverlistener.Bind(ipendpoint);
                serverlistener.Listen(1);

                Console.WriteLine($"Server started on port: {port}");

                Socket ClientSocket = serverlistener.Accept();

                while (ClientSocket.Connected)
                {
                    string text = "";
                    do
                    {
                        byteReceived = ClientSocket.Receive(recv);
                        text += Encoding.UTF8.GetString(recv);

                    } while (text[text.Length - 1] == '\n');
                    var user = JsonSerializer.Deserialize<User>(text);
                    users.Add(user);
                }
                serverlistener.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex}"); }
        }
    }
}

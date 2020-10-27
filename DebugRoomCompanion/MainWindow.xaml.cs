using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
namespace DebugRoomCompanion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket transferSock; // 메신저봇과의 연결에 사용될 소켓입니다.
        StreamReader sr; // 메신저봇이 전송한 데이터를 읽는데 사용합니다.
        StreamWriter sw; // 메신저봇에 데이터를 전송하는데 사용합니다.
        bool isConnected = false;


        public MainWindow()
        {
            InitializeComponent();
        }


        private void Read() // 메신저봇에서 송신하는 데이터를 읽어들입니다.
        {
            while (true)
            {
                string line = sr.ReadLine();
                if (line != null) // ReadLine의 결과가 null일 경우, 연결이 끊겼음을 의미합니다.
                {
                    ReceiveMessage(line);
                    Console.WriteLine(line);
                }
                else
                {
                    OnDisconnect();
                    break;
                }
            }
        }

        private void OnDisconnect() // 연결 해제시 반드시 호출해야 합니다.
        {
            Dispatcher.Invoke(() =>
            {
                connectButton.IsEnabled = true;
                connectButton.Content = "Connect";
                sendButton.IsEnabled = false;
            });
            isConnected = false;
        }


        private void OnDisconnect(IAsyncResult ar) // transferSock.BeginDisconnect 완료 시 호출됩니다.
        {
            Socket s = (Socket)ar.AsyncState;
            s.EndDisconnect(ar);
            OnDisconnect();
        }


        private void OnConnect(IAsyncResult ar) // transferSock.BeginConnect 완료 시 호출됩니다.
        {
            Dispatcher.Invoke(() =>
            {
                connectButton.IsEnabled = true;
                connectButton.Content = "Disconnect";
                sendButton.IsEnabled = true;
            });
            Console.WriteLine("Connected");
            Socket s = (Socket)ar.AsyncState;
            try
            {
                s.EndConnect(ar);
                isConnected = true;
                NetworkStream ns = new NetworkStream(transferSock);
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                Thread readThread = new Thread(Read);
                readThread.IsBackground = true;
                readThread.Start();
            }
            catch (Exception e)
            {
                OnDisconnect();
                MessageBox.Show(e.ToString(), "연결 오류");
            }
        }


        private void OnClickConnectButton(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                // 연결된 상태에서 같은 버튼을 다시 클릭 시 연결해제 합니다.
                connectButton.Content = "Disconnecting...";
                connectButton.IsEnabled = false;
                transferSock.BeginDisconnect(true, new AsyncCallback(OnDisconnect), transferSock);
            }
            else
            {
                int portNum = 9500;
                bool result = Int32.TryParse(portNumberText.Text, out portNum);
                if (!result || portNum < 0 || portNum > 65535)
                {
                    portNum = 9500;
                    portNumberText.Text = "9500";
                }
                Console.WriteLine("Connecting");
                connectButton.Content = "Connecting";
                connectButton.IsEnabled = false;

                // 설정한 포트를 통해 연결을 시도합니다. (비동기)
                transferSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                transferSock.BeginConnect(new IPEndPoint(IPAddress.Loopback, portNum), new AsyncCallback(OnConnect), transferSock);
            }
        }


        private void AppendChat(string botName, string roomName, string authorName, string message, bool isBot)
        {
            SolidColorBrush color = Brushes.Black;
            if (isBot)
            {
                color = Brushes.Blue;
            }
            Dispatcher.Invoke(() =>
            {
                chatLogText.Inlines.Add(new Run($"[{botName}/{roomName}]{authorName}: {message}\n") { Foreground = color });
                chatLogScroll.ScrollToEnd();
            });

        }


        private void OnClickSendButton(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }


        private void SendMessage()
        {
            if (botNameText.Text.Replace(" ", "").Length == 0)
            {
                MessageBox.Show("전송 대상 봇 이름을 정하세요.", "알림");
                return;
            }
            //AppendChat(botNameText.Text, roomNameText.Text, authorNameText.Text, messageText.Text, false);
            JObject json = new JObject();
            json.Add("name", "debugRoom");
            JObject data = new JObject();
            data.Add("isGroupChat", isGroupChatCheck.IsChecked);
            data.Add("botName", botNameText.Text);
            data.Add("packageName", packageNameText.Text);
            data.Add("roomName", roomNameText.Text);
            data.Add("authorName", authorNameText.Text);
            data.Add("message", messageText.Text);
            json.Add("data", data);
            messageText.Text = "";

            // 메신저봇에 데이터를 보냅니다.
            sw.WriteLine(json.ToString(Newtonsoft.Json.Formatting.None)); // 메신저봇은 요청을 줄 단위롤 받아드리기 때문에 Formatting을 해서는 안됩니다.
            sw.Flush();
        }


        private void ReceiveMessage(string line) // 메신저봇으로부터 전송받은 데이터를 처리합니다.
        {
            JObject json = JObject.Parse(line);
            string name = json.Value<string>("name");
            if (name == "debugRoom")
            {
                JObject data = json.Value<JObject>("data");
                string botName = data.Value<string>("botName");
                string roomName = data.Value<string>("roomName");
                string authorName = data.Value<string>("authorName");
                string message = data.Value<string>("message");
                bool isBot = data.Value<bool>("isBot");
                AppendChat(botName, roomName, authorName, message, isBot);
            }
        }


        private void OnMessageTextKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift)) // Shift+Enter시 개행, Enter시 메시지 전송
                {
                    int caret = messageText.CaretIndex;
                    string before = messageText.Text.Substring(0, caret);
                    string after = messageText.Text.Substring(caret, messageText.Text.Length - caret);
                    messageText.Text = before + "\n" + after;
                    messageText.CaretIndex = caret + 1;
                }
                else
                {
                    if (sendButton.IsEnabled)
                        SendMessage();
                }
            }
        }


        private void OnClickClearChat(object sender, RoutedEventArgs e)
        {
            chatLogText.Text = "";
        }
    }
}

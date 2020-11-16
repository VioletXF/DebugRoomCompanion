using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Mdb;
namespace DebugRoomCompanion
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DebugRoom debugRoom;
        ADB adb;
        bool isConnected = false;
        

        public MainWindow()
        {
            InitializeComponent();
            adb = new ADB("adb.exe");
            debugRoom = new DebugRoom(adb);
            debugRoom.Message += DebugRoom_Message;
            debugRoom.Error += DebugRoom_Error;
        }

        private void DebugRoom_Error(object sender, EventArgs e)
        {
            
            DebugRoom.ErrorEventArgs args = e as DebugRoom.ErrorEventArgs;
            MessageBox.Show(args.error, "오류");
            Console.WriteLine(args.error);
        }

        private void DebugRoom_Message(object sender, EventArgs e)
        {
            DebugRoom.MessageEventArgs args = e as DebugRoom.MessageEventArgs;
            DebugRoom.MessageData msg = args.messageData;
            AppendChat(msg.GetBotName(), msg.GetRoomName(), msg.GetAuthorName(), msg.GetMessage(), msg.GetIsBot());
        }

        private void OnClickConnectButton(object sender, RoutedEventArgs e)
        {
            
            if (isConnected)
            {
                debugRoom.Disconnect();
                connectButton.Content = "Connect";
                sendButton.IsEnabled = false;
                isConnected = false;
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
                debugRoom.Connect(portNum, portNum);
                connectButton.Content = "Disconnect";
                sendButton.IsEnabled = true;
                isConnected = true;
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

            
            DebugRoom.MessageData msg = new DebugRoom.MessageData();
            msg.SetIsGroupChat((bool)isGroupChatCheck.IsChecked)
                .SetBotName(botNameText.Text)
                .SetPackageName(packageNameText.Text)
                .SetRoomName(roomNameText.Text)
                .SetAuthorName(authorNameText.Text)
                .SetMessage(messageText.Text);
            messageText.Text = "";
            // 메신저봇에 데이터를 보냅니다.
            debugRoom.Send(msg);
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

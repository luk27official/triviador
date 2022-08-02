using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected) return; //prevents from connecting multiple times
            try
            {
                string hostName = this.ipTextBox.Text;
                if (!Int32.TryParse(this.portTextBox.Text, out int port))
                {
                    this.informationTextBox.Text = "Invalid port entered!";
                    return;
                }
                //int port = 13000;
                //string hostName = "127.0.0.1";

                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.

                TcpClient client = new TcpClient(hostName, port);
                _isConnected = true;

                NetworkStream stream = client.GetStream();

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data;
                data = new Byte[256];
                String responseData = String.Empty;
                Int32 bytes;

                while (true)
                {
                    //wait for beginning of the game and keep the information updated
                    bytes = await stream.ReadAsync(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    this.informationTextBox.Text = responseData;
                    Console.WriteLine("Received: {0}", responseData);
                    if (responseData == Constants.P2CONNECTED)
                    {
                        //the game starts here
                        GameWindow gw = new GameWindow(stream);
                        gw.Show();
                        this.Close();
                        break;
                    }
                }

                /*
                // Close everything.
                stream.Close();
                client.Close();
                */
            }
            catch (Exception e2)
            {
                this.informationTextBox.Text = 
                    String.Format("An error occured. Try again later or check the connection information. Error message: {0}", e2.Message);
                //err
            }
        }
    }
}

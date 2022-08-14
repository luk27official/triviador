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
using Commons;
using Newtonsoft.Json;

namespace client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// This window is shown at the start of the game.
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isConnected = false;
        private NetworkStream _networkStream;

        /// <summary>
        /// Constructor for MainWindow.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Method which receives some message from the server calls a method to process it.
        /// </summary>
        private void ReceiveAndProcessMessage()
        {
            string message = MessageController.ReceiveMessage(_networkStream);
            ProcessMessage(message);
        }

        /// <summary>
        /// Method which processes the message based on the message type.
        /// </summary>
        /// <param name="message">Message from the server.</param>
        private void ProcessMessage(string message)
        {
            BasicMessage? msgFromJson = JsonConvert.DeserializeObject<BasicMessage>(message);
            if (msgFromJson == null) return;

            switch (msgFromJson.Type)
            {
                case Constants.MESSAGE_DISCONNECT:
                    ClientCommon.HandleEnemyDisconnect();
                    break;
                case Constants.MESSAGE_CONNECTED_FIRST_PLAYER:
                    this.informationTextBox.Text = Constants.P1CONNECTED;
                    Task.Run(() => ReceiveAndProcessMessage());
                    break;
                case Constants.MESSAGE_CONNECTED_SECOND_PLAYER:
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.informationTextBox.Text = Constants.P2CONNECTED;
                        GameWindow gw = new GameWindow(_networkStream);
                        gw.Show();
                        this.Close();
                    });
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Method called when the connect button is clicked.
        /// </summary>
        /// <param name="sender">Connect button.</param>
        /// <param name="e">Event arguments.</param>
        private void connectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_isConnected) return; //prevents from connecting multiple times
            try
            {
                string hostName = this.ipTextBox.Text;
                if (!Int32.TryParse(this.portTextBox.Text, out int port))
                {
                    this.informationTextBox.Text = Constants.INVALID_PORT;
                    return;
                }

                TcpClient client = new TcpClient(hostName, port);
                _isConnected = true;

                this._networkStream = client.GetStream();

                ReceiveAndProcessMessage();
            }
            catch (Exception e2)
            {
                this.informationTextBox.Text = String.Format(Constants.ERROR, e2.Message);
            }
        }
    }
}

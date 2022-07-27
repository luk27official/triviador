using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace client
{
    /// <summary>
    /// Interakční logika pro GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private NetworkStream stream; //contains stream connected to the server
        private int clientID;

        public GameWindow(NetworkStream stream)
        {
            InitializeComponent();
            this.stream = stream;
            WaitForGameStart();
        }


        //here the game starts - wait for an assignment of ID
        private async void WaitForGameStart()
        {
            Byte[] data;
            data = new Byte[256];
            String responseData = String.Empty;
            Int32 bytes;

            while(true) //wait for the ID assignment
            {
                bytes = await stream.ReadAsync(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                if(responseData.StartsWith(Constants.PREFIX_ASSIGN))
                {
                    char lastChar = responseData[responseData.Length - 1];
                    this.clientID = Int32.Parse(lastChar.ToString());
                    this.playerIDLabel.Content = String.Format("My player ID is {0}", lastChar);
                    break;
                }
            }
        }

        private void CZJC_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZJM_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZKA_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZKR_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZLI_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZMO_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZOL_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZPA_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZZL_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZPL_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZPR_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZST_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZUS_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void CZVY_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}

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
        private GameInformation gameInformation; //contains information about current game
        private Path[] paths;

        public GameWindow(NetworkStream stream)
        {
            InitializeComponent();
            this.stream = stream;
            this.gameInformation = new GameInformation();
            this.paths = new Path[] { 
                CZJC,
                CZJM,
                CZKA,
                CZKR,

                CZLI,
                CZMO,
                CZOL,
                CZPA,

                CZZL,
                CZPL,
                CZPR,
                CZST,

                CZUS,
                CZVY 
            };

            Play();
        }

        private async void Play()
        {
            WaitForGameStart();
            //after the game starts, we are waiting for next instructions
            FirstRound();
        }

        private async void FirstRound()
        {
            this.gameStatusTextBox.Text = "First round will start in 3 seconds. Get ready to answer!";

            Byte[] data;
            data = new Byte[1024];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //wait for the first question
            {
                bytes = await stream.ReadAsync(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                this.gameStatusTextBox.Text = responseData;
                if (responseData.StartsWith(Constants.PREFIX_QUESTIONABCD)) //handle question
                {
                    Window questionWindow = new QuestionABCDWindow(responseData);
                    questionWindow.Show();
                    break;
                }
            }
        }

        //here the game starts - wait for an assignment of ID
        private void WaitForGameStart()
        {
            Byte[] data;
            data = new Byte[256];
            String responseData = String.Empty;
            Int32 bytes;

            while(true) //wait for the ID assignment
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                /*here we check whether we receive the right message*/
                if(responseData.StartsWith(Constants.PREFIX_ASSIGN))
                {
                    char lastChar = responseData[responseData.Length - 1];
                    this.clientID = Int32.Parse(lastChar.ToString());
                    this.playerIDLabel.Content = String.Format("My player ID is {0}", lastChar);
                    break;
                }
            }

            //now we got the ids, so the server needs to set the right information
            UpdateGameInformation();
        }

        private async void UpdateGameInformation()
        {
            Byte[] data;
            data = new Byte[1024];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //wait for new information about the game
            {
                bytes = await stream.ReadAsync(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                /*here we check whether we receive the right message*/
                if (responseData.StartsWith(Constants.PREFIX_GAMEUPDATE))
                {
                    //update the game data
                    gameInformation.UpdateGameInformationFromMessage(responseData);
                    UpdateWindowFromGameInformation();
                    break;
                }
            }
        }

        //updates the game window based on the gameinformation property
        private void UpdateWindowFromGameInformation()
        {
            this.p1pointsTextBox.Text = this.gameInformation.P1Points.ToString();
            this.p2pointsTextBox.Text = this.gameInformation.P2Points.ToString();

            //color the bases
            Constants.Regions base1 = this.gameInformation.P1Base;
            Color p1BaseColor = Color.FromArgb(255, 255, 0, 0);
            this.paths[(int)base1].Fill = new SolidColorBrush(p1BaseColor);

            Constants.Regions base2 = this.gameInformation.P2Base;
            Color p2BaseColor = Color.FromArgb(255, 0, 0, 255);
            this.paths[(int)base2].Fill = new SolidColorBrush(p2BaseColor);
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

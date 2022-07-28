using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
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
        private bool picking; //are we picking the region right now?
        private Window questionWindow;

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

        private void Play()
        {
            WaitForGameStart();
            //after the game starts, we are waiting for next instructions
            FirstRound();

            //50s is the length of the first round
            System.Timers.Timer timer = new System.Timers.Timer(50000);

            timer.Enabled = true;
            timer.Elapsed += FirstRound;
            timer.AutoReset = false;

            //100s for the second one
            System.Timers.Timer timer2 = new System.Timers.Timer(100000);

            timer2.Enabled = true;
            timer2.Elapsed += FirstRound;
            timer2.AutoReset = false;

            //150s for the third one
            System.Timers.Timer timer3 = new System.Timers.Timer(150000);

            timer3.Enabled = true;
            timer3.Elapsed += FirstRound;
            timer3.AutoReset = false;
        }

        private void FirstRound(object? sender, System.Timers.ElapsedEventArgs e)
        {
            FirstRound();
        }

        private async void FirstRound()
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = "The round will start in 3 seconds. Get ready to answer!"; });

            Byte[] data;
            data = new Byte[1024];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //answer the first question
            {
                bytes = await stream.ReadAsync(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received: " + responseData);
                App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                if (responseData.StartsWith(Constants.PREFIX_QUESTIONNUMBER)) //handle question numeric
                {
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow = new QuestionNumericWindow(responseData, stream, clientID);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Closed += QuestionWindow_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });
                    break;
                }
            }
        }

        private void QuestionWindow_Closed(object? sender, EventArgs e)
        {
            //we return from the question -> picking regions three times!
            PickingFirstRound();

            System.Timers.Timer timer1 = new System.Timers.Timer(8000);
            timer1.Enabled = true;
            timer1.Elapsed += PickingFirstRound;
            timer1.AutoReset = false;

            System.Timers.Timer timer2 = new System.Timers.Timer(16000);
            timer2.Enabled = true;
            timer2.Elapsed += PickingFirstRound;
            timer2.AutoReset = false;
        }

        private void PickingFirstRound(object? sender, System.Timers.ElapsedEventArgs e)
        {
            PickingFirstRound();
        }

        private async void PickingFirstRound()
        {
            Byte[] data;
            data = new Byte[1024];
            String responseData = String.Empty;
            Int32 bytes;

            while(true)
            {
                bytes = await stream.ReadAsync(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received firstRnd: " + responseData);
                App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                
                if (responseData.StartsWith(Constants.PREFIX_PICKREGION))
                {
                    string[] splitData = responseData.Split('_');
                    App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format("Player {0} is supposed to pick a region!", splitData[1]); });
                    if (clientID == Int32.Parse(splitData[1]))
                    {
                        //we are supposed to be picking!
                        picking = true;
                    }
                    else
                    {
                        SendPickedRegion(null);
                    }
                    break;
                }
            }

            //after 5s lets see if it picked something, if not, then pick random
            System.Timers.Timer timer = new System.Timers.Timer(5000);
            timer.Enabled = true;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = "Wait for the server to update information!"; });
            if (picking)
            {
                picking = false;
                foreach(var x in gameInformation.Regions[clientID - 1])
                {
                    Debug.WriteLine(x);
                }

                Debug.WriteLine("----");

                foreach(var x in gameInformation.Regions)
                {
                    foreach(var y in x)
                    {
                        Debug.WriteLine(y);
                    }
                    Debug.WriteLine("----");
                }

                Constants.Region? reg = Constants.PickRandomFreeNeighboringRegion(gameInformation.Regions[clientID - 1], gameInformation.Regions, clientID - 1);
                Debug.WriteLine(reg);
                SendPickedRegion(reg);
            }

            //Thread.Sleep(1000); //wait 1s for the server to send an update and then update the information
            App.Current.Dispatcher.Invoke((Action)delegate { UpdateGameInformation(); });
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
                Debug.WriteLine("Received: " + responseData);
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
                Debug.WriteLine("Received: " + responseData);
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
            this.p1pointsTextBox.Text = this.gameInformation.Points[0].ToString();
            this.p2pointsTextBox.Text = this.gameInformation.Points[1].ToString();

            Brush[] brushes = new Brush[Constants.MAX_PLAYERS];
            brushes[0] = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); //red
            brushes[1] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)); //blue

            Brush[] baseBrushes = new Brush[Constants.MAX_PLAYERS];
            baseBrushes[0] = new SolidColorBrush(Color.FromArgb(255, 125, 30, 30)); //dark red
            baseBrushes[1] = new SolidColorBrush(Color.FromArgb(255, 30, 30, 125)); //dark blue

            int i = 0; 
            foreach (var list in this.gameInformation.Regions)
            {
                foreach(Constants.Region reg in list)
                {
                    this.paths[(int)reg].Fill = brushes[i];
                }
                i++;
            }

            Constants.Region base1 = this.gameInformation.Bases[0];
            this.paths[(int)base1].Fill = baseBrushes[0];

            Constants.Region base2 = this.gameInformation.Bases[1];
            this.paths[(int)base2].Fill = baseBrushes[1];


            //color the bases
            /*
            Color border = Color.FromArgb(255, 255, 255, 0);

            Constants.Region base1 = this.gameInformation.Bases[0];
            this.paths[(int)base1].Stroke = new SolidColorBrush(border);
            this.paths[(int)base1].StrokeThickness = 2;


            Constants.Region base2 = this.gameInformation.Bases[1];
            this.paths[(int)base2].Stroke = new SolidColorBrush(border);
            this.paths[(int)base2].StrokeThickness = 2;
            */


        }

        private void SendPickedRegion(Constants.Region? region)
        {
            string message = Constants.PREFIX_PICKED + clientID + "_";
            if(region == null)
            {
                message += "-1";
            }
            else
            {
                message += region.Value;
            }
            SendMessageToServer(message);
        }

        private void SendMessageToServer(string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            stream.Write(msg, 0, msg.Length);
            Debug.WriteLine("Sent to the server: {0}", message);
        }

        private bool AreAnyMovesValid()
        {
            //the idea is to take all regions, then look at the free ones
            //then check if any of them neighbor any of our regions

            var allRegions = Enum.GetValues(typeof(Constants.Region)).Cast<Constants.Region>();
            List<Constants.Region> populatedRegions = new List<Constants.Region>();
            foreach (var list in this.gameInformation.Regions)
            {
                populatedRegions.AddRange(list);
            }
            var freeRegions = allRegions.Except(populatedRegions);
            var myRegions = this.gameInformation.Regions[clientID - 1];


            foreach (var region in freeRegions)
            {
                if(Constants.DoRegionsNeighbor(region, myRegions))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleRegionClick(object sender, Constants.Region region)
        {
            if (!picking) return;

            if (!gameInformation.Regions[0].Contains(region) && !gameInformation.Regions[1].Contains(region))
            {
                if(Constants.DoRegionsNeighbor(region, gameInformation.Regions[clientID - 1]) || !AreAnyMovesValid()) {
                    picking = false;
                    (sender as Path).Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                    this.gameStatusTextBox.Text = "You have picked: " + region.ToString();
                    SendPickedRegion(region);
                }
            }
        }

        private void CZJC_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZJC);
        }

        private void CZJM_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZJM);
        }

        private void CZKA_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZKA);
        }

        private void CZKR_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZKR);
        }

        private void CZLI_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZLI);
        }

        private void CZMO_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZMO);
        }

        private void CZOL_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZOL);
        }

        private void CZPA_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZPA);
        }

        private void CZZL_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZZL);
        }

        private void CZPL_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZPL);

        }

        private void CZPR_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZPR);
        }

        private void CZST_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZST);
        }

        private void CZUS_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZUS);
        }

        private void CZVY_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleRegionClick(sender, Constants.Region.CZVY);
        }
    }
}

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
    public static class BrushesAndColors {

        public static SolidColorBrush ATTACKED_REGION_BRUSH = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));

        public static Brush[] REGION_BRUSHES => new Brush[Constants.MAX_PLAYERS]
        {
            new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)), //red
            new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)), //blue
        };

        public static Brush[] BASEREGION_BRUSHES => new Brush[Constants.MAX_PLAYERS]
        {
            new SolidColorBrush(Color.FromArgb(255, 125, 30, 30)), //dark red
            new SolidColorBrush(Color.FromArgb(255, 30, 30, 125)), //dark blue
        };

        public static Brush HIGHVALUEREGION_BRUSH = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

        public static Brush REGIONCLICKED_BRUSH = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

    }
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private NetworkStream stream; //contains stream connected to the server
        private int clientID;
        private GameInformation gameInformation; //contains information about current game
        private Path[] paths;
        private bool picking; //are we picking the region right now?
        private Window questionWindow;
        private bool inAnotherWindow;
        private Constants.GameStatus gameStatus;
        private int attackRoundNumber;

        public GameWindow(NetworkStream stream)
        {
            InitializeComponent();
            this.stream = stream;
            this.gameStatus = Constants.GameStatus.Loading;
            this.gameInformation = new GameInformation();
            this.attackRoundNumber = 1;
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

            WaitForGameStart();

            Task.Run(() => Play()); //run the game in another thread, so we do not freeze the main one!
        }

        private void Play()
        {
            //after the game starts, we are waiting for next instructions

            this.gameStatus = Constants.GameStatus.FirstRound;
            for (int i = 0; i < Constants.FIRST_ROUND_QUESTIONS_COUNT; i++)
            {
                FirstRound();
            }

            this.gameStatus = Constants.GameStatus.SecondRound_FirstVersion;
            for (int i = 0; i < Constants.SECOND_ROUND_FIRST_VERSION_QUESTIONS_COUNT; i++)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Content = "Current round: " + this.attackRoundNumber++; });
                SecondRound();
            }

            this.gameStatus = Constants.GameStatus.SecondRound_SecondVersion;
            for (int i = 0; i < Constants.SECOND_ROUND_SECOND_VERSION_QUESTIONS_COUNT; i++)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Content = "Current round: " + this.attackRoundNumber++; });
                SecondRound();
            }

            UpdateGameInformation(); //for checking game over
        }

        private void PickingSecondRound()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true)
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received 2ndRnd: " + responseData);
                //App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });

                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_PICKREGION))
                {
                    string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
                    App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICK_REGION, splitData[1]); });
                    
                    //if we are supposed to be picking!
                    if (clientID == Int32.Parse(splitData[1]))
                    {
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
            Thread.Sleep(Constants.DELAY_CLIENT_PICK);

            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.SERVER_UPDATE; });
            if (picking)
            {
                picking = false;
                Constants.Region? reg;
                if (gameStatus == Constants.GameStatus.SecondRound_FirstVersion)
                {
                    reg = Constants.PickRandomEnemyRegion(gameInformation.Regions, clientID - 1, true);
                }
                else
                {
                    reg = Constants.PickRandomEnemyRegion(gameInformation.Regions, clientID - 1, false);
                }
                Debug.WriteLine(reg);
                SendPickedRegion(reg);
            }
        }

        private void WaitForAttack()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true)
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_ATTACK))
                {
                    string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
                    Enum.TryParse(splitData[1], out Constants.Region reg);
                    App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.REGION_UNDER_ATTACK, splitData[1]); });
                    App.Current.Dispatcher.Invoke((Action)delegate { this.paths[(int)reg].Fill = BrushesAndColors.ATTACKED_REGION_BRUSH; });
                    break;
                }

            }
        }

        private void SecondRound()
        {
            //firstly wait for the pick...
            PickingSecondRound();
            //then wait for the attack
            WaitForAttack();
            //3s to show the players
            Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
            //then wait for the question
            SecondRoundWaitForQuestion();
        }

        private void SecondRoundWaitForQuestion()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //answer the first question
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received: " + responseData);
                //App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONABCD)) //handle question
                {
                    this.inAnotherWindow = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow = new QuestionABCDWindow(responseData, stream, clientID - 1);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Closed += QuestionWindowABCD_Round2_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });
                    break;
                }
            }

            SecondRoundAfterFirstQuestion();
        }

        private void SecondRoundAfterFirstQuestion()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            //here we have to wait in the thread because there is an question open.
            SpinWait.SpinUntil(() => this.inAnotherWindow == false);

            //now we have to decide what to do...
            //there are 3 main options:
            //here the client can receive 3 types of answers!!
            //1) end game
            //2) game info update
            //3) new question (num/ABCD)

            while (true) //answer the first question
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received: " + responseData);
                // App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONABCD)) //handle question
                {
                    //this means that some base was damaged...
                    this.inAnotherWindow = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow = new QuestionABCDWindow(responseData, stream, clientID - 1);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Closed += QuestionWindowABCD_Round2_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });

                    SecondRoundAfterFirstQuestion();
                    return;
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONNUMBER))
                {
                    this.inAnotherWindow = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow = new QuestionNumericWindow(responseData, stream, clientID - 1);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Closed += QuestionWindowABCD_Round2_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    }); //TODO: change this??
                    break;
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEUPDATE))
                {
                    Debug.WriteLine("asdasd");
                    //update the game data
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        gameInformation.UpdateGameInformationFromMessage(responseData);
                        Thread.Sleep(Constants.DELAY_FASTUPDATE_MS);
                        UpdateWindowFromGameInformation();
                    });
                    return;
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEOVER))
                {
                    GameOver(responseData);
                }
            }

            Debug.WriteLine(this.inAnotherWindow);
            //here we have to wait in the thread because there is an question open.
            SpinWait.SpinUntil(() => this.inAnotherWindow == false);
            App.Current.Dispatcher.Invoke((Action)delegate {
                UpdateGameInformation();
            });
        }

        private void HandleEnemyDisconnect()
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Exclamation;
            MessageBoxResult result;

            result = MessageBox.Show(Constants.GAMEOVER_DISCONNECT, "Game over!", button, icon, MessageBoxResult.Yes);
            App.Current.Dispatcher.Invoke((Action)delegate {
                System.Windows.Application.Current.Shutdown();
                Environment.Exit(0);
            });
        }

        private void QuestionWindowABCD_Round2_Closed(object? sender, EventArgs e)
        {
            //we return from the question -> picking regions three times!
            this.inAnotherWindow = false;
        }

        private void FirstRound()
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.STARTING_SOON; });

            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //answer the first question
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received: " + responseData);
                //App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONNUMBER)) //handle question numeric
                {
                    this.inAnotherWindow = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow = new QuestionNumericWindow(responseData, stream, clientID);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.questionWindow.Closed += QuestionWindow_Round1_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });
                    break;
                }
            }

            Debug.WriteLine(this.inAnotherWindow);
            //here we have to wait in the thread because there is an question open.
            SpinWait.SpinUntil(() => this.inAnotherWindow == false);

            //after waiting for the answer we wait for the pick instruction 3 times
            PickingFirstRound();
            PickingFirstRound();
            PickingFirstRound();
        }

        private void QuestionWindow_Round1_Closed(object? sender, EventArgs e)
        {
            //we return from the question -> picking regions three times!
            this.inAnotherWindow = false;
        }

        private void PickingFirstRound()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while(true)
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received firstRnd: " + responseData);
                //App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = responseData; });
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_PICKREGION))
                {
                    string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
                    App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICK_REGION, splitData[1]); });
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
            Thread.Sleep(Constants.DELAY_CLIENT_PICK);

            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.SERVER_UPDATE; });
            if (picking)
            {
                picking = false;
                /*foreach(var x in gameInformation.Regions[clientID - 1])
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
                }*/

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
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while(true) //wait for the ID assignment
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received: " + responseData);
                /*here we check whether we receive the right message*/
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_ASSIGN))
                {
                    char lastChar = responseData[responseData.Length - 1];
                    this.clientID = Int32.Parse(lastChar.ToString());
                    this.playerIDLabel.Content = String.Format(Constants.PLAYER_ID_LABEL, lastChar);
                    break;
                }
            }

            //now we got the ids, so the server needs to set the right information
            UpdateGameInformation();
        }

        private void UpdateGameInformation()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //wait for new information about the game
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine("Received: " + responseData);
                /*here we check whether we receive the right message*/
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEUPDATE))
                {
                    //update the game data
                    gameInformation.UpdateGameInformationFromMessage(responseData);
                    Thread.Sleep(Constants.DELAY_FASTUPDATE_MS);
                    UpdateWindowFromGameInformation();
                    break;
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEOVER))
                {
                    GameOver(responseData);
                }
            }
        }

        private void GameOver(string data)
        {
            string[] splitData = data.Split(Constants.GLOBAL_DELIMITER);
            string messageBoxText = "";
            if (Int32.TryParse(splitData[1], out int id))
            {
                if(id == -1)
                {
                    messageBoxText = Constants.GAMEOVER_TIE;
                }
                else if(id == clientID)
                {
                    messageBoxText = Constants.GAMEOVER_WIN;
                }
                else
                {
                    messageBoxText = Constants.GAMEOVER_LOSE;
                }
            }
            this.gameStatus = Constants.GameStatus.GameOver;
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Asterisk;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, "Game over!", button, icon, MessageBoxResult.Yes);
            /*App.Current.Dispatcher.Invoke((Action)delegate {
                this.Close(); //end the app
            });*/
            App.Current.Dispatcher.Invoke((Action)delegate {
                System.Windows.Application.Current.Shutdown();
            });
        }

        //updates the game window based on the gameinformation property
        private void UpdateWindowFromGameInformation()
        {
            App.Current.Dispatcher.Invoke((Action)delegate { 
                this.p1pointsTextBox.Text = this.gameInformation.Points[0].ToString();
            });

            App.Current.Dispatcher.Invoke((Action)delegate {
                this.p2pointsTextBox.Text = this.gameInformation.Points[1].ToString();
            });

            App.Current.Dispatcher.Invoke((Action)delegate {
                this.p1HealthTextBox.Text = this.gameInformation.BaseHealths[0].ToString();
            });

            App.Current.Dispatcher.Invoke((Action)delegate {
                this.p2HealthTextBox.Text = this.gameInformation.BaseHealths[1].ToString();
            });

            int i = 0;
            List<Constants.Region> doneRegions = new List<Constants.Region>();

            foreach (var list in this.gameInformation.Regions)
            {
                foreach(Constants.Region reg in list)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this.paths[(int)reg].Fill = BrushesAndColors.REGION_BRUSHES[i];
                        Debug.Write(i + ": " + reg.ToString());
                    });
                }
                i++;
            }

            i = 0;
            foreach(Constants.Region reg in this.gameInformation.Bases)
            {
                App.Current.Dispatcher.Invoke((Action)delegate {
                    this.paths[(int)reg].Fill = BrushesAndColors.BASEREGION_BRUSHES[i];
                });
                i++;
            }

            foreach (Constants.Region region in gameInformation.HighValueRegions)
            {
                this.paths[(int)region].Stroke = BrushesAndColors.HIGHVALUEREGION_BRUSH;
                this.paths[(int)region].StrokeThickness = 2;
            }


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
            string message = Constants.PREFIX_PICKED + clientID + Constants.GLOBAL_DELIMITER;
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
            
            switch(this.gameStatus)
            {
                case Constants.GameStatus.FirstRound:
                    if (!gameInformation.Regions[0].Contains(region) && !gameInformation.Regions[1].Contains(region))
                    {
                        if (Constants.DoRegionsNeighbor(region, gameInformation.Regions[clientID - 1]) || !AreAnyMovesValid())
                        {
                            ActualRegionClickHandle(sender, region);
                        }
                    }
                    break;
                case Constants.GameStatus.SecondRound_FirstVersion:
                    if (!gameInformation.Regions[clientID - 1].Contains(region))
                    {
                        if (Constants.DoRegionsNeighbor(region, gameInformation.Regions[clientID - 1]))
                        {
                            ActualRegionClickHandle(sender, region);
                        }
                    }
                    break;
                case Constants.GameStatus.SecondRound_SecondVersion:
                    if (!gameInformation.Regions[clientID - 1].Contains(region))
                    {
                        ActualRegionClickHandle(sender, region);
                    }
                    break;
                default:
                    return;
            }
        }

        private void ActualRegionClickHandle(object sender, Constants.Region region)
        {
            picking = false;
            (sender as Path).Fill = BrushesAndColors.REGIONCLICKED_BRUSH;
            this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICKED, region.ToString());
            SendPickedRegion(region);
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

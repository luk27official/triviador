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
using Commons;
using System.Timers;
using System.IO;
using Newtonsoft.Json;

namespace client
{
    /// <summary>
    /// This static class contains all the brushes and colors used when coloring the board, buttons etc.
    /// </summary>
    public static class BrushesAndColors {

        public static SolidColorBrush ATTACKED_REGION_BRUSH = new(Color.FromArgb(255, 0, 255, 0));

        public static Color[] REGION_COLORS => new Color[Constants.MAX_PLAYERS]
        {
            Color.FromArgb(255, 255, 0, 0), //red
            Color.FromArgb(255, 0, 0, 255), //blue
        };

        public static Brush[] REGION_BRUSHES => new Brush[Constants.MAX_PLAYERS]
        {
            new SolidColorBrush(REGION_COLORS[0]), 
            new SolidColorBrush(REGION_COLORS[1]),
        };

        public static Color[] BASEREGION_COLORS => new Color[Constants.MAX_PLAYERS]
        {
            Color.FromArgb(255, 125, 30, 30), //dark red
            Color.FromArgb(255, 30, 30, 125), //dark blue
        };

        public static Brush[] BASEREGION_BRUSHES => new Brush[Constants.MAX_PLAYERS]
        {
            new SolidColorBrush(BASEREGION_COLORS[0]),
            new SolidColorBrush(BASEREGION_COLORS[1]),
        };

        public static Brush HIGHVALUEREGION_BRUSH = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)); //yellow, not used now

        public static Brush REGIONCLICKED_BRUSH = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)); //yellow

        public static Brush CORRECTANSWER_BRUSH = new SolidColorBrush(Color.FromArgb(255, 50, 255, 50)); //lime green

        public static Brush[] HIGHVALUEREGION_BRUSHES => new Brush[Constants.MAX_PLAYERS]
        {
            CreateHighValueRegionBrush(0),
            CreateHighValueRegionBrush(1),
        };

        /// <summary>
        /// Method creating a special brush for painting high valued regions.
        /// </summary>
        /// <param name="id">ID which picks the base background color.</param>
        /// <returns>A special brush for HV regions.</returns>
        public static Brush CreateHighValueRegionBrush(int id)
        {
            int size = 15;
            VisualBrush vb = new();
            vb.TileMode = TileMode.Tile;
            vb.Viewport = new Rect(0, 0, size, size);
            vb.ViewboxUnits = BrushMappingMode.Absolute;
            vb.Viewbox = new Rect(0, 0, size, size);
            vb.ViewportUnits = BrushMappingMode.Absolute;

            Grid g = new();
            g.Background = BrushesAndColors.REGION_BRUSHES[id];

            System.Windows.Shapes.Path p1 = new();

            LineGeometry myLineGeometry = new()
            {
                StartPoint = new Point(0, size),
                EndPoint = new Point(size, 0)
            };

            p1.Data = myLineGeometry;
            p1.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); //white

            g.Children.Add(p1);
            vb.Visual = g;

            return vb;
        }
    }

    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// This window controls the most part of the client side. It shows the board and game information.
    /// </summary>
    public partial class GameWindow : Window
    {
        private NetworkStream _stream;
        private GameInformation _gameInformation;
        private Window _questionWindow;                                     //currently opened window
        private System.Windows.Shapes.Path[] _gameBoardPaths;               //used for clicking/coloring the regions

        private bool _pickingRegion;                //are we picking the region right now?
        private bool _anotherWindowInFocus;
        private bool _secondRoundInProgress;

        private Constants.GameStatus _gameStatus;
        private int _attackRoundNumber;
        private int _clientID;

        private System.Timers.Timer timer;
        private int timerCounter;

        /// <summary>
        /// Constructor for GameWindow.
        /// </summary>
        /// <param name="stream">Stream connected to the server.</param>
        public GameWindow(NetworkStream stream)
        {
            InitializeComponent();
            this._stream = stream;
            this._gameStatus = Constants.GameStatus.Loading;
            this._gameInformation = new GameInformation();
            this._attackRoundNumber = 1;
            this._secondRoundInProgress = false;
            this._gameBoardPaths = new System.Windows.Shapes.Path[] { 
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

        /// <summary>
        /// Controls the entire game, calls the rounds.
        /// </summary>
        private void Play()
        {
            this._gameStatus = Constants.GameStatus.FirstRound;
            for (int i = 0; i < Constants.FIRST_ROUND_QUESTIONS_COUNT; i++)
            {
                FirstRound();
            }

            App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Visibility = Visibility.Visible; });

            this._gameStatus = Constants.GameStatus.SecondRound_FirstVersion;
            for (int i = 0; i < Constants.SECOND_ROUND_FIRST_VERSION_QUESTIONS_COUNT; i++)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Content = String.Format(Constants.CURRENT_ROUND, this._attackRoundNumber++); });
                SecondRound();
            }

            this._gameStatus = Constants.GameStatus.SecondRound_SecondVersion;
            for (int i = 0; i < Constants.SECOND_ROUND_SECOND_VERSION_QUESTIONS_COUNT; i++)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Content = String.Format(Constants.CURRENT_ROUND, this._attackRoundNumber++); });
                SecondRound();
            }

            UpdateGameInformation(); //for checking game over
        }

        /// <summary>
        /// Method called whilst returning from another window.
        /// </summary>
        /// <param name="sender">Window calling this method.</param>
        /// <param name="e">Event arguments.</param>
        private void QuestionWindow_Closed(object? sender, EventArgs e)
        {
            this._anotherWindowInFocus = false;
        }

        /// <summary>
        /// Method which processes the message based on the message type.
        /// </summary>
        /// <param name="message">Message from the server.</param>
        private void ProcessMessage(string message)
        {
            // here we should deserialize the message into json object
            // and then based on the type process the message
            BasicMessage? msgFromJson = JsonConvert.DeserializeObject<BasicMessage>(message);
            if (msgFromJson == null) return;

            switch(msgFromJson.Type)
            {
                case "assign":
                    if(msgFromJson.PlayerID != null)
                    {
                        this._clientID = Int32.Parse(msgFromJson.PlayerID);
                        this.playerIDLabel.Content = String.Format(Constants.PLAYER_ID_LABEL, msgFromJson.PlayerID);
                    }
                    break;
                case "disconnect":
                    ClientCommon.HandleEnemyDisconnect();
                    break;
                case "gameupdate":
                    if(msgFromJson.GameInformation != null)
                    {
                        this._gameInformation = msgFromJson.GameInformation;
                        Thread.Sleep(Constants.DELAY_FASTUPDATE_MS);
                        App.Current.Dispatcher.Invoke((Action)delegate {
                            UpdateWindowFromGameInformation();
                        });
                    }
                    break;
                case "questionnumeric":
                    if(msgFromJson.QuestionNumeric != null)
                    {
                        this._anotherWindowInFocus = true;

                        App.Current.Dispatcher.Invoke((Action)delegate {
                            this._questionWindow = new QuestionNumericWindow(msgFromJson.QuestionNumeric, _stream, _clientID);
                        });
                        App.Current.Dispatcher.Invoke((Action)delegate {
                            this._questionWindow.Show();
                        });
                        App.Current.Dispatcher.Invoke((Action)delegate {
                            this._questionWindow.Closed += QuestionWindow_Closed;
                        });

                        if(_secondRoundInProgress)
                        {
                            SecondRoundAfterFirstQuestion();
                        }
                    }
                    break;
                case "pickregion":
                    if(msgFromJson.PlayerID != null)
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICK_REGION, msgFromJson.PlayerID); });
                        if (_clientID == Int32.Parse(msgFromJson.PlayerID)) //we are supposed to be picking!
                        {
                            _pickingRegion = true;
                        }
                        else
                        {
                            SendPickedRegion(null);
                        }
                    }
                    break;
                case "attack":
                    if(msgFromJson.Region != null)
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.REGION_UNDER_ATTACK, msgFromJson.Region); });
                        App.Current.Dispatcher.Invoke((Action)delegate { this._gameBoardPaths[(int)msgFromJson.Region].Fill = BrushesAndColors.ATTACKED_REGION_BRUSH; });
                    }
                    break;
                case "questionabcd":
                    if (msgFromJson.QuestionABCD != null)
                    {
                        this._anotherWindowInFocus = true;

                        App.Current.Dispatcher.Invoke((Action)delegate {
                            this._questionWindow = new QuestionABCDWindow(msgFromJson.QuestionABCD, _stream, _clientID);
                        });
                        App.Current.Dispatcher.Invoke((Action)delegate {
                            this._questionWindow.Show();
                        });
                        App.Current.Dispatcher.Invoke((Action)delegate {
                            this._questionWindow.Closed += QuestionWindow_Closed;
                        });

                        if (_secondRoundInProgress)
                        {
                            SecondRoundAfterFirstQuestion();
                        }
                    }
                    break;
                case "gameover":
                    if(msgFromJson.PlayerID != null)
                    {
                        GameOver(msgFromJson.PlayerID);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Method which receives some message from the server calls a method to process it.
        /// </summary>
        private void ReceiveAndProcessMessage()
        {
            string message = MessageController.ReceiveMessage(_stream);
            ProcessMessage(message);
        }

        /// <summary>
        /// Entry point for the client. Server assigns some ID to the client.
        /// </summary>
        private void WaitForGameStart()
        {
            ReceiveAndProcessMessage();
            //now we got the ids, so the server needs to set the right information
            UpdateGameInformation();
        }

        /// <summary>
        /// Method updating game information such as points, regions, health and all other information needed based on server message.
        /// </summary>
        private void UpdateGameInformation()
        {
            ReceiveAndProcessMessage();
        }

        /// <summary>
        /// Contains game logic for the first round.
        /// First round consists of receiving a question, answering, picking regions and updating game data.
        /// </summary>
        private void FirstRound()
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.STARTING_SOON; });

            ReceiveAndProcessMessage();

            //here we have to wait in the thread because there is an question open.
            SpinWait.SpinUntil(() => this._anotherWindowInFocus == false);

            //after waiting for the answer we wait for the pick instruction 3 times
            PickingFirstRound();
            PickingFirstRound();
            PickingFirstRound();
        }

        /// <summary>
        /// This method waits for an instruction from the server to allow region picking.
        /// Then it sends data about the picked region, if needed.
        /// </summary>
        private void PickingFirstRound()
        {
            ReceiveAndProcessMessage();

            //firstly update the board
            this.timerCounter = Constants.DELAY_CLIENT_PICK / 1000; //divided by one second
            App.Current.Dispatcher.Invoke((Action)delegate { this.timeleftLabel.Content = String.Format(Constants.CLIENT_TIME_LEFT, this.timerCounter); });

            this.timer = new System.Timers.Timer(995); //basically 1s but to ensure it gets to zero it is a bit less
            timer.Elapsed += TimeLeftPickTimerTick;
            timer.AutoReset = true;
            timer.Enabled = true;

            //after 5s lets see if it picked something, if not, then pick random
            Thread.Sleep(Constants.DELAY_CLIENT_PICK);

            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.SERVER_UPDATE; });

            if (_pickingRegion)
            {
                _pickingRegion = false;
                Constants.Region? reg = Constants.PickRandomFreeNeighboringRegion(_gameInformation.Regions, _clientID - 1);
                SendPickedRegion(reg);
            }

            App.Current.Dispatcher.Invoke((Action)delegate { UpdateGameInformation(); });
        }

        /// <summary>
        /// A method called on timer tick while waiting for a pick.
        /// </summary>
        private void TimeLeftPickTimerTick(object? sender, ElapsedEventArgs e)
        {
            if(this.timerCounter == 0)
            {
                this.timer.Stop();
                this.timer.Dispose();
                return;
            }

            this.timerCounter--;
            App.Current.Dispatcher.Invoke((Action)delegate { this.timeleftLabel.Content = String.Format(Constants.CLIENT_TIME_LEFT, this.timerCounter); });
        }

        /// <summary>
        /// Contains game logic for the second round.
        /// Second round consists of picking a region, answering to a question (or more) and updating game data properly.
        /// </summary>
        private void SecondRound()
        {
            this._secondRoundInProgress = false;

            PickingSecondRound();

            WaitForAttack();

            Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);

            SecondRoundWaitForQuestion();
        }

        /// <summary>
        /// Method working similarly to PickingFirstRound. Waits for the pick and sends proper data to the server.
        /// </summary>
        private void PickingSecondRound()
        {
            ReceiveAndProcessMessage();

            this.timerCounter = Constants.DELAY_CLIENT_PICK / 1000; //divided by one second
            App.Current.Dispatcher.Invoke((Action)delegate { this.timeleftLabel.Content = String.Format(Constants.CLIENT_TIME_LEFT, this.timerCounter); });

            this.timer = new System.Timers.Timer(995); //basically 1s but to ensure it gets to zero it is a bit less
            timer.Elapsed += TimeLeftPickTimerTick;
            timer.AutoReset = true;
            timer.Enabled = true;

            //after 5s lets see if it picked something, if not, then pick random
            Thread.Sleep(Constants.DELAY_CLIENT_PICK);

            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.SERVER_UPDATE; });
            if (_pickingRegion)
            {
                _pickingRegion = false;
                Constants.Region? reg;
                if (_gameStatus == Constants.GameStatus.SecondRound_FirstVersion)
                {
                    reg = Constants.PickRandomEnemyRegion(_gameInformation.Regions, _clientID - 1, true);
                }
                else
                {
                    reg = Constants.PickRandomEnemyRegion(_gameInformation.Regions, _clientID - 1, false);
                }
                //Debug.WriteLine(reg);
                SendPickedRegion(reg);
            }
        }

        /// <summary>
        /// Method which colors the attacked region based on the server response.
        /// </summary>
        private void WaitForAttack()
        {
            ReceiveAndProcessMessage();
        }

        /// <summary>
        /// Method which handles waiting for and execution of the first question of the second round.
        /// </summary>
        private void SecondRoundWaitForQuestion()
        {
            ReceiveAndProcessMessage();
            SecondRoundAfterFirstQuestion();
        }

        /// <summary>
        /// Method handling return from the first question of the second round.
        /// Because there are multiple possibilities of server responses, it is better to have
        /// this method split.
        /// </summary>
        private void SecondRoundAfterFirstQuestion()
        {
            this._secondRoundInProgress = true;

            //here we have to wait in the thread because there is an question open.
            SpinWait.SpinUntil(() => this._anotherWindowInFocus == false);

            //here the client can receive more types of answers!!
            ReceiveAndProcessMessage();
        }

        /// <summary>
        /// Handles client reaction when the game is over.
        /// </summary>
        /// <param name="winnerID">String containing the winner ID.</param>
        private void GameOver(string winnerID)
        {
            string messageBoxText = "";
            if (Int32.TryParse(winnerID, out int id))
            {
                if(id == Constants.INVALID_CLIENT_ID)
                {
                    messageBoxText = Constants.GAMEOVER_TIE;
                }
                else if(id == _clientID)
                {
                    messageBoxText = Constants.GAMEOVER_WIN;
                }
                else
                {
                    messageBoxText = Constants.GAMEOVER_LOSE;
                }
            }
            this._gameStatus = Constants.GameStatus.GameOver;
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Asterisk;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, "Game over!", button, icon, MessageBoxResult.Yes);
            App.Current.Dispatcher.Invoke((Action)delegate {
                System.Windows.Application.Current.Shutdown();
            });
        }

        /// <summary>
        /// Updates the GameWindow based on the _gameInformation field. This includes updating textboxes and region colors.
        /// </summary>
        private void UpdateWindowFromGameInformation()
        {
            TextBox[] pointsTextBoxes = new TextBox[Constants.MAX_PLAYERS] { this.p1pointsTextBox, this.p2pointsTextBox };
            TextBox[] healthTextBoxes = new TextBox[Constants.MAX_PLAYERS] { this.p1HealthTextBox, this.p2HealthTextBox };

            for (int y = 0; y < Constants.MAX_PLAYERS; y++)
            {
                pointsTextBoxes[y].Text = this._gameInformation.Points[y].ToString();
                healthTextBoxes[y].Text = this._gameInformation.BaseHealths[y].ToString();
            }

            int i = 0;
            List<Constants.Region> doneRegions = new();

            foreach (var list in this._gameInformation.Regions)
            {
                foreach(Constants.Region reg in list)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._gameBoardPaths[(int)reg].Fill = BrushesAndColors.REGION_BRUSHES[i];
                    });
                }
                i++;
            }

            i = 0;
            foreach(Constants.Region reg in this._gameInformation.Bases)
            {
                App.Current.Dispatcher.Invoke((Action)delegate {
                    this._gameBoardPaths[(int)reg].Fill = BrushesAndColors.BASEREGION_BRUSHES[i];
                });
                i++;
            }

            foreach (Constants.Region region in _gameInformation.HighValueRegions)
            {
                i = 0;
                foreach(var list in this._gameInformation.Regions)
                {
                    if(list.Contains(region))
                    {
                        this._gameBoardPaths[(int)region].Fill = BrushesAndColors.HIGHVALUEREGION_BRUSHES[i];
                    }
                    i++;
                }
            }
        }

        /// <summary>
        /// Method which sends information about the picked region to the server.
        /// </summary>
        /// <param name="region">Picked region.</param>
        private void SendPickedRegion(Constants.Region? region)
        {
            string message = MessageController.EncodeMessageIntoJSONWithPrefix("picked", playerID: _clientID.ToString(), region: region);
            byte[] msg = Encoding.ASCII.GetBytes(message);
            _stream.Write(msg, 0, msg.Length);
        }

        /// <summary>
        /// Method checking if there are any valid free region picks for the player.
        /// </summary>
        /// <returns>True, if there is at least one free region neighboring to one of the players' regions.</returns>
        private bool AreAnyMovesValid()
        {
            //the idea is to take all regions, then look at the free ones
            //then check if any of them neighbor any of our regions

            var allRegions = Enum.GetValues(typeof(Constants.Region)).Cast<Constants.Region>();
            List<Constants.Region> populatedRegions = new();
            foreach (var list in this._gameInformation.Regions)
            {
                populatedRegions.AddRange(list);
            }
            var freeRegions = allRegions.Except(populatedRegions);
            var myRegions = this._gameInformation.Regions[_clientID - 1];

            foreach (var region in freeRegions)
            {
                if(Constants.DoRegionsNeighbor(region, myRegions))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Method handling all of the clicks from the map. Checks all of the pick logic.
        /// </summary>
        /// <param name="sender">Clicked region from the map (as C# "object").</param>
        /// <param name="region">Clicked region from the map (as C# "Region").</param>
        private void HandleRegionClick(object sender, Constants.Region region)
        {
            if (!_pickingRegion) return;
            
            switch(this._gameStatus)
            {
                case Constants.GameStatus.FirstRound:
                    if (!_gameInformation.Regions[0].Contains(region) && !_gameInformation.Regions[1].Contains(region))
                    {
                        if (Constants.DoRegionsNeighbor(region, _gameInformation.Regions[_clientID - 1]) || !AreAnyMovesValid())
                        {
                            ActualRegionClickHandle(sender, region);
                        }
                    }
                    break;
                case Constants.GameStatus.SecondRound_FirstVersion:
                    if (!_gameInformation.Regions[_clientID - 1].Contains(region))
                    {
                        if (Constants.DoRegionsNeighbor(region, _gameInformation.Regions[_clientID - 1]))
                        {
                            ActualRegionClickHandle(sender, region);
                        }
                    }
                    break;
                case Constants.GameStatus.SecondRound_SecondVersion:
                    if (!_gameInformation.Regions[_clientID - 1].Contains(region))
                    {
                        ActualRegionClickHandle(sender, region);
                    }
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Colors the region when a pick occurs. Called from HandleRegionClick.
        /// </summary>
        /// <param name="sender">Clicked region from the map (as C# "object").</param>
        /// <param name="region">Clicked region from the map (as C# "Region").</param>
        private void ActualRegionClickHandle(object? sender, Constants.Region region)
        {
            _pickingRegion = false;
            if(sender != null)
            {
                ((System.Windows.Shapes.Path)sender).Fill = BrushesAndColors.REGIONCLICKED_BRUSH;
            } 
            this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICKED, region.ToString());
            SendPickedRegion(region);
        }

        //All of the methods below just assure that clicks on the map are handled.

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

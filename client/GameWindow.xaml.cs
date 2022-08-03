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

namespace client
{
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

        public static Brush HIGHVALUEREGION_BRUSH = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

        public static Brush REGIONCLICKED_BRUSH = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

        public static Brush CORRECTANSWER_BRUSH = new SolidColorBrush(Color.FromArgb(255, 50, 255, 50));

        public static Brush[] HIGHVALUEREGION_BRUSHES => new Brush[Constants.MAX_PLAYERS]
        {
            CreateHighValueRegionBrush(0),
            CreateHighValueRegionBrush(1),
        };

        public static Brush CreateHighValueRegionBrush(int id)
        {
            VisualBrush vb = new();
            vb.TileMode = TileMode.Tile;
            vb.Viewport = new Rect(0, 0, 15, 15);
            vb.ViewboxUnits = BrushMappingMode.Absolute;
            vb.Viewbox = new Rect(0, 0, 15, 15);
            vb.ViewportUnits = BrushMappingMode.Absolute;

            Grid g = new Grid();
            g.Background = BrushesAndColors.REGION_BRUSHES[id];

            Path p1 = new Path();

            LineGeometry myLineGeometry = new LineGeometry();
            myLineGeometry.StartPoint = new Point(0, 15);
            myLineGeometry.EndPoint = new Point(15, 0);

            p1.Data = myLineGeometry;
            p1.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            g.Children.Add(p1);
            vb.Visual = g;

            return vb;
        }
    }

    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private NetworkStream _stream; //contains stream connected to the server
        private GameInformation _gameInformation; //contains information about current game
        private Window _questionWindow;
        private Path[] _gameBoardPaths;

        private bool _pickingRegion; //are we picking the region right now?
        private bool _anotherWindowInFocus;

        private Constants.GameStatus _gameStatus;
        private int _attackRoundNumber;
        private int _clientID;

        public GameWindow(NetworkStream stream)
        {
            InitializeComponent();
            this._stream = stream;
            this._gameStatus = Constants.GameStatus.Loading;
            this._gameInformation = new GameInformation();
            this._attackRoundNumber = 1;
            this._gameBoardPaths = new Path[] { 
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
            this._gameStatus = Constants.GameStatus.FirstRound;
            for (int i = 0; i < Constants.FIRST_ROUND_QUESTIONS_COUNT; i++)
            {
                FirstRound();
            }

            this._gameStatus = Constants.GameStatus.SecondRound_FirstVersion;
            for (int i = 0; i < Constants.SECOND_ROUND_FIRST_VERSION_QUESTIONS_COUNT; i++)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Content = "Current round: " + this._attackRoundNumber++; });
                SecondRound();
            }

            this._gameStatus = Constants.GameStatus.SecondRound_SecondVersion;
            for (int i = 0; i < Constants.SECOND_ROUND_SECOND_VERSION_QUESTIONS_COUNT; i++)
            {
                App.Current.Dispatcher.Invoke((Action)delegate { this.currentRndLabel.Content = "Current round: " + this._attackRoundNumber++; });
                SecondRound();
            }

            UpdateGameInformation(); //for checking game over
        }

        private void QuestionWindow_Closed(object? sender, EventArgs e)
        {
            //we return from the question -> picking regions three times!
            this._anotherWindowInFocus = false;
        }

        //here the game starts - wait for an assignment of ID
        private void WaitForGameStart()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            string responseData;
            Int32 bytes;

            while (true) //wait for the ID assignment
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_ASSIGN))
                {
                    char lastChar = responseData[^1];
                    this._clientID = Int32.Parse(lastChar.ToString());
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
            string responseData;
            Int32 bytes;

            while (true) //wait for new information about the game
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEUPDATE)) //update the game data
                {
                    _gameInformation.UpdateGameInformationFromMessage(responseData);
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

        private void FirstRound()
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = Constants.STARTING_SOON; });

            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true) //answer the first question
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONNUMBER)) //handle question numeric
                {
                    this._anotherWindowInFocus = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow = new QuestionNumericWindow(responseData, _stream, _clientID);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Closed += QuestionWindow_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });
                    break;
                }
            }

            //here we have to wait in the thread because there is an question open.
            SpinWait.SpinUntil(() => this._anotherWindowInFocus == false);

            //after waiting for the answer we wait for the pick instruction 3 times
            PickingFirstRound();
            PickingFirstRound();
            PickingFirstRound();
        }

        private void PickingFirstRound()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while(true)
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received firstRnd: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_PICKREGION))
                {
                    string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
                    App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICK_REGION, splitData[1]); });
                    if (_clientID == Int32.Parse(splitData[1])) //we are supposed to be picking!
                    {
                        _pickingRegion = true;
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

            if (_pickingRegion)
            {
                _pickingRegion = false;
                Constants.Region? reg = Constants.PickRandomFreeNeighboringRegion(_gameInformation.Regions, _clientID - 1);
                SendPickedRegion(reg);
            }

            App.Current.Dispatcher.Invoke((Action)delegate { UpdateGameInformation(); });
        }


        private void PickingSecondRound()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true)
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received 2ndRnd: " + responseData);

                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_PICKREGION))
                {
                    string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
                    App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.PLAYER_PICK_REGION, splitData[1]); });

                    //if we are supposed to be picking!
                    if (_clientID == Int32.Parse(splitData[1]))
                    {
                        _pickingRegion = true;
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

        private void WaitForAttack()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true)
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_ATTACK))
                {
                    string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
                    if(Enum.TryParse(splitData[1], out Constants.Region reg)) //this should happen every time
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate { this.gameStatusTextBox.Text = String.Format(Constants.REGION_UNDER_ATTACK, splitData[1]); });
                        App.Current.Dispatcher.Invoke((Action)delegate { this._gameBoardPaths[(int)reg].Fill = BrushesAndColors.ATTACKED_REGION_BRUSH; });
                    }
                    break;
                }

            }
        }

        private void SecondRound()
        {
            //firstly wait for the pick...
            PickingSecondRound();

            WaitForAttack();

            Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);

            SecondRoundWaitForQuestion();
        }

        private void SecondRoundWaitForQuestion()
        {
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;

            while (true)
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONABCD)) //handle question
                {
                    this._anotherWindowInFocus = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow = new QuestionABCDWindow(responseData, _stream, _clientID - 1);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Closed += QuestionWindow_Closed;
                        //when it closes, it means that we/oponnent are about to pick a region
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
            SpinWait.SpinUntil(() => this._anotherWindowInFocus == false);

            //here the client can receive 3 types of answers!!
            while (true)
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONABCD)) //this means that some base was damaged...
                {
                    this._anotherWindowInFocus = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow = new QuestionABCDWindow(responseData, _stream, _clientID - 1);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Closed += QuestionWindow_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });

                    SecondRoundAfterFirstQuestion();
                    return;
                }
                else if (responseData.StartsWith(Constants.PREFIX_QUESTIONNUMBER)) //this means it was a tie
                {
                    this._anotherWindowInFocus = true;

                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow = new QuestionNumericWindow(responseData, _stream, _clientID - 1);
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Show();
                    });
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        this._questionWindow.Closed += QuestionWindow_Closed; //when it closes, it means that we/oponnent are about to pick a region
                    });

                    SecondRoundAfterFirstQuestion();
                    return;
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEUPDATE)) //this means the attack is over
                {
                    App.Current.Dispatcher.Invoke((Action)delegate {
                        _gameInformation.UpdateGameInformationFromMessage(responseData);
                        Thread.Sleep(Constants.DELAY_FASTUPDATE_MS);
                        UpdateWindowFromGameInformation();
                    });
                    return;
                }
                else if (responseData.StartsWith(Constants.PREFIX_GAMEOVER)) //this means game is over
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

        //updates the game window based on the gameinformation property
        private void UpdateWindowFromGameInformation()
        {
            //basically this could be done in a foreach based on MAX_PLAYERS, but still

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

        private void SendPickedRegion(Constants.Region? region)
        {
            string message = Constants.PREFIX_PICKED + _clientID + Constants.GLOBAL_DELIMITER;
            if(region == null)
            {
                message += Constants.INVALID_CLIENT_ID.ToString();
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
            _stream.Write(msg, 0, msg.Length);
            //Debug.WriteLine("Sent to the server: {0}", message);
        }

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

        private void ActualRegionClickHandle(object? sender, Constants.Region region)
        {
            _pickingRegion = false;
            if(sender != null)
            {
                ((Path)sender).Fill = BrushesAndColors.REGIONCLICKED_BRUSH;
            } 
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

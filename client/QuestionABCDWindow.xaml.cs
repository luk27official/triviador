using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
    /// <summary>
    /// Interaction logic for QuestionABCDWindow.xaml
    /// </summary>
    public partial class QuestionABCDWindow : Window
    {
        private int _millisecondsElapsed;
        private int _millisecondsMaximum;
        private bool _questionAnswered;
        private System.Timers.Timer _timer;
        private NetworkStream _networkStream;
        private int _clientID;

        public QuestionABCDWindow(string data, NetworkStream stream, int clientID)
        {
            InitializeComponent();
            ParseQuestion(data);
            this._millisecondsElapsed = 0;
            this._millisecondsMaximum = 1000;
            this._networkStream = stream;
            this._clientID = clientID;
            TimerHandler();
        }

        private void TimerHandler()
        {
            _timer = new System.Timers.Timer(Constants.MS_MULTIPLIER);

            _timer.Elapsed += ChangeTimerLabel;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void ChangeTimerLabel(object? source, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.timerLabel.Content = String.Format(Constants.TIMELEFT, (_millisecondsMaximum - _millisecondsElapsed) / 100); });

            _millisecondsElapsed++;

            if (_millisecondsElapsed > _millisecondsMaximum)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if(_millisecondsElapsed > _millisecondsMaximum)
            {
                _timer.Stop();
                _timer.Dispose();
                TimeExpired();
            }
        }

        private void TimeExpired()
        {
            //now we should wait for the server to send us the results
            if (!_questionAnswered) //make sure we sent something
            {
                string message = Constants.PREFIX_ANSWER + _clientID + "_0"; //no answer - send zero
                byte[] msg = Encoding.ASCII.GetBytes(message);
                _networkStream.Write(msg, 0, msg.Length);
            }
            _questionAnswered = true;

            //now lets wait for the response with information
            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;
            while (true) //wait for the first question
            {
                bytes = _networkStream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                if (responseData.StartsWith(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_FINALANSWERS)) //handle answers
                {
                    ShowFinalAnswers(responseData);
                    break;
                }
            }

        }

        private void ShowFinalAnswers(string data)
        {
            string[] splitData = data.Split(Constants.GLOBAL_DELIMITER);
            //finalanswers_correctANS_P1ANS_P2ANS
            string correctAnswer = splitData[1];
            //this could be done maybe a bit better, but more players would change the visual,
            //so it would have to be re-done anyway...
            string p1Answer = splitData[2];
            string p2Answer = splitData[3];

            List<Button> buttons = new List<Button> { this.answerAbtn, this.answerBbtn, this.answerCbtn, this.answerDbtn };

            foreach(Button button in buttons)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (button.Content.ToString() == correctAnswer)
                    {
                        button.BorderThickness = new Thickness(5);
                        button.BorderBrush = BrushesAndColors.CORRECTANSWER_BRUSH;
                    }

                    if (button.Content.ToString() == p1Answer && (p1Answer == p2Answer))
                    {
                        LinearGradientBrush linGrBrush = new LinearGradientBrush(
                            BrushesAndColors.REGION_COLORS[0],
                            BrushesAndColors.REGION_COLORS[1],
                            0);
                        button.Background = linGrBrush;
                    }
                    else if (button.Content.ToString() == p1Answer)
                    {
                        button.Background = BrushesAndColors.REGION_BRUSHES[0];
                    }
                    else if (button.Content.ToString() == p2Answer)
                    {
                        button.Background = BrushesAndColors.REGION_BRUSHES[1];
                    }
                });
            }

            Thread.Sleep(Constants.DELAY_SHOWANSWERS);
            App.Current.Dispatcher.Invoke((Action)delegate { this.Close(); });
        }

        private Button PickRandomButton(List<Button> list)
        {
            Random rnd = new Random();
            int r = rnd.Next(list.Count);
            return list[r];
        }

        private void ParseQuestion(string data)
        {
            string[] splitData = data.Split(Constants.GLOBAL_DELIMITER);
            this.questionLabel.Text = splitData[1];

            List<Button> availableButtons = new List<Button> { this.answerAbtn, this.answerBbtn, this.answerCbtn, this.answerDbtn };
            
            for(int i = 2; i < splitData.Length; i++)
            {
                Button rand = PickRandomButton(availableButtons);
                rand.Content = splitData[i];
                availableButtons.Remove(rand);
            }
        }

        private void HandleClick(string? answer, object? sender)
        {
            if (_questionAnswered) return;

            _questionAnswered = true;

            if(sender != null)
            {
                ((Button)sender).Background = BrushesAndColors.REGION_BRUSHES[_clientID];
            }
            
            string message = Constants.PREFIX_ANSWER + _clientID + Constants.GLOBAL_DELIMITER + answer;
            byte[] msg = Encoding.ASCII.GetBytes(message);
            _networkStream.Write(msg, 0, msg.Length);
        }

        private void answerAbtn_Click(object sender, RoutedEventArgs e)
        {
            HandleClick(this.answerAbtn.Content.ToString(), sender);
        }

        private void answerBbtn_Click(object sender, RoutedEventArgs e)
        {
            HandleClick(this.answerBbtn.Content.ToString(), sender);
        }

        private void answerCbtn_Click(object sender, RoutedEventArgs e)
        {
            HandleClick(this.answerCbtn.Content.ToString(), sender);
        }

        private void answerDbtn_Click(object sender, RoutedEventArgs e)
        {
            HandleClick(this.answerDbtn.Content.ToString(), sender);
        }
    }
}

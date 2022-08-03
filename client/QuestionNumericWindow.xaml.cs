using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
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
    /// Interaction logic for QuestionNumericWindow.xaml
    /// </summary>
    public partial class QuestionNumericWindow : Window
    {
        private int _millisecondsElapsed;
        private readonly int _millisecondsMaximum;
        private System.Timers.Timer _timer;
        private bool _questionAnswered;
        private NetworkStream _networkStream;
        private int _clientID;
        private Stopwatch _stopwatch;

        public QuestionNumericWindow(string data, NetworkStream stream, int clientID)
        {
            InitializeComponent();
            ParseQuestion(data);
            this.answerTxtBox.Focus();
            this._millisecondsElapsed = 0;
            this._millisecondsMaximum = Constants.QUESTION_TIME;
            this._questionAnswered = false;
            this._networkStream = stream;
            this._clientID = clientID;
            this._timer = new System.Timers.Timer(Constants.MS_MULTIPLIER);
            TimerHandler();
            this._stopwatch = new();
            _stopwatch.Start();
        }

        private void TimerHandler()
        {
            _timer.Elapsed += ChangeTimerLabel;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void ChangeTimerLabel(object? source, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.timerLabel.Content = String.Format("Time left: {0} seconds", (_millisecondsMaximum - _millisecondsElapsed)/100); });

            _millisecondsElapsed++;

            if (_millisecondsElapsed > _millisecondsMaximum)
            {
                _timer.Stop();
                _timer.Dispose();
                TimeExpired();
            }
        }

        private void TimeExpired()
        {
            //now we should wait for the server to send us the results
            App.Current.Dispatcher.Invoke((Action)delegate { this.answerTxtBox.IsEnabled = false; });

            if (!_questionAnswered) //make sure we sent something
            {
                _stopwatch.Stop();
                string message = Constants.PREFIX_ANSWER + _clientID + "_0_" + _stopwatch.ElapsedMilliseconds;
                byte[] msg = Encoding.ASCII.GetBytes(message);
                _networkStream.Write(msg, 0, msg.Length);
            }
            _questionAnswered = true;

            Byte[] data;
            data = new Byte[Constants.DEFAULT_BUFFER_SIZE];
            String responseData = String.Empty;
            Int32 bytes;
            while (true) //wait for the question
            {
                bytes = _networkStream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                if (responseData.StartsWith(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_FINALANSWERS)) //handle question numeric
                {
                    ShowFinalAnswers(responseData);
                    break;
                }
            }
        }

        private void ShowFinalAnswers(string data)
        {
            string[] splitData = data.Split(Constants.GLOBAL_DELIMITER);
            App.Current.Dispatcher.Invoke((Action)delegate { this.p1label.Content = String.Format(Constants.QUESTION_RESULT, "1", splitData[1], splitData[3]); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.p2label.Content = String.Format(Constants.QUESTION_RESULT, "2", splitData[2], splitData[4]); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.playerWinlabel.Content = String.Format(Constants.QUESTION_WINNER, splitData[5], Int32.Parse(splitData[6])); });

            //wait some time so the clients can see the final answers
            Thread.Sleep(Constants.DELAY_SHOWANSWERS);
            App.Current.Dispatcher.Invoke((Action)delegate { this.Close(); });
        }

        private void ParseQuestion(string data)
        {
            string[] splitData = data.Split(Constants.GLOBAL_DELIMITER);
            this.questionLabel.Content = splitData[1];
        }

        private void SubmitAnswer()
        {
            this.answerTxtBox.IsEnabled = false;
            _stopwatch.Stop();
            if(!Int32.TryParse(this.answerTxtBox.Text, out int ans))
            {
                ans = 0; //if invalid value pass an 0
            }
            string message = Constants.PREFIX_ANSWER + _clientID + Constants.GLOBAL_DELIMITER + ans.ToString() + Constants.GLOBAL_DELIMITER + _stopwatch.ElapsedMilliseconds;
            byte[] msg = Encoding.ASCII.GetBytes(message);
            _networkStream.Write(msg, 0, msg.Length);
        }

        private void answerTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !_questionAnswered)
            {
                SubmitAnswer();
                _questionAnswered = true;
            }
        }

        private void submitBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!_questionAnswered)
            {
                SubmitAnswer();
                _questionAnswered = true;
            }
        }
    }
}

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
    /// This window contains a question with a text field to enter the numeric answer.
    /// It is shown when created from GameWindow.
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

        /// <summary>
        /// Constructor for QuestionNumericWindow.
        /// </summary>
        /// <param name="data">Question from the server.</param>
        /// <param name="stream">Stream used to communicate between the server and this client.</param>
        /// <param name="clientID">Current client identifier.</param>
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

        /// <summary>
        /// A method which starts a timer for the question.
        /// </summary>
        private void TimerHandler()
        {
            _timer.Elapsed += ChangeTimerLabel;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        /// <summary>
        /// A method which updates the time label.
        /// </summary>
        /// <param name="source">A timer from TimerHandler method.</param>
        /// <param name="e">Event arguments.</param>
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

        /// <summary>
        /// A method called by the timer when time for the question expires. 
        /// Firstly assures some info has been sent, waits for the answers.
        /// </summary>
        private void TimeExpired()
        {
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

        /// <summary>
        /// Updates the window with the answer and player information.
        /// </summary>
        /// <param name="data">Response data containing the answers and information about players.</param>
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

        /// <summary>
        /// Method updating the form label with the question.
        /// </summary>
        /// <param name="data">Data from the server containing the question and options.</param>
        private void ParseQuestion(string data)
        {
            string[] splitData = data.Split(Constants.GLOBAL_DELIMITER);
            this.questionLabel.Text = splitData[1];
        }

        /// <summary>
        /// Method handling the sending of the answer to the server.
        /// </summary>
        private void SubmitAnswer()
        {
            this.answerTxtBox.IsEnabled = false;
            _stopwatch.Stop();
            if(!Int32.TryParse(this.answerTxtBox.Text, out int ans))
            {
                ans = 0; //if entered invalid value pass an 0
            }
            string message = Constants.PREFIX_ANSWER + _clientID + Constants.GLOBAL_DELIMITER + ans.ToString() + Constants.GLOBAL_DELIMITER + _stopwatch.ElapsedMilliseconds;
            byte[] msg = Encoding.ASCII.GetBytes(message);
            _networkStream.Write(msg, 0, msg.Length);
        }

        /// <summary>
        /// Method calling answer submit based on "Enter" key.
        /// </summary>
        /// <param name="sender">A text box with the answer.</param>
        /// <param name="e">Event arguments.</param>
        private void answerTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !_questionAnswered)
            {
                SubmitAnswer();
                _questionAnswered = true;
            }
        }

        /// <summary>
        /// Method calling answer submit based on button click.
        /// </summary>
        /// <param name="sender">Submit button.</param>
        /// <param name="e">Event arguments.</param>
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

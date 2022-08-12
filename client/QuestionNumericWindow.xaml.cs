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
using Newtonsoft.Json;

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
        /// <param name="question">Question from the server.</param>
        /// <param name="stream">Stream used to communicate between the server and this client.</param>
        /// <param name="clientID">Current client identifier.</param>
        public QuestionNumericWindow(QuestionNumeric question, NetworkStream stream, int clientID)
        {
            InitializeComponent();
            ParseQuestion(question);
            this.answerTxtBox.Focus();
            this._millisecondsElapsed = 0;
            this._millisecondsMaximum = Constants.QUESTION_TIME;
            this._questionAnswered = false;
            this._networkStream = stream;
            this._clientID = clientID;
            this._timer = new System.Timers.Timer(Constants.MS_MULTIPLIER);
            TimerHandler();
            this._stopwatch = new(); 
            this.p1label.Visibility = Visibility.Hidden;
            this.p2label.Visibility = Visibility.Hidden;
            this.playerWinlabel.Visibility = Visibility.Hidden;
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

        private void ReceiveAndProcessMessage()
        {
            string message = MessageController.ReceiveMessage(this._networkStream);
            Debug.WriteLine(message);
            ProcessMessage(message);
        }

        private void ProcessMessage(string message)
        {
            BasicMessage? msgFromJson = JsonConvert.DeserializeObject<BasicMessage>(message);
            if (msgFromJson == null) return;

            switch (msgFromJson.Type)
            {
                case "disconnect":
                    ClientCommon.HandleEnemyDisconnect();
                    break;
                case "finalanswers":
                    //handle final answers numeric
                    ShowFinalAnswers(msgFromJson.P1Ans, msgFromJson.P2Ans, msgFromJson.P1Time, msgFromJson.P2Time, msgFromJson.PlayerID, msgFromJson.Correct);
                    break;
                default:
                    break;
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

            ReceiveAndProcessMessage();
        }

        /// <summary>
        /// Updates the window with the answer and player information.
        /// TODO: edit the params...
        /// </summary>
        /// <param name="data">Response data containing the answers and information about players.</param>
        private void ShowFinalAnswers(string? p1ans, string? p2ans, string? p1time, string? p2time, string? winnerID, string? correctAns)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.p1label.Content = String.Format(Constants.QUESTION_RESULT, "1", p1ans, p1time); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.p2label.Content = String.Format(Constants.QUESTION_RESULT, "2", p2ans, p2time); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.playerWinlabel.Content = String.Format(Constants.QUESTION_WINNER, correctAns, winnerID); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.playerWinlabel.Visibility = Visibility.Visible; });
            App.Current.Dispatcher.Invoke((Action)delegate { this.p1label.Visibility = Visibility.Visible; });
            App.Current.Dispatcher.Invoke((Action)delegate { this.p2label.Visibility = Visibility.Visible; });

            //wait some time so the clients can see the final answers
            Thread.Sleep(Constants.DELAY_SHOWANSWERS);
            App.Current.Dispatcher.Invoke((Action)delegate { this.Close(); });
        }

        /// <summary>
        /// Method updating the form label with the question.
        /// </summary>
        /// <param name="data">Data from the server containing the question and options.</param>
        private void ParseQuestion(QuestionNumeric data)
        {
            this.questionLabel.Text = data.Content;
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

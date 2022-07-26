﻿using System;
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
using Newtonsoft.Json;

namespace client
{
    /// <summary>
    /// Interaction logic for QuestionABCDWindow.xaml
    /// This window contains a question with four possible options.
    /// It is shown when created from GameWindow.
    /// </summary>
    public partial class QuestionABCDWindow : Window
    {
        private int _millisecondsElapsed;
        private int _millisecondsMaximum;
        private bool _questionAnswered;
        private System.Timers.Timer _timer;
        private NetworkStream _networkStream;
        private int _clientID;

        /// <summary>
        /// Constructor for QuestionABCDWindow.
        /// </summary>
        /// <param name="question">Question from the server.</param>
        /// <param name="stream">Stream used to communicate between the server and this client.</param>
        /// <param name="clientID">Current client identifier.</param>
        public QuestionABCDWindow(QuestionABCD question, NetworkStream stream, int clientID)
        {
            InitializeComponent();
            ParseQuestion(question);
            this._millisecondsElapsed = 0;
            this._millisecondsMaximum = Constants.QUESTION_TIME;
            this._networkStream = stream;
            this._clientID = clientID;
            this._timer = new System.Timers.Timer(Constants.MS_MULTIPLIER);
            TimerHandler();
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
            App.Current.Dispatcher.Invoke((Action)delegate { this.timerLabel.Content = String.Format(Constants.TIMELEFT, (_millisecondsMaximum - _millisecondsElapsed) / (Constants.QUESTION_TIME / Constants.MS_MULTIPLIER)); });

            _millisecondsElapsed++;

            if(_millisecondsElapsed > _millisecondsMaximum)
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
            if (!_questionAnswered) //make sure we sent something
            {
                string message = "";
                if (_clientID == 1) message = MessageController.EncodeMessageIntoJSONWithPrefix(Constants.MESSAGE_ABCD_ANSWER, p1ans: "0");
                else if (_clientID == 2) message = MessageController.EncodeMessageIntoJSONWithPrefix(Constants.MESSAGE_ABCD_ANSWER, p2ans: "0"); //no answer -> send zero
                byte[] msg = Encoding.ASCII.GetBytes(message);
                _networkStream.Write(msg, 0, msg.Length);
            }
            _questionAnswered = true;

            ReceiveAndProcessMessage();
        }

        /// <summary>
        /// Method which receives some message from the server calls a method to process it.
        /// </summary>
        private void ReceiveAndProcessMessage()
        {
            string message = MessageController.ReceiveMessage(this._networkStream);
            ProcessMessage(message);
        }

        /// <summary>
        /// Method which processes the message based on the message type.
        /// </summary>
        /// <param name="message">Message from the server.</param>
        private void ProcessMessage(string message)
        {
            BasicMessage? msgFromJson = JsonConvert.DeserializeObject<BasicMessage>(message);
            if (msgFromJson == null) return;

            switch (msgFromJson.Type)
            {
                case Constants.MESSAGE_DISCONNECT:
                    ClientCommon.HandleEnemyDisconnect();
                    break;
                case Constants.MESSAGE_FINAL_ANSWERS_ABCD:
                    if(msgFromJson.AnswerDetails != null && msgFromJson.AnswerDetails.Answers != null)
                    {
                        ShowFinalAnswers(msgFromJson.AnswerDetails.Answers[0], msgFromJson.AnswerDetails.Answers[1], msgFromJson.AnswerDetails.Correct);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Updates the window with the answers and player information.
        /// </summary>
        /// <param name="data">Response data containing the answers and information about players.</param>
        private void ShowFinalAnswers(string? p1ans, string? p2ans, string? correctAns)
        {
            //this could be done maybe a bit better, but more players would change the visual,
            //so it would have to be re-done anyway...

            List<Button> buttons = new List<Button> { this.answerAbtn, this.answerBbtn, this.answerCbtn, this.answerDbtn };

            foreach(Button button in buttons)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (button.Content.ToString() == correctAns)
                    {
                        button.BorderThickness = new Thickness(5);
                        button.BorderBrush = BrushesAndColors.CORRECTANSWER_BRUSH;
                    }

                    if (button.Content.ToString() == p1ans && (p1ans == p2ans))
                    {
                        LinearGradientBrush linGrBrush = new LinearGradientBrush(
                            BrushesAndColors.REGION_COLORS[0],
                            BrushesAndColors.REGION_COLORS[1],
                            0);
                        button.Background = linGrBrush;
                    }
                    else if (button.Content.ToString() == p1ans)
                    {
                        button.Background = BrushesAndColors.REGION_BRUSHES[0];
                    }
                    else if (button.Content.ToString() == p2ans)
                    {
                        button.Background = BrushesAndColors.REGION_BRUSHES[1];
                    }
                });
            }

            Thread.Sleep(Constants.DELAY_SHOWANSWERS);
            App.Current.Dispatcher.Invoke((Action)delegate { this.Close(); });
        }

        /// <summary>
        /// Method returning a random button from the list. Used for shuffling the answers.
        /// </summary>
        /// <param name="list">List containing all form buttons.</param>
        /// <returns>A random button from the list.</returns>
        private Button PickRandomButton(List<Button> list)
        {
            Random rnd = new Random();
            int r = rnd.Next(list.Count);
            return list[r];
        }

        /// <summary>
        /// Method updating the form labels and button contents with the question and possible options.
        /// </summary>
        /// <param name="question">Data from the server containing the question and options.</param>
        private void ParseQuestion(QuestionABCD question)
        {

            this.questionLabel.Text = question.Content;

            List<Button> availableButtons = new List<Button> { this.answerAbtn, this.answerBbtn, this.answerCbtn, this.answerDbtn };
            
            for(int i = 0; i < question.Answers.Length; i++)
            {
                Button rand = PickRandomButton(availableButtons);
                rand.Content = question.Answers[i];
                availableButtons.Remove(rand);
            }
        }

        /// <summary>
        /// Method handling the sending of the picked answer to the server.
        /// </summary>
        /// <param name="answer">Answer to be sent.</param>
        /// <param name="sender">A clicked button.</param>
        private void HandleClick(string? answer, object? sender)
        {
            if (_questionAnswered) return;

            _questionAnswered = true;

            if(sender != null)
            {
                ((Button)sender).Background = BrushesAndColors.REGION_BRUSHES[_clientID - 1];
            }
            
            string message = "";
            if (_clientID == 1) message = MessageController.EncodeMessageIntoJSONWithPrefix(Constants.MESSAGE_ABCD_ANSWER, p1ans: answer);
            else if (_clientID == 2) message = MessageController.EncodeMessageIntoJSONWithPrefix(Constants.MESSAGE_ABCD_ANSWER, p2ans: answer);
            byte[] msg = Encoding.ASCII.GetBytes(message);
            _networkStream.Write(msg, 0, msg.Length);
        }

        //All of the methods below just assure that clicks on the buttons are handled.
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

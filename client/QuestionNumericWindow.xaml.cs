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

namespace client
{
    /// <summary>
    /// Interakční logika pro QuestionNumericWindow.xaml
    /// </summary>
    public partial class QuestionNumericWindow : Window
    {
        private int msElapsed;
        private int msTotal;
        private System.Timers.Timer timer;
        private bool answered;
        private NetworkStream stream;
        private int clientID;
        private Stopwatch stopwatch;

        public QuestionNumericWindow(string data, NetworkStream stream, int clientID)
        {
            InitializeComponent();
            ParseQuestion(data);
            this.answerTxtBox.Focus();
            this.msElapsed = 0;
            this.msTotal = 1000;
            this.answered = false;
            this.stream = stream;
            this.clientID = clientID;
            TimerHandler();
            this.stopwatch = new();
            stopwatch.Start();
        }

        private void TimerHandler()
        {
            timer = new System.Timers.Timer(10);

            timer.Elapsed += ChangeTimerLabel;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void ChangeTimerLabel(object source, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.timerLabel.Content = String.Format("Time left: {0} seconds", (msTotal - msElapsed)/100); });

            msElapsed++;

            if (msElapsed > msTotal)
            {
                timer.Stop();
                timer.Dispose();
                TimeExpired();
            }
        }

        private void TimeExpired()
        {
            //now we should wait for the server to send us the results
            App.Current.Dispatcher.Invoke((Action)delegate { this.answerTxtBox.IsEnabled = false; });

            if (!answered) //make sure we sent something
            {
                stopwatch.Stop();
                string message = Constants.PREFIX_ANSWER + clientID + "_0_" + stopwatch.ElapsedMilliseconds;
                byte[] msg = Encoding.ASCII.GetBytes(message);
                stream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent to the server: {0}", message);
            }
            answered = true;

            //now lets wait for the response with information
            Byte[] data;
            data = new Byte[1024];
            String responseData = String.Empty;
            Int32 bytes;
            while (true) //wait for the first question
            {
                bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                if (responseData.StartsWith(Constants.PREFIX_FINALANSWERS)) //handle question numeric
                {
                    ShowFinalAnswers(responseData);
                    break;
                }
            }

        }

        private void ShowFinalAnswers(string data)
        {
            string[] splitData = data.Split('_');
            App.Current.Dispatcher.Invoke((Action)delegate { this.p1label.Content = String.Format("P1 answer and time: {0}, {1}", splitData[1], splitData[3]); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.p2label.Content = String.Format("P2 answer and time: {0}, {1}", splitData[2], splitData[4]); });
            App.Current.Dispatcher.Invoke((Action)delegate { this.playerWinlabel.Content = String.Format("The right answer was: {0} --> P{1} Wins!", splitData[5], Int32.Parse(splitData[6]) + 1); });

            //wait 5s so the clients can see the final answers
            Thread.Sleep(5000);
            App.Current.Dispatcher.Invoke((Action)delegate { this.Close(); });
        }

        private void ParseQuestion(string data)
        {
            string[] splitData = data.Split('_');
            this.questionLabel.Content = splitData[1];
        }

        private void SubmitAnswer(string answer)
        {
            //here send our response to the server
            this.answerTxtBox.IsEnabled = false;
            stopwatch.Stop();
            if(!Int32.TryParse(answerTxtBox.Text, out int ans))
            {
                ans = 0; //if invalid number pass an 0
            }
            string message = Constants.PREFIX_ANSWER + clientID + "_" + ans.ToString() + "_" + stopwatch.ElapsedMilliseconds;
            byte[] msg = Encoding.ASCII.GetBytes(message);
            stream.Write(msg, 0, msg.Length);
            Console.WriteLine("Sent to the server: {0}", message);
        }

        private void answerTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !answered)
            {
                SubmitAnswer(this.answerTxtBox.Text);
                answered = true;
            }
        }

        private void submitBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!answered)
            {
                SubmitAnswer(this.answerTxtBox.Text);
                answered = true;
            }
        }
    }
}

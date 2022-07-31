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

namespace client
{
    /// <summary>
    /// Interakční logika pro QuestionABCDWindow.xaml
    /// </summary>
    public partial class QuestionABCDWindow : Window
    {
        private int msElapsed;
        private int msTotal;
        private bool answered;
        private System.Timers.Timer timer;
        private NetworkStream stream;
        private int clientID;

        public QuestionABCDWindow(string data, NetworkStream stream, int clientID)
        {
            InitializeComponent();
            ParseQuestion(data);
            this.msElapsed = 0;
            this.msTotal = 1000;
            this.stream = stream;
            this.clientID = clientID;
            TimerHandler();
        }

        private void TimerHandler()
        {
            timer = new System.Timers.Timer(10);

            timer.Elapsed += ChangeTimerLabel;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void ChangeTimerLabel(object? source, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate { this.timerLabel.Content = String.Format("Time left: {0} seconds", (msTotal - msElapsed) / 100); });

            msElapsed++;

            if (msElapsed > msTotal)
            {
                timer.Stop();
                timer.Dispose();
            }

            if(msElapsed > msTotal)
            {
                timer.Stop();
                timer.Dispose();
                TimeExpired();
            }
        }

        private void TimeExpired()
        {
            //now we should wait for the server to send us the results
            /*
            App.Current.Dispatcher.Invoke((Action)delegate { answerAbtn.IsEnabled = false; });
            App.Current.Dispatcher.Invoke((Action)delegate { answerBbtn.IsEnabled = false; });
            App.Current.Dispatcher.Invoke((Action)delegate { answerCbtn.IsEnabled = false; });
            App.Current.Dispatcher.Invoke((Action)delegate { answerDbtn.IsEnabled = false; });
            */

            if (!answered) //make sure we sent something
            {
                string message = Constants.PREFIX_ANSWER + clientID + "_0"; //no answer
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
                if (responseData.StartsWith(Constants.PREFIX_FINALANSWERS)) //handle answers
                {
                    ShowFinalAnswers(responseData);
                    break;
                }
            }

        }
        private void ShowFinalAnswers(string data)
        {
            string[] splitData = data.Split('_');
            //finalanswers_correctANS_P1ANS_P2ANS
            string correctAnswer = splitData[1];
            string p1Answer = splitData[2];
            string p2Answer = splitData[3];

            List<Button> buttons = new List<Button> { this.answerAbtn, this.answerBbtn, this.answerCbtn, this.answerDbtn };

            bool bothCorrect = (p1Answer == p2Answer);

            foreach(Button button in buttons)
            {
                App.Current.Dispatcher.Invoke((Action)delegate {
                    if (button.Content.ToString() == correctAnswer)
                    {
                        button.BorderThickness = new Thickness(5);
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 255, 50));
                    }
                    else if (button.Content.ToString() == p1Answer && bothCorrect)
                    {
                        LinearGradientBrush linGrBrush = new LinearGradientBrush(
                            Color.FromArgb(255, 255, 0, 0),   // Opaque red
                            Color.FromArgb(255, 0, 0, 255), 0);  // Opaque blue
                                                                 //2 colors
                        button.Background = linGrBrush;
                    }
                    else if (button.Content.ToString() == p1Answer)
                    {
                        button.Background = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                        //red color
                    }
                    else if (button.Content.ToString() == p2Answer)
                    {
                        button.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
                        //blue color
                    }
                });
            }

            //wait 5s so the clients can see the final answers
            Thread.Sleep(5000);
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
            string[] splitData = data.Split('_');
            this.questionLabel.Content = splitData[1];

            List<Button> availableButtons = new List<Button> { this.answerAbtn, this.answerBbtn, this.answerCbtn, this.answerDbtn };
            
            for(int i = 2; i < splitData.Length; i++)
            {
                Button rand = PickRandomButton(availableButtons);
                rand.Content = splitData[i];
                availableButtons.Remove(rand);
            }
        }

        private void HandleClick(string? answer, object sender)
        {
            if (answered) return;

            answered = true;

            Brush[] brushes = new Brush[Constants.MAX_PLAYERS];
            brushes[0] = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); //red
            brushes[1] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)); //blue

            (sender as Button).Background = brushes[clientID];

            string message = Constants.PREFIX_ANSWER + clientID + "_" + answer; //no answer
            byte[] msg = Encoding.ASCII.GetBytes(message);
            stream.Write(msg, 0, msg.Length);
            Console.WriteLine("Sent to the server: {0}", message);
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

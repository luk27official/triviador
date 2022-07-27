using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private Timer timer;

        public QuestionABCDWindow(string data)
        {
            InitializeComponent();
            ParseQuestion(data);
            this.msElapsed = 0;
            this.msTotal = 1000;
            TimerHandler();
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
            App.Current.Dispatcher.Invoke((Action)delegate { this.timerLabel.Content = String.Format("Time left: {0} seconds", (msTotal - msElapsed)/100);});

            msTotal++;

            if(msElapsed > msTotal)
            {
                timer.Stop();
                timer.Dispose();
                //now we should wait for the server to send us the results
                App.Current.Dispatcher.Invoke((Action)delegate { answerAbtn.IsEnabled = false; });
                App.Current.Dispatcher.Invoke((Action)delegate { answerBbtn.IsEnabled = false; });
                App.Current.Dispatcher.Invoke((Action)delegate { answerCbtn.IsEnabled = false; });
                App.Current.Dispatcher.Invoke((Action)delegate { answerDbtn.IsEnabled = false; });
            }
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

        private void answerAbtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void answerBbtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void answerCbtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void answerDbtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

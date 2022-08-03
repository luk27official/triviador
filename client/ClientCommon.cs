using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Commons;

namespace client
{
    internal static class ClientCommon
    {
        public static void HandleEnemyDisconnect()
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Exclamation;
            MessageBoxResult result;

            result = MessageBox.Show(Constants.GAMEOVER_DISCONNECT, "Game over!", button, icon, MessageBoxResult.Yes);
            App.Current.Dispatcher.Invoke((Action)delegate {
                System.Windows.Application.Current.Shutdown();
                Environment.Exit(0);
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Configuration;

namespace VPNTorrent
{
    class Helper
    {
        private static int ConsoleMaxSize = 10000;
        public static ScrollViewer view;

        public static void doLog(String text, Boolean bDoLog = true)
        {
            if (bDoLog == false) {
                return;
            }

            if (view.Content != null && view.ToString().Length > ConsoleMaxSize) {
                view.Content = "";
            }

            view.Content += "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine;
            view.ScrollToBottom();
        }

        public static string FlattenException(Exception exception) {
            var stringBuilder = new StringBuilder();

            while (exception != null) {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}

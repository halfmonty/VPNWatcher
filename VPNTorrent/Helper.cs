using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace VPNTorrent
{
    class Helper
    {
        public static void doLog(ScrollViewer view, String text, Boolean bDoLog, int nConsoleMaxSize)
        {
            if (bDoLog == false) {
                return;
            }

            if (view.Content != null && view.ToString().Length > nConsoleMaxSize) {
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

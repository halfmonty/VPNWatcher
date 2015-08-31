using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Web.Script.Serialization;
using System.Reflection;

namespace VPNWatcher
{
    public static class UpdateChecker
    {
        public static bool checkForNewerVersion(System.Windows.Controls.Label updateLable)
        {
            if (isOnline())
            {
                Helper.doLog("isonline = true, checking github", Properties.Settings.Default.DebugMode);
                var json = new JavaScriptSerializer();
                var data = json.Deserialize<Dictionary<string, dynamic>>(getDataStringFromGithub());
                Helper.doLog(data["size"] + "", Properties.Settings.Default.DebugMode);

                if (data["size"] != getFileSize())
                {
                    updateLable.Content = "Update";
                    updateLable.ToolTip = "A new update is available on github.com/halfmonty/VPNwatcher";
                    return true;
                }
            }
            
            Helper.doLog("isonline = false, not checking github", Properties.Settings.Default.DebugMode);
            Helper.doLog(getFileSize().ToString());
            return false;
        }

        private static bool isOnline()
        {
            Uri url = new Uri("http://google.com");
            PingReply reply = new Ping().Send(url.Host, 3000);
            return (reply.Status == IPStatus.Success);
        }

        private static string getDataStringFromGithub()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Properties.Settings.Default.GithubUrl);
            request.Method = "GET";
            request.UserAgent = "Foo";

            WebResponse response = request.GetResponse(); //Error Here
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }

        private static int getFileSize()
        {           
            return (int)new FileInfo(Assembly.GetExecutingAssembly().Location).Length;
        }
    }
}

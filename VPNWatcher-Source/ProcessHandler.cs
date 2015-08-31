using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace VPNWatcher
{
    public static class ProcessHandler
    {
        // these apps should never get killed
        private static ArrayList listWhilelist = new ArrayList() { "explorer", "csrss", "dwm", "services" };
        private static List<string> _applicationsClosed = new List<string>();

        // simply clears out the List of closed applications
        public static void clearApplicationsList() {
            _applicationsClosed.Clear();
        }

        // Open any apps in the applicationsClosed list
        public static void openApps()
        {
            foreach (var application in _applicationsClosed)
            {
                Process.Start(application);
                Helper.doLog("Re-launching " + application);
            }
            clearApplicationsList();
        }

        // kill an application by its process name
        public static void closeApps(List<string> listApps)
        {
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
            {
                if (listApps.Contains(p.ProcessName) && !isAppOnWhitelist(p.ProcessName))
                {
                    Helper.doLog("process killed: " + p.ProcessName + " with id " + p.Id);
                    p.Kill();
                    if (!_applicationsClosed.Exists(x => x == p.MainModule.FileName))
                    {
                        _applicationsClosed.Add(p.MainModule.FileName);
                        Helper.doLog("Added " + p.ProcessName + " with id " + p.Id + "to list");
                    }
                    else
                    {
                        Helper.doLog(p.ProcessName + " with id " + p.Id + " already exists in list");
                    }
                }
            }
        }

        // we have a whitelist, check against it before killing
        private static Boolean isAppOnWhitelist(String strApp)
        {
            if (listWhilelist.Contains(strApp))
            {
                Helper.doLog("app " + strApp + " is on whitelist and connot be killed");
                return true;
            }

            return false;
        }
    }
}

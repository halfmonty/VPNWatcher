using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IWshRuntimeLibrary;
using System.Reflection;

namespace VPNWatcher
{
    public static class ShortcutHandler
    {
        // should grab the default startup folder location for shortcuts
        public static string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + Assembly.GetExecutingAssembly().GetName().Name + ".lnk";

        //creates a shortcut for this application
        public static void addToStartup()
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut("" + shortcutPath);

            shortcut.Description = "VPNTorrent Watcher";
            shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
            shortcut.Save();
        }

        //will remove shortcut if it exists
        public static void removeFromStartup()
        {
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
        }

        //return true if shortcut currently exists
        public static bool isInStartup()
        {
            return System.IO.File.Exists(shortcutPath);
        }
    }
}

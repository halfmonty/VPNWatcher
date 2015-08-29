using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UTorrentAPI;

namespace VPNWatcher
{
    public static class UtorrentHandler
    {
        private static UTorrentClient client;
        private static bool _isUtorrentConnected = false;        
        private static List<Torrent> torrentsHandled = new List<Torrent>();

        // setup uTorrent Client and validate connection
        public static bool SetupUtorrentConnection(ConfigHandler _configHandler)
        {
            client = new UTorrentClient(new System.Uri(_configHandler.uTorrentUrl), _configHandler.uTorrentUsername, _configHandler.uTorrentPassword);
            ConnectToUtorrent();
            return IsUtorrentConnected;
        }

        // determine if uTorrent is connected
        public static void ConnectToUtorrent()
        {
            try {
                var test = client.Torrents.Count;
                _isUtorrentConnected = true;
            } catch (Exception e) {
                _isUtorrentConnected = false;
            }
        }

        // public readable quick check for utorrent's current connection status
        public static bool IsUtorrentConnected{
            get { return _isUtorrentConnected; }
        }

        // start any torrents in the torrentsHandled list
        public static void StartUtorrent()
        {
            if (IsUtorrentOpen())
            {
                foreach (var torrent in torrentsHandled)
                {
                    Helper.doLog("Starting Torrent " + torrent.Name);
                    torrent.Start();
                }
                torrentsHandled.Clear();
            }
        }

        // unpauses any torrents in the torrentsHandled list
        public static void UnpauseUtorrent()
        {
            if (IsUtorrentOpen())
            {
                foreach (var torrent in torrentsHandled)
                {
                    Helper.doLog("UnPausing Torrent " + torrent.Name);
                    torrent.Unpause();
                }
                torrentsHandled.Clear();
            }
        }

        // pauses any running torrents and adds them to torrentsHandled list
        public static void PauseUtorrent()
        {          
            if (IsUtorrentOpen())
            {
                foreach (var torrent in client.Torrents)
                {
                    if (!torrent.StatusMessage.ToLower().Contains("finished") &&
                        !torrent.StatusMessage.ToLower().Contains("error") &&
                        !torrent.StatusMessage.ToLower().Contains("stopped") &&
                        !torrent.StatusMessage.ToLower().Contains("paused"))
                    {
                        if (!torrentsHandled.Exists(x => x.Name == torrent.Name))
                        {
                            Helper.doLog("Pausing Torrent ");
                            torrentsHandled.Add(torrent);
                            torrent.Pause();
                        }
                    }
                }
            }
        }

        // stops any running torrents and adds them to the torrentsHandled list
        public static void StopUtorrent()
        {
            if (IsUtorrentOpen())
            {
                foreach (var torrent in client.Torrents)
                {
                    if (!torrent.StatusMessage.ToLower().Contains("finished") &&
                        !torrent.StatusMessage.ToLower().Contains("error") &&
                        !torrent.StatusMessage.ToLower().Contains("stopped") &&
                        !torrent.StatusMessage.ToLower().Contains("paused"))
                    {
                        if (!torrentsHandled.Exists(x => x.Name == torrent.Name))
                        {
                            Helper.doLog("Stopping Torrent " + torrent.Name);
                            torrentsHandled.Add(torrent);
                            torrent.Stop();
                        }
                    }
                }
            }
        }

        // returns bool of if uTorrent is open
        public static bool IsUtorrentOpen()
        {
            return (System.Diagnostics.Process.GetProcessesByName("utorrent").Length > 0) ? true : false;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Media.Imaging;
using UTorrentAPI;

namespace VPNWatcher
{
    public static class UtorrentHandler
    {
        private static UTorrentClient client;
        private static bool _isUtorrentConnected = false;        
        private static List<Torrent> torrentsHandled = new List<Torrent>();
        private static string uTorrentUrl;
        public static System.Windows.Controls.Image statusIcon;

        private static bool checkUrl()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Connect("localhost", 23168);
                    return true;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        return false;
                    }
                }
                return false;
            }
        }

        // setup uTorrent Client and validate connection
        public static bool SetupUtorrentConnection()
        {
            uTorrentUrl = Properties.Settings.Default.uTorrentUrl;

            if (!String.IsNullOrWhiteSpace(uTorrentUrl) &&
                !String.IsNullOrWhiteSpace(Properties.Settings.Default.uTorrentUsername) &&
                !String.IsNullOrWhiteSpace(Properties.Settings.Default.uTorrentPassword))
            {
                client = new UTorrentClient(new System.Uri(uTorrentUrl), Properties.Settings.Default.uTorrentUsername, Properties.Settings.Default.uTorrentPassword);
                ConnectToUtorrent();
                return IsUtorrentConnected;
            }
            IsUtorrentConnected = false;
            return IsUtorrentConnected;
        }

        // determine if uTorrent is connected
        public static bool ConnectToUtorrent()
        {
            //if (checkUrl())
            //{
                try
                {
                    var test = client.StorageDirectories.Count();
                    return IsUtorrentConnected = true;
                }
                catch (Exception e)
                {
                    return IsUtorrentConnected = false;
                }
            //}
            //return IsUtorrentConnected = false;
        }

        // public readable quick check for utorrent's current connection status
        public static bool IsUtorrentConnected{
            get { return _isUtorrentConnected; }
            private set { _isUtorrentConnected = value;
            updateIcon();
            }
        }

        // start any torrents in the torrentsHandled list
        public static void StartUtorrent()
        {
            foreach (var torrent in torrentsHandled)
            {
                Helper.doLog("Starting Torrent " + torrent.Name);
                torrent.Start();
            }
            torrentsHandled.Clear();
        }

        // unpauses any torrents in the torrentsHandled list
        public static void UnpauseUtorrent()
        {
            foreach (var torrent in torrentsHandled)
            {
                Helper.doLog("UnPausing Torrent " + torrent.Name);
                torrent.Unpause();
            }
            torrentsHandled.Clear();

        }

        // pauses any running torrents and adds them to torrentsHandled list
        public static void PauseUtorrent()
        {
            foreach (var torrent in client.Torrents)
            {
                if (!torrent.StatusMessage.ToLower().Contains("finished") &&
                    !torrent.StatusMessage.ToLower().Contains("error") &&
                    !torrent.StatusMessage.ToLower().Contains("stopped") &&
                    !torrent.StatusMessage.ToLower().Contains("paused"))
                {
                    if (!torrentsHandled.Exists(x => x.Name == torrent.Name)) {
                        Helper.doLog("Pausing Torrent ");
                        torrentsHandled.Add(torrent);                            
                    }
                    torrent.Pause();
                }
            }
        }

        // stops any running torrents and adds them to the torrentsHandled list
        public static void StopUtorrent()
        {
            foreach (var torrent in client.Torrents)
            {
                if (!torrent.StatusMessage.ToLower().Contains("finished") &&
                    !torrent.StatusMessage.ToLower().Contains("error") &&
                    !torrent.StatusMessage.ToLower().Contains("stopped") &&
                    !torrent.StatusMessage.ToLower().Contains("paused"))
                {
                    if (!torrentsHandled.Exists(x => x.Name == torrent.Name)) {
                        Helper.doLog("Stopping Torrent " + torrent.Name);
                        torrentsHandled.Add(torrent);                            
                    }
                    torrent.Stop();
                }
            }
        }

        // returns bool of if uTorrent is open
        public static bool IsUtorrentOpen()
        {
            return (System.Diagnostics.Process.GetProcessesByName("utorrent").Length > 0) ? true : false;
        }


        public static void updateIcon(){
            if (_isUtorrentConnected == true)
            {
                statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ok.ico"));
                statusIcon.ToolTip = "Connected to Utorrent";
            }
            else if (_isUtorrentConnected == false)
            {
                statusIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/error.ico"));
                statusIcon.ToolTip = "Utorrent not connected";
            }
        }
    }
}

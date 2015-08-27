using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Configuration;
using System.Collections;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.IO;
using UTorrentAPI;

namespace VPNTorrent
{
    /// <summary>
    /// Main appliation
    /// </summary>
    public partial class MainWindow : Window
    {

//-------- Initialization -------//

        // these apps should never get killed
        ArrayList listWhilelist = new ArrayList(){ "explorer", "csrss", "dwm", "services" };
        UTorrentClient client = new UTorrentClient(new System.Uri("http://localhost:23168/gui"), "halfmonty", "J*kupo89");
        
        NotifyIcon m_nIcon = null;
        DispatcherTimer m_dispatcherTimer = null;

        ConfigHandler m_configHandler = null;
        InterfaceHandler m_interfaceHandler = null;

        Boolean m_bIsInitCompleted = false;
        List<Torrent> torrentsHandled = new List<Torrent>();

        public enum STATUS {
            VPN_CONNECTED,
            VPN_NOT_CONNECTED,
            UNDEFINED
        };

        public MainWindow() {
            InitializeComponent();

            m_configHandler = new ConfigHandler(scrollViewerLog);
            m_interfaceHandler = new InterfaceHandler(scrollViewerLog, m_configHandler);

            // read and display config values
            m_configHandler.loadConfigValues();
            showConfigValues();

            setupTimer();

            setupNotifyIcon();

            if (m_configHandler.StartMinimized) {
                doMinimize();
            }

            m_bIsInitCompleted = true;
        }

        // displays the config values previously read to the frontend
        private void showConfigValues()
        {
            Helper.doLog(scrollViewerLog, "showConfigValues", m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);

            textBoxSelectedInterface.Text = m_configHandler.VPNInterfaceName;

            textBoxApps.Clear();
            foreach (String str in m_configHandler.getListApplications())
            {
                textBoxApps.Text += str + Environment.NewLine;
            }
            textBoxApps.Text = textBoxApps.Text.Trim();

            checkBoxMinimized.IsChecked = m_configHandler.StartMinimized;

            checkBoxStrict.IsChecked = m_configHandler.StrictInterfaceHandling;

            comboBoxAction.SelectedIndex = m_configHandler.ActionIndex;

            checkBoxStartup.IsChecked = ShortcutHandler.isInStartup();
        }

        // the minimize-to-tray feature
        private void setupNotifyIcon() {
            m_nIcon = new NotifyIcon();
            m_nIcon.Icon = Properties.Resources.logo;
            m_nIcon.Visible = false;
            m_nIcon.Text = "VPN Watcher"; //TODO: put into resource?

            m_nIcon.DoubleClick += new System.EventHandler(notifyIcon_DoubleClick);
            m_nIcon.ContextMenu = new ContextMenu();
            MenuItem menuOpen = new MenuItem("&Open");
            menuOpen.Click += new System.EventHandler(menuOpen_Click);
            m_nIcon.ContextMenu.MenuItems.Add(menuOpen);

            MenuItem menuExit = new MenuItem("&Exit");
            menuExit.Click += new System.EventHandler(menuExit_Click);
            m_nIcon.ContextMenu.MenuItems.Add(menuExit);
        }

        // setup a timer to periodically check stuff
        private void setupTimer() {
            m_dispatcherTimer = new DispatcherTimer();
            m_dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            m_dispatcherTimer.Interval = new TimeSpan(0, 0, m_configHandler.TimerInSeconds);
            m_dispatcherTimer.Start();
        }
        
//------ Functions -------//

        // minimize the app to the trayicon
        private void doMinimize() {
            Helper.doLog(scrollViewerLog, "doMinimize", m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);

            m_nIcon.Visible = true;
            Hide();
            WindowState = WindowState.Minimized;
        }

        // maximize the app 
        private void doMaximize() {
            Helper.doLog(scrollViewerLog, "doMaximize", m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);

            m_nIcon.Visible = false;
            Show();
            WindowState = WindowState.Normal;

            Activate();
            Focus();
        }

        private void performApplicationAction() {
            int nSelection = m_configHandler.ActionIndex;

            Helper.doLog(scrollViewerLog, "performApplicationAction " + nSelection, m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);
            if (nSelection == 0) {
                closeApp();
            } else if (nSelection == 1) {
                pauseUtorrent();
            } else if (nSelection == 2) {
                stopUtorrent();
            }
        }

        private void performVpnConnectedApplicationAction()
        {
            int nSelection = m_configHandler.ActionIndex;

            Helper.doLog(scrollViewerLog, "performApplicationAction " + nSelection, m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);
            if (nSelection == 0)
            {
                openApp();
            }
            else if (nSelection == 1)
            {
                unpauseUtorrent();
            }
            else if (nSelection == 2)
            {
                startUtorrent();
            }       
        }

        private void openApp()
        {
            if (!isUtorrentOpen())
            {
                Helper.doLog(scrollViewerLog, "uTorrent is not open, launching now", true, m_configHandler.ConsoleMaxSize);
                Process.Start(@"C:\Users\halfmonty\AppData\Roaming\uTorrent\uTorrent.exe");
            }
        }

        private void startUtorrent()
        {
            if (isUtorrentOpen())
            {
                foreach (var torrent in torrentsHandled)
                {
                    Helper.doLog(scrollViewerLog, "Starting Torrent " + torrent.Name, true, m_configHandler.ConsoleMaxSize);
                    torrent.Start();
                }
                torrentsHandled.Clear();
            }
        }

        private void unpauseUtorrent()
        {
            if (isUtorrentOpen())
            {
                foreach (var torrent in torrentsHandled)
                {
                    Helper.doLog(scrollViewerLog, "UnPausing Torrent " + torrent.Name, true, m_configHandler.ConsoleMaxSize);
                    torrent.Unpause();
                }
                torrentsHandled.Clear();
            }
        }

        private void pauseUtorrent() 
        {
            //doLog("pauseUtorrent");            
            if (isUtorrentOpen())
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
                            Helper.doLog(scrollViewerLog, "Pausing Torrent " + torrent.Name, true, m_configHandler.ConsoleMaxSize);
                            torrentsHandled.Add(torrent);
                            torrent.Pause();
                        }
                    }
                }
            }
        }

        private void stopUtorrent()
        {            
            //doLog("stopUtorrent");
            if (isUtorrentOpen())
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
                            Helper.doLog(scrollViewerLog, "Stopping Torrent " + torrent.Name, true, m_configHandler.ConsoleMaxSize);
                            torrentsHandled.Add(torrent);
                            torrent.Stop();
                        }
                    }
                }
            }
        }

        private bool isUtorrentOpen()
        {
            List<String> listApps = getApps();
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
            {
                if (listApps.Contains(p.ProcessName) && !isAppOnWhitelist(p.ProcessName))
                {
                    return true;
                }
            }
            return false;
        }

        // kill an application by its process name
        private void closeApp() {
            List<String> listApps = getApps();
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses()) {
                if (listApps.Contains(p.ProcessName) && !isAppOnWhitelist(p.ProcessName)) {
                    Helper.doLog(scrollViewerLog, "process found " + p.ProcessName + " with id " + p.Id, true, m_configHandler.ConsoleMaxSize);
                    p.Kill();
                 }
            }
        }

        // read the apps from the textbox. TODO: data binding?
        private List<String> getApps() {
            List<String> listApps = new List<String>();

            if (textBoxApps.Text != null && textBoxApps.Text.Trim().Length > 1) {
                String[] arr = textBoxApps.Text.Trim().Split(Environment.NewLine.ToCharArray());
                foreach (String str in arr) {
                    if (str.Trim() != "") {
                        Helper.doLog(scrollViewerLog, "getApps " + str, m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);
                        listApps.Add(str.Trim());
                    }              
                }
            }

            return listApps;
        }

        // we have a whitelist, check against it before killing
        private Boolean isAppOnWhitelist(String strApp) {
            Helper.doLog(scrollViewerLog, "isAppOnWhitelist " + strApp, m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);

            if (listWhilelist.Contains(strApp)) {
                 Helper.doLog(scrollViewerLog, "app " + strApp + " is on whitelist and connet be killed", true, m_configHandler.ConsoleMaxSize);
                return true;
            }

            return false;
        }

        // callback funtcion by the timer
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            Helper.doLog(scrollViewerLog, "dispatcherTimer_Tick", m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);

            try {
                List<NetworkInterface> listNetworks = m_interfaceHandler.getActiveNetworkInterfaces();
                if (listBoxInterfaces.Items.Count != listNetworks.Count) {
                    Helper.doLog(scrollViewerLog, "network list changed, updating ...", true, m_configHandler.ConsoleMaxSize);
                    showNetworkInterfaces(listNetworks);
                }

                NetworkInterface selected = m_interfaceHandler.getNetworkDetails(m_configHandler.VPNInterfaceID);
                if (m_configHandler.StrictInterfaceHandling) {
                    Helper.doLog(scrollViewerLog, "dispatcherTimer_Tick strictmode VPN=" + m_configHandler.VPNInterfaceID + " select=" + selected, m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);
                    if (m_configHandler.VPNInterfaceID != "" && (selected == null || !m_interfaceHandler.isNetworkConnected(selected))) {
                        performApplicationAction();
                        iconAction(STATUS.VPN_NOT_CONNECTED);
                    } else if (m_configHandler.VPNInterfaceID != "" && selected != null && m_interfaceHandler.isNetworkConnected(selected)) {
                        performVpnConnectedApplicationAction();
                        iconAction(STATUS.VPN_CONNECTED);
                    }
                } else {
                    if (selected == null) {
                        iconAction(STATUS.UNDEFINED);
                    } else if (selected != null && !m_interfaceHandler.isNetworkConnected(selected)) {
                        performApplicationAction();
                        iconAction(STATUS.VPN_NOT_CONNECTED);
                    } else if (selected != null && m_interfaceHandler.isNetworkConnected(selected)) {
                        performVpnConnectedApplicationAction();
                        iconAction(STATUS.VPN_CONNECTED);
                    }
                }
            } catch (Exception excep) {
                Helper.doLog(scrollViewerLog, "Exception\r\n" + Helper.FlattenException(excep), m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);
            }
        }

        // change the icon according to the VPN status
        private void iconAction(STATUS vpnStatus) {
            if (vpnStatus == STATUS.VPN_CONNECTED){
                imageStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ok.ico"));
                imageStatus.ToolTip = "VPN connected";
            } else if (vpnStatus == STATUS.VPN_NOT_CONNECTED) {
                imageStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/error.ico"));
                imageStatus.ToolTip = "VPN not connected";
            }  else {
                imageStatus.Source = null ;
                imageStatus.ToolTip = "";
            }
        }

        // insert the connected networks into the listbox
        private void showNetworkInterfaces(List<NetworkInterface> listInterfaces) {
            Helper.doLog(scrollViewerLog, "showNetworkInterfaces", m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);

            listBoxInterfaces.Items.Clear();
            foreach (NetworkInterface i in listInterfaces)  {
                String description = i.Description;
                if (i.GetIPProperties() != null && i.GetIPProperties().UnicastAddresses != null && i.GetIPProperties().UnicastAddresses.Count > 0) {
                    description += " " + i.GetIPProperties().UnicastAddresses[0].Address;
                }
                listBoxInterfaces.Items.Add(description);
            }
        }
        

        // user clicked on the save button: look up the dtails of the selected interface and save it
        private void buttonSetVPN_Click(object sender, RoutedEventArgs e) {
            Object objSelected = listBoxInterfaces.SelectedValue;
            if (objSelected != null) {
                String strID = m_interfaceHandler.findSelectedInterface(objSelected.ToString());
                if (strID != null) {
                    m_configHandler.VPNInterfaceID = strID;
                    textBoxSelectedInterface.Text = m_interfaceHandler.getNetworkDetails(strID).Description;
                    m_configHandler.VPNInterfaceName = textBoxSelectedInterface.Text;
                    m_configHandler.saveValue(VPNTorrent.ConfigHandler.SETTING.VPN_ID_AND_NAME);
                } else {
                    Helper.doLog(scrollViewerLog, "selected interface not found " + strID, true, m_configHandler.ConsoleMaxSize);
                }
            }
        }

//////////////////////////////////////
//---------- UI Events -------------//
//////////////////////////////////////

        private void checkBoxMinimized_Checked(object sender, RoutedEventArgs e)
        {
            m_configHandler.StartMinimized = true;
            m_configHandler.saveValue(ConfigHandler.SETTING.START_MINIMIZED);
        }

        private void checkBoxMinimized_Unchecked(object sender, RoutedEventArgs e)
        {
            m_configHandler.StartMinimized = false;
            m_configHandler.saveValue(ConfigHandler.SETTING.START_MINIMIZED);
        }

        private void checkBoxStrict_Checked(object sender, RoutedEventArgs e)
        {
            m_configHandler.StrictInterfaceHandling = true;
            m_configHandler.saveValue(ConfigHandler.SETTING.STRICT);
        }

        private void checkBoxStrict_Unchecked(object sender, RoutedEventArgs e)
        {
            m_configHandler.StrictInterfaceHandling = false;
            m_configHandler.saveValue(ConfigHandler.SETTING.STRICT);
        }

        //this should be replaced by C# data binding
        private void onAppsChange(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

            if (!m_bIsInitCompleted)
            {
                return;
            }
            List<String> listApps = new List<String>();

            String strTmp;
            int nZeilen = textBoxApps.LineCount;
            for (int i = 0; i < nZeilen; i++)
            {
                strTmp = textBoxApps.GetLineText(i).Trim();
                if (strTmp.Length > 4)
                {
                    listApps.Add(strTmp);
                }
            }

            m_configHandler.setListApplications(listApps);
            m_configHandler.saveValue(VPNTorrent.ConfigHandler.SETTING.APPLICATIONS);
        }

        //this also should be replaced by a C# data binding
        private void onActionChange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            m_configHandler.ActionIndex = comboBoxAction.SelectedIndex;
            Helper.doLog(scrollViewerLog, "saving combo box selection " + comboBoxAction.SelectedIndex, true, m_configHandler.ConsoleMaxSize);
            m_configHandler.saveValue(VPNTorrent.ConfigHandler.SETTING.CHOSEN_ACTION);
        }

        private void checkBoxStartup_Checked(object sender, RoutedEventArgs e)
        {
            ShortcutHandler.addToStartup();
        }

        private void checkBoxStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            ShortcutHandler.removeFromStartup();
        }

////////////////////////////////////////////////
//---------- Minimized UI Events -------------//
////////////////////////////////////////////////

        // callback when doubleclick on trayicon
        private void notifyIcon_DoubleClick(object sender, System.EventArgs e)
        {
            doMaximize();
        }

        // before closing always remove the trayicon and copy the log to clipboard in debugmode
        private void onExit(object sender, System.EventArgs e)
        {
            m_nIcon.Visible = false;

            if (m_configHandler.DebugMode)
            {
                System.Windows.Forms.Clipboard.SetText(scrollViewerLog.Content.ToString());
            }
        }

        private void menuOpen_Click(object sender, System.EventArgs e)
        {
            doMaximize();
        }

        private void menuExit_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void onStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                doMinimize();
            }
        }
    }
}

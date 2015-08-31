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
using System.Threading;

namespace VPNWatcher
{
    /// <summary>
    /// Main appliation
    /// </summary>
    public partial class MainWindow : Window
    {

//-------- Initialization -------//

        NotifyIcon m_nIcon = null;
        DispatcherTimer m_dispatcherTimer = null;

        InterfaceHandler m_interfaceHandler = null;
        List<NetworkInterface> listNetworks;
        NetworkInterface selectedNetworkInterface;

        Boolean m_bIsInitCompleted = false;
        Stopwatch watch = new Stopwatch();

        public enum STATUS {
            VPN_CONNECTED,
            VPN_NOT_CONNECTED
        };

        public MainWindow() {
            InitializeComponent();
            init();
            UpdateChecker.checkForNewerVersion(updateNotifier);

            // temporary workaround... will come up with something better
            if (Properties.Settings.Default.uTorrentControlEnabled)
            {
                UtorrentHandler.SetupUtorrentConnection();
            }

        }

        // initialize all custom variables
        private void init()
        {
            m_interfaceHandler = new InterfaceHandler(scrollViewerLog);
            Helper.view = scrollViewerLog;
            UtorrentHandler.statusIcon = imageUtorrentStatus;
            checkBoxStartup.IsChecked = ShortcutHandler.isInStartup();
            
            setupTimer();
            setupNotifyIcon();

            if (Properties.Settings.Default.StartMinimized)
                doMinimize();

            m_bIsInitCompleted = true;
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
            m_dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 2000);
            m_dispatcherTimer.Start();
        }
        

//------ Functions -------//


        // callback function by the timer
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            watch.Start();
            Helper.doLog("dispatcherTimer_Tick", Properties.Settings.Default.DebugMode);

            try
            {
                updateNetworkInterfaces();
                selectedNetworkInterface = m_interfaceHandler.getNetworkDetails(Properties.Settings.Default.VPNInterfaceID);
                bool? status = null;

                if (Properties.Settings.Default.StrictInterfaceHandling) {
                    status = strictVPNCheck(selectedNetworkInterface);
                } else {
                    status = regularVPNCheck(selectedNetworkInterface);
                }
                iconVpnStatus(status);
            }
            catch (Exception excep)
            {
                Helper.doLog("Exception\r\n" + Helper.FlattenException(excep), Properties.Settings.Default.DebugMode);
            }

            Helper.doLog("" + watch.ElapsedMilliseconds, Properties.Settings.Default.DebugMode);
            watch.Stop();
            watch.Reset();            
        }

        // update UI with any missing network interfaces
        private void updateNetworkInterfaces()
        {
            listNetworks = m_interfaceHandler.getActiveNetworkInterfaces();
            if (listBoxInterfaces.Items.Count != listNetworks.Count)
            {
                Helper.doLog("network list changed, updating ...");
                showNetworkInterfaces(listNetworks);
            }
        }

        // insert the connected networks into the listbox
        private void showNetworkInterfaces(List<NetworkInterface> listInterfaces)
        {
            Helper.doLog("showNetworkInterfaces", Properties.Settings.Default.DebugMode);

            listBoxInterfaces.Items.Clear();
            foreach (NetworkInterface i in listInterfaces)
            {
                String description = i.Description;
                if (i.GetIPProperties() != null && i.GetIPProperties().UnicastAddresses != null && i.GetIPProperties().UnicastAddresses.Count > 0)
                {
                    description += " " + i.GetIPProperties().UnicastAddresses[0].Address;
                }
                listBoxInterfaces.Items.Add(description);
            }
        }

        // Check if VPN is connected
        private bool strictVPNCheck(NetworkInterface selectedNetworkInterface)
        {
            Helper.doLog("dispatcherTimer_Tick strictmode VPN=" + Properties.Settings.Default.VPNInterfaceID + " select=" + selectedNetworkInterface, Properties.Settings.Default.DebugMode);
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.VPNInterfaceID) && selectedNetworkInterface != null && m_interfaceHandler.isNetworkConnected(selectedNetworkInterface))
            {
                performVpnConnectedApplicationAction();
                iconAction(STATUS.VPN_CONNECTED);
                return true;
            }
            return false;
        }

        // Check if VPN is connected
        private bool? regularVPNCheck(NetworkInterface selectedNetworkInterface)
        {
            if (selectedNetworkInterface == null){
                return null;
            } else if (selectedNetworkInterface != null && !m_interfaceHandler.isNetworkConnected(selectedNetworkInterface)) {
                performApplicationAction();
                return false;
            }else if (selectedNetworkInterface != null && m_interfaceHandler.isNetworkConnected(selectedNetworkInterface)) {
                performVpnConnectedApplicationAction();
                return true;
            }
            return null;
        }

        // performed when VPN disconnects
        private void performApplicationAction() {
            ProcessHandler.closeApps(getApps());

            if (UtorrentHandler.IsUtorrentConnected && Properties.Settings.Default.uTorrentControlEnabled) {
                if (Properties.Settings.Default.uTorrentStop)
                {
                    UtorrentHandler.StopUtorrent();
                } else {
                    UtorrentHandler.PauseUtorrent();
                }
            }
        }

        // performed when VPN connects
        private void performVpnConnectedApplicationAction()
        {
            //Helper.doLog(scrollViewerLog, "performApplicationAction " + nSelection, m_configHandler.DebugMode, m_configHandler.ConsoleMaxSize);
            if (Properties.Settings.Default.ChosenActionIndex == 0){
                ProcessHandler.clearApplicationsList();
            } else {
                ProcessHandler.openApps();
            }

            if (UtorrentHandler.IsUtorrentConnected && Properties.Settings.Default.uTorrentControlEnabled)
            {
                if (Properties.Settings.Default.uTorrentStop)
                {
                    UtorrentHandler.StartUtorrent();
                } else {
                    UtorrentHandler.UnpauseUtorrent();
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
                        Helper.doLog("getApps " + str, Properties.Settings.Default.DebugMode);
                        listApps.Add(str.Trim());
                    }              
                }
            }
            return listApps;
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

        private void iconVpnStatus(bool? isVpnConnected)
        {
            if (isVpnConnected == true) {
                imageStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ok.ico"));
                imageStatus.ToolTip = "VPN Connected";
            } else if (isVpnConnected == false) {
                imageStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/error.ico"));
                imageStatus.ToolTip = "VPN not connected";
            } else {
                imageStatus.Source = null;
                imageStatus.ToolTip = null;
            }
        }



//////////////////////////////////////
//---------- UI Events -------------//
//////////////////////////////////////

        // user clicked on the save button: look up the dtails of the selected interface and save it
        private void buttonSetVPN_Click(object sender, RoutedEventArgs e)
        {
            Object objSelected = listBoxInterfaces.SelectedValue;
            if (objSelected != null)
            {
                String strID = m_interfaceHandler.findSelectedInterface(objSelected.ToString());
                if (strID != null)
                {
                    Properties.Settings.Default.VPNInterfaceID = strID;
                    textBoxSelectedInterface.Text = m_interfaceHandler.getNetworkDetails(strID).Description;
                    Properties.Settings.Default.VPNInterfaceName = textBoxSelectedInterface.Text;
                }
                else
                {
                    Helper.doLog("selected interface not found " + strID);
                }
            }
        }

        private void checkBoxStartup_Checked(object sender, RoutedEventArgs e)
        {
            ShortcutHandler.addToStartup();
        }

        private void checkBoxStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            ShortcutHandler.removeFromStartup();
        }

        private void utorrentEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            UtorrentHandler.SetupUtorrentConnection();
        }

        private void utorrentEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            imageUtorrentStatus.Source = null;
        }

        private void updateInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            m_dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, Properties.Settings.Default.TimerInMilliSeconds);
        }


        // Save Config When Closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
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

            if (Properties.Settings.Default.DebugMode)
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

        // minimize the app to the trayicon
        private void doMinimize()
        {
            Helper.doLog("doMinimize", Properties.Settings.Default.DebugMode);

            m_nIcon.Visible = true;
            Hide();
            WindowState = WindowState.Minimized;
        }

        // maximize the app 
        private void doMaximize()
        {
            Helper.doLog("doMaximize", Properties.Settings.Default.DebugMode);

            m_nIcon.Visible = false;
            Show();
            WindowState = WindowState.Normal;

            Activate();
            Focus();
        }

    }
}

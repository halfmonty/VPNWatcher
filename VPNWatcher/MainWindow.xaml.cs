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

        public ConfigHandler m_configHandler = null;
        InterfaceHandler m_interfaceHandler = null;
        List<NetworkInterface> listNetworks;
        NetworkInterface selectedNetworkInterface;

        Boolean m_bIsInitCompleted = false;

        public enum STATUS {
            VPN_CONNECTED,
            VPN_NOT_CONNECTED,
            UNDEFINED
        };

        public MainWindow() {
            InitializeComponent();

            m_configHandler = new ConfigHandler(scrollViewerLog);
            m_interfaceHandler = new InterfaceHandler(scrollViewerLog, m_configHandler);
            Helper.view = scrollViewerLog;

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
            Helper.doLog("showConfigValues", m_configHandler.DebugMode);
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
            CheckBoxUtorrentEnabled.IsChecked = m_configHandler.uTorrentControlEnabled;
            textBoxUtorrentUrl.Text = m_configHandler.uTorrentUrl;
            textBoxUtorrentUsername.Text = m_configHandler.uTorrentUsername;
            textBoxUtorrentPassword.Password = m_configHandler.uTorrentPassword;
            RadioButtonPause.IsChecked = !m_configHandler.uTorrentStop;
        }

        // the minimize-to-tray feature
        private void setupNotifyIcon() {
            m_nIcon = new NotifyIcon();
            m_nIcon.Icon = Properties.Resources.logo;
            m_nIcon.Visible = false;
            m_nIcon.Text = "VPN Watcher"; //ddTODO: put into resource?

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


        // callback function by the timer
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Helper.doLog("dispatcherTimer_Tick", m_configHandler.DebugMode);

            try
            {
                updateNetworkInterfaces();
                selectedNetworkInterface = m_interfaceHandler.getNetworkDetails(m_configHandler.VPNInterfaceID);

                if (m_configHandler.StrictInterfaceHandling) {
                    strictVPNCheck(selectedNetworkInterface);
                } else {
                    regularVPNCheck(selectedNetworkInterface);
                }
            }
            catch (Exception excep)
            {
                Helper.doLog("Exception\r\n" + Helper.FlattenException(excep), m_configHandler.DebugMode);
            }
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
            Helper.doLog("showNetworkInterfaces", m_configHandler.DebugMode);

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
            Helper.doLog("dispatcherTimer_Tick strictmode VPN=" + m_configHandler.VPNInterfaceID + " select=" + selectedNetworkInterface, m_configHandler.DebugMode);
            if (!String.IsNullOrWhiteSpace(m_configHandler.VPNInterfaceID) && selectedNetworkInterface != null && m_interfaceHandler.isNetworkConnected(selectedNetworkInterface)) {
                performVpnConnectedApplicationAction();
                iconAction(STATUS.VPN_CONNECTED);
                return true;
            }
            return false;
        }

        // Check if VPN is connected
        private void regularVPNCheck(NetworkInterface selectedNetworkInterface)
        {
            if (selectedNetworkInterface == null){
                iconAction(STATUS.UNDEFINED);
            } else if (selectedNetworkInterface != null && !m_interfaceHandler.isNetworkConnected(selectedNetworkInterface)) {
                performApplicationAction();
                iconAction(STATUS.VPN_NOT_CONNECTED);
            }else if (selectedNetworkInterface != null && m_interfaceHandler.isNetworkConnected(selectedNetworkInterface)) {
                performVpnConnectedApplicationAction();
                iconAction(STATUS.VPN_CONNECTED);
            }
        }

        // performed when VPN disconnects
        private void performApplicationAction() {
            ProcessHandler.closeApps(getApps());

            if (UtorrentHandler.IsUtorrentConnected && m_configHandler.uTorrentControlEnabled) {
                if (m_configHandler.uTorrentStop) {
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
            if (m_configHandler.ActionIndex == 0){
                ProcessHandler.clearApplicationsList();
            } else {
                ProcessHandler.openApps();
            }

            if (UtorrentHandler.IsUtorrentConnected && m_configHandler.uTorrentControlEnabled) {
                if (m_configHandler.uTorrentStop) {
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
                        Helper.doLog("getApps " + str, m_configHandler.DebugMode);
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

        // Change icon according to uTorrent Enabled Status
        private void iconUtorrentConnected(bool? isEnabled) {
            if (isEnabled == true) {
                imageUtorrentStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ok.ico"));
                imageStatus.ToolTip = "Connected to Utorrent";
            } else if (isEnabled == false) {
                imageUtorrentStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/error.ico"));
                imageStatus.ToolTip = "Utorrent not connected";
            } else {
                imageUtorrentStatus.Source = null;
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
                    m_configHandler.VPNInterfaceID = strID;
                    textBoxSelectedInterface.Text = m_interfaceHandler.getNetworkDetails(strID).Description;
                    m_configHandler.VPNInterfaceName = textBoxSelectedInterface.Text;
                    m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.VPN_ID_AND_NAME);
                }
                else
                {
                    Helper.doLog("selected interface not found " + strID);
                }
            }
        }

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
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.APPLICATIONS);
        }

        //this also should be replaced by a C# data binding
        private void onActionChange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            m_configHandler.ActionIndex = comboBoxAction.SelectedIndex;
            Helper.doLog("saving combo box selection " + comboBoxAction.SelectedIndex);
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.CHOSEN_ACTION);
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
            m_configHandler.uTorrentControlEnabled = true;
            Helper.doLog("saving uTorrent Enabled " + comboBoxAction.SelectedIndex);
            m_configHandler.saveValue(ConfigHandler.SETTING.UTORRENT_ENABLED);
            iconUtorrentConnected(UtorrentHandler.SetupUtorrentConnection(m_configHandler));
        }

        private void utorrentEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            m_configHandler.uTorrentControlEnabled = false;
            Helper.doLog("saving uTorrent Disabled " + comboBoxAction.SelectedIndex);
            m_configHandler.saveValue(ConfigHandler.SETTING.UTORRENT_ENABLED);
            iconUtorrentConnected(null);
        }

        private void textBoxUtorrentUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            m_configHandler.uTorrentUrl = textBoxUtorrentUrl.Text;
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.UTORRENT_URL);
        }

        private void textBoxUtorrentUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            m_configHandler.uTorrentUsername = textBoxUtorrentUsername.Text;
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.UTORRENT_USR);
        }

        private void textBoxUtorrentPassword_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            m_configHandler.uTorrentPassword = textBoxUtorrentPassword.Password;
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.UTORRENT_PWD);
            //textBoxUtorrentPassword.Password;
        }

        private void RadioButtonStop_Checked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            m_configHandler.uTorrentStop = (bool)RadioButtonStop.IsChecked;
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.UTORRENT_STOP);
        }

        private void RadioButtonStop_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsInitCompleted)
            {
                return;
            }
            m_configHandler.uTorrentStop = (bool)RadioButtonStop.IsChecked;
            m_configHandler.saveValue(VPNWatcher.ConfigHandler.SETTING.UTORRENT_STOP);
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

        // minimize the app to the trayicon
        private void doMinimize()
        {
            Helper.doLog("doMinimize", m_configHandler.DebugMode);

            m_nIcon.Visible = true;
            Hide();
            WindowState = WindowState.Minimized;
        }

        // maximize the app 
        private void doMaximize()
        {
            Helper.doLog("doMaximize", m_configHandler.DebugMode);

            m_nIcon.Visible = false;
            Show();
            WindowState = WindowState.Normal;

            Activate();
            Focus();
        }

    }
}

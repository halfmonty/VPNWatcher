using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Configuration;
using System.Windows.Controls;

namespace VPNWatcher
{
    public class ConfigHandler
    {
        Configuration m_config = null;
        ScrollViewer m_viewForLogging = null;

        public ConfigHandler(ScrollViewer viewForLogging) {
            m_viewForLogging = viewForLogging;
            try {
                m_config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            } catch(ConfigurationErrorsException) {
                Helper.doLog("error reading configfile, setting default values");
            }
        }

        /** config values */
        public int ConsoleMaxSize { get; set; }
        public Boolean IgnoreIPV6 { get; set; }
        public Boolean StartMinimized { get; set; }
        public int TimerInSeconds { get; set; }
        public Boolean DebugMode { get; set; }
        public int ActionIndex { get; set; }
        public String VPNInterfaceID { get; set; }
        public String VPNInterfaceName { get; set; }
        private List<String> m_ListApplications = new List<String>();
        public Boolean StrictInterfaceHandling { get; set; }
        public Boolean uTorrentControlEnabled { get; set; }
        public String uTorrentUrl { get; set; }
        public String uTorrentUsername { get; set; }
        public String uTorrentPassword { get; set; }
        public Boolean uTorrentStop { get; set; }

        public List<String> getListApplications() {
            List<String> copy = new List<String>();
            copy.AddRange(m_ListApplications);
            return copy;
        }

        public void setListApplications(List<String> listApps) {
            m_ListApplications.Clear();
            m_ListApplications.AddRange(listApps);
        }

        public enum SETTING {
            START_MINIMIZED,
            VPN_ID_AND_NAME,
            UTORRENT_ENABLED,
            UTORRENT_URL,
            UTORRENT_USR,
            UTORRENT_PWD,
            UTORRENT_STOP,
            APPLICATIONS,
            STRICT,
            CHOSEN_ACTION
        };

        //load all the config falues
        public void loadConfigValues() {
            String strValue;
            String[] strValueArray;
            int nValue;


            DebugMode = getConfigBoolean("DebugMode");
            Helper.doLog("loadConfigValues start", DebugMode);

            uTorrentControlEnabled = getConfigBoolean("uTorrentControlEnabled");

            strValue = getConfigString("uTorrentUrl");
            if (strValue != null)
            {
                uTorrentUrl = strValue.Trim();
            }
            else
            {
                uTorrentUrl = "";
            }

            strValue = getConfigString("uTorrentUsername");
            if (strValue != null)
            {
                uTorrentUsername = strValue.Trim();
            }
            else
            {
                uTorrentUsername = "";
            }

            strValue = getConfigString("uTorrentPassword");
            if (strValue != null)
            {
                uTorrentPassword = strValue.Trim();
            }
            else
            {
                uTorrentPassword = "";
            }

            uTorrentStop = getConfigBoolean("uTorrentStop");

            strValue = getConfigString("VPNInterfaceID");
            if (strValue != null) {
                VPNInterfaceID = strValue.Trim();
            } else {
                VPNInterfaceID = "";
            }

            strValue = getConfigString("VPNInterfaceName");
            if (strValue != null) {
                VPNInterfaceName = strValue.Trim();
            } else {
                VPNInterfaceName = "";
            }

            strValue = getConfigString("Applications");
            m_ListApplications.Clear();
            if (strValue != null) {
                if (strValue.Contains('#')) {
                    strValueArray = strValue.Split('#');
                    foreach (String str in strValueArray) {
                        if (str.Trim() != "") {
                            m_ListApplications.Add(str.Trim());
                        }
                    }
                } else {
                    m_ListApplications.Add(strValue.Trim());
                }
            }

            nValue = getConfigInt("ChosenActionIndex");
            if (nValue >= 0 && nValue <= 2)  {
                ActionIndex = nValue;
            } else {
                ActionIndex = 0;
            }

            nValue = getConfigInt("ConsoleMaxSize");
            if (nValue >= 10) {
                ConsoleMaxSize = nValue;
            } else {
                ConsoleMaxSize = 10000;
            }

            nValue = getConfigInt("TimerInSeconds");
            if (nValue <= 0) {
                nValue = 2;
            }
            TimerInSeconds = nValue;

            //bIgnoreIPV6 = getConfigBoolean("IgnoreIPV6");
            StartMinimized = getConfigBoolean("StartMinimized");

            StrictInterfaceHandling = getConfigBoolean("StrictInterfaceHandling");

            Helper.doLog("settings loaded");
        }

        private string getConfigString(string strKey) {
            String strReturn = null;
            if (m_config.AppSettings.Settings[strKey] != null && m_config.AppSettings.Settings[strKey].Value != "") {
                strReturn = m_config.AppSettings.Settings[strKey].Value;
                if (strReturn != null) {
                    strReturn = strReturn.Trim();
                }  
            }

            Helper.doLog("getConfig: key=" + strKey + " value=" + strReturn, DebugMode);
            return strReturn;
        }

        private int getConfigInt(string strKey) {
            int nReturn = -1;
            try {
                nReturn = Convert.ToInt32(m_config.AppSettings.Settings[strKey].Value);
            } catch (Exception) {
                Helper.doLog("error parsing " + strKey);
            }

            Helper.doLog("getConfigInt key=" + strKey + " value=" + nReturn, DebugMode);
            return nReturn;
        }

        private Boolean getConfigBoolean(string strKey) {
            Boolean bReturn = false;
            if (m_config.AppSettings.Settings[strKey] != null && m_config.AppSettings.Settings[strKey].Value != null &&
                m_config.AppSettings.Settings[strKey].Value.ToLower() == "true")  {
                bReturn = true;
            }
            Helper.doLog("getConfigBoolean " + strKey + "-" + bReturn, DebugMode);
            return bReturn;
        }

        private void setConfig(String strKey, string strValue) {
            Helper.doLog("setConfig " + strKey + "-" + strValue, DebugMode);

            m_config.AppSettings.Settings[strKey].Value = strValue;
        }

        public void saveValue(SETTING setting) {
            switch(setting) {
                case SETTING.VPN_ID_AND_NAME:
                    setConfig("VPNInterfaceID", VPNInterfaceID);
                    setConfig("VPNInterfaceName", VPNInterfaceName);
                    saveConfigfile();
                    break;
                case SETTING.START_MINIMIZED:
                    setConfig("StartMinimized", StartMinimized.ToString().ToLower());
                    saveConfigfile();
                    break;
                case SETTING.STRICT:
                    setConfig("StrictInterfaceHandling", StrictInterfaceHandling.ToString().ToLower());
                    saveConfigfile();
                    break;
                case SETTING.APPLICATIONS:
                    String strValue = "";
                    foreach (String str in m_ListApplications) {
                        if (str != "") {
                            strValue += str + "#";
                        }
                    }
                    strValue = strValue.TrimEnd('#');
                    setConfig("Applications", strValue);
                    saveConfigfile();
                    break;
                case SETTING.CHOSEN_ACTION:
                    setConfig("ChosenActionIndex", ActionIndex.ToString());
                    saveConfigfile();
                    break;
                case SETTING.UTORRENT_ENABLED:
                    setConfig("uTorrentControlEnabled", uTorrentControlEnabled.ToString().ToLower());
                    saveConfigfile();
                    break;
                case SETTING.UTORRENT_URL:
                    setConfig("uTorrentUrl", uTorrentUrl.ToString().ToLower());
                    saveConfigfile();
                    break;
                case SETTING.UTORRENT_USR:
                    setConfig("uTorrentUsername", uTorrentUsername.ToString().ToLower());
                    saveConfigfile();
                    break;
                case SETTING.UTORRENT_PWD:
                    setConfig("uTorrentPassword", uTorrentPassword.ToString());
                    saveConfigfile();
                    break;
                case SETTING.UTORRENT_STOP:
                    setConfig("uTorrentStop", uTorrentStop.ToString().ToLower());
                    saveConfigfile();
                    break;
                }
        }

        private void saveConfigfile() {
            try {
                m_config.Save(ConfigurationSaveMode.Full);
            } catch(ConfigurationErrorsException) {
                Helper.doLog("error saving configfile");
            }
        }
    }
}

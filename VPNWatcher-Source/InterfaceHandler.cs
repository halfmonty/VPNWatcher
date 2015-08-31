using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Windows.Controls;

namespace VPNWatcher
{
    class InterfaceHandler
    {
        Dictionary<String, NetworkInterface> m_dicAdapters = new Dictionary<String, NetworkInterface>();

        ScrollViewer m_viewForLogging = null;

        public InterfaceHandler(ScrollViewer viewForLogging) {
            m_viewForLogging = viewForLogging;
        }

        public List<NetworkInterface> getActiveNetworkInterfaces() {
            Helper.doLog("getNetworkInterfaces ", Properties.Settings.Default.DebugMode);

            m_dicAdapters.Clear();

            List<NetworkInterface> listInterfaces = new List<NetworkInterface>();

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters) {

                if (adapter.Id == null) {
                    continue;
                }

                //keep the dictionary updated
                m_dicAdapters[adapter.Id] = adapter;

                if (adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.GetIPProperties() != null && adapter.Description != null && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                    //String strAdapter = adapter.Description + "#" + adapter.Id + "#" + adapterProperties.UnicastAddresses[0].Address;
                        Helper.doLog("getNetworkInterfaces " + adapter.Id, Properties.Settings.Default.DebugMode);
                    listInterfaces.Add(adapter);
                }
            }

            return listInterfaces;
        }

        public NetworkInterface getNetworkDetails(String strID) {
            Helper.doLog("getNetworkDetails " + strID, Properties.Settings.Default.DebugMode);

            if (strID != null && m_dicAdapters.ContainsKey(strID)) {
                return m_dicAdapters[strID];
            } else {
                return null;
            }
        }

        public Boolean isNetworkConnected(NetworkInterface net) {
            Boolean bReturn = false;
            if (net != null && net.OperationalStatus == OperationalStatus.Up && net.GetIPProperties() != null && net.Description != null && net.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                bReturn =  true;
            }

            Helper.doLog("isNetworkConnected " + net.Id + " = " + bReturn, Properties.Settings.Default.DebugMode);

            return bReturn;
        }

        public String findSelectedInterface(String strSelection) {
            Helper.doLog("findSelectedInterface " + strSelection, Properties.Settings.Default.DebugMode);

            foreach (KeyValuePair<String, NetworkInterface> entry in m_dicAdapters) {
                // do something with entry.Value or entry.Key
                if (strSelection.StartsWith(entry.Value.Description)) {
                    Helper.doLog("findSelectedInterface found " + entry.Key + " " + entry.Value.Description, Properties.Settings.Default.DebugMode);
                    return entry.Key;
                }
            }

            return null;
        }
    }
}

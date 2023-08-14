using MelonLoader;
using UnityEngine;
using Open.Nat;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading.Tasks;

namespace MelonUPnP
{
    public class MelonUPnP : MelonMod
    {
        public static MelonUPnP Singleton;
        private NatDevice natDevice;
        private string localIp;

        private MelonPreferences_Category _UNP;
        private MelonPreferences_Entry<string> LocalIPAddress;
        private MelonPreferences_Entry<int> PortNumber;
        private MelonPreferences_Entry<Open.Nat.Protocol> _Protocol;

        public override void OnInitializeMelon()
        {
            //Melon Pref stuff

            _UNP = MelonPreferences.CreateCategory("UPNP");

            LocalIPAddress = _UNP.CreateEntry<string>("Local IP Address", ("127.0.0.1"));

            PortNumber = _UNP.CreateEntry<int>("Port Number", 7777);

            _Protocol = _UNP.CreateEntry<Open.Nat.Protocol>("Protocol", (Protocol.Udp));

            MelonLogger.Msg("Melon Preferences loaded!");
        }
        public override void OnApplicationStart()

        {
            Singleton = this;
            MelonLogger.Msg("UPnP has started.");

            FetchAndOpenPort();
        }

        public override void OnApplicationQuit()
        {
            if (natDevice != null)
            {
                ClosePort();
            }

            MelonLogger.Msg("UPnP has stopped.");
        }

        private async void FetchAndOpenPort()
        {
            try
            {
                var discoverer = new NatDiscoverer();
                var cts = new System.Threading.CancellationTokenSource(5000);
                natDevice = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

                if (natDevice != null)
                {
                    string externalIp = await FetchExternalIPv4Async();
                    localIp = FetchLocalIPv4();

                    if (!string.IsNullOrEmpty(externalIp) && !string.IsNullOrEmpty(localIp))
                    {
                        await OpenPortAsync(natDevice, externalIp);
                    }
                    else
                    {
                        MelonLogger.Msg("Failed to fetch IP addresses.");
                    }
                }
                else
                {
                    MelonLogger.Msg("No compatible NAT device found.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error: {ex.Message}");
            }
        }

        private async Task<string> FetchExternalIPv4Async()
        {
            using (var client = new WebClient())
            {
                try
                {
                    string response = await client.DownloadStringTaskAsync("https://api.ipify.org?format=json");
                    return response.Trim();
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error fetching external IPv4 address: {ex.Message}");
                    return null;
                }
            }
        }

        private string FetchLocalIPv4()
        {
            var localIp = LocalIPAddress.Value;

            return localIp;
        }

        private async Task OpenPortAsync(NatDevice device, string externalIp)
        {

            var _protocol = _Protocol.Value;

            try
            {
                //Open the port
                var portmap = new Mapping(_protocol, PortNumber.Value, PortNumber.Value, "MelonLoader"); ;
                await device.CreatePortMapAsync(portmap);

                MelonLogger.Msg($"Port {PortNumber.Value} has been opened. Protocol: {_protocol}, Local IPv4: {localIp}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error opening port: {ex.Message}");
            }
        }

        private async void ClosePort()

        //Close port OnApplicationQuit

        {
            var _protocol = _Protocol.Value;

            try
            {
                await natDevice.DeletePortMapAsync(new Mapping(_protocol, PortNumber.Value, PortNumber.Value));
                MelonLogger.Msg($"Port {PortNumber.Value} has been closed.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error closing port: {ex.Message}");
            }
        }
    }
}

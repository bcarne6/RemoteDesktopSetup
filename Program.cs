using Microsoft.Win32;
using System;
using NetFwTypeLib;
using System.IO;
using System.Text;
using System.Net;

namespace RemoteDesktopEnabled
{
    class Program
    {
        static void Main(string[] args)
        {
            //Enable Remote Desktop
            try
            {
                RegistryKey key = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, Environment.MachineName);
                key = key.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true);
                object val = key.GetValue("fDenyTSConnections");
                bool state = (int)val != 0;
                if (state)
                {
                    key.SetValue("fDenyTSConnections", 0, RegistryValueKind.DWord);
                    Console.WriteLine("Remote Desktop is now ENABLED");
                }
                else
                {
                    key.SetValue("fDenyTSConnections", 1, RegistryValueKind.DWord);
                    Console.WriteLine("Remote Desktop is now DISABLED");
                }
                key.Flush();
                if (key != null)
                    key.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("Error enabling Remote Desktop permissions: " + e);
                Console.ReadLine();
                Environment.Exit(1);
            }

            //Create Firewall Rule
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
                var currentProfiles = fwPolicy2.CurrentProfileTypes;

                // Let's create a new rule
                INetFwRule2 inboundRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                inboundRule.Enabled = true;
                //Allow through firewall
                inboundRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                //Using protocol TCP
                inboundRule.Protocol = 6; // TCP
                                          //Port 3389
                inboundRule.LocalPorts = "3389";
                //Name of rule
                inboundRule.Name = "TCP Remote Desktop on Port: 3389";
                
                inboundRule.Profiles = currentProfiles;

                // Now add the rule
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                firewallPolicy.Rules.Add(inboundRule);
            }
            catch(Exception e)
            {
                Console.WriteLine("There was an error creating the firewall rule: " + e);
                Console.ReadLine();
                Environment.Exit(1);
            }

            //Create RDP File for Local Connections
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\LocalConnection.rdp";
            try
            {
                // Delete the file if it exists.
                if (File.Exists(path))
                {
                    // Note that no lock is put on the
                    // file and the possibility exists
                    // that another process could do
                    // something with it between
                    // the calls to Exists and Delete.
                    File.Delete(path);
                }

                // Create the file.
                using (FileStream fs = File.Create(path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(@"screen mode id:i:1
                                                                    use multimon:i:0
                                                                    desktopwidth:i:1920
                                                                    desktopheight:i:1080
                                                                    session bpp:i:32
                                                                    winposstr:s:0,1,509,160,1662,970
                                                                    compression:i:1
                                                                    keyboardhook:i:2
                                                                    audiocapturemode:i:0
                                                                    videoplaybackmode:i:1
                                                                    connection type:i:7
                                                                    networkautodetect:i:1
                                                                    bandwidthautodetect:i:1
                                                                    displayconnectionbar:i:1
                                                                    enableworkspacereconnect:i:0
                                                                    disable wallpaper:i:0
                                                                    allow font smoothing:i:0
                                                                    allow desktop composition:i:0
                                                                    disable full window drag:i:1
                                                                    disable menu anims:i:1
                                                                    disable themes:i:0
                                                                    disable cursor setting:i:0
                                                                    bitmapcachepersistenable:i:1
                                                                    full address:s:" + Environment.MachineName + @"
                                                                    audiomode:i:0
                                                                    redirectprinters:i:1
                                                                    redirectcomports:i:0
                                                                    redirectsmartcards:i:1
                                                                    redirectclipboard:i:1
                                                                    redirectposdevices:i:0
                                                                    autoreconnection enabled:i:1
                                                                    authentication level:i:2
                                                                    prompt for credentials:i:0
                                                                    negotiate security layer:i:1
                                                                    remoteapplicationmode:i:0
                                                                    alternate shell:s:
                                                                    shell working directory:s:
                                                                    gatewayhostname:s:
                                                                    gatewayusagemethod:i:4
                                                                    gatewaycredentialssource:i:4
                                                                    gatewayprofileusagemethod:i:0
                                                                    promptcredentialonce:i:0
                                                                    gatewaybrokeringtype:i:0
                                                                    use redirection server name:i:0
                                                                    rdgiskdcproxy:i:0
                                                                    kdcproxyname:s:");
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
                Environment.Exit(1);
            }

            //Create RDP File for Remote Connections
            path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\RemoteConnection.rdp";
            try
            {
                // Delete the file if it exists.
                if (File.Exists(path))
                {
                    // Note that no lock is put on the
                    // file and the possibility exists
                    // that another process could do
                    // something with it between
                    // the calls to Exists and Delete.
                    File.Delete(path);
                }

                //Grab the user's external ip address to put into their .rdp file
                string externalip = new WebClient().DownloadString("http://icanhazip.com");

                // Create the file.
                using (FileStream fs = File.Create(path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(@"screen mode id:i:1
                                                                    use multimon:i:0
                                                                    desktopwidth:i:1920
                                                                    desktopheight:i:1080
                                                                    session bpp:i:32
                                                                    winposstr:s:0,1,509,160,1662,970
                                                                    compression:i:1
                                                                    keyboardhook:i:2
                                                                    audiocapturemode:i:0
                                                                    videoplaybackmode:i:1
                                                                    connection type:i:7
                                                                    networkautodetect:i:1
                                                                    bandwidthautodetect:i:1
                                                                    displayconnectionbar:i:1
                                                                    enableworkspacereconnect:i:0
                                                                    disable wallpaper:i:0
                                                                    allow font smoothing:i:0
                                                                    allow desktop composition:i:0
                                                                    disable full window drag:i:1
                                                                    disable menu anims:i:1
                                                                    disable themes:i:0
                                                                    disable cursor setting:i:0
                                                                    bitmapcachepersistenable:i:1
                                                                    full address:s:" + externalip + @"
                                                                    audiomode:i:0
                                                                    redirectprinters:i:1
                                                                    redirectcomports:i:0
                                                                    redirectsmartcards:i:1
                                                                    redirectclipboard:i:1
                                                                    redirectposdevices:i:0
                                                                    autoreconnection enabled:i:1
                                                                    authentication level:i:2
                                                                    prompt for credentials:i:0
                                                                    negotiate security layer:i:1
                                                                    remoteapplicationmode:i:0
                                                                    alternate shell:s:
                                                                    shell working directory:s:
                                                                    gatewayhostname:s:
                                                                    gatewayusagemethod:i:4
                                                                    gatewaycredentialssource:i:4
                                                                    gatewayprofileusagemethod:i:0
                                                                    promptcredentialonce:i:0
                                                                    gatewaybrokeringtype:i:0
                                                                    use redirection server name:i:0
                                                                    rdgiskdcproxy:i:0
                                                                    kdcproxyname:s:");
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
                Environment.Exit(1);
            }

            //Exit
            Console.WriteLine("\nSuccess! Please press enter to exit...");
            Console.ReadLine();
        }
    }
}

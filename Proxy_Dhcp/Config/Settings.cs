using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CloneDeploy_Proxy_Dhcp.Helpers;
using CloneDeploy_Proxy_Dhcp.Tftp;

namespace CloneDeploy_Proxy_Dhcp.Config
{
    class Settings
    {
        public static string Nic { get; private set; }
        public static string NextServer { get; private set; }
        public static bool ListenDiscover { get; private set; }
        public static bool ListenProxy { get; private set; }
        public static bool AllowAll { get; private set; }
        public static string ComServerURL { get; private set; }
        public static string BiosBootFile { get; private set; }
        public static string Efi32BootFile { get; private set; }
        public static string Efi64BootFile { get; private set; }
        public static string ServerIdentifierOverride { get; private set; }
        public static byte[] PXEClient { get; set; }
        public static bool CheckWebReservations { get; set; }
        public static bool CheckTftpCluster { get; set; }
        public static int TftpPollingInterval { get; set; }


        public void SetAll()
        {
            var isError = false;
           
            var reader = new IniReader();
            reader.CheckForConfig();

            Nic = reader.ReadConfig("settings","interface");
            NextServer = reader.ReadConfig("settings", "next-server");

            try
            {
                ListenDiscover = Convert.ToBoolean(reader.ReadConfig("settings", "listen-dhcp"));
            }
            catch (Exception)
            {
                Console.WriteLine("listen-dhcp Is Not Valid.  Valid Entries Include true or false");
                isError = true;
               
            }

            try
            {
                ListenProxy = Convert.ToBoolean(reader.ReadConfig("settings", "listen-proxy"));
            }
            catch (Exception)
            {
                Console.WriteLine("listen-proxy Is Not Valid.  Valid Entries Include true or false");
                isError = true;
            }

            try
            {
                AllowAll = Convert.ToBoolean(reader.ReadConfig("settings", "allow-all-mac"));
            }
            catch (Exception)
            {
                Console.WriteLine("allow-all-mac Is Not Valid.  Valid Entries Include true or false");
                isError = true;
            }

            try
            {
                CheckWebReservations = Convert.ToBoolean(reader.ReadConfig("settings", "check-web-reservations"));
            }
            catch (Exception)
            {
                Console.WriteLine("check-web-reservations Is Not Valid.  Valid Entries Include true or false");
                isError = true;
            }

            try
            {
                CheckTftpCluster = Convert.ToBoolean(reader.ReadConfig("settings", "check-tftp-cluster"));
            }
            catch (Exception)
            {
                Console.WriteLine("check-tftp-cluster Is Not Valid.  Valid Entries Include true or false");
                isError = true;
            }

            PXEClient = Encoding.UTF8.GetBytes("PXEClient");

            ComServerURL = reader.ReadConfig("settings", "comserver-url");
            if (!ComServerURL.Trim().EndsWith("/"))
                ComServerURL += "/";
            BiosBootFile = reader.ReadConfig("settings", "bios-bootfile");
            Efi32BootFile = reader.ReadConfig("settings", "efi32-bootfile");
            Efi64BootFile = reader.ReadConfig("settings", "efi64-bootfile");
            ServerIdentifierOverride = reader.ReadConfig("settings", "server-identifier-override");

           
            if (!string.IsNullOrEmpty(ComServerURL))
            {
                Console.Write("Com Server Is Populated.  Testing API ... ");
                var testResult = new ApiCalls.APICall().ProxyDhcpApi.Test();
                if (testResult != "true")
                {
                    Console.WriteLine("FAILED");
                    Console.WriteLine("... Web Reservations Will Not Be Processed");
                    Console.WriteLine("... Clustered Tftp Servers Will Not Be Processed");
                    ComServerURL = null;
                    CheckTftpCluster = false;
                    CheckWebReservations = false;
                }
                else
                {
                    Console.WriteLine("PASSED");
                }

               

                if (CheckTftpCluster)
                {
                    try
                    {
                        TftpPollingInterval = Int32.Parse(reader.ReadConfig("settings", "tftp-polling-interval"));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(
                            "tftp-polling-interval Is Not Valid.  Valid Entries Include A Positive Integer");
                        isError = true;
                        CheckTftpCluster = false;
                    }

                    try
                    {
                    Console.WriteLine("Clustered Tftp Server Check Is Enabled.  Enumerating Tftp Servers ...");

                        new TftpMonitor().Run();
                        //Fix me - the last item in the list seems to get cut off because it might not have finished the tftp get again.  Running it again, adds a delay.
                        new TftpMonitor().Run();
                      
                        for (int i = 0; i < TftpMonitor.TftpStatus.Keys.Count; i++)
                        {
                            Console.WriteLine(TftpMonitor.TftpStatus.ElementAt(i));
                        }
                        Console.WriteLine("... Complete");

                    }
                    catch (Exception)
                    {
                        Console.WriteLine(
                            "Could Not Enumerate Tftp Servers");
                        isError = true;
                        CheckTftpCluster = false;
                    }



                }
            }
            else
            {
                Console.WriteLine("Com Server Is Not Populated.  Web Reservations Will Not Be Processed");
            }

            Console.WriteLine();

            
            try
            {
                IPAddress.Parse(Nic);
            }
            catch
            {
                Console.WriteLine("Interface Is Not Valid.  Ensure The Interface Is A Valid IPv4 Address");
                isError = true;
            }

            try
            {
                IPAddress.Parse(NextServer);
            }
            catch
            {
                Console.WriteLine("Next-Server Is Not Valid.  Ensure The Next-Server Is A Valid IPv4 Address");
                isError = true;
            }

            if (Nic == "0.0.0.0" && string.IsNullOrEmpty(ServerIdentifierOverride))
            {
                Console.WriteLine("When the interface Is Set To Listen For Requests On Any IP, server-identifier-override Must Have An Address");
                isError = true;
            }

            if (Type.GetType("Mono.Runtime") != null)
            {
                if (Nic != "0.0.0.0")
                {
                    Console.WriteLine("When running on Mono the interface must be set to 0.0.0.0");
                    isError = true;
                }
                if (string.IsNullOrEmpty(ServerIdentifierOverride))
                {
                    Console.WriteLine("When running on Mono the server-identifier-override must have a value");
                    isError = true;
                }
            }

            if (isError)
            {
                Console.WriteLine("Toems Proxy DHCP Could Not Be Started");
                Console.WriteLine("Press [Enter] to Exit");
                Console.Read();
                Environment.Exit(1);
            }

            var rdr = new FileReader();
            if (!AllowAll)
            {
                if(File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"allow"))
                {
                    foreach (var mac in rdr.ReadFile("allow"))
                        DHCPServer.DHCPServer.AddAcl(PhysicalAddress.Parse(mac), false);
                }
            }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"deny"))
            {
                foreach (var mac in rdr.ReadFile("deny"))
                    DHCPServer.DHCPServer.AddAcl(PhysicalAddress.Parse(mac), true);
            }

            try
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"reservations"))
                {
                    foreach (var reservation in rdr.ReadFile("reservations"))
                    {
                        var arrayReservation = reservation.Split(',');

                        var serveroptions = new DHCPServer.DHCPServer.ReservationOptions();
                        if (arrayReservation.Length == 4)
                            serveroptions.ReserveBCDFile = arrayReservation[3];
                        serveroptions.ReserveBootFile = arrayReservation[2];
                        serveroptions.ReserveNextServer = arrayReservation[1];
                        DHCPServer.DHCPServer.Reservations.Add(PhysicalAddress.Parse(arrayReservation[0]), serveroptions);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could Not Parse Local Reservations File.  Local Reservations Will Not Be Processed");
                DHCPServer.DHCPServer.Reservations.Clear();
            }

            
          
          
        }  
    }
}

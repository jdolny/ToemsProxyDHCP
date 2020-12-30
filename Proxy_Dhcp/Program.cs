/*
Toems Proxy DHCP
http://theopenem.com

The MIT License (MIT)

Extended Proxy Functionality 
Copyright (c) 2015 Jon Dolny

Some DHCP code from WinDHCP
Copyright (c) 2015 Paul Wheeler

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using CloneDeploy_Proxy_Dhcp.Config;
using CloneDeploy_Proxy_Dhcp.DHCPServer;
using CloneDeploy_Proxy_Dhcp.ServiceHost;
using CloneDeploy_Proxy_Dhcp.Tftp;
using Mono.Unix;
using Mono.Unix.Native;

namespace CloneDeploy_Proxy_Dhcp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && (ContainsSwitch(args, "help") || ContainsSwitch(args, "?")))
            {
                Console.WriteLine();
                Console.WriteLine("\t--version\t Displays the current version");
                Console.WriteLine("\t--install\t Installs service on Windows");
                Console.WriteLine("\t--uninstall\t Uninstalls service on Windows");
                Console.WriteLine("\t--console\t Runs application in console mode without installing service");
                Console.WriteLine("\t--debug\t\t Runs the application in console mode with debug output");
                Console.WriteLine("\t--daemon\t Run the application for unix in daemon mode for use with Upstart, Systemd, etc.");
                Environment.Exit(0);
            }

            if (args.Length > 0 && ContainsSwitch(args, "version"))
            {
                Console.WriteLine("1.0.0");
                Environment.Exit(0);
            }

            if (args.Length > 0 && ContainsSwitch(args, "install"))
            {
                Install();
                Environment.Exit(0);
            }
            
            if (args.Length > 0 && ContainsSwitch(args, "uninstall"))
            {
                Uninstall();
                Environment.Exit(0);
            }

            if (ContainsSwitch(args, "debug"))
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            new Settings().SetAll();
            var server = new DHCPServer.DHCPServer(IPAddress.Parse(Settings.Nic), 67);
            var proxy = new DHCPServer.DHCPServer(IPAddress.Parse(Settings.Nic), 4011);
            var tftpMon = new TftpMonitor();

            server.OnDataReceived += new DHCPDataReceived().Process;
            proxy.OnDataReceived += new ProxyDataReceived().Process;
            DhcpHost host = new DhcpHost(server, proxy,tftpMon);

            //Run as Windows service
            if (args.Length == 0)
            {
                var servicesToRun = new ServiceBase[] {host};
                ServiceBase.Run(servicesToRun);
                return;
            }

            //Only used for unix because the Mono service version wasn't working correctly.
            if (ContainsSwitch(args, "daemon"))
            {
                host.ManualStart(args);
                UnixSignal[] signals =
                {
                    new UnixSignal(Signum.SIGINT),
                    new UnixSignal(Signum.SIGTERM)
                };

                for (var exit = false; !exit;)
                {
                    var id = UnixSignal.WaitAny(signals);

                    if (id >= 0 && id < signals.Length)
                    {
                        if (signals[id].IsSet) exit = true;
                    }
                }
            }
            else if (ContainsSwitch(args, "console") || ContainsSwitch(args, "debug"))
            {
               

                host.ManualStart(args);
                Console.WriteLine();
                Console.WriteLine("DHCP Service Running");
                Console.WriteLine("Press [Enter] to Exit.");
                Console.Read();
                host.ManualStop();
            }
            else
            {
                Console.WriteLine("Invalid Argument");
                Console.WriteLine("Press [Enter] to Exit.");
                Console.Read();
            }
        }

        public static bool HasAdministrativeRight()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void Install()
        {
            if (!HasAdministrativeRight())
            {
                Console.WriteLine("Administrative Privileges Required To Install Service");
            }
            else
            {
                Console.WriteLine("Installing Toems Proxy DHCP service");
                try
                {
                    System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[] { });
                    Installer.UseNewContext = true;
                    Installer.Install(null);
                    Installer.Commit(null);
                    Console.WriteLine();
                    Console.WriteLine("Successfully Installed Toems Proxy DHCP service");
                    Console.WriteLine("The Service Must Manually Be Started");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Could Not Install Toems Proxy DHCP");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void Uninstall()
        {
            if (!HasAdministrativeRight())
            {
                Console.WriteLine("Administrative Privileges Required To Uninstall Service");
            }
            else
            {
                Console.WriteLine("Uninstalling Toems Proxy DHCP service");

                try
                {
                    System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[] { });
                    Installer.UseNewContext = true;
                    Installer.Uninstall(null);
                    Console.WriteLine();
                    Console.WriteLine("Successfully Uninstalled Toems Proxy DHCP service");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Could Not Uninstall Toems Proxy DHCP");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static bool ContainsSwitch(string[] args, string switchStr)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--") && arg.Length > 2 &&
                    switchStr.StartsWith(arg.Substring(2), StringComparison.OrdinalIgnoreCase) ||
                    (arg.StartsWith("/") || arg.StartsWith("-")) && arg.Length > 1 &&
                    switchStr.StartsWith(arg.Substring(1), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
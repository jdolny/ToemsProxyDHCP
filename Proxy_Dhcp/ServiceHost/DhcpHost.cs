using System;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using CloneDeploy_Proxy_Dhcp.Config;
using CloneDeploy_Proxy_Dhcp.Tftp;

namespace CloneDeploy_Proxy_Dhcp.ServiceHost
{
    public partial class DhcpHost : ServiceBase
    {
        private readonly DHCPServer.DHCPServer _server;
        private readonly DHCPServer.DHCPServer _proxy;
        private readonly TftpMonitor _tftpMon;
        private readonly Timer _timer = new Timer();

        public DhcpHost(DHCPServer.DHCPServer server, DHCPServer.DHCPServer proxy, TftpMonitor tftpMon)
        {
            InitializeComponent();
            _server = server;
            _proxy = proxy;
            _tftpMon = tftpMon;
        }

        public void ManualStart(string[] args)
        {
            OnStart(args);
        }

        public void ManualStop()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            if (Settings.ListenDiscover)
                _server.Start();
            if (Settings.ListenProxy)
                _proxy.Start();

            if (!string.IsNullOrEmpty(Settings.ComServerURL) && Settings.CheckTftpCluster)
            {
                _timer.Elapsed += new ElapsedEventHandler(tmrExecutor_Elapsed);
                _timer.Interval = Settings.TftpPollingInterval * 1000;
                _timer.Enabled = true;
                _timer.Start();
            }


        }

        protected override void OnStop()
        {
            if (Settings.ListenDiscover)
                _server.Dispose();
            if (Settings.ListenProxy)
                _proxy.Dispose();

            _timer.Enabled = false;
        }

        private void tmrExecutor_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            for (int i = 0; i < TftpMonitor.TftpStatus.Keys.Count; i++)
            {
                Console.WriteLine(TftpMonitor.TftpStatus.ElementAt(i));
            }
            
            _tftpMon.Run();
        }
    }
}

using System;
using System.Collections.Generic;
using CloneDeploy_Proxy_Dhcp.ApiCalls;
using CloneDeploy_Proxy_Dhcp.Dtos;

namespace CloneDeploy_Proxy_Dhcp.Tftp
{
    public class TftpMonitor
    {
        private static readonly Dictionary<string, bool> _tftpStatus = new Dictionary<string, bool>();
        private TftpServerDTO _tftpServers;

        public static Dictionary<string, bool> TftpStatus
        {
            get { return _tftpStatus; }
        }

        public static void SetTftpStatus(string address, bool isUp)
        {
            if (_tftpStatus.ContainsKey(address))
                _tftpStatus[address] = isUp;
            else
                _tftpStatus.Add(address, isUp);
        }

        public void Run()
        {
            _tftpServers = new APICall().ProxyDhcpApi.GetAllTftpServers();
            if (_tftpServers == null)
            {
                Console.WriteLine("Could Not Retrieve Tftp Server Listing");
                return;
            }

            foreach (var tftpServer in _tftpServers.TftpServers)
            {
                new TftpGet().Start(tftpServer);
            }
        }
    }
}

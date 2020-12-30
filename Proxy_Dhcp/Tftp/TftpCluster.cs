using System;
using System.Collections.Generic;
using System.Net;

namespace CloneDeploy_Proxy_Dhcp.Tftp
{
    public class TftpCluster
    {
        private readonly Dictionary<string, bool> _availableTftpServers;

        public TftpCluster()
        {
            _availableTftpServers = new Dictionary<string, bool>(TftpMonitor.TftpStatus);
        }

        public IPAddress GetNextServer(string mac)
        {
            var clusterTftpServers = new ApiCalls.APICall().ProxyDhcpApi.GetComputerTftpServers(mac);
            var onlineTftpServers = new List<string>();
            foreach (var tftpServer in clusterTftpServers.TftpServers)
            {
                if (_availableTftpServers[tftpServer])
                    onlineTftpServers.Add(tftpServer);
            }

            var random = new Random();
            var index = random.Next(0, onlineTftpServers.Count);
            var ip = onlineTftpServers[index];

            return IPAddress.Parse(ip);            
        }
    }
}

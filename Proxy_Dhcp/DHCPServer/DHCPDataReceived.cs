using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using CloneDeploy_Proxy_Dhcp.Config;
using CloneDeploy_Proxy_Dhcp.Helpers;

namespace CloneDeploy_Proxy_Dhcp.DHCPServer
{
    public class DHCPDataReceived
    {
        public void Process(DHCPRequest dhcpRequest)
        {
            var requestType = dhcpRequest.GetMsgType();
            if (requestType != DHCPMsgType.DHCPDISCOVER && requestType != DHCPMsgType.DHCPINFORM)
                return;



                var clientHardwareAddress = new PhysicalAddress(dhcpRequest.GetChaddr());
                if (DHCPServer.AclList.ContainsKey(clientHardwareAddress) && !DHCPServer.AclList[clientHardwareAddress] ||
                    !DHCPServer.AclList.ContainsKey(clientHardwareAddress) && !Settings.AllowAll)
                {
                    Trace.WriteLine("Request Denied By ACL - Ignoring");
                    return;
                }
            

            var vendorId = dhcpRequest.GetVendorOptionData();
            if (vendorId != null)
            {
                Trace.WriteLine(requestType + " Request From " +
                                Utility.ByteArrayToString(dhcpRequest.GetChaddr(), true));

                var strVendorId = Utility.ByteArrayToString(vendorId, true);

                //Expected Format: 505845436C69656E743A417263683A30303030303A554E44493A303032303031 (PXEClient:Arch:00000:UNDI:002001)
                if (strVendorId.StartsWith("505845436C69656E74"))                 
                    ProcessPXERequest(dhcpRequest);
            }

            Trace.WriteLine("");

        }

        static void ProcessPXERequest(DHCPRequest dhcpRequest)
        {
            //Trace.WriteLine("Request Is A PXE Boot");
            var replyOptions = new DHCPReplyOptions();
            replyOptions.OtherOptions.Add(DHCPOption.Vendorclassidentifier, Settings.PXEClient);
            var reply = new DHCPReply(dhcpRequest);
            reply.Send(DHCPMsgType.DHCPOFFER, replyOptions, 68);
        }

    

     
    }
}

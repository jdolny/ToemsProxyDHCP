using System.Diagnostics;
using System.Net;
using System.Text;
using CloneDeploy_Proxy_Dhcp.ApiCalls;
using CloneDeploy_Proxy_Dhcp.Config;
using CloneDeploy_Proxy_Dhcp.Dtos;
using CloneDeploy_Proxy_Dhcp.Helpers;
using CloneDeploy_Proxy_Dhcp.Tftp;

namespace CloneDeploy_Proxy_Dhcp.DHCPServer
{
    public class ProxyDataReceived
    {
        public void Process(DHCPRequest dhcpRequest)
        {
            var requestType = dhcpRequest.GetMsgType();
            Trace.WriteLine(requestType + " Proxy Request From " +
                            Utility.ByteArrayToString(dhcpRequest.GetChaddr(), true) + " " +
                            dhcpRequest.GetSourceIP() + ":" + dhcpRequest.GetSourcePort());

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
                var strVendorId = Utility.ByteArrayToString(vendorId, true);
                Trace.WriteLine("Vendor Class Id " + strVendorId + " " + Encoding.Default.GetString(vendorId));

                if (requestType == DHCPMsgType.DHCPREQUEST && strVendorId.StartsWith("505845436C69656E74"))
                    //Expected Format: 505845436C69656E743A417263683A30303030303A554E44493A303032303031 (PXEClient:Arch:00000:UNDI:002001)
                    ProcessProxyRequest(dhcpRequest);
                else
                    Trace.WriteLine("Request Is Not A Proxy PXE Request - Ignoring");
            }
            else
                Trace.WriteLine("No Proxy Vendor Class Id Supplied - Ignoring");

            Trace.WriteLine("");
        }


        static void ProcessProxyRequest(DHCPRequest dhcpRequest)
        {
            Trace.WriteLine("Request Is A Proxy PXE Boot");

            bool isWebReservation = false;
            bool isLocalReservation = false;
            var replyOptions = new DHCPReplyOptions();
            
            var clientHardwareAddress = new PhysicalAddress(dhcpRequest.GetChaddr());
            if (DHCPServer.Reservations.ContainsKey(clientHardwareAddress))
            {
                isLocalReservation = true;
                Trace.WriteLine("Local Reservation Found");
                replyOptions.NextServer =
                    IPAddress.Parse(DHCPServer.Reservations[clientHardwareAddress].ReserveNextServer);
                replyOptions.BootFileName = DHCPServer.Reservations[clientHardwareAddress].ReserveBootFile;
                if (DHCPServer.Reservations[clientHardwareAddress].ReserveBCDFile != null)
                    replyOptions.OtherOptions.Add(DHCPOption.Wpad,
                        Encoding.UTF8.GetBytes(DHCPServer.Reservations[clientHardwareAddress].ReserveBCDFile));
            }
            else if (Settings.CheckWebReservations)
            {
                if (!string.IsNullOrEmpty(Settings.ComServerURL))
                {
                    ProxyReservationDTO webReservation;

                    lock (dhcpRequest)
                    {
                        var mac = Utility.AddHexColons(Utility.ByteArrayToString(dhcpRequest.GetChaddr(), true));
                        webReservation = new APICall().ProxyDhcpApi.GetProxyReservation(mac);
                    }


                    if (webReservation.BootFile != null && webReservation.BootFile != "NotFound" &&
                        webReservation.BootFile != "NotEnabled")
                    {
                        isWebReservation = true;
                        Trace.WriteLine("Web Reservation Found");
                        if(!string.IsNullOrEmpty(webReservation.NextServer))
                            replyOptions.NextServer = IPAddress.Parse(webReservation.NextServer);
                        else
                        {
                            if (Settings.CheckTftpCluster)
                            {
                                var mac = Utility.AddHexColons(Utility.ByteArrayToString(dhcpRequest.GetChaddr(), true));
                                replyOptions.NextServer = new TftpCluster().GetNextServer(mac);
                            }
                            else
                                replyOptions.NextServer = IPAddress.Parse(Settings.NextServer);
                        }
                        replyOptions.BootFileName = webReservation.BootFile;
                        if (webReservation.BcdFile != null)
                            replyOptions.OtherOptions.Add(DHCPOption.Wpad,
                                Encoding.UTF8.GetBytes(webReservation.BcdFile));
                    }
                }
            }

            if (!isWebReservation && !isLocalReservation)
            {

                if (Settings.CheckTftpCluster && !string.IsNullOrEmpty(Settings.ComServerURL))
                {
                    var mac = Utility.AddHexColons(Utility.ByteArrayToString(dhcpRequest.GetChaddr(), true));
                    replyOptions.NextServer = new TftpCluster().GetNextServer(mac);
                }
                else
                    replyOptions.NextServer = IPAddress.Parse(Settings.NextServer);

                var clientArch = dhcpRequest.GetClientSystemArch();
                if (clientArch != DHCPRequest.ClientSystemArch.Error)
                {
                    Trace.WriteLine("Client Architecture: " + clientArch);
                    bool unsupportedArch = false;
                    switch (clientArch)
                    {
                        case DHCPRequest.ClientSystemArch.Intelx86PC: //legacy bios
                            replyOptions.BootFileName = Settings.BiosBootFile;
                            replyOptions.OtherOptions.Add(DHCPOption.Wpad, Encoding.UTF8.GetBytes(@"\boot\BCDx86"));
                            break;
                        case DHCPRequest.ClientSystemArch.EFIIA32: //efi x86
                            replyOptions.BootFileName = Settings.Efi32BootFile;
                            replyOptions.OtherOptions.Add(DHCPOption.Wpad, Encoding.UTF8.GetBytes(@"\boot\BCDx86"));
                            break;
                        case DHCPRequest.ClientSystemArch.EFIBC: //efi x64
                            replyOptions.BootFileName = Settings.Efi64BootFile;
                            replyOptions.OtherOptions.Add(DHCPOption.Wpad, Encoding.UTF8.GetBytes(@"\boot\BCDx64"));
                            break;
                        case DHCPRequest.ClientSystemArch.EFIx8664: //efi x64
                            replyOptions.BootFileName = Settings.Efi64BootFile;
                            replyOptions.OtherOptions.Add(DHCPOption.Wpad, Encoding.UTF8.GetBytes(@"\boot\BCDx64"));
                            break;
                        default:
                            Trace.WriteLine("Unsupported Client System Architecture " + clientArch + " - Ignoring");
                            unsupportedArch = true;
                            break;
                    }

                    if (unsupportedArch) return;
                }
                else
                {
                    Trace.WriteLine("Unsupported Client System Architecture " + clientArch + " - Ignoring");
                    return;
                }
            }

            
            var replyPort = dhcpRequest.GetSourcePort() == 4011 ? 4011 : 68;
            var reply = new DHCPReply(dhcpRequest);
            reply.Send(DHCPMsgType.DHCPACK, replyOptions, replyPort);
        }
    }
}

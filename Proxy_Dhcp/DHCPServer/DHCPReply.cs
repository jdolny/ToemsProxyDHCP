using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CloneDeploy_Proxy_Dhcp.DHCPServer
{
    /// <summary>Reply options</summary>
    public class DHCPReplyOptions
    {
        /// <summary>Next Server</summary>
        public IPAddress NextServer = null;

        /// <summary>Boot File Name</summary>
        public string BootFileName = null;

        /// <summary>Other options which will be sent on request</summary>
        public Dictionary<DHCPOption, byte[]> OtherOptions = new Dictionary<DHCPOption, byte[]>();
    }

    public class DHCPReply
    {
        private readonly DHCPRequest _dhcpRequest;
        private const int PORT_TO_SEND_TO_CLIENT = 68;
        private const int PORT_TO_SEND_TO_RELAY = 67;

        public DHCPReply(DHCPRequest request)
        {
            _dhcpRequest = request;
        }

        private byte[] CreateOptionStruct(DHCPMsgType msgType, DHCPReplyOptions replyOptions)
        {
            byte[] resultOptions = null;

            // Option82?
            var relayInfo = OptionData.GetOptionData(DHCPOption.RelayInfo, _dhcpRequest.requestData);

            CreateOptionElement(ref resultOptions, DHCPOption.DHCPMessageTYPE, new [] { (byte)msgType });
            // Server identifier - our IP address
            if (_dhcpRequest.dhcpServer.ServerIdentifier != null)
                CreateOptionElement(ref resultOptions, DHCPOption.ServerIdentifier, _dhcpRequest.dhcpServer.ServerIdentifier.GetAddressBytes());

            // Requested options
            if (replyOptions != null)
                foreach (var option in replyOptions.OtherOptions.Keys)
                {
                    CreateOptionElement(ref resultOptions, option, replyOptions.OtherOptions[option]);
                }

            // Option 82? Send it back!
            if (relayInfo != null)
                CreateOptionElement(ref resultOptions, DHCPOption.RelayInfo, relayInfo);

            // Create the end option
            Array.Resize(ref resultOptions, resultOptions.Length + 1);
            Array.Copy(new byte[] { 255 }, 0, resultOptions, resultOptions.Length - 1, 1);
            return resultOptions;
        }

        private static void CreateOptionElement(ref byte[] options, DHCPOption option, byte[] data)
        {
            byte[] optionData;

            optionData = new byte[data.Length + 2];
            optionData[0] = (byte)option;
            optionData[1] = (byte)data.Length;
            Array.Copy(data, 0, optionData, 2, data.Length);
            if (options == null)
                Array.Resize(ref options, optionData.Length);
            else
                Array.Resize(ref options, options.Length + optionData.Length);
            Array.Copy(optionData, 0, options, options.Length - optionData.Length, optionData.Length);
        }

        private static byte[] BuildDataStructure(DHCPMessage.Packet packet)
        {
            byte[] mArray;

            try
            {
                mArray = new byte[0];
                AddOptionElement(new [] { packet.op }, ref mArray);
                AddOptionElement(new [] { packet.htype }, ref mArray);
                AddOptionElement(new [] { packet.hlen }, ref mArray);
                AddOptionElement(new [] { packet.hops }, ref mArray);
                AddOptionElement(packet.xid, ref mArray);
                AddOptionElement(packet.secs, ref mArray);
                AddOptionElement(packet.flags, ref mArray);
                AddOptionElement(packet.ciaddr, ref mArray);
                AddOptionElement(packet.yiaddr, ref mArray);
                AddOptionElement(packet.siaddr, ref mArray);
                AddOptionElement(packet.giaddr, ref mArray);
                AddOptionElement(packet.chaddr, ref mArray);
                AddOptionElement(packet.sname, ref mArray);
                AddOptionElement(packet.file, ref mArray);

                AddOptionElement(packet.mcookie, ref mArray);
                AddOptionElement(packet.options, ref mArray);
                return mArray;
            }
            finally
            {
                mArray = null;
            }
        }

        private static void AddOptionElement(byte[] fromValue, ref byte[] targetArray)
        {
            if (targetArray != null)
                Array.Resize(ref targetArray, targetArray.Length + fromValue.Length);
            else
                Array.Resize(ref targetArray, fromValue.Length);
            Array.Copy(fromValue, 0, targetArray, targetArray.Length - fromValue.Length, fromValue.Length);
        }

        /// <summary>
        /// Sends DHCP reply
        /// </summary>
        /// <param name="msgType">Type of DHCP message to send</param>
        /// <param name="replyData">Reply options (will be sent if requested)</param>
        /// <param name="replyPort">Port to send on</param>
        public void Send(DHCPMsgType msgType, DHCPReplyOptions replyData, int replyPort)
        {
            var replyBuffer = _dhcpRequest.requestData;
            replyBuffer.op = 2; // Reply
            replyBuffer.options = CreateOptionStruct(msgType, replyData); // Options
            if (!string.IsNullOrEmpty(_dhcpRequest.dhcpServer.ServerName))
            {
                var serverNameBytes = Encoding.ASCII.GetBytes(_dhcpRequest.dhcpServer.ServerName);
                int len = (serverNameBytes.Length > 63) ? 63 : serverNameBytes.Length;
                Array.Copy(serverNameBytes, replyBuffer.sname, len);
                replyBuffer.sname[len] = 0;
            }
            if (replyData.NextServer != null)
            {
                var nextServerBytes = replyData.NextServer.GetAddressBytes();
                Array.Copy(nextServerBytes, replyBuffer.siaddr, 4);
            }
            if (replyData.BootFileName != null)
            {
                var bootFileNameBytes = Encoding.ASCII.GetBytes(replyData.BootFileName);
                int len = (bootFileNameBytes.Length > 127) ? 127 : bootFileNameBytes.Length;
                Array.Copy(bootFileNameBytes, replyBuffer.file, len);
                replyBuffer.file[len] = 0;
            }
            lock (_dhcpRequest.requestSocket)
            {
                IPEndPoint endPoint;

                if (msgType == DHCPMsgType.DHCPACK)
                {
                    //Trace.WriteLine("Sending Acknowledgement to " + new IPAddress(replyBuffer.ciaddr) + ":" + replyPort);
                    _dhcpRequest.requestSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
                    endPoint = new IPEndPoint(new IPAddress(replyBuffer.ciaddr), replyPort);
                }
                else
                {
                    if ((replyBuffer.giaddr[0] == 0) && (replyBuffer.giaddr[1] == 0) &&
                        (replyBuffer.giaddr[2] == 0) && (replyBuffer.giaddr[3] == 0))
                    {
                        //Trace.WriteLine("Sending Offer To " + IPAddress.Broadcast + ":" + PORT_TO_SEND_TO_CLIENT);
                        _dhcpRequest.requestSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                        endPoint = new IPEndPoint(IPAddress.Broadcast, PORT_TO_SEND_TO_CLIENT);
                    }
                    else
                    {
                        //Trace.WriteLine("Sending Offer To " + new IPAddress(replyBuffer.giaddr) + ":" + PORT_TO_SEND_TO_RELAY);
                        _dhcpRequest.requestSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
                        endPoint = new IPEndPoint(new IPAddress(replyBuffer.giaddr), PORT_TO_SEND_TO_RELAY);
                    }
                }
                var DataToSend = BuildDataStructure(replyBuffer);
                _dhcpRequest.requestSocket.SendTo(DataToSend, endPoint);
            }
        }
    }
}

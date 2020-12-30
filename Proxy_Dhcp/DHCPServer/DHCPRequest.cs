using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CloneDeploy_Proxy_Dhcp.DHCPServer
{
   
    /// <summary>
    /// DHCP request
    /// </summary>
    public class DHCPRequest
    {
        public enum ClientSystemArch
        {
            Intelx86PC = 0,
            NECPC98 = 1,
            EFIItanium = 2,
            DECAlpha = 3,
            Arcx86 = 4,
            IntelLeanClient = 5,
            EFIIA32 = 6,
            EFIBC = 7,
            EFIXscale = 8,
            EFIx8664 = 9,
            Error = 99
        }

        /// <summary>DHCP relay information (option 82)</summary>
        public struct RelayInfo
        {
            /// <summary>Agent circuit ID</summary>
            public byte[] AgentCircuitID;
            /// <summary>Agent remote ID</summary>
            public byte[] AgentRemoteID;
        }

        public readonly DHCPServer dhcpServer;
        public readonly DHCPMessage.Packet requestData;
        public readonly Socket requestSocket;
        private readonly IPEndPoint remote;
        private const int OPTION_OFFSET = 240;
       

        internal DHCPRequest(byte[] data, Socket socket, IPEndPoint source, DHCPServer server)
        {
            dhcpServer = server;
            System.IO.BinaryReader rdr;
            System.IO.MemoryStream stm = new System.IO.MemoryStream(data, 0, data.Length);
            rdr = new System.IO.BinaryReader(stm);
            // Reading data
            requestData.op = rdr.ReadByte();
            requestData.htype = rdr.ReadByte();
            requestData.hlen = rdr.ReadByte();
            requestData.hops = rdr.ReadByte();
            requestData.xid = rdr.ReadBytes(4);
            requestData.secs = rdr.ReadBytes(2);
            requestData.flags = rdr.ReadBytes(2);
            requestData.ciaddr = rdr.ReadBytes(4);
            requestData.yiaddr = rdr.ReadBytes(4);
            requestData.siaddr = rdr.ReadBytes(4);
            requestData.giaddr = rdr.ReadBytes(4);
            requestData.chaddr = rdr.ReadBytes(16);
            requestData.sname = rdr.ReadBytes(64);
            requestData.file = rdr.ReadBytes(128);
            requestData.mcookie = rdr.ReadBytes(4);
            requestData.options = rdr.ReadBytes(data.Length - OPTION_OFFSET);
            requestSocket = socket;
            remote = source;

        }

        /// <summary>
        /// Returns array of requested by client options
        /// </summary>
        /// <returns>Array of requested by client options</returns>
        public DHCPOption[] GetRequestedOptionsList()
        {
            var reqList = OptionData.GetOptionData(DHCPOption.ParameterRequestList,requestData);
            var optList = new List<DHCPOption>();
            if (reqList != null) foreach (var option in reqList) optList.Add((DHCPOption)option); else return null;
            return optList.ToArray();
        }

        /// <summary>
        /// Returns all options
        /// </summary>
        /// <returns>Options dictionary</returns>
        public Dictionary<DHCPOption, byte[]> GetAllOptions()
        {
            var result = new Dictionary<DHCPOption, byte[]>();
            DHCPOption DDataID;
            byte DataLength = 0;

            for (int i = 0; i < requestData.options.Length; i++)
            {
                DDataID = (DHCPOption)requestData.options[i];
                if (DDataID == DHCPOption.END_Option) break;
                DataLength = requestData.options[i + 1];
                byte[] dumpData = new byte[DataLength];
                Array.Copy(requestData.options, i + 2, dumpData, 0, DataLength);
                result[DDataID] = dumpData;

                DataLength = requestData.options[i + 1];
                i += 1 + DataLength;
            }

            return result;
        }

        /// <summary>
        /// Returns ciaddr (client IP address)
        /// </summary>
        /// <returns>ciaddr</returns>
        public IPAddress GetCiaddr()
        {
            if ((requestData.ciaddr[0] == 0) &&
                (requestData.ciaddr[1] == 0) &&
                (requestData.ciaddr[2] == 0) &&
                (requestData.ciaddr[3] == 0)
                ) return null;
            return new IPAddress(requestData.ciaddr);
        }
        /// <summary>
        /// Returns giaddr (gateway IP address switched by relay)
        /// </summary>
        /// <returns>giaddr</returns>
        public IPAddress GetGiaddr()
        {
            if ((requestData.giaddr[0] == 0) &&
                (requestData.giaddr[1] == 0) &&
                (requestData.giaddr[2] == 0) &&
                (requestData.giaddr[3] == 0)
                ) return null;
            return new IPAddress(requestData.giaddr);
        }

        public int GetSourcePort()
        {
            return remote.Port;
        }

        public string GetSourceIP()
        {
            return remote.Address.ToString();
        }

        /// <summary>
        /// Returns chaddr (client hardware address)
        /// </summary>
        /// <returns>chaddr</returns>
        public byte[] GetChaddr()
        {
            var res = new byte[requestData.hlen];
            Array.Copy(requestData.chaddr, res, requestData.hlen);
            return res;
        }
        /// <summary>
        /// Returns requested IP (option 50)
        /// </summary>
        /// <returns>Requested IP</returns>
        public IPAddress GetRequestedIP()
        {
            var ipBytes = OptionData.GetOptionData(DHCPOption.RequestedIPAddress,requestData);
            if (ipBytes == null) return null;
            return new IPAddress(ipBytes);
        }
        /// <summary>
        /// Returns type of DHCP request
        /// </summary>
        /// <returns>DHCP message type</returns>
        public DHCPMsgType GetMsgType()
        {
            byte[] DData;
            DData = OptionData.GetOptionData(DHCPOption.DHCPMessageTYPE,requestData);
            if (DData != null)
                return (DHCPMsgType)DData[0];
            return 0;
        }

        public ClientSystemArch GetClientSystemArch()
        {
            byte[] DData;
            DData = OptionData.GetOptionData(DHCPOption.ClientSystemArchitecture, requestData);
            if (DData != null)
                return (ClientSystemArch) DData[1];
            return ClientSystemArch.Error;
        }

        public byte[] GetVendorOptionData()
        {
            return OptionData.GetOptionData(DHCPOption.Vendorclassidentifier,requestData);
        }

        public byte[] GetVendorSpecificInformation()
        {
            return OptionData.GetOptionData(DHCPOption.VendorSpecificInformation, requestData);
        }
        /// <summary>
        /// Returns entire content of DHCP packet
        /// </summary>
        /// <returns>DHCP packet</returns>
        public DHCPMessage.Packet GetRawPacket()
        {
            return requestData;
        }
        /// <summary>
        /// Returns relay info (option 82)
        /// </summary>
        /// <returns>Relay info</returns>
        public RelayInfo? GetRelayInfo()
        {
            var result = new RelayInfo();
            var relayInfo = OptionData.GetOptionData(DHCPOption.RelayInfo,requestData);
            if (relayInfo != null)
            {
                int i = 0;
                while (i < relayInfo.Length)
                {
                    var subOptID = relayInfo[i];
                    if (subOptID == 1)
                    {
                        result.AgentCircuitID = new byte[relayInfo[i + 1]];
                        Array.Copy(relayInfo, i + 2, result.AgentCircuitID, 0, relayInfo[i + 1]);
                    }
                    else if (subOptID == 2)
                    {
                        result.AgentRemoteID = new byte[relayInfo[i + 1]];
                        Array.Copy(relayInfo, i + 2, result.AgentRemoteID, 0, relayInfo[i + 1]);
                    }
                    i += 2 + relayInfo[i + 1];
                }
                return result;
            }
            return null;
        }

      
    }
}

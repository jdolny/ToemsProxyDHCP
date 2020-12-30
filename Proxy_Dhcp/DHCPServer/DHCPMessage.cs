namespace CloneDeploy_Proxy_Dhcp.DHCPServer
{
    public class DHCPMessage
    {
        /// <summary>
        /// Raw DHCP packet
        /// </summary>
        public struct Packet
        {
            /// <summary>Op code:   1 = boot request, 2 = boot reply</summary>
            public byte op;
            /// <summary>Hardware address type</summary>
            public byte htype;
            /// <summary>Hardware address length: length of MACID</summary>
            public byte hlen;
            /// <summary>Hardware options</summary>
            public byte hops;
            /// <summary>Transaction id</summary>
            public byte[] xid;
            /// <summary>Elapsed time from trying to boot</summary>
            public byte[] secs;
            /// <summary>Flags</summary>
            public byte[] flags;
            /// <summary>Client IP</summary>
            public byte[] ciaddr;
            /// <summary>Your client IP</summary>
            public byte[] yiaddr;
            /// <summary>Server IP</summary>
            public byte[] siaddr;
            /// <summary>Relay agent IP</summary>
            public byte[] giaddr;
            /// <summary>Client HW address</summary>
            public byte[] chaddr;
            /// <summary>Optional server host name</summary>
            public byte[] sname;
            /// <summary>Boot file name</summary>
            public byte[] file;
            /// <summary>Magic cookie</summary>
            public byte[] mcookie;
            /// <summary>Options (rest)</summary>
            public byte[] options;
        }
    }

    /// <summary>DHCP message type</summary>
    public enum DHCPMsgType
    {
        /// <summary>DHCP DISCOVER message</summary>
        DHCPDISCOVER = 1,
        /// <summary>DHCP OFFER message</summary>
        DHCPOFFER = 2,
        /// <summary>DHCP REQUEST message</summary>
        DHCPREQUEST = 3,
        /// <summary>DHCP DECLINE message</summary>
        DHCPDECLINE = 4,
        /// <summary>DHCP ACK message</summary>
        DHCPACK = 5,
        /// <summary>DHCP NAK message</summary>
        DHCPNAK = 6,
        /// <summary>DHCP RELEASE message</summary>
        DHCPRELEASE = 7,
        /// <summary>DHCP INFORM message</summary>
        DHCPINFORM = 8
    }

   

   

   




   
}

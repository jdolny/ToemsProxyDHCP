using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CloneDeploy_Proxy_Dhcp.Config;
using CloneDeploy_Proxy_Dhcp.Helpers;

namespace CloneDeploy_Proxy_Dhcp.DHCPServer
{

    internal class DhcpData
    {
        private int _mBufferSize;

        public IPEndPoint Source { get; set; }

        public byte[] MessageBuffer
        {
            get;
            private set;
        }

        public int BufferSize
        {
            get { return _mBufferSize; }

            set
            {
                _mBufferSize = value;

                var oldBuffer = MessageBuffer;
                MessageBuffer = new byte[_mBufferSize];

                var copyLen = Math.Min(oldBuffer.Length, _mBufferSize);
                Array.Copy(oldBuffer, MessageBuffer, copyLen);
            }
        }

        public IAsyncResult Result { get; set; }

        public DhcpData(byte[] messageBuffer)
        {
            MessageBuffer = messageBuffer;
            _mBufferSize = messageBuffer.Length;
        }

        public DhcpData(IPEndPoint source, byte[] messageBuffer)
        {
            Source = source;
            MessageBuffer = messageBuffer;
            _mBufferSize = messageBuffer.Length;
        }


    }
    /// <summary>
    /// DHCP Server
    /// </summary>
    public class DHCPServer : IDisposable
    {
        /// <summary>Delegate for DHCP message</summary>
        public delegate void DHCPDataReceivedEventHandler(DHCPRequest dhcpRequest);

        /// <summary>Will be called on any DHCP message</summary>
        public event DHCPDataReceivedEventHandler OnDataReceived = delegate { };
        /// <summary>Will be called on any DISCOVER message</summary>
        public event DHCPDataReceivedEventHandler OnDiscover = delegate { };
        /// <summary>Will be called on any REQUEST message</summary>
        public event DHCPDataReceivedEventHandler OnRequest = delegate { };
        /// <summary>Will be called on any DECLINE inform</summary>
        public event DHCPDataReceivedEventHandler OnInform = delegate { };

        /// <summary>Server name (optional)</summary>
        public string ServerName { get; set; }
        public IPAddress ServerIdentifier { get; set; }
        private Socket _socket;
        private readonly IPEndPoint localEndPoint;

        private readonly ReaderWriterLock _abortLock = new ReaderWriterLock();
        private Boolean _abort;
        private static readonly SortedList<PhysicalAddress, bool> _aclList = new SortedList<PhysicalAddress, bool>();
        private static readonly Dictionary<PhysicalAddress, ReservationOptions> _reservations = new Dictionary<PhysicalAddress, ReservationOptions>();

        public static SortedList<PhysicalAddress, bool> AclList
        {
            get { return _aclList; }
        }

        public static Dictionary<PhysicalAddress, ReservationOptions> Reservations
        {
            get { return _reservations; }
        }

        public struct ReservationOptions
        {
            public string ReserveNextServer;
            public string ReserveBootFile;
            public string ReserveBCDFile;
        }

        public static void AddAcl(PhysicalAddress address, bool deny)
        {
            if (_aclList.ContainsKey(address))
                _aclList[address] = !deny;
            else
                _aclList.Add(address, !deny);
        }

        /// <summary>
        /// Creates DHCP server, it will be started instantly
        /// </summary>
        /// <param name="bindIp">IP address to bind</param>
        /// <param name="port">Port to bind to</param>
        public DHCPServer(IPAddress bindIp,int port)
        {

            localEndPoint = new IPEndPoint(bindIp, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            if (Type.GetType("Mono.Runtime") == null)
            {
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                _socket.IOControl((int)SIO_UDP_CONNRESET, new [] { Convert.ToByte(false) }, null);
            }
            ServerName = "ToemsProxy";
            ServerIdentifier = string.IsNullOrEmpty(Settings.ServerIdentifierOverride)
                ? IPAddress.Parse(Settings.Nic)
                : IPAddress.Parse(Settings.ServerIdentifierOverride);
        }

        public void Start()
        {
            try
            {
                _socket.Bind(localEndPoint);
                Console.WriteLine("DHCP Service Listening On " + localEndPoint.Address + ":" + localEndPoint.Port);
            }
            catch
            {
                
                Console.WriteLine("Could Not Bind " + localEndPoint.Address + ":" + localEndPoint.Port);
                Console.WriteLine("Ensure The Interface Is Correct And The Ports Are Not In Use");
                Console.WriteLine();

                Console.WriteLine("Press [Enter] to Exit.");
                Console.Read();
                Environment.Exit(1);
            }
            
            ReceiveData();
        }

        /// <summary>Disposes DHCP server</summary>
        public void Dispose()
        {
            _abortLock.AcquireWriterLock(-1);
            try
            {
                _abort = true;
                _socket = null;
                
            }
            finally
            {
                _abortLock.ReleaseLock();
            }
        }

        private void ReceiveData()
        {
            _abortLock.AcquireReaderLock(-1);

            try
            {
                if (_abort)
                {
                    return;
                }
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = sender; 
                var buffer = new byte[1024];
                _socket.BeginReceiveFrom(buffer, 0, 1024, SocketFlags.None, ref remote, new AsyncCallback(this.OnReceive), buffer);
               
            }
            finally
            {
                _abortLock.ReleaseLock();
            }
                
            
        }

        private void OnReceive(IAsyncResult result)
        {
            DhcpData data = new DhcpData((Byte[])result.AsyncState);
            data.Result = result;
            // Queue this request for processing
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.CompleteRequest), data);
            ReceiveData();
        }

        private void CompleteRequest(object o)
        {
            DhcpData messageData = (DhcpData)o;
            EndPoint source = new IPEndPoint(IPAddress.Any, 0);
            
            try
            {
                _socket.EndReceiveFrom(messageData.Result, ref source);
                messageData.Source = (IPEndPoint)source;
                var dhcpRequest = new DHCPRequest(messageData.MessageBuffer, _socket, messageData.Source, this);
              
                OnDataReceived(dhcpRequest);
                var msgType = dhcpRequest.GetMsgType();
             
                switch (msgType)
                {
                    case DHCPMsgType.DHCPDISCOVER:
                        OnDiscover(dhcpRequest);
                        break;
                    case DHCPMsgType.DHCPREQUEST:
                        OnRequest(dhcpRequest);
                        break;
                    case DHCPMsgType.DHCPINFORM:
                        OnInform(dhcpRequest);
                        break;
                    default:
                        Trace.WriteLine("Message Type Not Handled By Toems Proxy DHCP - Ignoring Request");
                        break;
                }
            }
            catch
            {
                //throw;
            }
        }
    }

   
}

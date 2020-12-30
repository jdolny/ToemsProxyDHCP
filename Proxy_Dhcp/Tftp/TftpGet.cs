using System;
using System.IO;
using System.Net;
using System.Threading;
using Tftp.Net;

namespace CloneDeploy_Proxy_Dhcp.Tftp
{
    public class TftpGet
    {
        private static readonly AutoResetEvent TransferFinishedEvent = new AutoResetEvent(false);

        public void Start(string tftpServer)
        {
            try
            {
                IPAddress.Parse(tftpServer);
            }
            catch
            {
                return;
            }
            var client = new TftpClient(tftpServer);

            var transfer = client.Download("transfer-test.txt");
            transfer.RetryCount = 0;
            transfer.RetryTimeout = TimeSpan.FromSeconds(3);
            transfer.UserContext = tftpServer;

            transfer.OnFinished += new TftpEventHandler(transfer_OnFinished);
            transfer.OnError += new TftpErrorHandler(transfer_OnError);

            Stream stream = new MemoryStream();
            transfer.Start(stream);

            TransferFinishedEvent.WaitOne();
        }

        static void transfer_OnError(ITftpTransfer transfer, TftpTransferError error)
        {
            Tftp.TftpMonitor.SetTftpStatus(transfer.UserContext.ToString(), false);
            TransferFinishedEvent.Set();
            

        }

        static void transfer_OnFinished(ITftpTransfer transfer)
        {
            Tftp.TftpMonitor.SetTftpStatus(transfer.UserContext.ToString(), true);
            TransferFinishedEvent.Set();
            

        }
    }
}

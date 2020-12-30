using System;
using System.Linq;
using System.Text;

namespace CloneDeploy_Proxy_Dhcp.Helpers
{
    public class Utility
    {
        public static string ByteArrayToString(byte[] ar, bool simple)
        {
            var res = new StringBuilder();
            foreach (var b in ar)
            {
                res.Append(b.ToString("X2"));
            }
            if (!simple)
            {
                res.Append(" (");

                foreach (var b in ar)
                {
                    if ((b >= 32) && (b < 127))
                        res.Append(Encoding.ASCII.GetString(new byte[] { b }));
                    else res.Append(" ");
                }

                res.Append(")");
            }
            return res.ToString();
        }

        public static byte[] StringToByteArray(string hex)
        {
            var hexNoColons = hex.Replace(":", "");
            return Enumerable.Range(0, hexNoColons.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexNoColons.Substring(x, 2), 16))
                .ToArray();
        }

        public static string AddHexColons(string hex)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < hex.Length; i++)
            {
                if (i % 2 == 0 && i != 0)
                    sb.Append(':');
                sb.Append(hex[i]);
            }
            return sb.ToString();
        }

        public static string StringToHex(string hexstring)
        {
            var sb = new StringBuilder();
            foreach (char t in hexstring)
                sb.Append(Convert.ToInt32(t).ToString("X2"));
            return AddHexColons(sb.ToString());
        }

    }

   
}

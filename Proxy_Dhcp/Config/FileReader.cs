using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloneDeploy_Proxy_Dhcp.Config
{
    internal class FileReader
    {
        public IEnumerable<string> ReadFile(string fileName)
        {
            using (var sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var isComment = new string(line.Take(1).ToArray());
                    if (isComment != ";")
                        yield return line;
                }
            }
        }
    }
}

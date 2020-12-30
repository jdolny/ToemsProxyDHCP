using System;
using System.IO;
using IniParser;

namespace CloneDeploy_Proxy_Dhcp.Config
{
    internal class IniReader
    {
        public void CheckForConfig()
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"config.ini"))
            {
                Console.WriteLine("Could Not Locate config.ini");
                Environment.Exit(1);
            }
        }

        public string ReadConfig(string type, string key)
        {
            var ini = new FileIniDataParser();
            try
            {
                var parsedData = ini.LoadFile(AppDomain.CurrentDomain.BaseDirectory + @"config.ini");
                return parsedData[type][key];
            }
            catch
            {
                // ignored
            }
            return null;
        }
    }
}

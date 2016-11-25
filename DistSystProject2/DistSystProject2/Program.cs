using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public struct TCPConfig
    {
        public string dns;
        public string ip;
        public int port;
        public TCPConfig(string DNS, string IP, int P)
        {
            dns = DNS;
            ip = IP;
            port = P;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var n = Convert.ToInt32(args[0]);
            Dictionary<int, TCPConfig> tcpConfig = new Dictionary<int, TCPConfig>();
            using (StreamReader tcpReader = new StreamReader("tcp_config.txt"))
            {

                string line;
                char[] comma = { ',' };
                while ((line = tcpReader.ReadLine()) != null)
                {
                    string[] words = line.Split(comma);
                    tcpConfig.Add(Convert.ToInt32(words[0]), new TCPConfig(words[1], words[2], Convert.ToInt32(words[3])));
                }
            }
        }
    }
}

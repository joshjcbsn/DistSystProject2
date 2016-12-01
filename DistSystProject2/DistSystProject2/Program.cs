using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// IP isn't actually used
    /// </summary>
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

        public static bool operator ==(TCPConfig a, TCPConfig b)
        {
            return ((a.dns == b.dns) && (a.ip == b.ip) && (a.port == b.port));
        }

        public static bool operator !=(TCPConfig a, TCPConfig b)
        {
            return (!(a == b));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter server number:");
            var n = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine(n);
            Dictionary<int, TCPConfig> tcpConfig = new Dictionary<int, TCPConfig>();
            using (StreamReader tcpReader = new StreamReader("tcp_config.txt"))
            {

                string line;

                char[] comma = { ',' };
                while ((line = tcpReader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    string[] words = line.Split(comma);
                    tcpConfig.Add(Convert.ToInt32(words[0]), new TCPConfig(words[1], words[2], Convert.ToInt32(words[3])));
                }
            }
            try
            {
                ZAB zk = new ZAB(n, tcpConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine("sendMessage {0} {1}",ex.Message,ex.StackTrace);

            }
          //  Console.Read();
          //  zk.holdElection();
        }
    }
}

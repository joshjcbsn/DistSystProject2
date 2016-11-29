using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
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
            //create connection to server
            //Get process number
            Console.WriteLine("Enter Server #");
            int N = Convert.ToInt32(Console.ReadLine());
            //Read info from tcp_config.txt
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
            Connection socket = new Connection(tcpConfig[N]);
            socket.Connect();
            while (true)
            {
                string cmd = Console.ReadLine();
                if (cmd != "exit")
                {
                    Task request = Task.Factory.StartNew(() =>
                    {
                        var req = cmd;
                        socket.sendRequest(req);
                    });
                }
                else
                {
                    break;
                }
            }
        }
    }
}

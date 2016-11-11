using System;
using System.Collections.Generic;
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
    }
    class Program
    {
        static void Main(string[] args)
        {
            //create connection to server
            while (true)
            {
                string cmd = Console.ReadLine();
                //create new thread that sends message and waits for response
                    //create listener
                    //send tcp message
                    //start listener 
                //loop continues to listen for UI
            }
        }
    }
}

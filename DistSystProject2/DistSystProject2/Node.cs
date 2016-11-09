using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DistSystProject2
{
    class Node
    {
        public int n; //server number
        public TCPConfig tcp; //tcp configuration of this node
        public TcpListener listener; //tcp listener for this node

        /// <summary>
        /// Initiates server
        /// </summary>
        /// <param name="N">server number</param>
        /// <param name="TCP">tcp connection info</param>
        public Node(int N, TCPConfig TCP)
        {
            //set process number
            n = N;
            //set TCPConfig
            tcp = TCP;
            //start listener        
            try
            {
                listener = new TcpListener(IPAddress.Any, tcp.port);
                listener.Start();
            }
            catch (Exception ex) { Console.WriteLine(String.Format("error: {0}", ex.Message)); }
        }
    }
}

using System;
using System.Collections.Generic;
namespace Server
{
    public class ZAB
    {
        public int epoch;
        public string phase;
        public bool leader;
        public TCPConfig thisAddress;
        public Node thisNode;
        public Dictionary<int, TCPConfig> servers;
        public delegate void OnAck(object sender, EventArgs e);


        public ZAB(int n, Dictionary<int, TCPConfig> S)
        {
            servers = S;
            leader = false;
            epoch = 0;
            phase = "election";


            thisAddress = servers[n];

            thisNode = new Node(n, thisAddress);

        }
    }
}
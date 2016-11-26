using System;
using System.Collections.Generic;
namespace Server
{
    public class ZAB
    {
        public int epoch;
        public string phase;
        public bool leader;
        public FileSystem files;
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
            files = new FileSystem();

            thisAddress = servers[n];

            thisNode = new Node(n, thisAddress);
            thisNode.Create += new OnMsgHandler(OnCreate);
            thisNode.Delete += new OnMsgHandler(OnDelete);
            thisNode.Read += new OnMsgHandler(OnRead);
            thisNode.Append += new OnMsgHandler(OnAppend);
        }

        public void getConnections()
        {
            thisNode.getConnections();

        }

        public void Recover()
        {
            thisNode.Recover(0);

        }

        private void OnCreate(object sender, MsgEventArgs e)
        {
            epoch++;
        }

        private void OnDelete(object sender, MsgEventArgs e)
        {
            epoch++;
        }

        private void OnRead(object sender, MsgEventArgs e)
        {
            epoch++;
        }

        private void OnAppend(object sender, MsgEventArgs e)
        {
            epoch++;
        }
    }
}
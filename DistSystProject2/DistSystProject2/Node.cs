﻿using System;
using System.Collections.Generic;
 using System.Data;
 using System.Linq;
using System.Net;
using System.Net.Sockets;
 using System.Security.Cryptography.X509Certificates;
 using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Node
    {
        public int n; //server number
        public TCPConfig tcp; //tcp configuration of this node
        public TcpListener listener; //tcp listener for this node

        public FileSystem files;
        /// <summary>
        /// Initiates server
        /// </summary>
        /// <param name="N">server number</param>
        /// <param name="TCP">tcp connection info</param>
        public Node(int N, TCPConfig TCP)
        {

            files = new FileSystem();
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

        public void getConnections()
        {

            try
            {
                //AcceptTcpClient blocks this thread until it recieves a connection
                Console.WriteLine("Waiting for connection");
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    //start new instance to accept next connection
                    Task newConnection = Task.Factory.StartNew(() => getConnections());
                    TCP t = new TCP(client);
                    var msg = t.getMessage();
                  //  OnMsgEventArgs msgArgs = new OnMsgEventArgs(msg, t.getRemoteAddress());
                    //OnMsg(this, msgArgs);
                    msgHandler(msg, t.getRemoteAddress());

                }
            }
        }

        public void msgHandler(string msg, TCPConfig sender)
        {
            char[] space = {' '};
            var commands = msg.Split(space);
            if (commands[0] == "CREATE")
            {
                files.AddFile(commands[1]);
            }
            else if (commands[0] == "DELETE")
            {
                files.DeleteFile(commands[1]);
            }
            else if (commands[0] == "READ")
            {
                files.ReadFile(commands[1]);
            }
            else if (commands[0] == "APPEND")
            {
                files.AppendFile(commands[1], commands[2]);
            }
        }

        //message event
        public event OnMsgHandler OnMsg;

        protected virtual void OnTcpMsg(OnMsgEventArgs e)
        {
            if (OnMsg != null)
            {
                OnMsg(this, e);
            }
        }


    }
    /// <summary>
    /// Message event handler
    /// creates delegate for message event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void OnMsgHandler(object sender, OnMsgEventArgs e);

    /// <summary>
    /// Creates class to store event arguments for the the message event
    /// holds the content of the message and info on the sender
    /// </summary>
    public class OnMsgEventArgs : EventArgs
    {
        public string data { get; private set; }
        public TCPConfig client { get; private set; }

        public OnMsgEventArgs(string _data, TCPConfig _client)
        {
            data = _data;
            client = _client;
        }
    }

}

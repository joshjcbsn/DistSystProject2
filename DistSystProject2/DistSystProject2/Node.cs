﻿using System;
using System.Collections.Generic;
 using System.Data;
 using System.IO;
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
        public FileStream history;

        /// <summary>
        /// Initiates server
        /// </summary>
        /// <param name="N">server number</param>
        /// <param name="TCP">tcp connection info</param>
        public Node(int N, TCPConfig TCP)
        {

            history = new FileStream("history.txt", FileMode.CreateNew, FileAccess.ReadWrite);
            //set process number
            n = N;
            //set TCPConfig
            tcp = TCP;
            //start listener, listening only on port in TCPConfig object
            try
            {
                listener = new TcpListener(IPAddress.Any, tcp.port);
                listener.Start();
            }
            catch (Exception ex) { Console.WriteLine(String.Format("error: {0}", ex.Message)); }
        }

        /// <summary>
        /// gets incoming connections on listener port
        /// only the client should send connections on this port
        /// </summary>
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
                    //  MsgEventArgs msgArgs = new MsgEventArgs(msg, t.getRemoteAddress());
                    //Msg(this, msgArgs);
                    msgHandler(msg, t.getRemoteAddress());
                    using (StreamWriter wHistory = new StreamWriter(history))
                    {
                        wHistory.WriteLine(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Recover(int i)
        {
            using (StreamReader rHistory = new StreamReader(history))
            {
                int l = 0;
                while (l < i)
                {
                    rHistory.ReadLine();
                    l++;
                }
                string line;
                while ((line = rHistory.ReadLine()) != null)
                {
                    msgHandler(line, tcp);
                }
            }
        }

        public void msgHandler(string msg, TCPConfig sender)
        {
            char[] space = {' '};
            var commands = msg.Split(space, 2);
            if (commands[0] == "CREATE")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnCreate(msgArgs);
            }
            else if (commands[0] == "DELETE")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnDelete(msgArgs);
            }
            else if (commands[0] == "READ")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnRead(msgArgs);
            }
            else if (commands[0] == "APPEND")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnAppend(msgArgs);
            }
        }

        //message event
        public event OnMsgHandler Msg;

        protected virtual void OnMsg(MsgEventArgs e)
        {
            if (Msg != null)
            {
                Msg(this, e);
            }
        }

        public event OnMsgHandler Read;

        protected virtual void OnRead(MsgEventArgs e)
        {
            if (Read != null)
            {
                Read(this, e);
            }
        }

        public event OnMsgHandler Append;

        protected virtual void OnAppend(MsgEventArgs e)
        {
            if (Append != null)
            {
                Append(this, e);
            }
        }

        public event OnMsgHandler Delete;

        protected virtual void OnDelete(MsgEventArgs e)
        {
            if (Delete != null)
            {
                Delete(this, e);
            }
        }

        public event OnMsgHandler Create;

        protected virtual void OnCreate(MsgEventArgs e)
        {
            if (Create != null)
            {
                Create(this, e);
            }
        }


    }
    /// <summary>
    /// Message event handler
    /// creates delegate for message event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void OnMsgHandler(object sender, MsgEventArgs e);

    /// <summary>
    /// Creates class to store event arguments for the the message event
    /// holds the content of the message and info on the sender
    /// </summary>
    public class MsgEventArgs : EventArgs
    {
        public string data { get; private set; }
        public TCPConfig client { get; private set; }

        public MsgEventArgs(string _data, TCPConfig _client)
        {
            data = _data;
            client = _client;
        }
    }

}

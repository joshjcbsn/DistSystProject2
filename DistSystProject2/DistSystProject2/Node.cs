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
            Console.WriteLine(tcp.dns);
            //start listener, listening only on port in TCPConfig object
            try
            {
                listener = new TcpListener(IPAddress.Any, tcp.port);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("error: {0}", ex.Message));

            }
        }

        public void sendMsg(string msg, TCPConfig target)
        {
            try
            {
                string host = target.dns;
                int portNum = target.port;
                Console.WriteLine("Sending {0}", msg);
                using (TcpClient client = new TcpClient(host, portNum))
                {
                    TCP t = new TCP();
                    t.sendTcpMessage(msg,client.GetStream());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendMsg {0} {1}",ex.Message, ex.StackTrace);
            }
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
                    DnsEndPoint dnsep = (DnsEndPoint) client.Client.RemoteEndPoint;;
                    string dnsHost = dnsep.Host;
                    int dnsPort = dnsep.Port;
                    //start new instance to accept next connection
                    Task newConnection = Task.Factory.StartNew(() => getConnections());
                    TCP t = new TCP();
                    //EndPoint ep2 = client.Client.RemoteEndPoint;

                   // IPEndPoint ipep = (IPEndPoint) ep2;

                    TCPConfig remoteAddress = new TCPConfig(dnsHost, tcp.ip, dnsPort);
                    var msg = t.getMessage(client.GetStream());
                    //  MsgEventArgs msgArgs = new MsgEventArgs(msg, t.getRemoteAddress());
                    //Msg(this, msgArgs);

                   // Console.WriteLine("{0} from {1}", msg, t.getRemoteAddress().dns);
                    msgHandler(msg, remoteAddress);
                    client.Close();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1} {2}",ex.Message,ex.StackTrace, ex.TargetSite.ToString());
            }
        }

/*
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
*/
        public void msgHandler(string msg, TCPConfig sender)
        {
            char[] space = {' '};
            var commands = msg.Split(space, 2);
            if (commands[0] == "create")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnCreate(msgArgs);
            }
            else if (commands[0] == "delete")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnDelete(msgArgs);
            }
            else if (commands[0] == "read")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnRead(msgArgs);
            }
            else if (commands[0] == "append")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnAppend(msgArgs);
            }
            else if (commands[0] == "lock")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnLock(msgArgs);
            }
            else if (commands[0] == "unlock")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnUnlock(msgArgs);
            }
            else if (commands[0] == "proposal")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnProposal(msgArgs);
            }
            else if (commands[0] == "ack")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnAck(msgArgs);
            }
            else if (commands[0] == "commit")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnCommit(msgArgs);
            }
            else if (commands[0] == "history")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnGetHistory(msgArgs);
            }
            else if (commands[0] == "gethistory")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                OnSendHistory(msgArgs);
            }
            else if (commands[0] == "connect")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(null, sender);
                OnConnect(msgArgs);
            }

            /* else if (commands[0] == "ELECTION")
             {
                 MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                 OnElection(msgArgs);
             }
             else if (commands[0] == "COORDINATOR")
             {
                 MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                 OnCoordinator(msgArgs);
             }
             else if (commands[0] == "OK")
             {
                 MsgEventArgs msgArgs = new MsgEventArgs(commands[1], sender);
                 OnOk(msgArgs);
             }
             */

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

        public event OnMsgHandler Lock;

        protected virtual void OnLock(MsgEventArgs e)
        {
            if (Lock != null)
            {
                Lock(this, e);
            }
        }
        public event OnMsgHandler Unlock;

        protected virtual void OnUnlock(MsgEventArgs e)
        {
            if (Unlock != null)
            {
                Unlock(this, e);
            }
        }

        public event OnMsgHandler Election;

        protected virtual void OnElection(MsgEventArgs e)
        {
            if (Election != null)
            {
                Election(this, e);
            }
        }

        public event OnMsgHandler Coordinator;

        protected virtual void OnCoordinator(MsgEventArgs e)
        {
            if (Coordinator != null)
            {
                Coordinator(this, e);
            }
        }

        public event OnMsgHandler Ok;

        protected virtual void OnOk(MsgEventArgs e)
        {
            if (Ok != null)
            {
                Ok(this, e);
            }
        }

        public event OnMsgHandler Proposal;

        protected virtual void OnProposal(MsgEventArgs e)
        {
            if (Proposal != null)
            {
                Proposal(this, e);
            }
        }
        public event OnMsgHandler Ack;

        protected virtual void OnAck(MsgEventArgs e)
        {
            if (Ack != null)
            {
                Ack(this, e);
            }
        }

        public event OnMsgHandler Commit;

        protected virtual void OnCommit(MsgEventArgs e)
        {
            if (Commit != null)
            {
                Commit(this, e);
            }
        }
        public event OnMsgHandler GetHistory;

        protected virtual void OnGetHistory(MsgEventArgs e)
        {
            if (GetHistory != null)
            {
                GetHistory(this, e);
            }
        }
        public event OnMsgHandler SendHistory;

        protected virtual void OnSendHistory(MsgEventArgs e)
        {
            if (SendHistory != null)
            {
                SendHistory(this, e);
            }
        }

        public event OnMsgHandler Connect;

        protected virtual void OnConnect(MsgEventArgs e)
        {
            if (Connect != null)
            {
                Connect(this, e);
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

﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    public class Connection
    {
        public TCPConfig remoteAddress;
        public TcpListener listener;
        public Connection(TCPConfig tcp)
        {
            remoteAddress = tcp;
            try
            {
                listener = new TcpListener(IPAddress.Any, tcp.port);
                listener.Start();
            }
            catch (Exception ex) { Console.WriteLine(String.Format("error: {0}", ex.Message)); }

        }

        public void Connect()
        {
            sendRequest("connect");
        }

        public void sendRequest(string req)
        {
            try
            {
                using (TcpClient server = new TcpClient(remoteAddress.dns, remoteAddress.port))
                {
                    TCP t = new TCP(server);
                    t.sendMessage(req);
                }
                getResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void getResponse()
        {
            Console.WriteLine("Waiting for response");
            using (TcpClient server = listener.AcceptTcpClient())
            {
                TCP t = new TCP(server);
                string response = t.getMessage();
                ResponseEventArgs e = new ResponseEventArgs(response);
                OnResponse(e);
            }
        }

        public event ResponseHandler Response;

        protected virtual void OnResponse(ResponseEventArgs e)
        {
            if (Response != null)
            {
                Response(this, e);
            }
        }
    }

    public delegate void ResponseHandler(object sender, ResponseEventArgs e);

    public class ResponseEventArgs : EventArgs
    {
        public string data { get; private set; }

        public ResponseEventArgs(string _data)
        {
            data = _data;
        }
    }
}
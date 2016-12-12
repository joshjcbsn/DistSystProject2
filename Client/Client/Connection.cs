using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class Connection
    {
        public TCPConfig remoteAddress;
        public TcpListener listener;
        public string publicAddress;
        public Connection(TCPConfig tcp)
        {
            var dnsRequest = WebRequest.Create("http://169.254.169.254/latest/meta-data/public-hostname");
            //dnsRequest.ContentType = "application/json";
            dnsRequest.Method = "POST";
          //  byte[] buffer = Encoding.GetEncoding("UTF-8").GetBytes("{\"channels\": [\"\"], \"data\": { \"alert\": \" " + messaggio + "\" } }");
            //string result = System.Convert.ToBase64String(buffer);
            Stream reqstr = dnsRequest.GetRequestStream();
           // reqstr.Write(buffer, 0, buffer.Length);
            byte[] buffer;
            reqstr.Write();
            reqstr.Close();
            var requestContent = new FormUrlEncodedContent(new [] {
                new KeyValuePair<string, string>("text", "http://169.254.169.254/latest/meta-data/public-hostname"),
            });



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
            string dnsAddress = Dns.GetHostName();
            sendRequest(String.Format("connect {0}", dnsAddress));
        }

        public void sendRequest(string req)
        {
            try
            {
                using (TcpClient server = new TcpClient(remoteAddress.dns, remoteAddress.port))
                {
                    TCP t = new TCP();
                    t.sendMessage(req, remoteAddress);
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
                TCP t = new TCP();
                string response = t.getMessage(server);
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
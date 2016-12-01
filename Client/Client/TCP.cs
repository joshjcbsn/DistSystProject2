using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Client;

namespace Client
{
    class TCP
    {

        public TCP()
        {
        }

        /// <summary>
        /// Reads message. Blocks calling thread until the message has been read
        /// </summary>
        /// <param name="client">TCP client recieving message</param>
        /// <returns>message</returns>
        public string getMessage(TcpClient client)
        {
            try
            {
                byte[] bytes = new byte[1024];
                string data = null;
                Console.WriteLine("Connected");
                NetworkStream stream = client.GetStream();
                int i;
                // Loop to receive all the data sent by the client.
                i = stream.Read(bytes, 0, bytes.Length);
                while (i != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine(String.Format("Received: {0}", data));
                    // Process the data sent by the client.
                    i = stream.Read(bytes, 0, bytes.Length);
                }
                return data;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Sends TCP message to address specified in config
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="config"></param>
        public void sendMessage(string msg, TCPConfig config)
        {
            try
            {
                using (TcpClient client = new TcpClient(config.dns, config.port))
                {
                    try
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] msgBytes = Encoding.ASCII.GetBytes(msg);
                            stream.Write(msgBytes, 0, msgBytes.Length);
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class TCP
    {

        public TCP()
        {
        }

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

    }
}

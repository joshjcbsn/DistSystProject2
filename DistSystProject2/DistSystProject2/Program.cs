﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistSystProject2
{
    public struct TCPConfig
    {
        public string dns;
        public string ip;
        public int port;
        public TCPConfig(string DNS, string IP, int P)
        {
            dns = DNS;
            ip = IP;
            port = P;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var x = 1;
            var y = 2;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{


    public class ZAB
    {
        public int n;
        public int epoch;
        public int counter;
        public string phase;
        public int leader;
        public TCPConfig mostCurrentServer;
        public zxid lastId;
        public FileSystem files;
        public Dictionary<string, List<TCPConfig>> lockFiles;
        public TCPConfig thisAddress;
        public Node thisNode;
        public Dictionary<int, TCPConfig> servers;
        public HashSet<int> followers;
        public Dictionary<Proposal, List<TCPConfig>> proposals;
        public Dictionary<int, zxid> ServerIds;


        private bool response;


        public ZAB(int N, Dictionary<int, TCPConfig> S)
        {
            n = N;
            servers = new Dictionary<int, TCPConfig>(S);
            leader = 0;
            epoch = 0;
            counter = 0;
            Dictionary<string, TCPConfig> addrs = new Dictionary<string, TCPConfig>();
            foreach (var tcp in servers.Values)
            {
                addrs.Add(tcp.ip, tcp);
            }
            files = new FileSystem();
            followers = new HashSet<int>();
            proposals = new Dictionary<Proposal, List<TCPConfig>>();
            //history = new FileStream("history.txt", FileMode.CreateNew, FileAccess.ReadWrite);
            Console.WriteLine(n);
            FileStream hist = new FileStream("history.txt", FileMode.OpenOrCreate);
            hist.Close();


            thisAddress = servers[n];
            Console.WriteLine(thisAddress.dns);
            thisNode = new Node(n,thisAddress, addrs);
            mostCurrentServer = thisAddress;
            thisNode.Connect += new OnMsgHandler(OnConnect);
            thisNode.Create += new OnMsgHandler(OnCreate);
            thisNode.Delete += new OnMsgHandler(OnDelete);
            thisNode.Read += new OnMsgHandler(OnRead);
            thisNode.Append += new OnMsgHandler(OnAppend);
          /*  thisNode.Election += new OnMsgHandler(OnElection);
            thisNode.Coordinator += new OnMsgHandler(OnCoordinator);
            thisNode.Ok += new OnMsgHandler(OnOk); */
            thisNode.Ack += new OnMsgHandler(OnAck);
            thisNode.Commit += new OnMsgHandler(OnCommit);
            thisNode.Lock += new OnMsgHandler(OnLock);
            thisNode.Unlock += new OnMsgHandler(OnUnlock);
            thisNode.Proposal += new OnMsgHandler(OnProposal);
            thisNode.GetHistory += new OnMsgHandler(OnGetHistory);
            thisNode.SendHistory += new OnMsgHandler(OnSendHistory);
            thisNode.GetZxid += new OnMsgHandler(OnGetZxid);
            thisNode.Zxid += new OnMsgHandler(OnZxid);
           // thisNode.getConnections();
            Task socket = Task.Factory.StartNew(() => thisNode.getConnections());
            ServerIds = new Dictionary<int, zxid>();
            getZxids();

            holdElection();



        }

        public void getConnections()
        {
            thisNode.getConnections();

        }
        /// <summary>
        /// Holds election to get new leader
        /// </summary>
        public void holdElection()
        {
            phase = "election";
            //response = false;
            getZxids();
            Console.WriteLine("Holding election");
            Proposal election = new Proposal("election", new zxid(epoch,counter));
            proposals.Add(election, new List<TCPConfig>());
            proposals[election].Add(thisAddress);
            foreach (int p in ServerIds.Keys)
            {
                if (ServerIds[p] > ServerIds[n])
                {

                    sendProposal(election,servers[p]);
                    /*using (TcpClient client = new TcpClient(servers[p].dns, servers[p].port))
                    {
                        TCP t = new TCP(client);
                        t.sendTcpMessage(String.Format("ELECTION {0} {1}", epoch, counter));
                        Console.WriteLine("sent ELECTION to process {0}", p);

                    }*/
                }
            }
            Console.WriteLine("waiting");
            Func<bool> hasAck = delegate() { return (proposals[election].Count == servers.Count); };
            SpinWait.SpinUntil(hasAck, 5000);
            Console.WriteLine("waited");
            if (proposals[election].Count == 1)
            {
                leader = n;
                Proposal coordinator = new Proposal(String.Format("coordinator {0}", n), new zxid(epoch, counter));

                foreach (int s in ServerIds.Keys)
                {
                    if (ServerIds[s] < ServerIds[n])
                    {
                        followers.Add(s);
                        Console.WriteLine("test1");
                        sendProposal(coordinator,servers[s]);
                    }
                }
                proposals.Add(coordinator, new List<TCPConfig>());
                proposals[coordinator].Add(thisAddress);
                Dicover(coordinator);
                //end election
                //Proposal discover = new Proposal("discover", new);
              //  sendBroadcast();

            }
            else
            {
                response = false;
                Func<bool> hasCoord = delegate() { return response; };
                SpinWait.SpinUntil(hasCoord, 5000);
                //OnCoordinator handles changes if coord message is recieved
                if (!(response))
                {
                    proposals.Remove(election);
                    holdElection();
                }
            }
        }
        /// <summary>
        /// Replays proposals stored on disk, then searches for any additional missing proposals
        /// </summary>
        public void Recover()
        {
            holdElection();
        }

        public void Dicover(Proposal coord)
        {
            phase = "discover";
            if (leader == n)
            {
                //leader
                Func<bool> hasCoordQuorum = delegate() { return proposals[coord].Count > (followers.Count / 2); };
                SpinWait.SpinUntil(hasCoordQuorum);
                var e1 = epoch + 1;
                Proposal newEpoch = new Proposal(String.Format("newepoch {0}", e1),new zxid(epoch,counter));
                proposals.Add(newEpoch, new List<TCPConfig>());
                foreach (var tcp in proposals[coord])
                {
                    sendProposal(newEpoch, tcp);
                }


                Func<bool> hasEpochQuorum = delegate() { return (proposals[newEpoch].Count == proposals[coord].Count); };
                SpinWait.SpinUntil(hasEpochQuorum, 10000);
                if (mostCurrentServer != thisAddress)
                {
                    sendMessage("gethistory", mostCurrentServer);
                }




            }


        }

        private void AppendHistory(Proposal prop)
        {
            using (FileStream fHistory = new FileStream("history.txt", FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter sHistory = new StreamWriter(fHistory))
                {
                    sHistory.WriteLine("{0} {1} {2}", prop.z.epoch, prop.z.counter, prop.v);
                }
            }
        }

        private void OnGetHistory(object sender, MsgEventArgs e)
        {
            using (FileStream fHistory = new FileStream("history.txt", FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sHistory = new StreamWriter(fHistory))
                {
                    //overwrites proposal history
                    sHistory.Write(e.data);
                }
            }
        }
         private void OnSendHistory(object sender, MsgEventArgs e)
        {
            using (FileStream fHistory = new FileStream("history.txt", FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sHistory = new StreamReader(fHistory))
                {
                    var history = sHistory.ReadToEnd();
                    sendMessage(String.Format("history {0}", history), e.client);

                }


            }
        }

        private void ExecuteHistory(object sender, MsgEventArgs e)
        {
            using (FileStream fHistory = new FileStream("history.txt", FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (StreamReader sHistory = new StreamReader(fHistory))
                {
                    string line;
                    while ((line = sHistory.ReadLine())!=null)
                    {
                        char[] space = {' '};
                        string[] args = line.Split(space, 3);
                        Proposal thisProp = new Proposal(args[2], new zxid(Convert.ToInt32(args[0]), Convert.ToInt32(args[1])));
                        if (thisProp.z > lastId)
                        {
                            ProposalHandler(thisProp, sender, e.client);
                        }
                    }
                }
            }
        }
        private void sendMessage(string msg, TCPConfig tcp)
        {
            try
            {
                using (TcpClient client = new TcpClient(tcp.dns, tcp.port))
                {
                    TCP t = new TCP();
                    t.sendTcpMessage(msg,client.GetStream());
                    Console.WriteLine("Sent message {0} to {1}", msg, tcp.dns);
                    client.Close();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("sendMessage {0} {1}",ex.Message,ex.StackTrace);
                if (leader != 0)
                {
                    if (tcp == servers[leader])
                    {
                        holdElection();
                    }
                }

            }
        }


        public void sendProposal(Proposal p, TCPConfig tcp)
        {
            try
            {
                using (TcpClient client = new TcpClient(tcp.dns, tcp.port))
                {
                    TCP t = new TCP();
                    var text = String.Format("proposal {0} {1} {2}", (int)p.z.epoch, (int)p.z.counter, p.v);
                    t.sendTcpMessage(text,client.GetStream());
                    Console.WriteLine("Sent proposal ({0}, {1}, '{2}') to {3}", (int)p.z.epoch, (int)p.z.counter, p.v, tcp.dns);
                    client.Close();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("sendProposal {0} {1}",ex.Message, ex.StackTrace);
                if (leader != 0)
                {
                    if (tcp == servers[leader])
                    {
                        holdElection();
                    }
                }
            }
        }

        public Proposal sendBroadcast(string msg)
        {
            if (phase == "broadcast")
            {
                counter++;
                lastId = new zxid(epoch, counter);
                Proposal p = new Proposal(msg, new zxid(epoch, counter));
                proposals.Add(p, new List<TCPConfig>());
                proposals[p].Add(thisAddress);
                AppendHistory(p);
                foreach (int s in followers)
                {
                    sendProposal(p, servers[s]);
                }
                return p;
            }
            else
            {
                throw (new Exception("not broadcasting"));
            }

        }


        public void sendAck(MsgEventArgs e)
        {
            Console.WriteLine("ack {0} {1}",e.client.dns, e.client.port);
            sendMessage(String.Format("ack {0}", e.data), e.client);
        }

        private void OnConnect(object sender, MsgEventArgs e)
        {
            sendAck(e);
        }

        private void OnProposal(object sender, MsgEventArgs e)
        {
            sendAck(e);
            Proposal prop = parseProposal(e.data);
            ProposalHandler(prop, sender, e.client);






        }

        private void ProposalHandler(Proposal P, object sender, TCPConfig client)
        {
            char[] space = {' '};
            string[] args = P.v.Split(space, 2);
            Console.WriteLine("handling proposal ({0}, {1}, {2})", P.z.epoch, P.z.counter, P.v);


            if (args[0] == "election")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(null, client);
                OnElection(sender, msgArgs);
            }
            else if (args[0] == "coordinator")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(args[1], client);
                OnCoordinator(sender, msgArgs);
            }
            else if (args[0] == "newepoch")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(args[1], client);
                OnNewEpoch(sender, msgArgs);

            }
            else if (args[0] == "newleader")
            {

            }
            else if (args[0] == "create")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(args[1], client);
                OnCreate(sender, msgArgs);
            }
            else if (args[0] == "append")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(args[1], client);
                OnAppend(sender, msgArgs);
            }
            else if (args[0] == "delete")
            {
                MsgEventArgs msgArgs = new MsgEventArgs(args[1], client);
                OnDelete(sender, msgArgs);
            }
        }

        private void getZxids()
        {
            ServerIds.Remove(n);
            ServerIds.Add(n, new zxid(epoch, counter));
            foreach (int s in servers.Keys)
            {
                if (s != n)
                    sendMessage("getzxid",servers[s]);
            }
            Func<bool> hasIds = delegate() { return ServerIds.Count == servers.Count; };
            SpinWait.SpinUntil(hasIds, 5000);
        }

        private void OnGetZxid(object sender, MsgEventArgs e)
        {
            sendMessage(String.Format("zxid {0} {1} {2}", n, epoch, counter), e.client);
        }
        private void OnZxid(object sender, MsgEventArgs e)
        {
            char[] space = {' '};
            string[] args = e.data.Split(space);
            ServerIds.Remove(Convert.ToInt32(args[0]));
            ServerIds.Add(Convert.ToInt32(args[0]), new zxid(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])));

        }

        /// <summary>
        /// Handles create request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCreate(object sender, MsgEventArgs e)
        {
            if (phase != "election")
            {



                if (leader == n)
                {
                    counter++;
                    Proposal create =  sendBroadcast(String.Format("create {0}", e.data));

                }
                else
                {
                    LockFile(e.data);

                    sendMessage(String.Format("create {0}", e.data), servers[leader]);



                }
            }


            //create lock file
            //if follower
                //send request to leader to create lock file
            //if leader
                //check that lock file does not exist
                //if file does not exist, create file and alert followers that it has been created
            //wait for reply from leader
        }

        /// <summary>
        /// handles delete request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDelete(object sender, MsgEventArgs e)
        {
        }

        /// <summary>
        /// handles read request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRead(object sender, MsgEventArgs e)
        {
           //counter++;
            try
            {
                using (TcpClient client = new TcpClient(e.client.dns, e.client.port))
                {
                    TCP t = new TCP();
                    t.sendTcpMessage(files.ReadFile(e.data),client.GetStream());
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// handles append request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppend(object sender, MsgEventArgs e)
        {
            counter++;
        }

        /// <summary>
        /// handles election proposal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnElection(object sender, MsgEventArgs e)
        {

            if (phase != "election")
            {
                holdElection();
            }
        }

        /// <summary>
        /// handles coordinator proposal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCoordinator(object sender, MsgEventArgs e)
        {
            response = true;
            if ((n > Convert.ToInt32(e.data)) &&
                (phase != "election"))
            {
                holdElection();
            }
            else
            {
                leader = Convert.ToInt32(e.data);
                Dicover(parseProposal(e.data));
            }
        }

        private void OnNewEpoch(object sender, MsgEventArgs e)
        {
            if (leader != n)
            {
                if (Convert.ToInt32(e.data) > epoch)
                {
                    epoch = Convert.ToInt32(e.data);
                    sendAck(e);
                    //go to synch
                }

            }
        }


        /// <summary>
        /// handles Ok response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOk(object sender, MsgEventArgs e)
        {
            response = true;
            followers.Add(Convert.ToInt32(e.data));
        }



        /// <summary>
        /// handles ack response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAck(object sender, MsgEventArgs e)
        {

            Proposal prop = parseProposal(e.data);

            if (prop.z > lastId)
            {
                mostCurrentServer = e.client;
            }
            proposals[prop].Add(e.client);
            Console.WriteLine("got ack {0}", proposals[prop].Count);
            if (proposals[prop].Count > (followers.Count / 2))
            {
                foreach (int f in followers)
                {
                    sendMessage(String.Format("commit {0}", e.data), servers[f]);

                }
            }
        }

        private void Deliver(MsgEventArgs e)
        {
            char[] space = {' '};
            string[] args = e.data.Split(space, 2);
            if (args[0] == "create")
            {
                files.AddFile(args[1]);
                sendMessage(String.Format("Created file '{0}'", args[1]), e.client);
            }
            else if (args[0] == "append")
            {
                string[] fileargs = args[1].Split(space, 2);
                files.AppendFile(fileargs[0], fileargs[1]);
                sendMessage(String.Format("Appended '{0}' to file '{1}'", fileargs[0], fileargs[1]), e.client);
            }
            else if (args[0] == "delete")
            {
                files.DeleteFile(args[1]);
                sendMessage(String.Format("Deleted file {0}", args[1]), e.client);
            }

        }

        /// <summary>
        /// handles commit proposal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommit(object sender, MsgEventArgs e)
        {
            //Proposal prop = parseProposal(e.data);

            //stage for
        }

        private void LockFile(string filename)
        {
            try
            {
                using (TcpClient client = new TcpClient(servers[leader].dns, servers[leader].port))
                {
                    TCP t = new TCP();
                    t.sendTcpMessage(String.Format("lock {0}", filename),client.GetStream());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LockFile {0} {1}",ex.Message, ex.StackTrace);
            }
        }
        private void UnlockFile(string filename)
        {
            try
            {
                using (TcpClient client = new TcpClient(servers[leader].dns, servers[leader].port))
                {
                    TCP t = new TCP();
                    t.sendTcpMessage(String.Format("unlock {0}", filename),client.GetStream());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UnlockFile {0} {1}",ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// handles locking
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLock(object sender, MsgEventArgs e)
        {
            if (leader == n)
            {
                //if the server is the leader
                if (!(lockFiles.ContainsKey(e.data)))
                {
                    try
                    {
                        lockFiles.Add(e.data, new List<TCPConfig>());
                        using (TcpClient client = new TcpClient(e.client.dns, e.client.port))
                        {
                            TCP t = new TCP();
                            t.sendTcpMessage(String.Format("lock {0}", e.data),client.GetStream());
                        }

                    }
                    catch (Exception ex)
                    {
                        //probably means leader crashed
                        Console.WriteLine("OnLock {0} {1}",ex.Message, ex.StackTrace);
                        holdElection();
                    }

                }
                else
                {
                    lockFiles[e.data].Add(e.client);
                }
            }
            else
            {
                //follower obtained lock
                lockFiles.Add(e.data, new List<TCPConfig>());

            }
        }

        /// <summary>
        /// handles unlocking
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnlock(object sender, MsgEventArgs e)
        {
            if (leader == n)
            {
                if (lockFiles[e.data].Count > 0)
                {
                    //grant lock on next file
                    TCPConfig nextLock = lockFiles[e.data][0];
                    try
                    {
                        using (TcpClient client = new TcpClient(nextLock.dns, nextLock.port))
                        {
                            TCP t = new TCP();
                            t.sendTcpMessage(String.Format("lock {0}", e.data),client.GetStream());
                            lockFiles[e.data].RemoveAt(0);
                        }

                    }
                    catch (Exception ex)
                    {
                        //remove from queue and run unlock again
                        Console.WriteLine("OnUnlock {0} {1}",ex.Message, ex.StackTrace);
                        lockFiles[e.data].RemoveAt(0);
                        OnUnlock(sender, e);
                    }
                }
                else
                {
                    lockFiles.Remove(e.data);
                }
            }
            else
            {
                //delete lockfile and alert leader
                lockFiles.Remove(e.data);
                try
                {
                    using (TcpClient client = new TcpClient(servers[leader].dns, servers[leader].port))
                    {
                        TCP t = new TCP();
                        t.sendTcpMessage(String.Format("unlock {0}", e.data),client.GetStream());
                    }
                }
                catch (Exception ex)
                {
                    //probably means leader failed
                    Console.WriteLine("OnUnlock {0} {1}",ex.Message, ex.StackTrace);
                    holdElection();
                }
            }

        }

        private Proposal parseProposal(string msg)
        {
            char[] space = {' '};
            string[] args = msg.Split(space);
            return new Proposal(args[2], new zxid(Convert.ToInt32(args[0]), Convert.ToInt32(args[1])));
        }

    }
    public struct zxid
    {
        public int epoch;
        public int counter;

        public zxid(int e, int c)
        {
            epoch = e;
            counter = c;
        }

        public override bool Equals(Object o)
        {
            return ((o is zxid) && (this == (zxid) o));
        }

        public static bool operator ==(zxid a, zxid b)
        {
            return ((a.epoch == b.epoch) && (a.counter == b.counter));
        }
        public static bool operator !=(zxid a, zxid b)
        {
            return !(a == b);
        }

        public static bool operator <(zxid a, zxid b)
        {
            return ((a.epoch < b.epoch) ||
                    (a.epoch == b.epoch && a.counter < b.counter));
        }
        public static bool operator >(zxid a, zxid b)
        {
            return ((a.epoch > b.epoch) ||
                    (a.epoch == b.epoch && a.counter > b.counter));
        }
    }
    public struct Proposal
    {
        public string v;
        public zxid z;

        public Proposal(string value, zxid id)
        {
            v = value;
            z = id;
        }
        public override bool Equals(Object o)
        {
            return ((o is Proposal) && (this == (Proposal) o));
        }

        public static bool operator ==(Proposal a, Proposal b)
        {
            return ((a.v == b.v) && (a.z == b.z));
        }
        public static bool operator !=(Proposal a, Proposal b)
        {
            return !(a == b);
        }

    }
}
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Server
{

    public class ZAB
    {
        public int n;
        public int epoch;
        public int counter;
        public string phase;
        public int leader;
        public FileSystem files;
        public Dictionary<string, List<TCPConfig>> lockFiles;
        public TCPConfig thisAddress;
        public Node thisNode;
        public Dictionary<int, TCPConfig> servers;
        public HashSet<int> followers;
        public Dictionary<string, int> proposals;

        private bool response;


        public ZAB(int N, Dictionary<int, TCPConfig> S)
        {
            n = n;
            servers = S;
            leader = n;
            epoch = 0;
            counter = 0;
            files = new FileSystem();
            followers = new HashSet<int>();
            proposals = new Dictionary<string, int>();
            thisAddress = servers[n];

            thisNode = new Node(n, thisAddress);
            thisNode.Create += new OnMsgHandler(OnCreate);
            thisNode.Delete += new OnMsgHandler(OnDelete);
            thisNode.Read += new OnMsgHandler(OnRead);
            thisNode.Append += new OnMsgHandler(OnAppend);
            thisNode.Election += new OnMsgHandler(OnElection);
            thisNode.Coordinator += new OnMsgHandler(OnCoordinator);
            thisNode.Ok += new OnMsgHandler(OnOk);
            thisNode.Ack += new OnMsgHandler(OnAck);
            thisNode.Commit += new OnMsgHandler(OnCommit);
            thisNode.Lock += new OnMsgHandler(OnLock);
            thisNode.Unlock += new OnMsgHandler(OnUnlock);
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
            response = false;
            foreach (int p in servers.Keys)
            {
                if (p > n)
                {
                    using (TcpClient client = new TcpClient(servers[p].dns, servers[p].port))
                    {
                        TCP t = new TCP(client);
                        t.sendMessage(String.Format("ELECTION {0} {1}", epoch, counter));
                        Console.WriteLine("sent ELECTION to process {0}", p);

                    }
                }
            }
            Thread.Sleep(3000);
            if (!(response))
            {
                leader = n;
                foreach (int s in servers.Keys)
                {
                    if (s < n)
                    {
                        using (TcpClient client = new TcpClient(servers[s].dns, servers[s].port))
                        {
                            TCP t = new TCP(client);
                            t.sendMessage(String.Format("COORDINATOR {0}", n));
                        }
                    }
                }
                //end election

            }
            else
            {
                response = false;
                Thread.Sleep(3000);
                //OnCoordinator handles changes if coord message is recieved
                if (!(response))
                {
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
            thisNode.Recover(0);

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
                counter++;
                if (leader == n)
                {
                    foreach (int p in followers)
                    {

                    }
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
            counter++;
        }

        /// <summary>
        /// handles read request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRead(object sender, MsgEventArgs e)
        {
           //counter++;
            Console.WriteLine(files.ReadFile(e.data));

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
            using (TcpClient client = new TcpClient(e.client.dns, e.client.port))
            {
                TCP t = new TCP(client);
                t.sendMessage(String.Format("OK {0}", n));
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
            proposals[e.data] += 1;
            if (proposals[e.data] > (followers.Count / 2))
            {
                foreach (int f in followers)
                {
                    try
                    {
                        //send commit
                         using
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// handles commit proposal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommit(object sender, MsgEventArgs e)
        {
            //stage for
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
                            TCP t = new TCP(client);
                            t.sendMessage(String.Format("LOCK {0}", e.data));
                        }

                    }
                    catch (Exception ex)
                    {
                        //probably means leader crashed
                        Console.WriteLine(ex.Message);
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
                            TCP t = new TCP(client);
                            t.sendMessage(String.Format("LOCK {0}", e.data));
                            lockFiles[e.data].RemoveAt(0);
                        }

                    }
                    catch (Exception ex)
                    {
                        //remove from queue and run unlock again
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
                        TCP t = new TCP(client);
                        t.sendMessage(String.Format("UNLOCK {0}", e.data));
                    }
                }
                catch (Exception ex)
                {
                    //probably means leader failed
                    Console.WriteLine(ex.Message);
                    holdElection();
                }
            }

        }
    }
}
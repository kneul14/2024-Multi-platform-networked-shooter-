using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static System.Net.Mime.MediaTypeNames;

namespace _6018CEM____Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerClass serverClass = new ServerClass();
            serverClass.ServerStartUp();
            serverClass.ServerCloseDown();
        }
    }

    //public class gameObjectInfo
    //{
    //    public string name { get; set; }
    //    public string type { get; set; }
    //    public string position { get; set; }
    //    public int health { get; set; }
    //}

    public class ServerClass
    {
        static int lastAssignedGlobalID = 100;                                                                 // GlobalIDs:)
        static Dictionary<int, byte[]> gameState = new Dictionary<int, byte[]>();                              // Stores each clients data
        static List<IPEndPoint> existingClients = new List<IPEndPoint>();                                      // Should make it so multiple clients can connect
       
        public void ServerStartUp()
        {
            int recv = 0;
            string address = "127.0.0.1";                                                                      // 127.0.0.1 as hexadecimal. This IP address allows the machine to connect to and communicate with itself.
            int port = 9050;
            byte[] data = new byte[4092];

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(address), port);
            Socket newSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            newSock.Blocking = false;
            newSock.Bind(ipep);                                                                                // Bind the socket to our given IP and Port

            Console.WriteLine("Socket Open");
            Console.WriteLine("Press esc to close server.");

            RunServer(recv, newSock, data);
        }
        public void RunServer(int recv, Socket newSock, byte[] data)
        {
            ConsoleKeyInfo close;
            bool state = false;
                                                                                                               //make a bool that changes to false
            while (!state)
            {
                // Server runs until it is closed down with the esc key.
                if (Console.KeyAvailable)
                {
                    close = Console.ReadKey();
                    if (close.Key == ConsoleKey.Escape)
                    {
                        state = true;
                    }
                }
                try
                {
                    IPEndPoint sender     = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint remote       = (EndPoint)(sender);

                    // Clears the data buffer so the previous memory isn't held on to
                    data = new byte[4092];
                                                                                                               //Receives a datagram into the data buffer
                    recv                  = newSock.ReceiveFrom(data, ref remote);

                                                                                                               //This will show the senders/clients IP Address and Port Number
                    //Console.WriteLine("Message received from " + remote.ToString());

                    string receivedMsg    = Encoding.ASCII.GetString(data, 0, recv);

                    if (receivedMsg.Contains("Unreal") || receivedMsg.Contains("Unity")) 
                    {
                        Console.WriteLine(receivedMsg);
                    }

                    if (receivedMsg.Contains("UNID"))
                    {
                        char sentinelChar = ':';
                        bool isIT         = receivedMsg.Contains(sentinelChar);

                        if (isIT)
                        {
                            string UNID = AssignUNIDs(receivedMsg);
                            newSock.SendTo(Encoding.ASCII.GetBytes(UNID), Encoding.ASCII.GetBytes(UNID).Length, SocketFlags.None, remote);
                        }
                    }

                    if (receivedMsg.Contains("PlayerDamaged"))
                    {
                        string damageInfo = PlayerHealthUpdate(receivedMsg, data, newSock);
                        //newSock.SendTo(Encoding.ASCII.GetBytes(damageInfo), Encoding.ASCII.GetBytes(damageInfo).Length, SocketFlags.None, remote);
                    }

                    if (receivedMsg.Contains("PlayerDestroyed"))
                    {
                        string destroyPlayerInfo = PlayerStateUpdate(receivedMsg, data, newSock);
                        //newSock.SendTo(Encoding.ASCII.GetBytes(destroyPlayerInfo), Encoding.ASCII.GetBytes(destroyPlayerInfo).Length, SocketFlags.None, remote);
                    }

                    if (receivedMsg.Contains("PositionInformation"))
                    {
                        string posINFO = PositionUpdate(receivedMsg, data, newSock);
                    }

                    ServerConnect(newSock, remote);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    //Do nothing
                }
            }
        }
        public void ServerCloseDown()
        {
            ConsoleKeyInfo close;
            Console.WriteLine(" Server closed.");
            do
            {
                close = Console.ReadKey();
                                                                         // do something with each key press until escape key is pressed
            } while (close.Key != ConsoleKey.Escape);

            //newSock.Close();
        }

        public void Greeting(Socket newSock, EndPoint remote)
        {
            string connected = "Connected to server...";
            newSock.SendTo(Encoding.ASCII.GetBytes(connected), Encoding.ASCII.GetBytes(connected).Length, SocketFlags.None, remote);
        }

        // Makes it so you can connect servers.
        public void ServerConnect(Socket newSock, EndPoint remote)
        {                                                                
             bool doesIPExist            = false;
             IPEndPoint senderIPEndPoint = (IPEndPoint)remote;
             
            
            // Check if the client IP already exists in the server's IP list
            foreach (IPEndPoint ep in existingClients)
            {
                 if (senderIPEndPoint.ToString().Equals(ep.ToString()))
                 {
                     doesIPExist         = true;
                 }
            }
                                                                         // If the client IP doesn't exist make it 
             if (!doesIPExist)
             {                
                existingClients.Add(senderIPEndPoint);
                Greeting(newSock, remote);
                Console.WriteLine("A new client just connected! There are now " + existingClients.Count + " clients connected.");
             }

                                                                                                               // Send the game state to each client
             foreach (IPEndPoint ep in existingClients)
             {
                 //Console.WriteLine("Sending client state to " + ep.ToString());

                 if (ep.Port != 0)
                 {
                     foreach (KeyValuePair<int, byte[]> kvp in gameState)
                     {
                         newSock.SendTo(kvp.Value, kvp.Value.Length, SocketFlags.None, ep);
                         //Console.WriteLine("Data Sent");
                     }
                 }
             }
            
        }



        // Server data handling
        string AssignUNIDs(string receivedMsg)
        {
            //Console.WriteLine(receivedMsg.Substring(receivedMsg.IndexOf(':')));

            int getObjectLocalID = Int32.Parse(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1));           // Gets the Local ID and parses it to int
            lastAssignedGlobalID++;
            string UNID          = ("Assigned UNID:" + getObjectLocalID + ";" + lastAssignedGlobalID);        // Prepares UNID data to be sent to the client

            //Console.WriteLine(UNID);

            return UNID;
        }
        string PositionUpdate(string receivedMsg, byte[] data, Socket newSock)
        {
            //Console.WriteLine(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1));

            int getObjectGlobalID          = Int32.Parse(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1, 3)); // Retrieves the global id which has an index of i + 1 from the ':'
            string UNID                    = ("GlobalID is:" + getObjectGlobalID);
            string positionInfo            = ("PositionInformation is:" + receivedMsg.Substring(receivedMsg.IndexOf(':') + 1));
            //Console.WriteLine(UNID); 
            //Console.WriteLine(positionInfo);


            if (gameState.ContainsKey(getObjectGlobalID))
            {
                gameState[getObjectGlobalID] = data;                                                           //update the byte array value with the new packet as this is the newest data
            }
            else
            {
                gameState.Add(getObjectGlobalID, data);                                                        //add the object id and the byte array data into the dictionary as its a new object
            }

            return positionInfo;
        }
        string PlayerHealthUpdate(string receivedMsg, byte[] data, Socket newSock)
        {
            Console.WriteLine(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1));

            int getObjectGlobalID = Int32.Parse(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1, 3)); // Retrieves the global id which has an index of i + 1 from the ':'
            string UNID = ("GlobalID is:" + getObjectGlobalID);
            string damageInfo = ("PlayerDamaged:" + receivedMsg.Substring(receivedMsg.IndexOf(':') + 1));
            Console.WriteLine(damageInfo);

            // Send the updated player state information to all connected clients
            foreach (IPEndPoint ep in existingClients)
            {
                if (ep.Port != 0)
                {
                    foreach (KeyValuePair<int, byte[]> kvp in gameState)
                    {
                        newSock.SendTo(Encoding.ASCII.GetBytes(damageInfo), Encoding.ASCII.GetBytes(damageInfo).Length, SocketFlags.None, ep);

                    }
                }
            }

            return damageInfo;
        }

        string PlayerStateUpdate(string receivedMsg, byte[] data, Socket newSock)
        {
            Console.WriteLine(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1));

            int getObjectGlobalID = Int32.Parse(receivedMsg.Substring(receivedMsg.IndexOf(':') + 1, 3)); // Retrieves the global id which has an index of i + 1 from the ':'
            string UNID = ("GlobalID is:" + getObjectGlobalID);
            string destroyedPlayerInfo = ("PlayerDestroyed:" + getObjectGlobalID);
            Console.WriteLine(destroyedPlayerInfo);

            //gameState.Remove(getObjectGlobalID);

            // Send the updated player state information to all connected clients
            foreach (IPEndPoint ep in existingClients)
            {

                if (ep.Port != 0)
                {
                    foreach (KeyValuePair<int, byte[]> kvp in gameState)
                    {
                        newSock.SendTo(Encoding.ASCII.GetBytes(destroyedPlayerInfo), Encoding.ASCII.GetBytes(destroyedPlayerInfo).Length, SocketFlags.None, ep);

                    }
                }
            }

                    return destroyedPlayerInfo;
        }
    }
}
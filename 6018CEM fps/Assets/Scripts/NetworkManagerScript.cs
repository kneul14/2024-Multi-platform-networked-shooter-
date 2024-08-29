using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEditor.VersionControl;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEditor.Sprites;
using UnityEngine.UI;
using TMPro.Examples;
using TMPro;

public class NetworkManagerScript : MonoBehaviour
{
    #region UPDClient Setup
    static string address = "127.0.0.1"; //127.0.0.1 as hexadecimal. This IP address allows the machine to connect to and communicate with itself.
    static int port = 9050;
    struct UpdState { public UdpClient u; public IPEndPoint e; };
    static UpdState state;
    static UdpClient client = new UdpClient();
    #endregion

    //Our server IP. This is set to local (127.0.0.1) on socket 9050.
    static IPEndPoint ep = new IPEndPoint(IPAddress.Parse(address), port);

    #region Incoming data handling
    char sentinelChar1 = ':', sentinelChar2 = ';';
    int indexChar1, indexChar2;

    string receivedMsg = "";
    byte[] receiveBytes;

    #endregion

    public List<NetworkGameObjectScript> networkObjects;
    public List<NetworkGameObjectScript> worldState;

    public GameObject networkedPlayerPrefab; // For instantiation when a new player joins.

    public bool isConnected = false, isServerMsg = false;
    public string serverMsg = "";

    void ReceiveAsyncCallback(IAsyncResult result)
    {
        //Get the packet
        //Function description in documentation = Ends a pending asynchronous receive.
        receiveBytes = client.EndReceive(result, ref ep);

        //Decode the packet 
        receivedMsg = Encoding.ASCII.GetString(receiveBytes);

        //Display the packet 
        Debug.Log("Received " + receivedMsg + " from " + ep.ToString());


        if (receivedMsg.Contains("UNID"))
        {
            SetUNIDs(receivedMsg);
        }

        if (receivedMsg.Contains("Connected to server"))
        {
            isConnected = true;
            isServerMsg = true;
            serverMsg = receivedMsg;
        }

        //Self-callback, meaning this loops infinitely 
        //The same function you used in the Start() function
        //Function description in documentation = Receives a datagram from a remote host asynchronously.
        client.BeginReceive(ReceiveAsyncCallback, state);
    }


    // Start is called before the first frame update
    void Start()
    {
        worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObjectScript>());

        client.Connect(ep);

        Debug.Log("Client complete...");

        // * Move to Update() eventually
        string myMessage = "I'm a Unity client - Hi!";
        byte[] array = Encoding.ASCII.GetBytes(myMessage);
        client.Send(array, array.Length);

        client.BeginReceive(ReceiveAsyncCallback, state);

        RequestUNIDs();

        StartCoroutine(SendNetworkUpdates());
        StartCoroutine(UpdateWorldState());
    }

    // Update is called once per frame
    void Update()
    {
    }

    void GetNetworkObjects()
    {
        networkObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObjectScript>());
    }

    #region UNID Handling
    void RequestUNIDs()
    {
        networkObjects = new List<NetworkGameObjectScript>();
        GetNetworkObjects();
        for (int i = 0; i < networkObjects.Count; i++)
        {
            if (networkObjects[i].isLocallyOwned && networkObjects[i].uniqueAssignedNetworkID == 0)
            {
                string myMessage = "Please give UNID for the game object that has the localID:" + networkObjects[i].GetComponent<NetworkGameObjectScript>().localID.ToString();
                byte[] array = Encoding.ASCII.GetBytes(myMessage);
                client.Send(array, array.Length);
                Console.WriteLine(myMessage);
            }
        }
    }
    void SetUNIDs(string receivedMsg)
    {

        bool isColon = receivedMsg.Contains(sentinelChar1);
        bool isSemicolon = receivedMsg.Contains(sentinelChar2);

        if (isColon)
        {
            indexChar1 = receivedMsg.IndexOf(sentinelChar1);
            if (isSemicolon)
            {
                indexChar2 = receivedMsg.IndexOf(sentinelChar2);


                string localIDString = receivedMsg.Substring(indexChar1 + 1, indexChar2 - indexChar1 - 1);
                int getObjectLocalID = Int32.Parse(localIDString);
                Debug.Log("localID:" + localIDString);


                string globalIDString = receivedMsg.Substring(indexChar2 + 1);
                int globalVal = Int32.Parse(globalIDString);
                Debug.Log("globalID:" + globalVal);

                for (int i = 0; i < networkObjects.Count; i++)
                {
                    if (networkObjects[i].localID == getObjectLocalID)
                    {
                        networkObjects[i].uniqueAssignedNetworkID = globalVal;
                    }
                }
            }
        }
    }
    //public void RequestUNID(NetworkGameObjectScript go)
    //{
    //    if(go.uniqueAssignedNetworkID == 0)
    //    {
    //        string myMessage = "Please give UNID for the game object that has the localID:" + go.GetComponent<NetworkGameObjectScript>().localID.ToString();
    //        byte[] array = Encoding.ASCII.GetBytes(myMessage);
    //        client.Send(array, array.Length);
    //        Console.WriteLine(myMessage);
    //    }
    //}
    #endregion

    #region GlobalID retrieval
    int GetGlobalID(string receivedMsg)
    {
        // This is so I don't have to keep tying the code out over and over again.
        //Returns the GlobalID that the packet sends.
        char[] delims = { ';', ':' };
        return Int32.Parse(receivedMsg.Split(delims)[1]);
    }
    #endregion

    #region Death and Damage handling


    // Method to notify the server about object destruction
    public void DamageNotification(float damage, int UNID)
    {
        string message = "PlayerDamaged:" + UNID + ";" + damage;
        byte[] data = Encoding.ASCII.GetBytes(message);
        client.Send(data, data.Length);
    }

    public void DeathNotification(int UNID)     // To send to server when this client kills a player
    {
        string message = "PlayerDestroyed:" + UNID;
        byte[] data = Encoding.ASCII.GetBytes(message);
        client.Send(data, data.Length);
    }

    void DestroyDead(int UNID)
    {
        // world state with UNID of that UNID
        foreach (NetworkGameObjectScript nGO in worldState)
        {
            if (nGO.uniqueAssignedNetworkID == UNID)
            {
                Debug.Log("destroying player:" + UNID);
                worldState.Remove(nGO);
                nGO.gameObject.SetActive(false);
                //Destroy(nGO.gameObject);
            }
        }
    }
    #endregion

    // This function will continuously send transform information about all the objects this client currently owns and has a valid unique global ID.
    IEnumerator SendNetworkUpdates()
    {
        while (true)
        {
            networkObjects = new List<NetworkGameObjectScript>();
            GetNetworkObjects();
            for (int i = 0; i < networkObjects.Count; i++)
            {
                if (networkObjects[i].isLocallyOwned && networkObjects[i].uniqueAssignedNetworkID != 0) // A check to make sure that the absolute correct gameobjects are updated
                {
                    Vector3 currentPosition = networkObjects[i].transform.position;

                    if (currentPosition != networkObjects[i].previousPosition)
                    {
                        byte[] data = networkObjects[i].ToPacket();

                        client.Send(data, data.Length);
                    }// we send the packet we get from the NObject to the server
                }
            }
            yield return new WaitForSeconds(.2f);                                                      //send a packet every 200 milliseconds
        }
    }

    IEnumerator UpdateWorldState()
    {
        while (true)
        {
            // Find all the network game objects in the scene.
            worldState = new List<NetworkGameObjectScript>();
            worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObjectScript>());

            // This will change either when the first packet ever arrives or when a new packet comes in.
            string theReceivedMsg = receivedMsg;

            bool doesObjectExist = false; // For later when checking if the GO is in the scene

            if (theReceivedMsg.Contains("PlayerDamaged"))
            {
                char[] delims = { ';', ':' };
                int UNID = GetGlobalID(theReceivedMsg);
                int damage = Int32.Parse(theReceivedMsg.Split(delims)[2]);

                // world state with UNID of that UNID
                foreach (NetworkGameObjectScript nGO in worldState)
                {
                    if (nGO != null && nGO.uniqueAssignedNetworkID == UNID)
                    {
                        // minus the damage from their health.
                        nGO.currentHealth -= damage;
                    }
                }

                Debug.Log(damage + "hello");
            }
            else if (theReceivedMsg.Contains("PlayerDestroyed"))
            {
                int UNID = GetGlobalID(theReceivedMsg);
                Debug.Log(theReceivedMsg);

                // Destroy the game object with that UNID so now even if this client didn't kill the player, they will still die in the scene
                DestroyDead(UNID);
            }
            //Check if the received string is position data
            else if (theReceivedMsg.Contains("PositionInformation"))
            {
                int UNID = GetGlobalID(theReceivedMsg);

                if (UNID != 0)
                {
                    // For every network GameObject in the scene
                    foreach (NetworkGameObjectScript nGO in worldState)
                    {
                        Debug.Log("World state size: " + worldState.Count);
                        if (nGO.uniqueAssignedNetworkID == 0)
                            doesObjectExist = true;

                        // If it's unique ID matches the GlobalID sent by the packet, update it's position
                        if (nGO.uniqueAssignedNetworkID == UNID)
                        {
                            // Only update it if we don't own it - you might want to try disabling and seeing the effect
                            if (nGO.isLocallyOwned != true)
                            {
                                Debug.Log("Found - update object");
                                doesObjectExist = true; // If the UNID matches then the GO is in the world
                                nGO.FromPacket(theReceivedMsg);

                            }
                            else
                                doesObjectExist = true;
                        }
                    }

                    if (doesObjectExist == false)
                    {
                        Debug.Log("Not found - spawn object");
                        // Instantiate a network object from the prefab
                        GameObject newPlayerConnected = Instantiate(networkedPlayerPrefab);
                        //newPlayerConnected.transform.SetParent(GameObject.FindGameObjectWithTag("Alien Container").transform);

                        // Set the global ID of the instantiated object from the packet data
                        newPlayerConnected.GetComponent<NetworkGameObjectScript>().SetUNID(GetGlobalID(theReceivedMsg));

                        // Set all the data of the instantiated object from the packet data
                        newPlayerConnected.GetComponent<NetworkGameObjectScript>().FromPacket(receiveBytes);

                    }

                }
                // Do nothing:))                
            }
            // We will only iterate the coroutine if the incoming string changes from the cached away local variable.
            yield return new WaitUntil(() => !receivedMsg.Equals(theReceivedMsg));
        }
    }
}
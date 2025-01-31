using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using System;
using System.Text;
using UnityEditor.VersionControl;
using static UnityEngine.InputSystem.InputRemoting;
using UnityEngine.UIElements;

public class NetworkGameObjectScript : MonoBehaviour
{
    public bool isLocallyOwned;                // Is the Game Object owned by this client?
    public int uniqueAssignedNetworkID;        // Global ID (UNID)
    public int localID;                        // Local ID
    public static int lastAssignedLocalID = 0; // It belongs to the class, rather than a specific instance.

    public int maxHealth = 100, currentHealth;

    public NetworkManagerScript networkManagerScript;
    public EnemyScript enemyScript;
    public GunScript gunScript;
    public bool isAlive = true;

    public Vector3 previousPosition;

    private void Awake()
    {
        if (isLocallyOwned)
            localID = lastAssignedLocalID++;

        networkManagerScript = GameObject.FindObjectOfType<NetworkManagerScript>(); // Automatically finds the NWMScript so that any object with this script attached will be able to access variables from it. 
        if (this.gameObject.tag == "Player")
            gunScript = gameObject.GetComponentInChildren<GunScript>();
        if (this.gameObject.tag == "Alien")
            enemyScript = gameObject.GetComponentInChildren<EnemyScript>();

        currentHealth = maxHealth;

        previousPosition = transform.position;

        //tried to make it so that the UNID is requested in the Awake() of each GO but it seems to turn off the script?
        //RunFunc(isLocallyOwned);
    }
    private void Update()
    {
        if (gunScript != null)
        {
            if (gunScript.enemyHit == true)
            {
                Debug.Log("Gun has hit an enemy!");
            }

        }

        //if (!isAlive) GameObject.Destroy(gameObject);


        if (currentHealth < 0 || currentHealth == 0)
        {
            this.gameObject.SetActive(false);
        }

    }

    /// <summary>
    /// You may want to consider making these virtual methods to allow them to be overridden.
    /// Doing this would allow the creation of different subclasses that have specific types of data and behaviours.
    /// </summary>
    /// <returns></returns>
    public byte[] ToPacket()
    {
        if (isLocallyOwned)
        {

            String myMessage = "PositionInformation:" + uniqueAssignedNetworkID + ";" + transform.position.x + ";" + transform.position.y + ";" + transform.position.z
                                                      + ";" + transform.rotation.w + ";" + transform.rotation.x + ";" + transform.rotation.y + ";" + transform.rotation.z + ";" + "1";
            byte[] packet = Encoding.ASCII.GetBytes(myMessage);

            return packet;
        }
        return null;
    }
    public void FromPacket(byte[] packet)
    {
        //Decode the packet 
        string receivedMsg = Encoding.ASCII.GetString(packet);

        // These are delimiters, they will be used to break up the incoming string
        char sentinelChar1 = ':', sentinelChar2 = ';';
        int indexChar1 = receivedMsg.IndexOf(sentinelChar1);
        int indexChar2 = receivedMsg.IndexOf(sentinelChar2);

        String UNID = receivedMsg.Substring(sentinelChar1 + 1, indexChar2);    // UNID is separated from the rest of the info
        String transData = receivedMsg.Substring(indexChar2 + 1);                   // Gets all transform data as a substring

        string[] transFloats = transData.Split(';');                                // Splits up the string into digestible values

        float positionX = float.Parse(transFloats[0]);
        float positionY = float.Parse(transFloats[1]);
        float positionZ = float.Parse(transFloats[2]);

        float rotationW = float.Parse(transFloats[3]);
        float rotationX = float.Parse(transFloats[4]);
        float rotationY = float.Parse(transFloats[5]);
        float rotationZ = float.Parse(transFloats[6]);

        int client = int.Parse(transFloats[7]);
        if (client == 1) //unity
        {

            this.transform.position = new Vector3(positionX, positionY, positionZ);     // Sets the position of the object for the alien client
            this.transform.rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW); // Sets the rotation of the object for the alien client
        }

        if (client == 2) //unreal
        {
            this.transform.position = new Vector3(positionX, positionZ, positionY);     // Sets the position of the object for the alien client
            this.transform.rotation = new Quaternion(rotationX, rotationZ, rotationY, rotationW); // Sets the rotation of the object for the alien client
        }
    }
    public void FromPacket(string receivedMsg)
    {
        //Decode the packet 

        // These are delimiters, they will be used to break up the incoming string
        char sentinelChar1 = ':', sentinelChar2 = ';';
        int indexChar1 = receivedMsg.IndexOf(sentinelChar1);
        int indexChar2 = receivedMsg.IndexOf(sentinelChar2);

        String UNID = receivedMsg.Substring(sentinelChar1 + 1, indexChar2);    // UNID is separated from the rest of the info
        String transData = receivedMsg.Substring(indexChar2 + 1);                   // Gets all transform data as a substring

        string[] transFloats = transData.Split(';');                                // Splits up the string into digestible values

        float positionX = float.Parse(transFloats[0]);
        float positionY = float.Parse(transFloats[1]);
        float positionZ = float.Parse(transFloats[2]);

        float rotationW = float.Parse(transFloats[3]);
        float rotationX = float.Parse(transFloats[4]);
        float rotationY = float.Parse(transFloats[5]);
        float rotationZ = float.Parse(transFloats[6]);

        int client = int.Parse(transFloats[7]);

        if (client == 1) //unity
        {

            this.transform.position = new Vector3(positionX, positionY, positionZ);     // Sets the position of the object for the alien client
            this.transform.rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW); // Sets the rotation of the object for the alien client
        }

        if (client == 2) //unreal
        {
            this.transform.position = new Vector3(positionX, positionZ, -positionY);     // Sets the position of the object for the alien client
            this.transform.rotation = new Quaternion(rotationX, rotationZ, rotationY, rotationW); // Sets the rotation of the object for the alien client
        }
    }

    // Attempted to get each gameObject to request their own UNIDs...
    void RunFunc(bool locallyO)
    {
        if (locallyO)
        {
            //networkManagerScript.RequestUNID(this);

        }
        else
        {
            // do nothing
        }
    }

    public void SetUNID(int unid)
    {
        uniqueAssignedNetworkID = unid;
    }

}

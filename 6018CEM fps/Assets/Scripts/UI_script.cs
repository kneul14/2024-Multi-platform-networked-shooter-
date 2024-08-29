using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.UI;

public class UI_script : MonoBehaviour
{
    public string msg, lastMsg;
    string lastMSGSent;
    public Text newestMSG;
    public Text middleMSG;
    public Text oldestMSG;
    public TextMeshProUGUI serverMSG;
    public TextMeshProUGUI health;
    float delay = 3f;

    public NetworkManagerScript NetworkManagerScript;
    public NetworkGameObjectScript NetworkGameObjectScript;

    // Start is called before the first frame update
    void Start()
    {
        DisplayMsg(msg);
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManagerScript.isServerMsg)
        {
            DisplayMsg(NetworkManagerScript.serverMsg);
        }

        if (serverMSG != null)
        {
            if (msg == lastMsg)
            {
                StartCoroutine(ClearMsg(msg, lastMsg));
            }
            else
            {
                lastMsg = msg; // Update lastMsg when message changes
            }
        }

        if (health != null)
        {
            health.text = NetworkGameObjectScript.currentHealth.ToString();
        }

        ChatUpdate();
    }

    public void DisplayMsg(string newMsg) 
    {
        msg = newMsg;
        serverMSG.text = msg;
    }

    void ChatUpdate()
    {
        if (lastMSGSent != newestMSG.text)
        {
            // Update oldest message with the previous middle message
            oldestMSG.text = middleMSG.text;

            // Update middle message with the previous newest message
            middleMSG.text = newestMSG.text;

            // Update lastMSGSent to track the newest message
            lastMSGSent = newestMSG.text;
        }
    }

    IEnumerator ClearMsg(string msg, string lastMsg)
    {
        yield return new WaitForSeconds(delay); // After 3 seconds clear the message
        DisplayMsg(""); // Clear the message
        NetworkManagerScript.serverMsg = "";
    }
}

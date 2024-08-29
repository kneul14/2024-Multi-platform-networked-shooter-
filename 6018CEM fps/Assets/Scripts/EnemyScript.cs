using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;
using UnityEngine.UI;
using TMPro;

public class EnemyScript : MonoBehaviour
{
    [SerializeField] private HealthBarScript HealthBar;
    [SerializeField] private TextMeshProUGUI unidText;
    public bool isAlive = true;

    public NetworkGameObjectScript networkGameObjectScript;
    public NetworkManagerScript networkManagerScript;
    public int UNID;

    // Start is called before the first frame update
    void Start()
    {
        networkManagerScript = GameObject.FindObjectOfType<NetworkManagerScript>();
        networkGameObjectScript = gameObject.GetComponent<NetworkGameObjectScript>();
        UNID = networkGameObjectScript.uniqueAssignedNetworkID;

        if (HealthBar != null) HealthBar.UpdateHealthBar(networkGameObjectScript.currentHealth, networkGameObjectScript.maxHealth);

        if (unidText != null) unidText.text = UNID.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (unidText != null) unidText.transform.LookAt(GameObject.Find("UNITY PLAYER").transform.position);
    }

    public void TakeDamage(float d)
    {
        //lastHealth = enemyCurrentHealth;
        //enemyCurrentHealth -= d;
        HealthBar.UpdateHealthBar(networkGameObjectScript.currentHealth, networkGameObjectScript.maxHealth);
        if (networkGameObjectScript.currentHealth < 0 || networkGameObjectScript.currentHealth == 0f)
        {
            networkManagerScript.GetComponent<NetworkManagerScript>().DeathNotification(UNID);
        }
        else
        {
            networkManagerScript.GetComponent<NetworkManagerScript>().DamageNotification(d, UNID);
        }
    }
}

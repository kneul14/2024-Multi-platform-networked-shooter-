using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
            
    }

    void OnTriggerEnter(Collider objectName)
    {
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }
}

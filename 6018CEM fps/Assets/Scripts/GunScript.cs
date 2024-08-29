using UnityEngine;

public class GunScript : MonoBehaviour
{
    public enum GunType {nullGun, pistol, riffle };
    public GunType gunType;

    public GameObject bullet;
    public GameObject bulletSpawnPoint;

    public float gunDamage;
    public float gunRange;
    public float gunImpactForce = 30f;

    public float gunFireRate = 5f;
    public float timeTilNextBullet = 0f;

    public int clientGID = 0;
    public GameObject pistol, riffle;

    public bool enemyHit=false;

    public Camera fpsCamera;

    // Start is called before the first frame update
    void Start()
    {
        gunType = GunType.nullGun;
        pistol = GameObject.FindGameObjectWithTag("pistol");
        riffle = GameObject.FindGameObjectWithTag("riffle");

        pistol.SetActive(false);
        riffle.SetActive(false);

        fpsCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();        
    }

    // Update is called once per frame
    void Update()
    {
        if (gunType == GunType.pistol)
        {
            gunDamage = 25;
            gunRange = 75f;
        }
        if (gunType == GunType.riffle)
        {
            gunDamage = 45;
            gunRange = 1000f;
        }

        if (Input.GetKeyDown("f")) // Left mouse button clicked
        {
            SelectGun();

        }

        if (Input.GetMouseButtonDown(0) && gunType != GunType.nullGun && gunType != GunType.riffle) // Left mouse button clicked
        {
            ShootGun();
        }
        else if (Input.GetMouseButton(0) && Time.time > timeTilNextBullet && gunType == GunType.riffle) // Left mouse button held
        {
            timeTilNextBullet = Time.time + 1 / gunFireRate;
            ShootGun();
        }
    }

    void ShootGun()
    {
        GameObject currentBullet = Instantiate(bullet, bulletSpawnPoint.transform.position, bulletSpawnPoint.transform.rotation);

        currentBullet.GetComponent<Rigidbody>().AddForce(fpsCamera.transform.forward * 100, ForceMode.Impulse);

        RaycastHit hit;
        if(Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, gunRange))
        {
            //Debug.Log(hit.transform.name);

            EnemyScript enemy = hit.transform.GetComponent<EnemyScript>();
            if(enemy != null)
            {
                clientGID = enemy.UNID;
                enemy.TakeDamage(gunDamage);
                enemyHit = true;
            }
        }

    }

    void SelectGun()
    {
        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, 100))
        {
            Debug.Log(hit.transform.name);
            if(hit.transform.name == "Pistol Box")
            {
                riffle.gameObject.SetActive(false);
                gunType = GunType.pistol;
                pistol.gameObject.SetActive(true);
                bulletSpawnPoint = GameObject.FindWithTag("Bullet Spawner");
            }
            if (hit.transform.name == "Riffle Box")
            {
                pistol.gameObject.SetActive(false);
                gunType = GunType.riffle;
                riffle.gameObject.SetActive(true);
                bulletSpawnPoint = GameObject.FindWithTag("Bullet Spawner");
            }
        }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image healthLevel;

    // Update is called once per frame
    void Update()
    {
        if (slider.value < 0.30f)
            Healthbelow();

        slider.transform.LookAt(GameObject.Find("UNITY PLAYER").transform.position);
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (slider != null)
        {
            // Gets the percentile representation of the
            // current health so it can be shown on the health bar.
            slider.value = currentHealth / maxHealth;
        }
    }

    void Healthbelow()
    {
        healthLevel.color = Color.red;
    }
}

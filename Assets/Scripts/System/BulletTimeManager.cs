using UnityEngine;
using UnityEngine.UI;


public class BulletTimeManager : MonoBehaviour
{
    public KeyCode bulletTimeKey = KeyCode.Mouse2; 

    [Header("Time Scale")]
    [Range(0.05f, 1f)]
    public float slowScale = 0.2f;

    [Header("Energy Settings")]
    public float maxEnergy = 5f;         
    public float drainPerSecond = 2f;     
    public float regenPerSecond = 0.1f;   

    [Header("UI")]
    public Slider energySlider;

    private float energy;
    private float defaultFixedDeltaTime;
    private bool isSlowing = false;

    void Start()
    {
        energy = maxEnergy;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        UpdateEnergyUI();
    }

    void Update()
    {
        bool wantSlow = Input.GetKey(bulletTimeKey) && energy > 0f;

        if (wantSlow != isSlowing)
        {
            SetSlow(wantSlow);
        }

        if (isSlowing)
        {
           
            energy -= drainPerSecond * Time.unscaledDeltaTime;
            if (energy <= 0f)
            {
                energy = 0f;
                SetSlow(false);
            }
        }
        else
        {
            
            if (energy < maxEnergy)
            {
                energy += regenPerSecond * Time.unscaledDeltaTime;
                if (energy > maxEnergy)
                    energy = maxEnergy;
            }
        }

        UpdateEnergyUI();
    }

    void SetSlow(bool slow)
    {
        isSlowing = slow;

        if (slow)
        {
            Time.timeScale = slowScale;
        }
        else
        {
            Time.timeScale = 1f;
        }

        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = energy / maxEnergy;
        }
    }

    void OnDisable()
    {
        
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}

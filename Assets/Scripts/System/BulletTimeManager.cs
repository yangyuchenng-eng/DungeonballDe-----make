using UnityEngine;
using UnityEngine.UI;

public class BulletTimeManager : MonoBehaviour
{
    public KeyCode bulletTimeKey = KeyCode.Mouse2; // 鼠标中键

    [Header("Time Scale")]
    [Range(0.05f, 1f)]
    public float slowScale = 0.2f;

    [Header("Energy Settings")]
    public float maxEnergy = 5f;          // 最大能量（秒数大致 = 可持续子弹时间）
    public float drainPerSecond = 1f;     // 子弹时间时每秒消耗多少能量
    public float regenPerSecond = 0.5f;   // 非子弹时间时每秒恢复多少能量

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
            // 子弹时间启用时消耗能量，用 unscaledDeltaTime，让消耗速度不被 timeScale 影响
            energy -= drainPerSecond * Time.unscaledDeltaTime;
            if (energy <= 0f)
            {
                energy = 0f;
                SetSlow(false);
            }
        }
        else
        {
            // 非子弹时间时回复能量
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
        // 安全起见，脚本被禁用时，把时间恢复正常
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// AI-assisted: player health with smooth UI and death handling.
// IMPROVED: 在 Die() 方法中禁用玩家的输入脚本，防止死亡后还能操控
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    [Tooltip("血条 UI 平滑变化速度")]
    public float uiLerpSpeed = 8f;

    [Header("Screen Flash (Damage/Heal)")]
    [Tooltip("全屏Image，用来做屏幕边缘闪红/绿")]
    public Image screenFlashImage;

    [Tooltip("受伤闪红的颜色（建议Alpha低一点）")]
    public Color damageFlashColor = new Color(1f, 0f, 0f, 0.35f);

    [Tooltip("回血闪绿的颜色（建议Alpha低一点）")]
    public Color healFlashColor = new Color(0f, 1f, 0f, 0.30f);

    [Tooltip("闪一下的进入时间")]
    public float flashInTime = 0.06f;

    [Tooltip("淡出的时间")]
    public float flashOutTime = 0.18f;

    private float targetHealth01 = 1f;
    private bool isDead = false;

    private Coroutine flashCo;

    void Start()
    {
        currentHealth = maxHealth;
        targetHealth01 = 1f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }

        // 初始化屏幕闪烁为透明
        if (screenFlashImage != null)
        {
            var c = screenFlashImage.color;
            c.a = 0f;
            screenFlashImage.color = c;
            screenFlashImage.raycastTarget = false;
        }
    }

    void Update()
    {
        // 只负责让 UI 慢慢接近 targetHealth01
        if (healthSlider != null)
        {
            float current = healthSlider.value;
            float desired = targetHealth01;
            healthSlider.value = Mathf.MoveTowards(
                current,
                desired,
                uiLerpSpeed * Time.unscaledDeltaTime
            );
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        // ✅ 玩家受击音效：统一在这里播，避免其它地方重复播
        AudioManager.I?.PlayPlayerHit();

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        targetHealth01 = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // ✅ 受伤闪红
        FlashDamage();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        int before = currentHealth;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        targetHealth01 = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // ✅ 真正回血才闪绿（避免满血吃药也闪）
        if (currentHealth > before)
        {
            FlashHeal();
        }
    }

    void FlashDamage()
    {
        if (screenFlashImage == null) return;
        StartFlash(damageFlashColor);
    }

    void FlashHeal()
    {
        if (screenFlashImage == null) return;
        StartFlash(healFlashColor);
    }

    void StartFlash(Color flashColor)
    {
        if (flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(FlashRoutine(flashColor));
    }

    IEnumerator FlashRoutine(Color flashColor)
    {
        // 进入：从0到目标alpha
        float t = 0f;

        Color c = flashColor;
        c.a = 0f;
        screenFlashImage.color = c;

        while (t < flashInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = (flashInTime <= 0f) ? 1f : Mathf.Clamp01(t / flashInTime);

            Color cc = flashColor;
            cc.a = Mathf.Lerp(0f, flashColor.a, k);
            screenFlashImage.color = cc;

            yield return null;
        }

        // 淡出：从目标alpha回到0
        t = 0f;
        while (t < flashOutTime)
        {
            t += Time.unscaledDeltaTime;
            float k = (flashOutTime <= 0f) ? 1f : Mathf.Clamp01(t / flashOutTime);

            Color cc = flashColor;
            cc.a = Mathf.Lerp(flashColor.a, 0f, k);
            screenFlashImage.color = cc;

            yield return null;
        }

        // 收尾归零
        Color end = flashColor;
        end.a = 0f;
        screenFlashImage.color = end;

        flashCo = null;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // IMPROVED: 禁用玩家的输入和移动脚本，防止死亡后还能操控
        SimpleFPSMovement movement = GetComponent<SimpleFPSMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        PlayerHands hands = GetComponent<PlayerHands>();
        if (hands != null)
        {
            hands.enabled = false;
        }

        if (GameSystemsTMP.I != null)
        {
            GameSystemsTMP.I.ShowDeath();
        }
        else
        {
            var ui = FindFirstObjectByType<GameStateUI>();
            if (ui != null) ui.ShowDeath();
        }
    }
}

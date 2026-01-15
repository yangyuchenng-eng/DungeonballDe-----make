using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 瞄准准心管理脚本
/// - 白色小圈，固定在屏幕中央
/// - 检测到敌人在准心范围内时，轻微变大
/// - 提醒玩家瞄准完毕
/// 
/// 使用方式：
/// 1. 在 Canvas 中创建一个 Image，命名为 "Crosshair"
/// 2. 设置图片为白色圆形（可以用 UI 默认的圆形或自己画）
/// 3. 挂上这个脚本，将 Image 拖到 crosshairImage 字段
/// 4. 根据需要调整参数
/// </summary>
public class AimCrosshair : MonoBehaviour
{
    [Header("References")]
    public Image crosshairImage;        // 准心 UI Image
    public Camera cam;

    [Header("Appearance")]
    [Tooltip("准心的正常大小（宽度）")]
    public float normalSize = 20f;

    [Tooltip("准心的放大大小（宽度）")]
    public float enlargedSize = 28f;

    [Tooltip("准心的颜色")]
    public Color crosshairColor = Color.white;

    [Tooltip("准心放大时的颜色（可以设为不同颜色来提示）")]
    public Color enlargedColor = Color.yellow;

    [Header("Detection")]
    [Tooltip("准心检测敌人的范围（屏幕像素）")]
    public float detectionRadiusPixels = 80f;

    [Tooltip("敌人检测距离（世界单位）")]
    public float detectionWorldDistance = 30f;

    [Header("Animation")]
    [Tooltip("准心放大/缩小的速度")]
    public float sizeChangeSpeed = 8f;

    [Tooltip("颜色变化的速度")]
    public float colorChangeSpeed = 6f;

    private float currentSize;
    private Color currentColor;
    private bool isEnlarged = false;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (crosshairImage == null)
        {
            Debug.LogError("[AimCrosshair] crosshairImage 未设置！");
            return;
        }

        // 初始化准心
        currentSize = normalSize;
        currentColor = crosshairColor;
        UpdateCrosshairUI();

        // 将准心固定在屏幕中央
        RectTransform rectTransform = crosshairImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    void Update()
    {
        // 检测敌人是否在准心范围内
        bool hasEnemyInCrosshair = CheckEnemyInCrosshair();

        // 根据检测结果更新准心状态
        if (hasEnemyInCrosshair)
        {
            isEnlarged = true;
        }
        else
        {
            isEnlarged = false;
        }

        // 平滑更新准心大小和颜色
        UpdateCrosshairAppearance();
    }

    /// <summary>
    /// 检测是否有敌人在准心范围内
    /// </summary>
    bool CheckEnemyInCrosshair()
    {
        if (cam == null)
            return false;

        // 获取屏幕中央的位置
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 获取玩家位置（摄像机位置）
        Vector3 playerPos = cam.transform.position;

        // 优先使用 EnemyManager，如果不存在则降级到 FindObjectsByType
        EnemyHealth[] enemies = null;

        EnemyManager enemyManager = EnemyManager.Instance;
        if (enemyManager != null)
        {
            enemies = enemyManager.GetAllEnemies();
        }
        else
        {
            enemies = FindObjectsByType<EnemyHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
        }

        if (enemies == null || enemies.Length == 0)
            return false;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            Transform enemyTransform = enemy.transform;

            // 检查世界距离
            float worldDist = Vector3.Distance(playerPos, enemyTransform.position);
            if (worldDist > detectionWorldDistance)
                continue;

            // 检查屏幕距离
            Vector3 screenPos = cam.WorldToScreenPoint(enemyTransform.position);
            if (screenPos.z <= 0f)
                continue;  // 在摄像机后面

            Vector2 screenPos2D = new Vector2(screenPos.x, screenPos.y);
            float screenDist = Vector2.Distance(screenPos2D, screenCenter);

            if (screenDist <= detectionRadiusPixels)
            {
                return true;  // 找到了一个在准心范围内的敌人
            }
        }

        return false;
    }

    /// <summary>
    /// 平滑更新准心的大小和颜色
    /// </summary>
    void UpdateCrosshairAppearance()
    {
        // 目标大小
        float targetSize = isEnlarged ? enlargedSize : normalSize;
        currentSize = Mathf.Lerp(currentSize, targetSize, sizeChangeSpeed * Time.deltaTime);

        // 目标颜色
        Color targetColor = isEnlarged ? enlargedColor : crosshairColor;
        currentColor = Color.Lerp(currentColor, targetColor, colorChangeSpeed * Time.deltaTime);

        // 更新 UI
        UpdateCrosshairUI();
    }

    /// <summary>
    /// 更新准心 UI 的大小和颜色
    /// </summary>
    void UpdateCrosshairUI()
    {
        if (crosshairImage == null)
            return;

        // 更新大小
        RectTransform rectTransform = crosshairImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);
        }

        // 更新颜色
        crosshairImage.color = currentColor;
    }
}

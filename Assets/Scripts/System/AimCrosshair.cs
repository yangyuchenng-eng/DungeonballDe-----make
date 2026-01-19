using UnityEngine;
using UnityEngine.UI;


public class AimCrosshair : MonoBehaviour
{
    [Header("References")]
    public Image crosshairImage;       
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

        
        currentSize = normalSize;
        currentColor = crosshairColor;
        UpdateCrosshairUI();

        
        RectTransform rectTransform = crosshairImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    void Update()
    {
        
        bool hasEnemyInCrosshair = CheckEnemyInCrosshair();

        
        if (hasEnemyInCrosshair)
        {
            isEnlarged = true;
        }
        else
        {
            isEnlarged = false;
        }

       
        UpdateCrosshairAppearance();
    }

    
    bool CheckEnemyInCrosshair()
    {
        if (cam == null)
            return false;

       
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

       
        Vector3 playerPos = cam.transform.position;

       
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

           
            float worldDist = Vector3.Distance(playerPos, enemyTransform.position);
            if (worldDist > detectionWorldDistance)
                continue;

           
            Vector3 screenPos = cam.WorldToScreenPoint(enemyTransform.position);
            if (screenPos.z <= 0f)
                continue; 

            Vector2 screenPos2D = new Vector2(screenPos.x, screenPos.y);
            float screenDist = Vector2.Distance(screenPos2D, screenCenter);

            if (screenDist <= detectionRadiusPixels)
            {
                return true;  
            }
        }

        return false;
    }

    
    void UpdateCrosshairAppearance()
    {
        
        float targetSize = isEnlarged ? enlargedSize : normalSize;
        currentSize = Mathf.Lerp(currentSize, targetSize, sizeChangeSpeed * Time.deltaTime);

        
        Color targetColor = isEnlarged ? enlargedColor : crosshairColor;
        currentColor = Color.Lerp(currentColor, targetColor, colorChangeSpeed * Time.deltaTime);

        
        UpdateCrosshairUI();
    }

    
    void UpdateCrosshairUI()
    {
        if (crosshairImage == null)
            return;

        
        RectTransform rectTransform = crosshairImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);
        }

        
        crosshairImage.color = currentColor;
    }
}

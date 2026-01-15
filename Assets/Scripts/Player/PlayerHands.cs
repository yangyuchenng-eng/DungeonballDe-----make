using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// AI-assisted script: structure and some code snippets generated with ChatGPT,
// then adapted and integrated by the student.

public class PlayerHands : MonoBehaviour
{
    [Header("References")]
    public Camera cam;

    // 手握东西的位置（手掌 / 手基点）
    public Transform leftHandBase;
    public Transform rightHandBase;

    // 手臂（会被脚本临时拉长、旋转对准目标，结束后恢复原样）
    public Transform leftHandArm;
    public Transform rightHandArm;

    // 左右手的 2D 准星圈（Canvas 上的圆形 Image）——只用于“抓”的范围
    public RectTransform leftGrabCircle;
    public RectTransform rightGrabCircle;

    [Header("Screen Grab Settings")]
    public float maxGrabWorldDistance = 8f;   // 物体离手太远就不抓（世界空间距离）
    public bool useRectSizeAsRadius = true;   // true: 用 UI 宽度/2 做半径；false: 用 override
    public float leftRadiusOverride = 80f;    // 像素
    public float rightRadiusOverride = 80f;   // 像素

    [Header("Throw Settings")]
    public float throwSpeed = 40f;            // 扔出去的直线飞行速度
    public LayerMask pickupLayerMask;         // 可抓物体的 Layer（demo 可设 Everything）

    [Header("Arm Animation")]
    public float extendDuration = 0.08f;      // 手臂伸出去的时间
    public float retractDuration = 0.12f;     // 手臂缩回来的时间
    public float armMinLength = 0.1f;         // 伸缩时的最短“距离”，防止除以 0

    [Header("Aim Assist")]
    public RectTransform aimAssistCircle;     // 鼠标跟随的瞄准圈（Canvas 上的 Image）
    public bool enableAimAssist = true;
    public float aimAssistMaxWorldDistance = 30f; // 自动瞄准敌人的最大世界距离

    // 当前每只手拿的物体
    private PickupItem leftHeldItem;
    private PickupItem rightHeldItem;

    // 手臂原始局部变换（用来恢复原姿势）
    private Vector3 leftArmOrigLocalPos;
    private Quaternion leftArmOrigLocalRot;
    private Vector3 leftArmOrigLocalScale;

    private Vector3 rightArmOrigLocalPos;
    private Quaternion rightArmOrigLocalRot;
    private Vector3 rightArmOrigLocalScale;

    // 防止同一只手在抓的过程中再次触发
    private bool leftHandBusy = false;
    private bool rightHandBusy = false;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (leftHandArm != null)
        {
            leftArmOrigLocalPos = leftHandArm.localPosition;
            leftArmOrigLocalRot = leftHandArm.localRotation;
            leftArmOrigLocalScale = leftHandArm.localScale;
        }

        if (rightHandArm != null)
        {
            rightArmOrigLocalPos = rightHandArm.localPosition;
            rightArmOrigLocalRot = rightHandArm.localRotation;
            rightArmOrigLocalScale = rightHandArm.localScale;
        }
    }

    void Update()
    {
        // 左键控制左手
        if (Input.GetMouseButtonDown(0))
        {
            if (leftHeldItem == null)
            {
                if (!leftHandBusy)
                {
                    TryGrabWithHand(
                        isLeftHand: true,
                        handBase: leftHandBase,
                        handArm: leftHandArm,
                        grabCircle: leftGrabCircle,
                        radiusOverride: leftRadiusOverride
                    );
                }
            }
            else
            {
                // 左手按鼠标方向 / 自动瞄准扔球
                ThrowFromHand(true, ref leftHeldItem);
            }
        }

        // 右键控制右手
        if (Input.GetMouseButtonDown(1))
        {
            if (rightHeldItem == null)
            {
                if (!rightHandBusy)
                {
                    TryGrabWithHand(
                        isLeftHand: false,
                        handBase: rightHandBase,
                        handArm: rightHandArm,
                        grabCircle: rightGrabCircle,
                        radiusOverride: rightRadiusOverride
                    );
                }
            }
            else
            {
                // 右手按鼠标方向 / 自动瞄准扔球
                ThrowFromHand(false, ref rightHeldItem);
            }
        }

        // 鼠标瞄准圈跟着鼠标移动
        if (aimAssistCircle != null)
        {
            aimAssistCircle.position = Input.mousePosition;
        }
    }

    float GetCircleRadiusPixels(RectTransform circle, float overrideRadius)
    {
        if (circle == null) return 0f;

        if (useRectSizeAsRadius)
        {
            float width = circle.rect.width * circle.lossyScale.x;
            return width * 0.5f;
        }
        else
        {
            return overrideRadius;
        }
    }

    /// <summary>
    /// 利用 2D 圆准星，在屏幕空间选出一个可抓物体，然后用手臂抓。
    /// </summary>
    void TryGrabWithHand(
        bool isLeftHand,
        Transform handBase,
        Transform handArm,
        RectTransform grabCircle,
        float radiusOverride)
    {
        if (cam == null || handBase == null || handArm == null || grabCircle == null) return;

        Vector2 circleCenter = grabCircle.position; // Screen Space Overlay 下就是屏幕坐标
        float radius = GetCircleRadiusPixels(grabCircle, radiusOverride);
        if (radius <= 0f) return;

        PickupItem[] allItems = FindObjectsByType<PickupItem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        if (allItems == null || allItems.Length == 0) return;

        PickupItem bestItem = null;
        float bestScreenSqrDist = float.MaxValue;

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (!item.isPickupable) continue;
            if (item.isHeld) continue;

            // Layer 过滤（可选）
            if (pickupLayerMask.value != 0)
            {
                if (((1 << item.gameObject.layer) & pickupLayerMask.value) == 0)
                    continue;
            }

            Vector3 screenPos = cam.WorldToScreenPoint(item.transform.position);
            if (screenPos.z <= 0f) continue; // 在摄像机后面

            Vector2 screenPos2D = new Vector2(screenPos.x, screenPos.y);
            float screenSqrDist = (screenPos2D - circleCenter).sqrMagnitude;

            if (screenSqrDist > radius * radius) continue;

            float worldDist = Vector3.Distance(handBase.position, item.transform.position);
            if (worldDist > maxGrabWorldDistance) continue;

            if (screenSqrDist < bestScreenSqrDist)
            {
                bestScreenSqrDist = screenSqrDist;
                bestItem = item;
            }
        }

        if (bestItem == null) return;

        if (isLeftHand)
        {
            StartCoroutine(ArmGrabCoroutine(
                true,
                handBase,
                handArm,
                bestItem,
                leftArmOrigLocalPos,
                leftArmOrigLocalRot,
                leftArmOrigLocalScale
            ));
        }
        else
        {
            StartCoroutine(ArmGrabCoroutine(
                false,
                handBase,
                handArm,
                bestItem,
                rightArmOrigLocalPos,
                rightArmOrigLocalRot,
                rightArmOrigLocalScale
            ));
        }
    }

    /// <summary>
    /// 手臂从原姿势 → 伸到物体 → 带着物体缩回原姿势。
    /// </summary>
    IEnumerator ArmGrabCoroutine(
        bool isLeftHand,
        Transform handBase,
        Transform handArm,
        PickupItem item,
        Vector3 armOrigLocalPos,
        Quaternion armOrigLocalRot,
        Vector3 armOrigLocalScale)
    {
        if (handBase == null || handArm == null || item == null) yield break;

        if (isLeftHand) leftHandBusy = true;
        else rightHandBusy = true;

        item.isHeld = true;
        item.wasThrownByPlayer = false;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 handPos = handBase.position;
        Vector3 itemPos = item.transform.position;
        Vector3 dir = itemPos - handPos;
        float totalDist = dir.magnitude;
        if (totalDist < armMinLength) totalDist = armMinLength;
        Vector3 dirNorm = dir / totalDist;

        // 阶段 1：伸出去
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / extendDuration;
            if (t > 1f) t = 1f;

            float currentDist = Mathf.Lerp(armMinLength, totalDist, t);
            Vector3 midPoint = handPos + dirNorm * (currentDist * 0.5f);
            handArm.position = midPoint;
            handArm.up = dirNorm;

            handArm.localScale = new Vector3(
                armOrigLocalScale.x,
                currentDist * 0.5f,
                armOrigLocalScale.z
            );

            yield return null;
        }

        // 阶段 2：带着物体缩回来
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / retractDuration;
            if (t > 1f) t = 1f;

            float currentDist = Mathf.Lerp(totalDist, armMinLength, t);

            Vector3 midPoint = handPos + dirNorm * (currentDist * 0.5f);
            handArm.position = midPoint;
            handArm.up = dirNorm;

            handArm.localScale = new Vector3(
                armOrigLocalScale.x,
                currentDist * 0.5f,
                armOrigLocalScale.z
            );

            Vector3 tipPos = handPos + dirNorm * currentDist;
            item.transform.position = tipPos;

            yield return null;
        }

        AttachItemToHand(isLeftHand, handBase, item);

        handArm.localPosition = armOrigLocalPos;
        handArm.localRotation = armOrigLocalRot;
        handArm.localScale = armOrigLocalScale;

        if (isLeftHand) leftHandBusy = false;
        else rightHandBusy = false;
    }

    void AttachItemToHand(bool isLeftHand, Transform handBase, PickupItem item)
    {
        if (item == null || handBase == null) return;

        if (isLeftHand)
            leftHeldItem = item;
        else
            rightHeldItem = item;

        item.isHeld = true;
        item.wasThrownByPlayer = false;

        item.transform.SetParent(handBase);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 最基础的鼠标扔球方向（摄像机射线）。
    /// </summary>
    Vector3 GetThrowDirectionFromMouse()
    {
        if (cam == null) return transform.forward;

        Vector3 mousePos = Input.mousePosition;
        Ray ray = cam.ScreenPointToRay(mousePos);
        return ray.direction.normalized;
    }

    /// <summary>
    /// 带自动瞄准的扔球方向：
    /// 1) 默认是鼠标射线方向；
    /// 2) 如果瞄准圈内有敌人，改成 origin → 敌人的方向。
    /// </summary>
    Vector3 GetAutoAimDirectionFromMouse(Vector3 originWorldPos)
    {
        Vector3 baseDir = GetThrowDirectionFromMouse();

        if (!enableAimAssist || cam == null || aimAssistCircle == null)
            return baseDir;

        Vector2 circleCenter = aimAssistCircle.position;
        float radius = aimAssistCircle.rect.width * aimAssistCircle.lossyScale.x * 0.5f;
        if (radius <= 0f) return baseDir;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        EnemyHealth bestTarget = null;
        float bestSqrScreenDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            Transform et = enemy.transform;

            Vector3 screenPos = cam.WorldToScreenPoint(et.position);
            if (screenPos.z <= 0f) continue;

            float worldDist = Vector3.Distance(originWorldPos, et.position);
            if (worldDist > aimAssistMaxWorldDistance) continue;

            Vector2 screen2D = new Vector2(screenPos.x, screenPos.y);
            float sqrScreenDist = (screen2D - circleCenter).sqrMagnitude;

            if (sqrScreenDist > radius * radius) continue;

            if (sqrScreenDist < bestSqrScreenDist)
            {
                bestSqrScreenDist = sqrScreenDist;
                bestTarget = enemy;
            }
        }

        if (bestTarget == null)
        {
            return baseDir;
        }

        Vector3 targetPos = bestTarget.transform.position;
        Vector3 aimDir = (targetPos - originWorldPos).normalized;
        return aimDir;
    }

    /// <summary>
    /// 从某只手扔出物体：方向由鼠标+自动瞄准决定，扔时交给 ThrowableBall 控制直线飞行。
    /// </summary>
    void ThrowFromHand(bool isLeftHand, ref PickupItem handSlot)
    {
        if (handSlot == null) return;

        PickupItem item = handSlot;
        handSlot = null;

        item.isHeld = false;
        item.wasThrownByPlayer = true;

        Transform t = item.transform;
        t.SetParent(null);

        Transform handBase = isLeftHand ? leftHandBase : rightHandBase;
        Vector3 origin = (handBase != null) ? handBase.position : t.position;

        Vector3 throwDir = GetAutoAimDirectionFromMouse(origin);

        ThrowableBall projectile = item.GetComponent<ThrowableBall>();

        if (projectile != null)
        {
            projectile.BeginKinematicThrow(throwDir, throwSpeed);
        }
        else
        {
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = throwDir * throwSpeed;
            }
        }
    }
}

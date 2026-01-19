using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHands : MonoBehaviour
{
    [Header("References")]
    public Camera cam;

    public Transform leftHandBase;
    public Transform rightHandBase;

    public Transform leftHandArm;
    public Transform rightHandArm;

    public RectTransform leftGrabCircle;
    public RectTransform rightGrabCircle;

    [Header("Screen Grab Settings")]
    public float maxGrabWorldDistance = 8f;
    public bool useRectSizeAsRadius = true;
    public float leftRadiusOverride = 80f;
    public float rightRadiusOverride = 80f;

    [Header("Throw Settings")]
    public float throwSpeed = 40f;
    public LayerMask pickupLayerMask;

    [Header("Arm Animation")]
    public float extendDuration = 0.08f;
    public float retractDuration = 0.12f;
    public float armMinLength = 0.1f;

    [Header("Aim Assist")]
    public RectTransform aimAssistCircle;
    public bool enableAimAssist = true;
    public float aimAssistMaxWorldDistance = 30f;

    private PickupItem leftHeldItem;
    private PickupItem rightHeldItem;

    private Vector3 leftArmOrigLocalPos;
    private Quaternion leftArmOrigLocalRot;
    private Vector3 leftArmOrigLocalScale;

    private Vector3 rightArmOrigLocalPos;
    private Quaternion rightArmOrigLocalRot;
    private Vector3 rightArmOrigLocalScale;

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
        // 左键：左手
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
                // ✅ 若不可扔，忽略
                if (leftHeldItem.throwable)
                    ThrowFromHand(true, ref leftHeldItem);
            }
        }

        // 右键：右手
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
                // ✅ 若不可扔，忽略
                if (rightHeldItem.throwable)
                    ThrowFromHand(false, ref rightHeldItem);
            }
        }
    }

    void TryGrabWithHand(
        bool isLeftHand,
        Transform handBase,
        Transform handArm,
        RectTransform grabCircle,
        float radiusOverride)
    {
        if (cam == null || handBase == null || grabCircle == null) return;

        Vector2 circleCenter = grabCircle.position;
        float radius = useRectSizeAsRadius
            ? (grabCircle.rect.width * grabCircle.lossyScale.x * 0.5f)
            : radiusOverride;

        if (radius <= 0f) return;

        PickupItem[] allItems = FindObjectsByType<PickupItem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        PickupItem bestItem = null;
        float bestSqrScreenDist = float.MaxValue;

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (!item.isPickupable) continue;
            if (item.isHeld) continue;

            if (pickupLayerMask.value != 0)
            {
                if (((1 << item.gameObject.layer) & pickupLayerMask.value) == 0)
                    continue;
            }

            Vector3 worldPos = item.transform.position;

            float worldDist = Vector3.Distance(handBase.position, worldPos);
            if (worldDist > maxGrabWorldDistance)
                continue;

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f) continue;

            Vector2 screen2D = new Vector2(screenPos.x, screenPos.y);
            float sqrScreenDist = (screen2D - circleCenter).sqrMagnitude;

            if (sqrScreenDist <= radius * radius)
            {
                if (sqrScreenDist < bestSqrScreenDist)
                {
                    bestSqrScreenDist = sqrScreenDist;
                    bestItem = item;
                }
            }
        }

        if (bestItem != null)
        {
            // ✅ 改动点：不再“直接吃掉”。药水也先拉回来，再到手瞬间自动使用。
            StartCoroutine(ArmGrabCoroutine(
                isLeftHand,
                handBase,
                handArm,
                bestItem,
                isLeftHand ? leftArmOrigLocalPos : rightArmOrigLocalPos,
                isLeftHand ? leftArmOrigLocalRot : rightArmOrigLocalRot,
                isLeftHand ? leftArmOrigLocalScale : rightArmOrigLocalScale
            ));
        }
    }

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

        ThrowableBall tb0 = item.GetComponent<ThrowableBall>();
        if (tb0 != null) tb0.StopForPickupOrHold();

        int pickedLayer = LayerMask.NameToLayer("Picked");
        if (pickedLayer != -1)
        {
            item.gameObject.layer = pickedLayer;
        }

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

        // 伸手
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

        // 收回 + 拉物体
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

        // ✅ 到手瞬间：如果是药水，自动使用并消失，不占手位
        var potion = item.GetComponent<HealthPotionPickup>();
        if (potion != null)
        {
            var ph = GetComponent<PlayerHealth>();
            potion.Consume(ph);

            // 确保手里不占用
            if (isLeftHand) leftHeldItem = null;
            else rightHeldItem = null;
        }
        else
        {
            // 不是药水：正常挂到手上
            AttachItemToHand(isLeftHand, handBase, item);
        }

        // 手臂复位
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

        ThrowableBall tb1 = item.GetComponent<ThrowableBall>();
        if (tb1 != null) tb1.StopForPickupOrHold();

        item.transform.SetParent(handBase);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    Vector3 GetThrowDirectionFromMouse()
    {
        if (cam == null) return transform.forward;

        Vector3 mousePos = Input.mousePosition;
        Ray ray = cam.ScreenPointToRay(mousePos);
        return ray.direction.normalized;
    }

    Vector3 GetAutoAimDirectionFromMouse(Vector3 originWorldPos)
    {
        if (cam == null || aimAssistCircle == null)
        {
            return GetThrowDirectionFromMouse();
        }

        Vector2 circleCenter = aimAssistCircle.position;
        float radius = aimAssistCircle.rect.width * aimAssistCircle.lossyScale.x * 0.5f;
        if (radius <= 0f)
        {
            return GetThrowDirectionFromMouse();
        }

        Ray ray = cam.ScreenPointToRay(circleCenter);

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, aimAssistMaxWorldDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * aimAssistMaxWorldDistance;
        }

        Vector3 baseDir = (targetPoint - originWorldPos);
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = ray.direction;
        baseDir = baseDir.normalized;

        if (!enableAimAssist)
            return baseDir;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        if (enemies == null || enemies.Length == 0)
            return baseDir;

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

        Vector3 aimDir = (bestTarget.transform.position - originWorldPos);
        if (aimDir.sqrMagnitude < 0.0001f)
            return baseDir;
        return aimDir.normalized;
    }

    // 只展示需要修改的那个函数完整体（其余代码保持你文件原样）
    void ThrowFromHand(bool isLeftHand, ref PickupItem handSlot)
    {
        if (handSlot == null) return;

        PickupItem item = handSlot;
        handSlot = null;

        item.isHeld = false;
        item.wasThrownByPlayer = true;

        int pickupLayer = LayerMask.NameToLayer("Pickup");
        if (pickupLayer != -1)
        {
            item.gameObject.layer = pickupLayer;
        }

        Transform t = item.transform;
        t.SetParent(null);

        Transform handBase = isLeftHand ? leftHandBase : rightHandBase;
        Vector3 origin = (handBase != null) ? handBase.position : t.position;

        Vector3 throwDir = GetAutoAimDirectionFromMouse(origin);

        // ✅ 扔球音效：确定要扔出时播一次
        AudioManager.I?.PlayThrow();

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

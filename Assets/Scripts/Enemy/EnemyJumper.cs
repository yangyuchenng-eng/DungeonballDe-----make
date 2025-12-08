using System.Collections;
using UnityEngine;

// AI-assisted base jumper enemy for Dungeonball demo.
[RequireComponent(typeof(Collider))]
public class EnemyJumper : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;          // 水平移动速度
    public float hopHeight = 1.0f;        // 每次跳起的高度
    public float hopDuration = 0.6f;      // 一次跳的总时长
    public float idleBetweenHops = 0.3f;  // 两次跳之间的停顿

    [Header("Target")]
    public Transform player;              // 拖 Player 进来
    public float stopDistance = 1.5f;     // 离玩家多近就不再继续挤上去

    private bool isMoving = true;
    private float groundY;                // 当前所在地面的 Y

    void Start()
    {
        if (player == null)
        {
            // 简单找一下
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        groundY = transform.position.y;
        StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        while (isMoving)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            // 计算水平方向
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (distance > 0.01f)
            {
                Vector3 dir = toPlayer.normalized;
                // 朝玩家转向
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir, Vector3.up),
                    0.2f
                );

                if (distance > stopDistance)
                {
                    // 做一次“向前的跳跃”
                    yield return StartCoroutine(HopStep(dir));
                    // 跳完歇一小会
                    yield return new WaitForSeconds(idleBetweenHops);
                    continue;
                }
            }

            // 太近 / 找不到方向就原地等一帧
            yield return null;
        }
    }

    IEnumerator HopStep(Vector3 dir)
    {
        float t = 0f;
        Vector3 startPos = transform.position;

        while (t < hopDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / hopDuration);

            // 水平位移
            Vector3 horizontalMove = dir * moveSpeed * Time.deltaTime;
            Vector3 pos = transform.position + horizontalMove;

            // 垂直偏移：简单抛物线 4x(1-x)
            float yOffset = hopHeight * 4f * normalized * (1f - normalized);
            pos.y = groundY + yOffset;

            transform.position = pos;

            yield return null;
        }

        // 落地时对齐到 groundY
        Vector3 endPos = transform.position;
        endPos.y = groundY;
        transform.position = endPos;
    }
}

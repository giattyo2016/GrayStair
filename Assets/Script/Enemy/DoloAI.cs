using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DoloAI : MonoBehaviour
{
    public enum AIState { Wander, Chase, Attack, Flee }

    [Header("目前狀態")]
    public AIState currentState = AIState.Wander;

    [Header("目標設定")]
    public Transform player;

    [Header("感知能力")]
    public float detectionRadius = 15f;
    [Range(0, 360)]
    public float detectionAngle = 120f;

    [Header("避光雷達設定 (僅漫遊有效)")]
    public LayerMask laserLayer;        // 請確保雷射陷阱的 Layer 設為這個
    public float avoidLaserDistance = 7f; // 雷達探測距離 (稍微調長一點讓牠有時間反應)
    public float whiskersAngle = 30f;   // 左右探測射線的角度

    [Header("遊走設定")]
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float walkSpeed = 3.5f;

    [Header("追擊與攻擊設定")]
    public float runSpeed = 8f;
    public float attackRange = 2.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("懼光逃跑設定")]
    public float fleeSpeed = 12f;
    public float fleeDistance = 15f;
    public float fleeDuration = 4f;

    [Header("攻擊力設定")]
    public float damageAmount = 20f; // 碰到一次扣多少理智

    private NavMeshAgent agent;
    private float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stateTimer = wanderTimer;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Wander: UpdateWanderState(); break;
            case AIState.Chase: UpdateChaseState(); break;
            case AIState.Attack: UpdateAttackState(); break;
            case AIState.Flee: UpdateFleeState(); break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"<color=cyan>[Dolo 大腦]</color> 狀態切換：{currentState} ? <b>{newState}</b>");
        currentState = newState;
        stateTimer = 0f;
    }

    // ================== 漫遊狀態 (具備避光能力) ==================

    private void UpdateWanderState()
    {
        agent.speed = walkSpeed;

        // 1. 避光雷達最優先：漫遊時持續檢查前方是否有雷射
        if (CheckWhiskersForLaser())
        {
            Debug.Log("<color=yellow>[Dolo 避險]</color> 嘖！前面有光，我不過去！(重新尋路)");
            PickNewWanderDestination(); // 發現雷射，立刻換個方向走
        }
        else
        {
            // 2. 判斷是否已經抵達目的地
            // pathPending 代表是否還在計算路徑中，remainingDistance 則是離目標還有多遠
            // 我們加一個 0.1f 的緩衝值，避免浮點數誤差導致永遠判定不到達
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                // 【已經抵達目的地】
                // 開始在原地讀秒
                stateTimer += Time.deltaTime;

                // 原地數滿你設定的秒數 (wanderTimer) 後，才決定下一個地點
                if (stateTimer >= wanderTimer)
                {
                    PickNewWanderDestination();
                }
            }
            else
            {
                // 【還在走路的過程中】
                // 把計時器歸零 (或保持為 0)，確保他「到了之後」才會從 0 開始數秒
                stateTimer = 0f;
            }
        }

        // 3. 隨時保持警戒
        if (DetectPlayer()) ChangeState(AIState.Chase);
    }

    private void PickNewWanderDestination()
    {
        Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
        agent.SetDestination(newPos);
        stateTimer = 0;
    }

    // 三向雷達探測 (左、中、右)
    private bool CheckWhiskersForLaser()
    {
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);

        // 中間射線
        if (Physics.Raycast(rayStart, transform.forward, avoidLaserDistance, laserLayer)) return true;

        // 左側斜射線
        Vector3 leftDir = Quaternion.Euler(0, -whiskersAngle, 0) * transform.forward;
        if (Physics.Raycast(rayStart, leftDir, avoidLaserDistance, laserLayer)) return true;

        // 右側斜射線
        Vector3 rightDir = Quaternion.Euler(0, whiskersAngle, 0) * transform.forward;
        if (Physics.Raycast(rayStart, rightDir, avoidLaserDistance, laserLayer)) return true;

        return false;
    }

    // ================== 追擊狀態 (失去理智，無視雷達) ==================

    private void UpdateChaseState()
    {
        agent.speed = runSpeed;
        // 追逐狀態下，Dolo 滿腦子只有玩家，所以完全不呼叫 CheckWhiskersForLaser()
        if (player != null) agent.SetDestination(player.position);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange) ChangeState(AIState.Attack);
        else if (!DetectPlayer()) ChangeState(AIState.Wander);
    }

    // ================== 其他邏輯 (保持不變) ==================

    public void ReactToLaser(Vector3 laserSourcePos)
    {
        if (currentState == AIState.Flee) return;
        ChangeState(AIState.Flee);
        Debug.Log("<color=blue>[Dolo 恐懼]</color> 嗚啊！！被射中了！");

        Vector3 fleeDirection = (transform.position - laserSourcePos).normalized;
        Vector3 targetFleePoint = transform.position + fleeDirection * fleeDistance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetFleePoint, out hit, 10f, NavMesh.AllAreas))
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(RandomNavSphere(transform.position, fleeDistance, -1));
        }
    }

    private void UpdateFleeState()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= fleeDuration) ChangeState(AIState.Wander);
    }

    private void UpdateAttackState()
    {
        agent.isStopped = true;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Debug.Log("<color=magenta>[Dolo 戰鬥]</color> 飛撲！！");

            // 【修正：在這裡真正造成傷害！】
            // Dolo 撲上去的瞬間，直接抓取玩家並扣除理智
            PlayerSanity playerSanity = player.GetComponent<PlayerSanity>();
            if (playerSanity != null)
            {
                playerSanity.TakeDamage(damageAmount);
            }

            lastAttackTime = Time.time;
        }

        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            agent.isStopped = false;
            ChangeState(AIState.Chase);
        }
    }

    private bool DetectPlayer()
    {
        if (player == null) return false;
        Vector3 dir = player.position - transform.position;
        if (dir.magnitude <= detectionRadius)
        {
            if (Vector3.Angle(transform.forward, dir) <= detectionAngle / 2f)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir.normalized, out RaycastHit hit, detectionRadius))
                {
                    if (hit.transform == player || hit.transform.CompareTag("Player")) return true;
                }
            }
        }
        return false;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FoniaClone : MonoBehaviour
{
    public enum CloneState { Wander, Chase }

    [Header("目前狀態")]
    public CloneState currentState = CloneState.Wander;

    [Header("基本設定")]
    public float lifeTime = 25f;       // 分身最多存在 25 秒
    public float wanderRadius = 15f;
    public float wanderSpeed = 3.5f;

    [Header("感知與追擊設定")]
    public float detectionRadius = 15f; // 視野距離 (能看多遠)
    [Range(0, 360)]
    public float detectionAngle = 120f; // 視野角度 (扇形範圍)
    public float chaseSpeed = 6f;       // 發現玩家後的狂奔速度
    public float catchDistance = 1.5f;  // 抓到玩家的距離

    private FoniaAI realFonia;
    private Transform player;
    private NavMeshAgent agent;
    private float timer = 0f;

    public void Initialize(FoniaAI mainScript, Transform targetPlayer)
    {
        realFonia = mainScript;
        player = targetPlayer;
        agent = GetComponent<NavMeshAgent>();

        // 誕生時預設是漫遊模式
        agent.speed = wanderSpeed;
        PickRandomDestination();
    }

    void Update()
    {
        // 1. 生命週期倒數 (這像個定時炸彈，滴答滴答...)
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            // 時間到！分身能量耗盡消散，回報本體一切安全
            if (realFonia != null) realFonia.OnCloneExpired();
            Destroy(gameObject);
            return;
        }

        // 2. 狀態機執行
        switch (currentState)
        {
            case CloneState.Wander:
                UpdateWanderState();
                break;
            case CloneState.Chase:
                UpdateChaseState();
                break;
        }
    }

    // ================== 漫遊狀態 ==================
    private void UpdateWanderState()
    {
        agent.speed = wanderSpeed;

        // 【核心修改】：套用 Dolo 的扇形視野防穿牆偵測
        if (DetectPlayer())
        {
            Debug.Log("<color=red>[Fonia 分身]</color> 看到玩家了！啟動死纏爛打模式！");
            // 只要一看到，就切換成追殺模式，再也不放過玩家
            currentState = CloneState.Chase;
            return;
        }

        // 繼續在迷宮裡亂逛尋找玩家
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            PickRandomDestination();
        }
    }

    // ================== 追殺狀態 ==================
    private void UpdateChaseState()
    {
        agent.speed = chaseSpeed;

        if (player != null)
        {
            // 緊緊跟隨玩家的座標
            agent.SetDestination(player.position);

            // 如果分身摸到玩家了！
            if (Vector3.Distance(transform.position, player.position) <= catchDistance)
            {
                Debug.Log("<color=red>[Fonia 分身]</color> 抓到你了！通報本體降臨！");

                // 呼叫本體開殺！
                if (realFonia != null) realFonia.OnCloneFoundPlayer();

                // 任務完成，分身消散
                Destroy(gameObject);
            }
        }
    }

    // ================== 視覺偵測系統 ==================
    private bool DetectPlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = player.position - transform.position;

        // 1. 檢查距離有沒有在視線範圍內
        if (dirToPlayer.magnitude <= detectionRadius)
        {
            // 2. 檢查角度 (玩家是否在分身正前方的扇形視角內)
            if (Vector3.Angle(transform.forward, dirToPlayer) <= detectionAngle / 2f)
            {
                // 3. 射線檢查防穿牆 (隔著牆壁看不到)
                Vector3 cloneEyePos = transform.position + Vector3.up * 1.5f;
                Vector3 playerBodyPos = player.position + Vector3.up * 1.0f;
                Vector3 rayDir = (playerBodyPos - cloneEyePos).normalized;
                float dist = Vector3.Distance(cloneEyePos, playerBodyPos);

                if (Physics.Raycast(cloneEyePos, rayDir, out RaycastHit hit, dist))
                {
                    // 如果射線打到的真的是玩家，代表視野暢通！
                    if (hit.transform == player || hit.transform.CompareTag("Player"))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void PickRandomDestination()
    {
        Vector3 randDirection = Random.insideUnitSphere * wanderRadius;
        randDirection += transform.position;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, wanderRadius, -1))
        {
            agent.SetDestination(navHit.position);
        }
    }
}
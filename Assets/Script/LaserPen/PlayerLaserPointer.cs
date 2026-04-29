using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class PlayerLaserPointer : MonoBehaviour
{
    [Header("裝備狀態")]
    public bool hasPickedUp = false;

    [Header("發射點設定")]
    public Transform firePoint;

    [Header("雷射設定")]
    public float laserRange = 50f;
    public LayerMask laserHitMask;

    [Header("電池與 UI 設定")]
    public float maxBattery = 100f;
    public float currentBattery;
    public float drainRate = 10f;

    public GameObject uiContainer;
    public Image batteryFillImage;

    [Header("靈異干擾設定")]
    public float interferenceRadius = 10f;
    public float flickerSpeed = 0.05f;

    private LineRenderer lineRenderer;
    private bool isLaserToggledOn = false;
    private float flickerTimer = 0f;

    void Awake()
    {
        // 把 GetComponent 移到 Awake，確保 OnEnable 時絕對找得到它
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        lineRenderer.enabled = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        // 如果還沒撿起來，一開始先藏好 UI
        if (!hasPickedUp && uiContainer != null) uiContainer.SetActive(false);
    }

    // ================== 核心：拔槍與收槍邏輯 ==================

    // 當這個物件被 SetActive(true) 時會自動觸發 (例如滾輪切換過來)
    void OnEnable()
    {
        // 只有當玩家已經解鎖這個道具時，才顯示 UI
        if (hasPickedUp && uiContainer != null)
        {
            uiContainer.SetActive(true);
            UpdateBatteryUI();
        }
    }

    // 當這個物件被 SetActive(false) 時會自動觸發 (例如滾輪切換走)
    void OnDisable()
    {
        // 【強制收槍】：把雷射光關掉、開關切換為 off，並且隱藏 UI
        isLaserToggledOn = false;
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (uiContainer != null) uiContainer.SetActive(false);
    }

    // =========================================================

    void Update()
    {
        if (!hasPickedUp) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (currentBattery > 0)
            {
                isLaserToggledOn = !isLaserToggledOn;
            }
        }

        if (isLaserToggledOn && currentBattery > 0)
        {
            HandleLaserActive();
        }
        else
        {
            if (currentBattery <= 0) isLaserToggledOn = false;
            TurnOffLaser();
        }

        UpdateBatteryUI();
    }

    private void HandleLaserActive()
    {
        currentBattery -= drainRate * Time.deltaTime;
        currentBattery = Mathf.Max(currentBattery, 0f);

        bool isInterfered = CheckForCloneInterference();

        if (isInterfered)
        {
            flickerTimer += Time.deltaTime;
            if (flickerTimer >= flickerSpeed)
            {
                flickerTimer = 0f;
                lineRenderer.enabled = Random.value > 0.5f;
            }
        }
        else
        {
            lineRenderer.enabled = true;
        }

        if (lineRenderer.enabled)
        {
            ShootLaser();
        }
    }

    private void TurnOffLaser()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    private void ShootLaser()
    {
        Vector3 startPoint = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 rayDir = (firePoint != null) ? firePoint.forward : transform.forward;

        Vector3 endPoint = startPoint + rayDir * laserRange;

        if (Physics.Raycast(startPoint, rayDir, out RaycastHit hit, laserRange, laserHitMask))
        {
            endPoint = hit.point;

            ILaserReceiver receiver = hit.collider.GetComponent<ILaserReceiver>();
            if (receiver != null)
            {
                float remaining = laserRange - hit.distance;
                Vector3 nextStart, nextDir;
                receiver.ProcessLaser(hit.point, hit.normal, rayDir, hit.collider, ref remaining, null, out nextStart, out nextDir);
            }
        }

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    private bool CheckForCloneInterference()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interferenceRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<FoniaClone>() != null) return true;
        }
        return false;
    }

    private void UpdateBatteryUI()
    {
        if (batteryFillImage != null)
        {
            batteryFillImage.fillAmount = currentBattery / maxBattery;
        }
    }

    public void EquipLaser()
    {
        hasPickedUp = true;
        currentBattery = maxBattery;
        isLaserToggledOn = false;

        // 第一次撿起來的瞬間，強制開啟 UI
        if (uiContainer != null) uiContainer.SetActive(true);
        UpdateBatteryUI();

        Debug.Log("<color=green>[系統]</color> 獲得雷射筆並充滿電！");
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class ChargingStation : MonoBehaviour
{
    [Header("充電站設定")]
    public float maxCapacity = 200f;
    public float currentCapacity;
    public float chargeRate = 5f;

    [Header("充電站 UI 設定")]
    public GameObject stationUiContainer;
    public Image stationFillImage;
    [Tooltip("淡入淡出需要花費幾秒鐘")]
    public float fadeDuration = 0.5f;

    [Header("玩家鎖定設定")]
    [Tooltip("請把玩家身上負責『移動』的腳本拖進來 (例如 FirstPersonController)")]
    public MonoBehaviour playerMovementScript;
    [Tooltip("請把負責『視角轉動』的腳本拖進來 (可選，若你希望充電時連頭都不能轉)")]
    public MonoBehaviour playerCameraScript;

    private bool isPlayerNear = false;
    private bool isCharging = false; // 【新增】追蹤現在是不是正在「被釘在原地」充電中
    private PlayerLaserPointer playerHandLaser;

    private CanvasGroup uiCanvasGroup;
    private Coroutine currentFadeCoroutine;

    void Start()
    {
        currentCapacity = maxCapacity;
        GetComponent<BoxCollider>().isTrigger = true;

        if (stationUiContainer != null)
        {
            uiCanvasGroup = stationUiContainer.GetComponent<CanvasGroup>();
            if (uiCanvasGroup == null) uiCanvasGroup = stationUiContainer.AddComponent<CanvasGroup>();

            uiCanvasGroup.alpha = 0f;
            stationUiContainer.SetActive(false);
        }

        UpdateStationUI();
    }

    void Update()
    {
        // 1. 判斷玩家是否有意願 (按著F) 且有裝備雷射筆
        bool isTryingToCharge = isPlayerNear && Input.GetKey(KeyCode.F) && playerHandLaser != null && playerHandLaser.hasPickedUp;

        // 2. 判斷物理上是否允許充電 (充電站有電，且雷射筆還沒滿)
        bool canActuallyCharge = currentCapacity > 0f && (playerHandLaser != null && playerHandLaser.currentBattery < playerHandLaser.maxBattery);

        if (isTryingToCharge && canActuallyCharge)
        {
            // 如果剛剛還沒開始充，現在開始充了 ? 鎖定玩家！
            if (!isCharging)
            {
                isCharging = true;
                SetPlayerLock(true);
            }

            ChargePlayerLaser(); // 執行實質的扣電/加電邏輯
        }
        else
        {
            // 如果鬆開了 F，或者是電滿了/沒電了 ? 解除鎖定！
            if (isCharging)
            {
                isCharging = false;
                SetPlayerLock(false);
            }
        }
    }

    // ================== 核心：鎖定與解除玩家控制 ==================
    private void SetPlayerLock(bool isLocked)
    {
        // 啟用控制 = 反轉鎖定狀態 (鎖定時為 false，不鎖定時為 true)
        bool enableControl = !isLocked;

        // 開關玩家的移動腳本
        if (playerMovementScript != null)
            playerMovementScript.enabled = enableControl;

        // 開關玩家的視角腳本 (如果有設定的話)
        if (playerCameraScript != null)
            playerCameraScript.enabled = enableControl;

        if (isLocked)
            Debug.Log("<color=yellow>[充電站]</color> 玩家充電中... 鎖定行動！");
        else
            Debug.Log("<color=yellow>[充電站]</color> 停止充電... 恢復行動！");
    }

    private void ChargePlayerLaser()
    {
        // 數學防呆計算
        float amountToTransfer = chargeRate * Time.deltaTime;
        amountToTransfer = Mathf.Min(amountToTransfer, currentCapacity);
        float spaceLeftInLaser = playerHandLaser.maxBattery - playerHandLaser.currentBattery;
        amountToTransfer = Mathf.Min(amountToTransfer, spaceLeftInLaser);

        currentCapacity -= amountToTransfer;
        playerHandLaser.currentBattery += amountToTransfer;

        UpdateStationUI();
    }

    private void UpdateStationUI()
    {
        if (stationFillImage != null)
        {
            stationFillImage.fillAmount = currentCapacity / maxCapacity;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerHandLaser = other.transform.root.GetComponentInChildren<PlayerLaserPointer>(true);

            // 自動嘗試抓取玩家身上的常見移動腳本 (如果你懶得手動拖曳)
            if (playerMovementScript == null)
            {
                // 如果你的腳本叫別的名字 (例如 PlayerController)，請手動在 Inspector 拖曳
                // 這裡我們不寫死，保留彈性
            }

            if (stationUiContainer != null)
            {
                UpdateStationUI();
                if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = StartCoroutine(FadeUI(1f));
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 【防呆】：如果玩家透過某些 Bug (例如被怪撞飛) 離開了範圍，確保一定要解除鎖定
            if (isCharging)
            {
                isCharging = false;
                SetPlayerLock(false);
            }

            isPlayerNear = false;
            playerHandLaser = null;

            if (stationUiContainer != null)
            {
                if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = StartCoroutine(FadeUI(0f));
            }
        }
    }

    private IEnumerator FadeUI(float targetAlpha)
    {
        if (targetAlpha > 0f) stationUiContainer.SetActive(true);
        float startAlpha = uiCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        uiCanvasGroup.alpha = targetAlpha;
        if (targetAlpha == 0f) stationUiContainer.SetActive(false);
    }
}
using UnityEngine;
using TMPro; // 【新增】：引入 TextMeshPro UI 庫

public class PlayerInteractor : MonoBehaviour
{
    [Header("互動設定")]
    [Tooltip("玩家可以按 F 互動的最遠距離")]
    public float interactRange = 3.0f;
    [Tooltip("互動按鍵")]
    public KeyCode interactKey = KeyCode.F;

    [Header("UI 設定")]
    [Tooltip("請把畫面中央用來顯示提示的 TextMeshPro 拖曳到這裡")]
    public TextMeshProUGUI promptText;

    private Camera playerCamera;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();

        // 遊戲開始時先隱藏提示文字
        if (promptText != null) promptText.enabled = false;
    }

    void Update()
    {
        // 如果遊戲暫停，就不執行
        if (Time.timeScale == 0f) return;

        // 【關鍵 1】：每一幀一開始，預設先關閉 UI
        if (promptText != null) promptText.enabled = false;

        // 【關鍵 2】：把射線移到外面，讓它「無時無刻」都在掃描前方
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // 檢查被打到的東西身上，有沒有 IInteractable 身分證？
            IInteractable interactableObject = hit.collider.GetComponentInParent<IInteractable>();

            if (interactableObject != null)
            {
                // ================== 動態 UI 顯示邏輯 ==================
                if (promptText != null)
                {
                    promptText.enabled = true; // 看到可互動的物件了，把文字打開

                    // 檢查這個物件是不是「手動門 (ManualDoor)」
                    ManualDoor door = hit.collider.GetComponentInParent<ManualDoor>();

                    if (door != null && door.targetDoor != null)
                    {
                        // 根據門的狀態，顯示不同的文字
                        promptText.text = door.targetDoor.isOpen ? "按 F 關" : "按 F 開";
                    }
                    else
                    {
                        // 如果是其他可互動的物件（例如藥丸、雷射筆）
                        promptText.text = "按 F 互動";
                    }
                }
                // ======================================================

                // 當玩家按下 F 鍵時，執行互動
                if (Input.GetKeyDown(interactKey))
                {
                    interactableObject.OnInteract(this.transform);
                }
            }
        }
    }
}
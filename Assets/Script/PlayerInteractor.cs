using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("互動設定")]
    [Tooltip("玩家可以按 F 互動的最遠距離")]
    public float interactRange = 3.0f;
    [Tooltip("互動按鍵")]
    public KeyCode interactKey = KeyCode.F;

    private Camera playerCamera;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // 如果遊戲暫停，就不執行
        if (Time.timeScale == 0f) return;

        // 當玩家按下 F 鍵
        if (Input.GetKeyDown(interactKey))
        {
            // 從畫面正中央發射一條隱形射線
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                // 檢查被打到的東西身上，有沒有 IInteractable 身分證？
                IInteractable interactableObject = hit.collider.GetComponentInParent<IInteractable>();

                if (interactableObject != null)
                {
                    // 【關鍵修改】：把玩家自己的 Transform 傳過去！
                    interactableObject.OnInteract(this.transform);
                }
            }
        }
    }
}
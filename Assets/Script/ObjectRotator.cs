using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("旋轉設定")]
    [Tooltip("每次按下按鍵要旋轉的角度")]
    public float rotationStep = 15f;

    [Header("互動限制")]
    [Tooltip("玩家需要靠多近才能操作？(單位: 公尺)")]
    public float interactDistance = 2.5f;

    private Camera mainCamera;

    void Start()
    {
        // 遊戲開始時，自動尋找場景中掛有 "MainCamera" 標籤的玩家攝影機
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("<color=red>[錯誤]</color> 找不到玩家攝影機！請確定你的攝影機有設定 'MainCamera' 標籤。");
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // 1. 產生一條從「攝影機正中心」往「正前方」發射的隱形射線
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // 2. 發射射線，並限制它的最遠距離為 interactDistance
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // 3. 如果射線打中了東西，檢查那個東西是不是「我自己 (這個掛著腳本的物件)」
            ObjectRotator target = hit.collider.GetComponent<ObjectRotator>();

            if (target == this)
            {
                // 只有在「距離夠近」且「準心剛好指著我」的時候，才允許按 Q/E
                HandleRotationInput();
            }
        }
    }

    private void HandleRotationInput()
    {
        // 按下 Q 鍵向左轉 (改為以自身 Z 軸逆時針轉)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 【修改這裡】：把 -rotationStep 移到第三個參數 (Z軸)
            transform.Rotate(0, 0, -rotationStep, Space.Self);
            Debug.Log($"<color=cyan>[物件旋轉]</color> 玩家對準並沿 Z 軸向左轉了 {rotationStep} 度！");
        }

        // 按下 E 鍵向右轉 (改為以自身 Z 軸順時針轉)
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 【修改這裡】：把 rotationStep 移到第三個參數 (Z軸)
            transform.Rotate(0, 0, rotationStep, Space.Self);
            Debug.Log($"<color=cyan>[物件旋轉]</color> 玩家對準並沿 Z 軸向右轉了 {rotationStep} 度！");
        }
    }
}
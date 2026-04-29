using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LaserPickup : MonoBehaviour
{
    private bool isPlayerNear = false;

    // 把我們要記住的東西宣告在這裡 (全域變數)
    private PlayerLaserPointer playerHandLaser;
    private InventoryManager playerInventory;

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // 檢查撞到我們的是不是玩家
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;

            // 1. 記住玩家手上的雷射筆
            playerHandLaser = other.transform.root.GetComponentInChildren<PlayerLaserPointer>(true);

            // 2. 【核心修復】：順便記住玩家身上的背包系統！
            playerInventory = other.transform.root.GetComponentInChildren<InventoryManager>();

            Debug.Log("<color=yellow>[撿拾系統]</color> 玩家進入範圍！可以按 [F] 撿起雷射筆了！");

            if (playerInventory == null)
            {
                Debug.LogError("<color=red>[嚴重錯誤]</color> 我找不到玩家身上的 InventoryManager 腳本！");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            // 玩家離開時，把記憶清空
            playerHandLaser = null;
            playerInventory = null;
            Debug.Log("<color=grey>[撿拾系統]</color> 玩家離開了雷射筆範圍。");
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (playerInventory != null)
            {
                // 【修正這裡】：把 inventory 改回 playerInventory
                if (playerInventory.AddItemToInventory(0, 1, 1))
                {
                    // 呼叫雷射筆本身的設定 (充飽電等)
                    if (playerHandLaser != null)
                    {
                        playerHandLaser.EquipLaser();
                    }

                    Debug.Log("<color=green>[撿拾系統]</color> 成功撿起並收進背包！");
                    Destroy(gameObject); // 塞成功了，銷毀地上的道具
                }
            }
        }
    }
}
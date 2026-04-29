using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PillPickup : MonoBehaviour
{
    private bool isPlayerNear = false;
    private InventoryManager playerInventory;

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerInventory = other.transform.root.GetComponentInChildren<InventoryManager>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            playerInventory = null;
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (playerInventory != null)
            {
                // 【關鍵參數】：代表這是一號武器(理智藥)，給 1 瓶，最大可疊加 3 瓶！
                if (playerInventory.AddItemToInventory(1, 1, 3))
                {
                    Debug.Log("<color=green>[撿拾系統]</color> 獲得理智藥！");
                    Destroy(gameObject);
                }
            }
        }
    }
}
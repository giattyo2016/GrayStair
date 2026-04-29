using UnityEngine;

public class PlayerSanityPill : MonoBehaviour
{
    [Header("理智藥設定")]
    public float healAmount = 30f;

    // 【新增】：吃下一瓶藥的冷卻時間 (秒)
    [Tooltip("連續吃藥的間隔時間")]
    public float consumeCooldown = 0.5f;

    private PlayerSanity playerSanity;
    private InventoryManager inventory;
    private bool isConsuming = false;

    void Awake()
    {
        playerSanity = GetComponentInParent<PlayerSanity>();
        inventory = GetComponentInParent<InventoryManager>();
    }

    void OnEnable()
    {
        isConsuming = false; // 每次拿出來時，確保是可吃狀態
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isConsuming)
        {
            ConsumePill();
        }
    }

    private void ConsumePill()
    {
        if (playerSanity != null && playerSanity.currentSanity >= playerSanity.maxSanity)
        {
            Debug.Log("<color=yellow>[道具]</color> 理智已經是滿的，不需要吃藥！");
            return;
        }

        isConsuming = true;

        Debug.Log("<color=green>[道具]</color> 吞下理智藥！視野逐漸清晰...");

        if (playerSanity != null)
        {
            playerSanity.HealSanity(healAmount);
        }

        if (inventory != null)
        {
            inventory.ConsumeCurrentItem();
        }

        // 【新增魔法】：設定一個鬧鐘，在 consumeCooldown 秒之後，自動呼叫 ResetConsume 方法來解鎖！
        Invoke(nameof(ResetConsume), consumeCooldown);
    }

    // 【新增】：用來解鎖吃藥狀態的方法
    private void ResetConsume()
    {
        isConsuming = false;
    }
}
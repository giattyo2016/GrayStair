using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("漂浮設定")]
    [Tooltip("漂浮的高度 (上下移動的最高點與最低點差距)")]
    public float floatHeight = 0.5f;

    [Tooltip("漂浮的速度 (數字越大，上下飄得越快)")]
    public float floatSpeed = 2.0f;

    // 用來記錄物件一開始出現在場景的絕對位置
    private Vector3 startPos;

    void Start()
    {
        // 遊戲開始時，先把物件原本的位子記下來
        startPos = transform.position;
    }

    void Update()
    {
        // ==========================================
        // 核心魔法：使用 Sin 波來產生平滑的上下起伏
        // Time.time 會隨著遊戲時間不斷增加
        // Mathf.Sin() 會把輸入的數字轉換成 -1 到 1 之間來回平滑變動的數值
        // ==========================================
        float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatHeight);

        // 把計算出來的新 Y 軸座標套用到物件上，X 跟 Z 維持不變
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
using UnityEngine;

public class BridgeController : MonoBehaviour
{
    [Header("橋樑設定")]
    [Tooltip("橋樑完全延伸時的最大長度 (向前長多少公尺)")]
    public float maxBridgeLength = 10f;

    [Tooltip("橋樑延伸與收回的速度")]
    public float extendSpeed = 5f;

    private float currentLength = 0f; // 目前的長度 (預設為0)
    private bool isExtending = false; // 目前是伸長還是收回狀態？

    private Vector3 initialScale; // 記憶一開始的寬度和厚度

    void Start()
    {
        // 記錄初始的 X 和 Y 縮放值 (確保你的橋維持原本的寬度跟厚度)
        initialScale = transform.localScale;

        // 遊戲一開始，強制把橋的長度設為 0 (隱藏狀態)
        currentLength = 0f;
        UpdateBridgeTransform();
    }

    void Update()
    {
        // 決定目標長度：如果是啟動狀態，目標就是 maxBridgeLength；否則就是 0
        float targetLength = isExtending ? maxBridgeLength : 0f;

        // 使用 Lerp 讓數值平滑過渡，產生「生長」的動畫感
        currentLength = Mathf.Lerp(currentLength, targetLength, Time.deltaTime * extendSpeed);

        // 隨時更新橋的大小和位置
        UpdateBridgeTransform();
    }

    private void UpdateBridgeTransform()
    {
        // 1. 改變 Z 軸的縮放 (也就是長度)
        transform.localScale = new Vector3(initialScale.x, initialScale.y, currentLength);

        // 2. 【單向延伸的數學魔法】：把位置往前推「長度的一半」
        // 這樣起點就永遠會固定在同一個地方！
        transform.localPosition = new Vector3(0, 0, currentLength / 2f);
    }

    // 提供給雷射感應器呼叫的公開方法 (跟門一模一樣！)
    public void SetBridgeState(bool state)
    {
        isExtending = state;
    }
}
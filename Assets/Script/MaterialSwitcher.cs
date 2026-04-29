using UnityEngine;

// 這個標籤會確保掛載此腳本的物件，身上一定有 MeshRenderer (否則無法變色)
[RequireComponent(typeof(MeshRenderer))]
public class MaterialSwitcher : MonoBehaviour
{
    [Header("材質設定")]
    [Tooltip("初始/關閉時的材質 (例如：紅色)")]
    public Material offMaterial;

    [Tooltip("按下/啟動時的材質 (例如：綠色)")]
    public Material onMaterial;

    private MeshRenderer meshRenderer;
    private bool isSwitchedOn = false; // 記錄目前的狀態

    void Start()
    {
        // 抓取物件身上的渲染器
        meshRenderer = GetComponent<MeshRenderer>();

        // 遊戲一開始，強制顯示初始材質
        if (offMaterial != null)
        {
            meshRenderer.material = offMaterial;
        }
    }

    /// <summary>
    /// 提供給其他程式 (如按鈕) 呼叫的切換函式
    /// </summary>
    public void ToggleVisual()
    {
        // 狀態反轉
        isSwitchedOn = !isSwitchedOn;

        // 根據狀態切換材質
        if (meshRenderer != null && offMaterial != null && onMaterial != null)
        {
            meshRenderer.material = isSwitchedOn ? onMaterial : offMaterial;
        }
    }
}
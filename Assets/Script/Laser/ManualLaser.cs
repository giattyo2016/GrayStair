using UnityEngine;

public class ManualLaser : MonoBehaviour, IInteractable
{
    [Header("連動設定")]
    [Tooltip("把你要控制的雷射發射器拖曳到這裡！")]
    public LaserEmitter targetLaser;

    [Header("視覺回饋 (選填)")]
    [Tooltip("如果按鈕本身會變色，把掛有 MaterialSwitcher 的物件拖來這裡")]
    public MaterialSwitcher visualFeedback;

    public void OnInteract(Transform interactor)
    {
        // 1. 開關雷射
        if (targetLaser != null)
        {
            targetLaser.ToggleLaser();
        }

        // 2. 如果你有綁定變色積木，就叫它變色！
        if (visualFeedback != null)
        {
            visualFeedback.ToggleVisual();
        }
    }
}
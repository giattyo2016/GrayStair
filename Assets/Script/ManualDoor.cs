using UnityEngine;

// 簽署 IInteractable 身分證，代表它可以被玩家按 F 互動
public class ManualDoor : MonoBehaviour, IInteractable
{
    [Header("連動設定")]
    [Tooltip("把你要控制的門拖曳到這裡！")]
    public DoorController targetDoor;

    [Header("視覺回饋 (選填)")]
    [Tooltip("如果按鈕本身會變色，把掛有 MaterialSwitcher 的物件拖來這裡")]
    public MaterialSwitcher visualFeedback;

    // 接收玩家的位置資訊
    public void OnInteract(Transform interactor)
    {
        // 1. 呼叫門的智慧開關功能
        if (targetDoor != null)
        {
            targetDoor.ToggleDoor(interactor.position);
        }

        // 2. 如果你有綁定變色積木，就叫它變色！
        if (visualFeedback != null)
        {
            visualFeedback.ToggleVisual();
        }
    }
}
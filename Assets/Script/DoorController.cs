using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum DoorType { Sliding, Rotating }

    [Header("門的類型設定")]
    public DoorType doorType = DoorType.Sliding;

    [Header("滑動門設定 (如果選 Sliding)")]
    public Vector3 openOffset = new Vector3(0, 3, 0);

    [Header("旋轉門設定 (如果選 Rotating)")]
    public Vector3 openRotationOffset = new Vector3(0, 90, 0);

    [Tooltip("如果發現門總是往你臉上拍，就把這個打勾反轉方向！")]
    public bool invertPushDirection = false;

    [Header("共通設定")]
    public float openSpeed = 5.0f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen = false;

    void Start()
    {
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;

        openPosition = closedPosition + openOffset;
        openRotation = closedRotation * Quaternion.Euler(openRotationOffset);
    }

    void Update()
    {
        if (doorType == DoorType.Sliding)
        {
            Vector3 targetPos = isOpen ? openPosition : closedPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * openSpeed);
        }
        else if (doorType == DoorType.Rotating)
        {
            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * openSpeed);
        }
    }

    // 【新增】：專門給玩家手動按 F 用的「智慧雙向開關」
    public void ToggleDoor(Vector3 interactorPosition)
    {
        isOpen = !isOpen; // 切換開關狀態

        if (isOpen && doorType == DoorType.Rotating)
        {
            // 1. 將玩家的「世界座標」轉換成門鉸鏈的「局部相對座標」
            Vector3 localPlayerPos = transform.InverseTransformPoint(interactorPosition);

            // 2. 判斷玩家在門的正面(+Z)還是背面(-Z)
            float multiplier = (localPlayerPos.z > 0) ? 1f : -1f;

            // 3. 如果方向剛好相反，就套用反轉係數
            if (invertPushDirection) multiplier *= -1f;

            // 4. 重新計算這次打開應該要轉的角度！
            openRotation = closedRotation * Quaternion.Euler(openRotationOffset * multiplier);
        }
    }

    // 保持不變：給雷射接收器 (LaserSensor) 用的標準開關
    public void SetDoorState(bool state)
    {
        if (isOpen == state) return; // 狀態沒變就不做事

        isOpen = state;
        if (isOpen && doorType == DoorType.Rotating)
        {
            // 雷射開門一律使用預設方向
            openRotation = closedRotation * Quaternion.Euler(openRotationOffset);
        }
    }
}
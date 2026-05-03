using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum DoorType { Sliding, Rotating }

    // 【新增】：設定速度模式的選項
    public enum SpeedMode { SameSpeed, SeparateSpeeds }

    [Header("門的類型設定")]
    public DoorType doorType = DoorType.Sliding;

    [Header("滑動門設定 (如果選 Sliding)")]
    public Vector3 openOffset = new Vector3(0, 3, 0);

    [Header("旋轉門設定 (如果選 Rotating)")]
    public Vector3 openRotationOffset = new Vector3(0, 90, 0);

    [Tooltip("如果發現門總是往你臉上拍，就把這個打勾反轉方向！")]
    public bool invertPushDirection = false;

    [Header("速度設定")]
    [Tooltip("SameSpeed(A選項): 開關速度一樣 \nSeparateSpeeds(B選項): 可獨立調整開門與關門速度")]
    public SpeedMode speedMode = SpeedMode.SameSpeed;

    [Tooltip("開門的速度 (若為 SameSpeed，則開關門都看這個數值)")]
    public float openSpeed = 5.0f;

    [Tooltip("關門的速度 (只有當上方選項切換為 SeparateSpeeds 時才有效)")]
    public float closeSpeed = 5.0f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    // 讓玩家的射線能讀取它的狀態！
    public bool isOpen = false;

    void Start()
    {
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;

        openPosition = closedPosition + openOffset;
        openRotation = closedRotation * Quaternion.Euler(openRotationOffset);
    }

    void Update()
    {
        // 【新增】：動態決定現在要用哪一個速度
        float currentSpeed = openSpeed; // 預設使用開門速度 (A選項)

        // 如果玩家選了 B 選項 (SeparateSpeeds)，且現在門的狀態是「關閉」，就切換成關門速度
        if (speedMode == SpeedMode.SeparateSpeeds && !isOpen)
        {
            currentSpeed = closeSpeed;
        }

        // 下方的運算把原本寫死的 openSpeed，換成我們剛剛算出來的 currentSpeed
        if (doorType == DoorType.Sliding)
        {
            Vector3 targetPos = isOpen ? openPosition : closedPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * currentSpeed);
        }
        else if (doorType == DoorType.Rotating)
        {
            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * currentSpeed);
        }
    }

    // 專門給玩家手動按 F 用的「智慧雙向開關」
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

    // 給雷射接收器 (LaserSensor) 用的標準開關
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
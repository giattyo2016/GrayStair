using UnityEngine;

public class HeadBobbing : MonoBehaviour
{
    [Header("連結設定")]
    [Tooltip("自動抓取玩家身上的 CharacterController")]
    public CharacterController playerController;

    [Header("走路步伐設定")]
    public float walkBobSpeed = 12f;
    public float walkBobAmount = 0.05f;
    public float tiltAngle = 1.5f;

    [Header("跑步步伐設定")]
    public float runBobSpeed = 18f;
    public float runBobAmount = 0.1f;
    public float runTiltAngle = 2.5f;

    [Header("待機呼吸設定")]
    public float idleBobSpeed = 2f;
    public float idleBobAmount = 0.01f;

    [Header("恐慌震動設定 (Sanity Shake)")]
    public float shakeSpeed = 25f;
    public float currentShakeMagnitude = 0f;

    [Header("平滑過渡")]
    public float smoothSpeed = 10f;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private float timer = 0;

    // 【新增】：用來記錄玩家「上一幀」的真實世界座標
    private Vector3 previousPlayerPosition;

    void Start()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;

        if (playerController == null)
        {
            playerController = GetComponentInParent<CharacterController>();
        }

        // 遊戲開始時，先記錄第一筆座標
        if (playerController != null)
        {
            previousPlayerPosition = playerController.transform.position;
        }
    }

    void Update()
    {
        bool isMoving = false;

        if (playerController != null)
        {
            // 【終極修正】：不相信內建的速度！我們自己用座標算！
            // 拿「現在的座標」減去「上一幀的座標」，忽略掉落的 Y 軸
            Vector3 currentPlayerPosition = playerController.transform.position;
            Vector3 flatMovement = new Vector3(
                currentPlayerPosition.x - previousPlayerPosition.x,
                0f,
                currentPlayerPosition.z - previousPlayerPosition.z
            );

            // 計算真實每秒移動速度 (距離 ÷ 時間)
            float currentSpeed = flatMovement.magnitude / Time.deltaTime;

            // 只要真實位移大於 0.1 才算是有在走
            isMoving = currentSpeed > 0.1f;

            // 算完之後，把現在的座標存起來，留給下一幀比較
            previousPlayerPosition = currentPlayerPosition;
        }
        else
        {
            // 防呆機制：如果真的沒抓到控制器，才退回舊版的鍵盤讀取
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        }

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving;

        Vector3 baseTargetPosition = defaultPosition;
        Quaternion baseTargetRotation = defaultRotation;

        if (isMoving)
        {
            float currentBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float currentBobAmount = isRunning ? runBobAmount : walkBobAmount;
            float currentTiltAngle = isRunning ? runTiltAngle : tiltAngle;

            timer += Time.deltaTime * currentBobSpeed;

            baseTargetPosition.y = defaultPosition.y + Mathf.Sin(timer) * currentBobAmount;

            float newZ = Mathf.Cos(timer / 2f) * currentTiltAngle;
            baseTargetRotation = defaultRotation * Quaternion.Euler(0, 0, newZ);
        }
        else
        {
            timer += Time.deltaTime * idleBobSpeed;

            baseTargetPosition.y = defaultPosition.y + Mathf.Sin(timer) * idleBobAmount;
            baseTargetRotation = defaultRotation;
        }

        // ================== 核心魔法：柏林雜訊恐慌震動 ==================
        float shakeX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * currentShakeMagnitude;
        float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * currentShakeMagnitude;
        float shakeZ = (Mathf.PerlinNoise(Time.time * shakeSpeed, Time.time * shakeSpeed) - 0.5f) * 2f * (currentShakeMagnitude * 10f);

        Vector3 finalTargetPosition = baseTargetPosition + new Vector3(shakeX, shakeY, 0f);
        Quaternion finalTargetRotation = baseTargetRotation * Quaternion.Euler(0, 0, shakeZ);

        // ================== 平滑運算 (Lerp) ==================
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalTargetPosition, Time.deltaTime * smoothSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, finalTargetRotation, Time.deltaTime * smoothSpeed);
    }
}
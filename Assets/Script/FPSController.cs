using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6.0f;
    public float runSpeed = 10.0f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    // 【修改核心 1】：把 Camera 型別改成 Transform
    // 這樣我們才能把「沒有攝影機組件」的空物件 (CameraHolder) 拖進來！
    [Tooltip("請把 Player 底下的 CameraHolder 空物件拖進來")]
    public Transform cameraHolder;

    public float mouseSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Interaction")]
    public PhysicsGrabber grabber;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;
    private bool isPaused = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 【修改核心 2】：防呆機制。如果你忘記拖曳，自動尋找名為 CameraHolder 的子物件
        if (cameraHolder == null)
        {
            Transform foundHolder = transform.Find("CameraHolder");
            if (foundHolder != null)
            {
                cameraHolder = foundHolder;
            }
            else
            {
                Debug.LogError("<color=red>[FPSController]</color> 找不到 CameraHolder！請確定你有建立這個空物件並拖入欄位！");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPaused) return;

        // --- 處理滑鼠旋轉 (Look Logic) ---
        if (canMove && cameraHolder != null)
        {
            if (grabber == null || !grabber.isInspecting)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                rotationX -= mouseY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                // 【修改核心 3】：滑鼠上下看時，轉動的是 CameraHolder！不再是 Camera 本身了！
                cameraHolder.localRotation = Quaternion.Euler(rotationX, 0, 0);

                // 左右看時，轉動玩家本體
                transform.rotation *= Quaternion.Euler(0, mouseX, 0);
            }
        }

        // --- 處理移動 (Movement Logic) ---
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // --- 處理跳躍與重力 (Jump & Gravity) ---
        if (characterController.isGrounded)
        {
            moveDirection.y = -0.5f;

            if (canMove && Input.GetButtonDown("Jump"))
            {
                moveDirection.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }
        }
        else
        {
            moveDirection.y = movementDirectionY + (gravity * Time.deltaTime);
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canMove = false;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            canMove = true;
        }
    }
}
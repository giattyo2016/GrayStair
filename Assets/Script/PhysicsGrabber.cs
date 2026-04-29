using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float playerMaxStrength = 15.0f;

    [Header("Grab Physics Settings")]
    [SerializeField] private float grabRange = 4.0f;
    [SerializeField] private float holdDistance = 2.5f;

    [SerializeField] private float maxGrabSpeed = 20.0f;
    [SerializeField] private float minGrabSpeed = 3.0f;
    [SerializeField] private float maxSinkAmount = 0.6f;
    [SerializeField] private float rotateSpeed = 15.0f;

    [Header("Input Setup")]
    [SerializeField] private KeyCode grabKey = KeyCode.E;       // 撿起按鍵
    [SerializeField] private KeyCode dropKey = KeyCode.Mouse0;  // 放下按鍵 (滑鼠左鍵)

    // --- 新增：手動檢視旋轉的按鍵與靈敏度 ---
    [SerializeField] private KeyCode manualRotateKey = KeyCode.Mouse1; // 手動旋轉按鍵 (滑鼠右鍵)
    [SerializeField] private float manualRotateSensitivity = 3.0f;     // 滑鼠轉動物件的靈敏度

    // 【新增】：讓外部腳本可以讀取的狀態變數！
    // { get; private set; } 代表別人只能讀取，不能亂改它
    public bool isInspecting { get; private set; } = false;

    private Camera playerCamera;
    private Rigidbody heldRigidbody;

    private float originalLinearDamping;
    private float originalAngularDamping;
    private float heldObjectMass = 0f;

    private CharacterController characterController;
    private Quaternion initialRotationOffset;
    private Vector3 localGrabOffset;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null || characterController == null)
        {
            Debug.LogError("PhysicsGrabber: 找不到 Camera 或 CharacterController，請檢查設定！");
            enabled = false;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (heldRigidbody != null) ReleaseObject();
            return;
        }

        if (heldRigidbody != null)
        {
            CheckIfStandingOnHeldObject();
        }

        if (heldRigidbody == null)
        {
            // 【新增】：如果手上沒東西，絕對不在檢視狀態
            isInspecting = false;

            if (Input.GetKeyDown(grabKey))
            {
                TryGrabObject();
            }
        }
        else
        {
            // ==========================================
            // 【新增邏輯】：按住右鍵時，旋轉手上的物件
            // ==========================================
            if (Input.GetKey(manualRotateKey))
            {
                // 【新增】：告訴全世界，我現在正在檢視！
                isInspecting = true;

                float mouseX = Input.GetAxis("Mouse X") * manualRotateSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * manualRotateSensitivity;

                Quaternion xRotation = Quaternion.AngleAxis(mouseY, Vector3.right);
                Quaternion yRotation = Quaternion.AngleAxis(-mouseX, Vector3.up);

                initialRotationOffset = yRotation * xRotation * initialRotationOffset;
            }
            else
            {
                // 【新增】：放開右鍵時，解除檢視狀態
                isInspecting = false;
            }

            if (Input.GetKeyDown(dropKey))
            {
                // 【新增】：把東西丟掉時，也解除檢視狀態
                isInspecting = false;
                ReleaseObject();
            }
        }
    }

    void FixedUpdate()
    {
        if (heldRigidbody != null)
        {
            MoveObjectWithWeightFeeling();
        }
    }

    private void TryGrabObject()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabRange))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

            if (rb != null && (hit.collider.CompareTag("Grabbable") || hit.collider.GetComponent<Grabbable>() != null))
            {
                if (rb.mass <= playerMaxStrength)
                {
                    GrabObject(rb, hit);
                }
                else
                {
                    Debug.Log("太重了完全拿不起來！這個物品重達: " + rb.mass + " kg");
                }
            }
        }
    }

    private void GrabObject(Rigidbody rbToGrab, RaycastHit hit)
    {
        heldRigidbody = rbToGrab;
        heldObjectMass = heldRigidbody.mass;

        initialRotationOffset = Quaternion.Inverse(playerCamera.transform.rotation) * heldRigidbody.rotation;
        localGrabOffset = Quaternion.Inverse(heldRigidbody.rotation) * (heldRigidbody.position - hit.point);

        originalLinearDamping = heldRigidbody.linearDamping;
        originalAngularDamping = heldRigidbody.angularDamping;

        heldRigidbody.linearDamping = 10f;
        heldRigidbody.angularDamping = 10f;

        heldRigidbody.useGravity = false;
    }

    private void MoveObjectWithWeightFeeling()
    {
        float weightFactor = Mathf.Clamp01(heldObjectMass / playerMaxStrength);
        float currentGrabSpeed = Mathf.Lerp(maxGrabSpeed, minGrabSpeed, weightFactor);

        Quaternion targetRotation = playerCamera.transform.rotation * initialRotationOffset;

        Quaternion rotDelta = targetRotation * Quaternion.Inverse(heldRigidbody.rotation);
        rotDelta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.1f)
        {
            heldRigidbody.angularVelocity = (axis * angle * Mathf.Deg2Rad) * rotateSpeed;
        }

        Vector3 baseTargetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
        Vector3 sinkOffset = playerCamera.transform.up * -(maxSinkAmount * weightFactor);
        Vector3 finalHoldPosition = baseTargetPosition + sinkOffset;

        Vector3 currentGrabOffset = targetRotation * localGrabOffset;
        Vector3 targetCenterPosition = finalHoldPosition + currentGrabOffset;

        Vector3 moveDirection = targetCenterPosition - heldRigidbody.position;
        heldRigidbody.linearVelocity = moveDirection * currentGrabSpeed;
    }

    private void ReleaseObject()
    {
        heldRigidbody.useGravity = true;
        heldRigidbody.linearDamping = originalLinearDamping;
        heldRigidbody.angularDamping = originalAngularDamping;
        heldRigidbody = null;
        heldObjectMass = 0f;
    }

    private void CheckIfStandingOnHeldObject()
    {
        Vector3 rayStart = characterController.bounds.center;
        float castDistance = (characterController.height / 2f) + 0.3f;

        if (Physics.SphereCast(rayStart, characterController.radius, Vector3.down, out RaycastHit hit, castDistance))
        {
            if (hit.collider.attachedRigidbody == heldRigidbody)
            {
                Debug.Log("⚠️ 警告：企圖踩著物件起飛！已自動強制解除抓取。");
                ReleaseObject();
            }
        }
    }
}
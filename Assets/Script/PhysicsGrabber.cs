using UnityEngine;
using TMPro; // 需要引用 TextMeshPro 來控制 UI

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

    [SerializeField] private KeyCode manualRotateKey = KeyCode.Mouse1; // 手動旋轉按鍵 (滑鼠右鍵)
    [SerializeField] private float manualRotateSensitivity = 3.0f;     // 滑鼠轉動物件的靈敏度

    [Header("UI & Highlight Settings")]
    [SerializeField] private TextMeshProUGUI grabPromptText; // 拖曳你的 "按 E 互動" UI 到這裡
    [ColorUsage(true, true)] // 允許開啟 HDR 高亮度顏色
    [SerializeField] private Color highlightColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 發光的顏色 (預設微亮白)

    public bool isInspecting { get; private set; } = false;

    private Camera playerCamera;
    private Rigidbody heldRigidbody;

    private float originalLinearDamping;
    private float originalAngularDamping;
    private float heldObjectMass = 0f;

    private CharacterController characterController;
    private Quaternion initialRotationOffset;
    private Vector3 localGrabOffset;

    // --- 新增：紀錄目前瞄準到的物件，用來處理發光 ---
    private GameObject currentTargetObject;
    private Material[] targetOriginalMaterials;
    private bool isHighlighting = false;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null || characterController == null)
        {
            Debug.LogError("PhysicsGrabber: 找不到 Camera 或 CharacterController，請檢查設定！");
            enabled = false;
        }

        // 遊戲開始時，確保提示文字是隱藏的
        if (grabPromptText != null)
        {
            grabPromptText.gameObject.SetActive(false);
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

        // ====================================================
        // 核心邏輯：空手狀態時，不斷發射射線偵測前方物件
        // ====================================================
        if (heldRigidbody == null)
        {
            isInspecting = false;
            HandleRaycastAndHighlight(); // 處理準心對準與發光

            if (Input.GetKeyDown(grabKey) && currentTargetObject != null)
            {
                // 如果有對準東西，而且按下了 E 鍵，就抓起來！
                TryGrabObject();
            }
        }
        else
        {
            // 如果手上有拿著東西，強制關閉瞄準提示與發光
            ClearTargetAndHighlight();

            if (Input.GetKey(manualRotateKey))
            {
                isInspecting = true;
                float mouseX = Input.GetAxis("Mouse X") * manualRotateSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * manualRotateSensitivity;

                Quaternion xRotation = Quaternion.AngleAxis(mouseY, Vector3.right);
                Quaternion yRotation = Quaternion.AngleAxis(-mouseX, Vector3.up);

                initialRotationOffset = yRotation * xRotation * initialRotationOffset;
            }
            else
            {
                isInspecting = false;
            }

            if (Input.GetKeyDown(dropKey))
            {
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

    // ====================================================
    // 新增：處理射線偵測、UI 顯示與材質發光
    // ====================================================
    private void HandleRaycastAndHighlight()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // 如果射線有打到東西，且在抓取距離內
        if (Physics.Raycast(ray, out hit, grabRange))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

            // 檢查是否是可以抓的物件
            if (rb != null && (hit.collider.CompareTag("Grabbable") || hit.collider.GetComponent<Grabbable>() != null))
            {
                // 檢查是否太重
                if (rb.mass <= playerMaxStrength)
                {
                    // 如果這是我這幀才剛看著的新物件，或者我一直看著它
                    if (currentTargetObject != hit.collider.gameObject)
                    {
                        // 先清除舊的發光
                        ClearTargetAndHighlight();
                        // 記錄新的物件並讓它發光
                        currentTargetObject = hit.collider.gameObject;
                        ApplyHighlight(currentTargetObject);
                    }

                    // 顯示 UI
                    if (grabPromptText != null) grabPromptText.gameObject.SetActive(true);
                    return; // 成功找到可互動物件，結束這個函式
                }
            }
        }

        // 如果射線沒有打到任何可以抓的東西，或者距離太遠，清除所有的提示
        ClearTargetAndHighlight();
    }

    private void ApplyHighlight(GameObject target)
    {
        if (isHighlighting) return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 讓物件的材質開啟 Emission (發光) 效果
            foreach (Material mat in renderer.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", highlightColor);
            }
            isHighlighting = true;
        }
    }

    private void ClearTargetAndHighlight()
    {
        // 隱藏 UI
        if (grabPromptText != null)
        {
            grabPromptText.gameObject.SetActive(false);
        }

        // 關閉發光
        if (currentTargetObject != null && isHighlighting)
        {
            Renderer renderer = currentTargetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    // 把發光顏色設定回黑色 (關閉發光)
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
            isHighlighting = false;
        }

        currentTargetObject = null;
    }

    // ====================================================

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
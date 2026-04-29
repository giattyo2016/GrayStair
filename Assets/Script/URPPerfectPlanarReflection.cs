using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class URPPerfectPlanarReflection : MonoBehaviour
{
    [Header("鏡面設定")]
    [Tooltip("材質使用的解析度 (1024為佳)")]
    public int resolution = 1024;

    [Range(0.0f, 0.1f)]
    [Tooltip("安全偏移量：防止自我破圖 (建議調 0.01)")]
    public float m_CullingSafetyOffset = 0.01f;

    [Tooltip("哪些層級的物件會顯示在鏡子裡？")]
    public LayerMask m_ReflectLayers = -1;

    // 存放資源的字典
    private static Dictionary<Camera, Camera> m_ReflectionCameras = new Dictionary<Camera, Camera>();

    private RenderTexture m_ReflectionTexture;
    private Material m_DynamicMaterial;
    private bool isExecuting = false;

    // 【新增】：用來抓取鏡子本體的渲染器，判斷它有沒有在畫面上
    private Renderer m_Renderer;

    void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += ExecutePlanarReflections;
    }

    void OnDisable()
    {
        CleanupResources();
        RenderPipelineManager.beginCameraRendering -= ExecutePlanarReflections;
    }

    void OnDestroy()
    {
        CleanupResources();
    }

    void CleanupResources()
    {
        if (m_ReflectionTexture != null)
        {
            m_ReflectionTexture.Release();
            DestroyImmediate(m_ReflectionTexture);
        }
        if (m_DynamicMaterial != null)
        {
            DestroyImmediate(m_DynamicMaterial);
        }

        foreach (var cam in m_ReflectionCameras)
        {
            if (cam.Value != null) DestroyImmediate(cam.Value.gameObject);
        }
        m_ReflectionCameras.Clear();
    }

    void ExecutePlanarReflections(ScriptableRenderContext context, Camera camera)
    {
        // 防呆機制
        if (isExecuting || !enabled || !gameObject.activeInHierarchy || camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
            return;

        // ==========================================
        // 【效能優化魔法】：視錐體剔除 (Frustum Culling)
        // 檢查這個攝影機的視野範圍，有沒有碰到鏡子的模型邊界。
        // 如果玩家根本沒看著鏡子，我們就直接 return 罷工，省下巨量效能！
        // ==========================================
        if (m_Renderer != null)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (!GeometryUtility.TestPlanesAABB(planes, m_Renderer.bounds))
            {
                return; // 沒看到鏡子？直接結束這回合，不浪費效能！
            }
        }

        isExecuting = true;

        InitializeResources();

        Camera reflectionCamera;
        if (!m_ReflectionCameras.TryGetValue(camera, out reflectionCamera) || reflectionCamera == null)
        {
            GameObject go = new GameObject("Planar_Reflection_Camera_" + camera.name + "_" + gameObject.name, typeof(Camera), typeof(UniversalAdditionalCameraData));
            go.hideFlags = HideFlags.HideAndDontSave;
            reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.enabled = false;
            m_ReflectionCameras[camera] = reflectionCamera;
        }

        UpdateReflectionCamera(camera, reflectionCamera);

        // API 升級：SubmitRenderRequest
        UniversalRenderPipeline.SingleCameraRequest requestData = new UniversalRenderPipeline.SingleCameraRequest();
        requestData.destination = m_ReflectionTexture;
        RenderPipeline.SubmitRenderRequest(reflectionCamera, requestData);

        GetComponent<MeshRenderer>().material = m_DynamicMaterial;

        isExecuting = false;
    }

    void InitializeResources()
    {
        if (m_ReflectionTexture == null || m_ReflectionTexture.width != resolution)
        {
            if (m_ReflectionTexture != null)
            {
                m_ReflectionTexture.Release();
                DestroyImmediate(m_ReflectionTexture);
            }
            m_ReflectionTexture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32);
            m_ReflectionTexture.name = "Auto_Mirror_RT_" + gameObject.name;
            m_ReflectionTexture.isPowerOfTwo = true;
            m_ReflectionTexture.Create();
        }

        if (m_DynamicMaterial == null)
        {
            m_DynamicMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m_DynamicMaterial.SetTexture("_BaseMap", m_ReflectionTexture);
            m_DynamicMaterial.mainTextureScale = new Vector2(-1, 1);
            m_DynamicMaterial.mainTextureOffset = new Vector2(1, 0);
        }
    }

    void UpdateReflectionCamera(Camera realCamera, Camera reflectionCamera)
    {
        if (realCamera == null || reflectionCamera == null) return;

        reflectionCamera.CopyFrom(realCamera);
        reflectionCamera.cullingMask = m_ReflectLayers;
        reflectionCamera.useOcclusionCulling = false;

        var additionalData = reflectionCamera.GetUniversalAdditionalCameraData();
        additionalData.renderShadows = true;
        additionalData.renderPostProcessing = false;

        Transform mirrorPlane = transform;

        Vector3 localPlayerPos = mirrorPlane.InverseTransformPoint(realCamera.transform.position);
        localPlayerPos.z = -localPlayerPos.z;
        reflectionCamera.transform.position = mirrorPlane.TransformPoint(localPlayerPos);

        Vector3 mirrorForward = Vector3.Reflect(realCamera.transform.forward, mirrorPlane.forward);
        Vector3 mirrorUp = Vector3.Reflect(realCamera.transform.up, mirrorPlane.forward);
        reflectionCamera.transform.rotation = Quaternion.LookRotation(mirrorForward, mirrorUp);

        // 防透視魔法：斜錐體剔除平面 (Oblique Frustum)
        Vector4 worldPlane = CameraSpacePlane(reflectionCamera, mirrorPlane.position, mirrorPlane.forward, 1.0f);
        reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(worldPlane);
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * (-m_CullingSafetyOffset);
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cameraPos = m.MultiplyPoint(offsetPos);
        Vector3 cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPos, cameraNormal));
    }
}
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserEmitter : MonoBehaviour
{
    [Header("雷射開關設定")]
    [Tooltip("雷射預設是開啟還是關閉？")]
    public bool isLaserOn = true;

    [Header("雷射參數")]
    public float maxDistance = 100f;
    public int maxBounces = 10;
    private LineRenderer lineRenderer;

    [Header("自動動態光源 (新增)")]
    [Tooltip("是否要讓雷射自動產生真實的點光源照亮環境？")]
    public bool enableDynamicLights = true;
    public Color lightColor = new Color(1f, 0.5f, 0f); // 預設橘色，請改成跟你的雷射一樣
    public float lightIntensity = 2.0f;
    public float lightRange = 2.0f;

    [Tooltip("注意：開啟陰影會很耗效能，建議維持 false")]
    public bool enableLightShadows = false;

    // 用來存放自動產生的燈光 (物件池)
    private List<Light> dynamicLights = new List<Light>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // 只有在開關開啟時，才計算並畫出雷射
        if (isLaserOn)
        {
            CalculateLaser();
        }
        else
        {
            // 如果開關被關閉了，把畫線的點數歸零，並關閉所有燈光
            lineRenderer.positionCount = 0;
            TurnOffAllLights();
        }
    }

    public void ToggleLaser()
    {
        isLaserOn = !isLaserOn;
    }

    private void CalculateLaser()
    {
        // === 原本的雷射計算邏輯完全保留 ===
        List<Vector3> laserPoints = new List<Vector3>();
        Vector3 currentPos = transform.position;
        Vector3 currentDir = transform.forward;

        laserPoints.Add(currentPos); // 第一個點：發射口
        float remainingDistance = maxDistance;

        for (int i = 0; i < maxBounces; i++)
        {
            if (Physics.Raycast(currentPos, currentDir, out RaycastHit hit, remainingDistance))
            {
                laserPoints.Add(hit.point); // 擊中點或折射點
                remainingDistance -= hit.distance;

                ILaserReceiver receiver = hit.collider.GetComponent<ILaserReceiver>();

                if (receiver != null)
                {
                    bool continueLaser = receiver.ProcessLaser(
                        hit.point, hit.normal, currentDir, hit.collider,
                        ref remainingDistance, laserPoints,
                        out currentPos, out currentDir
                    );

                    if (!continueLaser) break;
                }
                else
                {
                    break; // 打到一般牆壁，停止折射
                }
            }
            else
            {
                laserPoints.Add(currentPos + currentDir * remainingDistance);
                break;
            }

            if (remainingDistance <= 0) break;
        }

        lineRenderer.positionCount = laserPoints.Count;
        lineRenderer.SetPositions(laserPoints.ToArray());

        // === 【新增】：更新動態光源 ===
        UpdateDynamicLights(laserPoints);
    }

    // ==========================================
    // 【魔法核心】：自動管理點光源
    // ==========================================
    private void UpdateDynamicLights(List<Vector3> points)
    {
        if (!enableDynamicLights)
        {
            TurnOffAllLights();
            return;
        }

        int requiredLights = points.Count;

        // 如果目前的燈光數量不夠，就自動生出新的燈光來補足
        while (dynamicLights.Count < requiredLights)
        {
            GameObject lightObj = new GameObject("Auto_LaserLight_" + dynamicLights.Count);
            lightObj.transform.SetParent(this.transform); // 把燈光設為雷射發射器的子物件，保持場景乾淨

            Light newLight = lightObj.AddComponent<Light>();
            newLight.type = LightType.Point;
            dynamicLights.Add(newLight);
        }

        // 把燈光放到雷射的每一個轉折點/擊中點上
        for (int i = 0; i < dynamicLights.Count; i++)
        {
            if (i < requiredLights)
            {
                dynamicLights[i].enabled = true;
                dynamicLights[i].transform.position = points[i];
                dynamicLights[i].color = lightColor;
                dynamicLights[i].intensity = lightIntensity;
                dynamicLights[i].range = lightRange;
                dynamicLights[i].shadows = enableLightShadows ? LightShadows.Soft : LightShadows.None;
            }
            else
            {
                // 多出來的燈光先關掉備用
                dynamicLights[i].enabled = false;
            }
        }
    }

    private void TurnOffAllLights()
    {
        foreach (var light in dynamicLights)
        {
            if (light != null) light.enabled = false;
        }
    }
}
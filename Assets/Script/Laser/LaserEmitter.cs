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
            // 如果開關被關閉了，把畫線的點數歸零，雷射就會消失
            lineRenderer.positionCount = 0;
        }
    }

    // 【新增】：提供給按鈕呼叫的切換功能
    public void ToggleLaser()
    {
        isLaserOn = !isLaserOn;
    }

    private void CalculateLaser()
    {
        // 這裡面的邏輯完全不變！
        List<Vector3> laserPoints = new List<Vector3>();
        Vector3 currentPos = transform.position;
        Vector3 currentDir = transform.forward;

        laserPoints.Add(currentPos);
        float remainingDistance = maxDistance;

        for (int i = 0; i < maxBounces; i++)
        {
            if (Physics.Raycast(currentPos, currentDir, out RaycastHit hit, remainingDistance))
            {
                laserPoints.Add(hit.point);
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
                    break;
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
    }
}
using System.Collections.Generic;
using UnityEngine;

public class LaserGlass : MonoBehaviour, ILaserReceiver
{
    [Tooltip("玻璃的折射率 (空氣1.0，水1.33，玻璃1.5，鑽石2.4)")]
    public float refractionIndex = 1.5f;

    public bool ProcessLaser(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir, Collider hitCollider, ref float remainingDistance, List<Vector3> laserPoints, out Vector3 nextStartPoint, out Vector3 nextDirection)
    {
        // 1. 計算進入玻璃的折射方向
        Vector3 refractDir = CalculateRefraction(incomingDir, hitNormal, 1.0f, refractionIndex);

        if (refractDir == Vector3.zero)
        {
            // 發生全反射進不去，直接停止
            nextStartPoint = Vector3.zero; nextDirection = Vector3.zero; return false;
        }

        // 2. 找出口點 (完美解法)
        float boundsSize = hitCollider.bounds.size.magnitude * 2f; // 確保射線夠長
        Vector3 farPoint = hitPoint + refractDir * boundsSize;
        Ray backwardRay = new Ray(farPoint, -refractDir);

        // 【關鍵修復】：Collider.Raycast 只會偵測這塊玻璃，絕對不會被後面的牆壁或地板干擾！
        if (hitCollider.Raycast(backwardRay, out RaycastHit exitHit, boundsSize * 2f))
        {
            // 記錄內部光線的轉折點
            laserPoints.Add(exitHit.point);
            remainingDistance -= Vector3.Distance(hitPoint, exitHit.point);

            // 3. 計算穿出玻璃的折射方向 (記得法線要反轉 -exitHit.normal)
            nextDirection = CalculateRefraction(refractDir, -exitHit.normal, refractionIndex, 1.0f);

            if (nextDirection == Vector3.zero)
            {
                // 光線被困在內部 (全反射)
                nextStartPoint = Vector3.zero; return false;
            }

            nextStartPoint = exitHit.point + nextDirection * 0.001f;
            return true; // 成功穿出！
        }

        nextStartPoint = Vector3.zero; nextDirection = Vector3.zero;
        return false;
    }

    private Vector3 CalculateRefraction(Vector3 incident, Vector3 normal, float n1, float n2)
    {
        float n = n1 / n2;
        float cosI = -Vector3.Dot(normal, incident);
        float sinT2 = n * n * (1.0f - cosI * cosI);
        if (sinT2 > 1.0f) return Vector3.zero;
        float cosT = Mathf.Sqrt(1.0f - sinT2);
        return n * incident + (n * cosI - cosT) * normal;
    }
}
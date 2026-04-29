using System.Collections.Generic;
using UnityEngine;

// 實作 ILaserReceiver，代表這是一個雷射互動元件
public class LaserMirror : MonoBehaviour, ILaserReceiver
{
    public bool ProcessLaser(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir, Collider hitCollider, ref float remainingDistance, List<Vector3> laserPoints, out Vector3 nextStartPoint, out Vector3 nextDirection)
    {
        // 鏡子超簡單，就只是反射
        nextDirection = Vector3.Reflect(incomingDir, hitNormal);

        // 稍微推前一點避免下一條線打到自己
        nextStartPoint = hitPoint + nextDirection * 0.001f;

        return true; // 告訴槍：請繼續發射下一段光線
    }
}
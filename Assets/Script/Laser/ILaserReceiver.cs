using System.Collections.Generic;
using UnityEngine;

// 這是一張身分證。任何掛上實作此介面的物件，都能接收雷射光。
public interface ILaserReceiver
{
    /// <summary>
    /// 處理雷射光的行為
    /// </summary>
    /// <returns>回傳 true 代表光線繼續飛，回傳 false 代表光線被吸收停止</returns>
    bool ProcessLaser(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir, Collider hitCollider, ref float remainingDistance, List<Vector3> laserPoints, out Vector3 nextStartPoint, out Vector3 nextDirection);
}
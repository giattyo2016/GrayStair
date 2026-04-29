using System.Collections.Generic;
using UnityEngine;

public class LaserSensor : MonoBehaviour, ILaserReceiver
{
    [Header("連動設定")]
    [Tooltip("把所有你要控制的『門』拖曳到這個列表裡！")]
    public DoorController[] targetDoors;

    // 【新增】：一個用來裝橋樑的陣列！
    [Tooltip("把所有你要控制的『橋樑』拖曳到這個列表裡！")]
    public BridgeController[] targetBridges;

    private float lastHitTime = -1f;
    private float activeDuration = 0.1f;

    void Update()
    {
        bool isBeingHit = (Time.time - lastHitTime) <= activeDuration;

        foreach (DoorController door in targetDoors)
        {
            if (door != null) door.SetDoorState(isBeingHit);
        }

        // 【新增】：通知陣列裡所有的橋樑伸長或縮短！
        foreach (BridgeController bridge in targetBridges)
        {
            if (bridge != null) bridge.SetBridgeState(isBeingHit);
        }
    }

    public bool ProcessLaser(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir, Collider hitCollider, ref float remainingDistance, List<Vector3> laserPoints, out Vector3 nextStartPoint, out Vector3 nextDirection)
    {
        lastHitTime = Time.time;
        nextStartPoint = Vector3.zero;
        nextDirection = Vector3.zero;
        return false;
    }
}
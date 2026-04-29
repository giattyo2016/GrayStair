using System.Collections.Generic;
using UnityEngine;

// 1. 掛上這張「身分證」(實作 ILaserReceiver 介面)
public class DoloLaserReceiver : MonoBehaviour, ILaserReceiver
{
    private DoloAI doloBrain;

    void Start()
    {
        // 抓取 Dolo 的大腦 (考慮到你可能使用了父子物件法，所以用 GetComponentInParent 確保一定抓得到)
        doloBrain = GetComponentInParent<DoloAI>();
    }

    // 2. 實作介面規定的 ProcessLaser 方法
    public bool ProcessLaser(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir, Collider hitCollider, ref float remainingDistance, List<Vector3> laserPoints, out Vector3 nextStartPoint, out Vector3 nextDirection)
    {
        if (doloBrain != null)
        {
            // 【計算逃跑方向】：
            // 我們的 Dolo 大腦需要知道「雷射是從哪裡射過來的」才知道要往反方向逃。
            // 由於雷射光是朝著 incomingDir 飛過來的，我們只要把打擊點 (hitPoint) 往 incomingDir 的「反方向」推一段距離，
            // 就能完美假造出一個「發射源座標」，讓 Dolo 知道光是從背後還是前面來的！
            Vector3 fakeLaserSource = hitPoint - (incomingDir.normalized * 10f);

            doloBrain.ReactToLaser(fakeLaserSource);
        }

        // 因為 Dolo 是實體的怪物，肉體不會透光。
        // 所以光線打到牠身上就會被「吸收」並停止，因此這裡我們回傳 false。
        // out 參數因為不用繼續折射，隨便給個 Vector3.zero 即可。
        nextStartPoint = Vector3.zero;
        nextDirection = Vector3.zero;

        return false;
    }
}
using UnityEngine;

public class PlayerPushForce : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("玩家推擠物品的力量大小")]
    public float pushPower = 2.0f; // 你可以隨時在 Inspector 調整這個數值

    // 這是 Unity 的內建函式：當 Character Controller 移動時撞到東西，就會觸發
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 1. 取得被打到的東西的 Rigidbody (剛體)
        Rigidbody body = hit.collider.attachedRigidbody;

        // 2. 安全檢查：
        // 如果被打到的東西沒有剛體、或是它是運動學物件(不受力影響)、或是它是玩家自己
        if (body == null || body.isKinematic)
        {
            return; // 直接離開，不處理
        }

        // 3. 安全檢查：如果是被打到的東西在玩家腳下 (例如你踩在方塊上)，通常不推它，否則會發生奇怪的物理抖動
        if (hit.moveDirection.y < -0.3f)
        {
            return;
        }

        // 4. 計算推力的方向
        // 我們取玩家移動的方向 (`hit.moveDirection`)，只保留 X 和 Z 軸 (左右和前後)，不向上或向下推
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // 5. 計算最終推力 (Unity 6 物理推薦寫法)
        // 使用 hit.moveLength (玩家移動的速度) 加上我們的 pushPower 力量
        // 我們將推力除以物件的質量 (body.mass)。
        // 這樣輕的東西會飛出去，重的東西會慢慢移動，產生擬真的力量轉移感。
        Vector3 finalForce = pushDir * hit.moveLength * pushPower / body.mass;

        // 6. 將推力套用給物件的速度 (Unity 6 新語法 linearVelocity)
        // 我們用 += 的方式把力加進去，而不是覆蓋原本的速度
        body.linearVelocity += finalForce;
    }
}
using UnityEngine;

// 飛び道具（波動拳）の移動・判定を管理するスクリプト
public class Projectile : MonoBehaviour
{
    public float speed = 15f; // 飛び道具の移動速度
    public int damage = 40; // 飛び道具のダメージ量
    public FighterStats owner; // 飛び道具を撃ったキャラクターのステータス
    public float lifetime = 2f; // 生成されてから消滅するまでの時間
    
    private float direction; // 発射方向

    // 初期化および発射方向の設定処理
    public void Initialize(FighterStats ownerStats, float dir)
    {
        owner = ownerStats;
        direction = dir;
        Destroy(gameObject, lifetime); // lifetime経過後に自動で消滅させる
    }

    // 毎フレームの移動処理
    void Update()
    {
        // 飛び道具を指定方向に移動させる
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime, Space.World);
    }

    // 他のオブジェクトに接触した際の判定処理
    void OnTriggerEnter(Collider other)
    {
        // 相手キャラクターのFighterStatsを取得する
        FighterStats target = other.GetComponent<FighterStats>();
        if (target == null) target = other.GetComponentInParent<FighterStats>();
        
        // 自分自身（発射主）以外に当たった場合の処理
        if (target != null && target != owner)
        {
            if (!target.IsDead)
            {
                // ガード不能攻撃ではないためfalseを設定してダメージを与える
                target.TakeDamage(damage, transform.position, false);
                Destroy(gameObject); // ヒット後に飛び道具を消滅させる
            }
        }
    }
}

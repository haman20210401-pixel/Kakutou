using UnityEngine;

public class FighterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float attackCooldown = 0.5f;
    public HitBox hitBox;
    public Transform opponent;

    private float lastAttackTime = -999f;
    private FighterStats stats;

    void Start()
    {
        stats = GetComponent<FighterStats>();
    }

    void Update()
    {
        if (stats != null && stats.IsDead)
            return;

        Move();
        Attack();
        FaceOpponent();
    }

    void Move()
    {
        float move = 0f;

        if (CompareTag("Player"))
        {
            if (Input.GetKey(KeyCode.A)) move = -1f;
            if (Input.GetKey(KeyCode.D)) move = 1f;
        }
        else if (CompareTag("Enemy"))
        {
            if (Input.GetKey(KeyCode.LeftArrow)) move = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) move = 1f;
        }

        transform.Translate(Vector3.right * move * moveSpeed * Time.deltaTime, Space.World);
    }

    void Attack()
    {
        bool attackKey = false;

        if (CompareTag("Player"))
        {
            attackKey = Input.GetKeyDown(KeyCode.Space);
        }
        else if (CompareTag("Enemy"))
        {
            attackKey = Input.GetKeyDown(KeyCode.Return);
        }

        if (!attackKey) return;

        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (hitBox != null)
        {
            hitBox.ActivateHitBox(0.2f);
        }
    }

    void FaceOpponent()
    {
        if (opponent == null) return;

        Vector3 scale = transform.localScale;

        if (opponent.position.x > transform.position.x)
            scale.x = Mathf.Abs(scale.x);
        else
            scale.x = -Mathf.Abs(scale.x);

        transform.localScale = scale;
    }
}
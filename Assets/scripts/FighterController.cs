using System.Collections;
using UnityEngine;

public class FighterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float attackCooldown = 0.5f;
    public HitBox hitBox;
    public Transform opponent;

    private float lastAttackTime = -999f;
    private FighterStats stats;
    private AudioSource audioSource;

    private Vector3 originalScale;
    private bool isAttacking = false;

    void Start()
    {
        stats = GetComponent<FighterStats>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (stats != null && stats.IsDead)
            return;

        if (!GameManager.CanControl)
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

        if (!isAttacking)
        {
            StartCoroutine(AttackAnimation());
        }
    }

    IEnumerator AttackAnimation()
    {
        isAttacking = true;

        float signX = Mathf.Sign(transform.localScale.x);

        transform.localScale = new Vector3(
            1.2f * signX,
            originalScale.y * 0.9f,
            originalScale.z
        );

        yield return new WaitForSeconds(0.1f);

        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y,
            originalScale.z
        );

        isAttacking = false;
    }

    void FaceOpponent()
    {
        if (opponent == null) return;

        Vector3 scale = transform.localScale;

        float absOriginalX = Mathf.Abs(originalScale.x);

        if (opponent.position.x > transform.position.x)
            scale.x = absOriginalX;
        else
            scale.x = -absOriginalX;

        if (isAttacking)
        {
            scale.x *= 1.3f;
            scale.y = originalScale.y * 0.8f;
        }
        else
        {
            scale.y = originalScale.y;
        }

        transform.localScale = scale;
    }
}
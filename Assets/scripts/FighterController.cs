using System.Collections;
using UnityEngine;

public class FighterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float attackCooldown = 0.5f;
    public float jumpForce = 7f;
    public HitBox hitBox;
    public Transform opponent;
    public Rigidbody rb;

    public bool isCPU = false;
    public float attackRange = 1.5f;

    private float lastAttackTime = -999f;
    private FighterStats stats;
    private AudioSource audioSource;

    private Vector3 originalScale;
    private bool isAttacking = false;
    private bool isGrounded = true;

    void Start()
    {
        stats = GetComponent<FighterStats>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        if (stats != null && stats.IsDead)
            return;

        if (!GameManager.CanControl)
            return;

        if (isCPU)
        {
            CPUControl();
        }
        else
        {
            Move();
            Jump();
            Attack();
        }

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

    void Jump()
    {
        if (!isGrounded) return;
        if (rb == null) return;

        bool jumpKey = false;

        if (CompareTag("Player"))
        {
            jumpKey = Input.GetKeyDown(KeyCode.W);
        }
        else if (CompareTag("Enemy"))
        {
            jumpKey = Input.GetKeyDown(KeyCode.UpArrow);
        }

        if (jumpKey)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            isGrounded = false;
        }
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

        TryAttack();
    }

    void CPUControl()
    {
        if (opponent == null) return;

        float distance = Mathf.Abs(opponent.position.x - transform.position.x);

        if (distance > attackRange)
        {
            float dir = opponent.position.x > transform.position.x ? 1f : -1f;
            transform.Translate(Vector3.right * dir * moveSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
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
            Mathf.Sign(transform.localScale.x) * Mathf.Abs(originalScale.x),
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
            scale.x *= 1.2f;
            scale.y = originalScale.y * 0.9f;
        }
        else
        {
            scale.y = originalScale.y;
        }

        transform.localScale = scale;
    }

    private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
    {
        isGrounded = true;
    }
}
}
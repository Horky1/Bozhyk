using UnityEngine;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    public float speed = 2f;
    public float groundCheckDistance = 1f;
    public LayerMask groundLayer;
    public float checkOffsetX = 1f;
    public float chaseRange = 5f;
    public float stopChaseRange = 15f;
    public Transform player;
    public Animator animator;

    private Rigidbody2D rb;
    private bool movingRight;
    private bool chasing;
    private bool isDead = false;

    [SerializeField] private int health = 3;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 2f;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movingRight = transform.localScale.x > 0;
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer < chaseRange)
        {
            chasing = true;
            animator.SetBool("IsWalking", true);
        }
        else if (distanceToPlayer > stopChaseRange)
        {
            chasing = false;
            animator.SetBool("IsWalking", true);
        }

        float moveDir = movingRight ? 1f : -1f;

        if (chasing)
        {
            moveDir = player.position.x > transform.position.x ? 1f : -1f;

            if ((moveDir > 0 && !movingRight) || (moveDir < 0 && movingRight))
                Flip();

            if (distanceToPlayer < 2f)
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAttacking", true);
                rb.linearVelocity = Vector2.zero;

                DealDamage();
            }
            else
            {
                animator.SetBool("IsAttacking", false);
                rb.linearVelocity = new Vector2(moveDir * speed, rb.linearVelocity.y);
            }
        }
        else
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsAttacking", false);
            rb.linearVelocity = new Vector2(moveDir * speed, rb.linearVelocity.y);
        }

        Vector2 groundCheckPos = (Vector2)transform.position + new Vector2(moveDir * checkOffsetX, -0.5f);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawLine(groundCheckPos, groundCheckPos + Vector2.down * groundCheckDistance, Color.red);

        Vector2 wallCheckPos = (Vector2)transform.position + new Vector2(moveDir * checkOffsetX, 1f);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, Vector2.right * moveDir, 0.2f, groundLayer);
        Debug.DrawLine(wallCheckPos, wallCheckPos + Vector2.right * moveDir * 0.2f, Color.blue);

        if (!groundHit.collider || (!chasing && wallHit.collider))
        {
            Flip();
            return;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1f);
        foreach (var collider in colliders)
        {
            if (collider != null && collider.CompareTag("Enemy") && collider.gameObject != gameObject)
            {
                Flip();
                break;
            }
        }
    }

    void Flip()
    {
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"Enemy отримав {amount} шкоди від {Hero.Instance.name}. Залишилось HP: {health}");

        if (health <= 0)
            Die();
    }

    public void DealDamage()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance < 2.5f)
        {
            player.GetComponent<Hero>()?.TakeDamage(attackDamage);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Lava") && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        animator.SetTrigger("Die");

        animator.SetBool("IsWalking", false);
        animator.SetBool("IsAttacking", false);

        StartCoroutine(FreezeAfterDeath(1f));
    }

    private IEnumerator FreezeAfterDeath(float delay)
    {
        yield return new WaitForSeconds(delay);
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        rb.simulated = false;
    }
}

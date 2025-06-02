using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : MonoBehaviour
{
    private enum States
    {
        Idle,
        Run,
        Jump,
        Death,
        Attack
    }

    [SerializeField] private float speed = 3f;
    [SerializeField] private int lives = 5;
    [SerializeField] private float jumpForce = 6f; // Став стабільний jump force
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayers;

    private bool isGrounded = false;
    private bool isOnStairs = false;
    private bool isAttacking = false;
    private bool isDead = false;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;
    private Vector3 attackPointOriginalLocalPos;

    public static Hero Instance { get; set; }

    private States State
    {
        get => (States)anim.GetInteger("state");
        set => anim.SetInteger("state", (int)value);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        Instance = this;

        attackPointOriginalLocalPos = attackPoint.localPosition;
    }

    private void FixedUpdate()
    {
        if (!isDead)
            CheckGround();
    }

    private void Update()
    {
        if (isDead || isAttacking) return;

        if ((isGrounded || isOnStairs) && !Input.GetButton("Horizontal"))
            State = States.Idle;

        if (Input.GetButton("Horizontal"))
            Run();

        if ((isGrounded || isOnStairs) && Input.GetButtonDown("Jump"))
            Jump();

        if (Input.GetMouseButtonDown(0))
            StartCoroutine(Attack());
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        State = States.Attack;
        anim.speed = 1f;

        yield return new WaitForSeconds(0.2f);
        DealDamage();

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    private void DealDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyPatrol ep = enemy.GetComponent<EnemyPatrol>();
            if (ep != null)
            {
                ep.TakeDamage(1);
                Debug.Log($"Герой вдарив ворога {enemy.name}");
            }
        }
    }

    private void Run()
    {
        if (isGrounded || isOnStairs)
            State = States.Run;

        float dirX = Input.GetAxis("Horizontal");
        Vector3 dir = transform.right * dirX;
        transform.position = Vector3.MoveTowards(transform.position, transform.position + dir, speed * Time.deltaTime);
        sprite.flipX = dir.x < 0.0f;

        attackPoint.localPosition = new Vector3(
            sprite.flipX ? -Mathf.Abs(attackPointOriginalLocalPos.x) : Mathf.Abs(attackPointOriginalLocalPos.x),
            attackPointOriginalLocalPos.y,
            attackPointOriginalLocalPos.z
        );
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // Стрибок через зміну швидкості
    }

    private void CheckGround()
    {
        Collider2D[] collider = Physics2D.OverlapCircleAll(transform.position, 0.3f);
        isGrounded = collider.Length > 1 && !isOnStairs;

        if (!isGrounded && !isOnStairs)
            State = States.Jump;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        lives -= damage;
        Debug.Log("Герою нанесено урон. Залишилось HP: " + lives);

        if (lives <= 0)
            Die();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("Stairs"))
        {
            isOnStairs = true;
            rb.gravityScale = 0f;
        }
        else if (collision.CompareTag("Lava"))
        {
            Die();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Stairs"))
        {
            isOnStairs = false;
            rb.gravityScale = 1f;
        }
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        State = States.Death;
        anim.speed = 1f;

        StartCoroutine(FreezeOnLastFrame());
    }

    private IEnumerator FreezeOnLastFrame()
    {
        yield return new WaitForSeconds(1.65f);
        anim.speed = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}

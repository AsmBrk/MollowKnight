using UnityEngine;

public class Knight_sc : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 12f;
    public float dashSpeed = 15f;
    public float dashTime = 0.2f;
    public float attackRate = 2f; 
    private float nextAttackTime = 0f;
    public float attackRange = 1.05f;
    private bool isDead = false;
    
    public int maxHealth = 3;
    private int currentHealth;

    public LayerMask groundLayer;
    public Transform groundCheck;
    public LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Animator anim;

    private float horizonalInput;
    private bool isGrounded;
    private bool isDashing;
    private bool canDash = true;
    private float originalGravity;
    private bool isFacingRight = true;
    private int facingDirection = 1;

    [Header("Sound Effects")]
    public AudioClip jumpSfx;
    public AudioClip dashSfx;
    public AudioClip attackSfx;

    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        originalGravity = rb.gravityScale;
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isDead) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        if (isGrounded && !isDashing)
        {
            canDash = true;
        }

        if (isDashing) return;

        horizonalInput = Input.GetAxisRaw("Horizontal");

        anim.SetFloat("Speed", Mathf.Abs(horizonalInput));

        if (horizonalInput > 0 && !isFacingRight){
            Flip();
        }
        else if (horizonalInput < 0 && isFacingRight){
            Flip();
        }
        
        if (Input.GetKeyDown(KeyCode.Z) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            audioSource.PlayOneShot(jumpSfx);
        }

        if (Input.GetKeyDown(KeyCode.C) && canDash)
        {
            StartDash();
            canDash = false;
        }
        if(Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead || isDashing) return;

        rb.linearVelocity = new Vector2(horizonalInput * speed, rb.linearVelocity.y);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        facingDirection *= -1; 
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    void StartDash()
    {
        audioSource.PlayOneShot(dashSfx);
        isDashing = true;
        rb.gravityScale = 0f; 
        rb.linearVelocity = Vector2.zero; 
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
        Invoke(nameof(StopDash), dashTime);
    }

    void StopDash()
    {
        rb.gravityScale = originalGravity; 
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }

    // --- ÖNEMLİ DEĞİŞİKLİK: BURADAKİ OnTrigger SİLİNDİ ---
    // Hasar verme işini Enemy_sc içindeki kod yapıyor.
    // Buradaki eski kod çakışma yaratıyordu.

    // --- ÖNEMLİ DEĞİŞİKLİK: PUBLIC EKLENDİ ---
    // Artık Enemy bu fonksiyona erişebilir.
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Knight Hasar Aldı! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Knight öldü!");
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        Destroy(gameObject, 1f);
    }

    void Attack()
    {
        audioSource.PlayOneShot(attackSfx);   
        anim.SetTrigger("Attack");
        Debug.Log("Nail Savruldu!"); 

        Vector2 attackPos = new Vector2(transform.position.x + (facingDirection * 1f), transform.position.y);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPos, attackRange);

        foreach(Collider2D enemy in hitEnemies)
        {
            Enemy_sc enemyScript = enemy.GetComponent<Enemy_sc>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(1);
                Debug.Log(enemy.name + " isimli düşmana vurdum!");
            }
        }
        Debug.DrawLine(transform.position, attackPos, Color.red, 0.5f);
    }
}
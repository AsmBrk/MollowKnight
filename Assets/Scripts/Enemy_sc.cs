using UnityEngine;

public class Enemy_sc : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpForce = 10f;

    [Header("Combat")]
    public int maxHealth = 3;
    public int attackDamage = 1;

    private int currentHealth;

    private Rigidbody2D rb;

    private bool isAlive = true;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    public void Move(float direction)
    {
        if (!isAlive) 
            return;

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        Flip(direction);
    }

    public void StopMove()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    public void Jump()
    {
        if (!isAlive) 
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    public void Attack(GameObject player)
    {
        if (!isAlive) 
            return;

        if (player != null)
            player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive) 
            return;

        currentHealth -= damage;

        Debug.Log("Enemy can: " + currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        isAlive = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        Debug.Log("Enemy öldü");
        Destroy(gameObject, 2f);
    }

    void Flip(float direction)
    {
        if (direction > 0 && !facingRight)
        {
            facingRight = true;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (direction < 0 && facingRight)
        {
            facingRight = false;
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
    
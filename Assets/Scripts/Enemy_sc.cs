using UnityEngine;
using System.Collections.Generic;

public class Enemy_sc : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    
    [Header("AI Settings")]
    public bool trainMode = false; // EĞİTİM YAPARKEN TİKLE
    public bool loadAIFromDisk = true; // AI MODELINI YÜKLESİN
    public float decisionRate = 0.1f; // Daha hızlı karar verme (0.2 -> 0.1)

    [Header("Dash Ayarları")]
    public float dashCooldown = 1.5f; // Dash attıktan sonra 1.5 sn beklesin
    private float lastDashTime = -99f; // Sayacın başlangıcı

    // MENÜDEN GELEN GLOBAL DEĞİŞKENLER
    public static bool LoadAIFromDisk = true;  // Kaydedilen modeli otomatik yükle
    public static bool TrainMode = false; 

    private Rigidbody2D rb;
    private bool isAlive = true;
    private bool facingRight = true;
    private int maxHealth = 30;
    private int currentHealth;

    // Jump ve Dash
    private bool isGrounded = false;
    private bool isDashing = false;
    private bool canDash = true;
    private float originalGravity;
    public float jumpForce = 8f;
    public float dashSpeed = 12f;
    public float dashTime = 0.15f;
    public LayerMask groundLayer;

    // Referanslar
    private QLearningBrain qBrain;
    private Animator anim;
    
    private GameObject playerTarget;
    private float decisionTimer;

    // Q-Learning State takibi
    private List<float> lastInputs;
    private int lastAction = -1;
    private string lastStateKey;

    // --- YENİ EKLENEN DEĞİŞKEN (Sadece burada tanımlı olacak) ---
    private float prevDistance = 999f; 

    void Awake()
    {
        qBrain = GetComponent<QLearningBrain>();
        anim = GetComponent<Animator>();
        qBrain.exploration = 0.5f;
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // --- BU SATIRLARI EKLE (EŞİTLEME) ---
        // Menüden gelen emri (Static), yerel değişkene (Inspector) aktar.
        loadAIFromDisk = LoadAIFromDisk; 
        trainMode = TrainMode;
        // ------------------------------------

        currentHealth = maxHealth;
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        originalGravity = rb.gravityScale;

        // Başlangıç mesafesini ölçelim
        if(playerTarget != null)
            prevDistance = Vector2.Distance(transform.position, playerTarget.transform.position);

        if (qBrain != null)
        {
            RegisterActions();

            // AI modunun başlatılması
            // ARTIK BURASI DOĞRU ÇALIŞACAK ÇÜNKÜ EŞİTLEDİK
            if (loadAIFromDisk && !trainMode) 
            {
                bool success = qBrain.LoadModel();
                if (success)
                {
                    Debug.Log("✓ AI Modu: Ağırlık Dosyası Yüklendi - Akıllı Davranıyor");
                }
                else
                {
                    Debug.LogWarning("⚠ AI Dosyası Yüklenemedi - Rastgele Davranışa Dönüldü");
                }
            }
            else if (!trainMode)
            {
                Debug.Log("AI Modu: Dosya Seçilmedi - Rastgele Davranıyor");
            }

            if (trainMode)
            {
                Debug.Log("★ EĞİTİM MODU AÇIK - Aracı 'P' tuşu ile kaydetmek için press etmeyi unutmayınız!");
            }
        }
    }

    void Update()
    {
        if (!isAlive || qBrain == null || playerTarget == null) return;

        // Ground kontrol
        isGrounded = Physics2D.OverlapCircle(transform.position + Vector3.down * 0.5f, 0.2f, groundLayer);
        
        if (isGrounded && !isDashing)
        {
            canDash = true;
        }

        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0)
        {
            decisionTimer = decisionRate;
            
            // 1. Mevcut durumu algıla
            List<float> currentInputs = GetSensorInputs();
            qBrain.SetInputs(currentInputs);
            
            // State key oluştur 
            string currentStateKey = string.Join("_", currentInputs);

            // 2. Eğitim modundaysak ve önceki hareket varsa ÖDÜL VER
            if (trainMode && lastAction != -1)
            {
                float reward = CalculateReward(); // Yeni zeki ödül sistemi burada çalışıyor
                qBrain.UpdateQTable(lastStateKey, lastAction, reward, currentStateKey);
                Debug.Log($"EĞİTİM: State {lastStateKey} -> Action {lastAction} -> Reward {reward:F2}");
            }

            // 3. Karar Ver ve Uygula
            int actionIndex = qBrain.DecideAction();
            qBrain.ExecuteAction(actionIndex);

            // 4. Kayıt Tut
            lastInputs = currentInputs;
            lastStateKey = currentStateKey;
            lastAction = actionIndex;
        }

        // --- MANUEL KAYIT (KLAVYE 'P' TUŞU) ---
        if (trainMode && Input.GetKeyDown(KeyCode.P))
        {
            qBrain.SaveModel();
            Debug.Log("★★★ BEYİN KAYDEDİLDİ! EĞİTİM TAMAM - Dosya konumu kontrol et! ★★★");
        }
    }

    // --- SENSÖRLER (STATE) ---
    private List<float> GetSensorInputs()
    {
        List<float> inputs = new List<float>();

        if (playerTarget == null) return inputs;

        // 1. MESAFE (0.0 - 1.0 aralığında normalize)
        float dist = Vector2.Distance(transform.position, playerTarget.transform.position);
        float normalizedDist = Mathf.Clamp01(1.0f - (dist / 15.0f)); // 15 unit = max uzaklık
        inputs.Add(normalizedDist);

        // 2. X YÖNÜ (Player solda mı [-1], sağda mı [+1], ortada mı [0])
        float dx = playerTarget.transform.position.x - transform.position.x;
        float dirX = Mathf.Clamp(dx / 5.0f, -1.0f, 1.0f); // -1 ile 1 arasında
        inputs.Add(dirX);

        // 3. Y YÖNÜ (Player aşağıda mı [-1], yukarıda mı [+1])
        float dy = playerTarget.transform.position.y - transform.position.y;
        float dirY = Mathf.Clamp(dy / 5.0f, -1.0f, 1.0f);
        inputs.Add(dirY);

        // 4. KATEGORİK DURUM (çok yakın mı?)
        inputs.Add(dist < 1.5f ? 1.0f : 0.0f);

        return inputs;
    }

    // --- ÖDÜL SİSTEMİ (Geliştirilmiş) ---
    private float CalculateReward()
    {
        float currentDistance = Vector2.Distance(transform.position, playerTarget.transform.position);
        float reward = 0;

        // 1. Saldırı BAŞARISI (ÇOOK BÜYÜK ÖDÜL)
        if (currentDistance < 1.5f) 
        {
            reward = 20.0f; // Saldırı ödülü arttırıldı!
            if (trainMode) Debug.Log($"★★★ ATTACK BAŞARILI! Reward: {reward}");
        }
        // 2. Saldırı mesafesine yaklaşma (BONUS ÖDÜL)
        else if (currentDistance < 3.0f)
        {
            reward = 3.0f; // Saldırı aralığına girmek = ödül
            if (trainMode) Debug.Log($"★ Saldırı Aralığına Girildi! Reward: {reward}");
        }
        // 3. Normal yaklaşma mesafesine göre ödül
        else if (currentDistance < prevDistance)
        {
            float improvement = prevDistance - currentDistance;
            reward = Mathf.Clamp(improvement * 0.5f, 0.1f, 2.0f);
        }
        // 4. Uzaklaşma cezası
        else
        {
            reward = -2.0f; // Ceza arttırıldı (daha kesin saldırması için)
        }

        prevDistance = currentDistance;
        return reward;
    }

    // --- AKSİYONLAR ---
    private void RegisterActions()
    {
        qBrain.RegisterAction("Towards Player", (args) => MoveTowardsPlayer(), 0);
        qBrain.RegisterAction("Away From Player", (args) => MoveAwayFromPlayer(), 0);
        qBrain.RegisterAction("Jump", (args) => Jump(), 0);
        qBrain.RegisterAction("Dash", (args) => Dash(), 0);
        qBrain.RegisterAction("Stop", (args) => Move(0), 0);
        qBrain.RegisterAction("Attack", (args) => Attack(), 0);
    }

    public void Move(float dir)
    {
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(dir));
        if (dir != 0) Flip(dir);
    }

    private void MoveTowardsPlayer()
    {
        if (playerTarget == null) return;
        
        float dirX = playerTarget.transform.position.x - transform.position.x;
        float direction = dirX > 0 ? 4 : -4;
        Move(direction);
    }

    private void MoveAwayFromPlayer()
    {
        if (playerTarget == null) return;
        
        float dirX = playerTarget.transform.position.x - transform.position.x;
        float direction = dirX > 0 ? -4 : 4;
        Move(direction);
    }

    public void Jump()
    {
        Debug.Log("Jump çağrıldı. isGrounded: " + isGrounded);
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            if (anim != null) anim.SetTrigger("Jump");
            Debug.Log("✓ Enemy Jump!");
        }
    }

    public void Dash()
    {
        // KONTROLLER:
        // 1. canDash: Havada hakkı var mı? (Update içinde yere basınca true oluyor)
        // 2. !isDashing: Şu an zaten kayıyor mu?
        // 3. Time.time...: Cooldown süresi doldu mu? (Spam Engeli)
        if (canDash && !isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            lastDashTime = Time.time; // Saati kur, cooldown başlasın

            isDashing = true;
            canDash = false; // HAKKINI KULLANDI (Yere değene kadar bir daha atamaz)
            
            originalGravity = rb.gravityScale; // Yerçekimini kaydet
            rb.gravityScale = 0f; // Yerçekimini kapat (Havada düz gitmesi için)
            rb.linearVelocity = Vector2.zero; // Mevcut hızı sıfırla

            // Animasyon veya efekt yok, sadece Debug mesajı
            Debug.Log("✓ Enemy Dash Attı! (Cooldown Başladı)");
            
            // Yön belirle ve fırlat
            float dashDir = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0);
            
            // Süre bitince durdur
            Invoke(nameof(StopDash), dashTime);
        }
    }

    private void StopDash()
    {
        // Dash bitti, her şeyi normale döndür
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero; // Durdur ki kaymaya devam etmesin
        isDashing = false;
    }

    public void Attack()
    {
        if (playerTarget == null) return;
        
        float distance = Vector2.Distance(transform.position, playerTarget.transform.position);
        Debug.Log("Attack kontrol - Mesafe: " + distance.ToString("F2"));
        
        if (distance < 3.0f)
        {
            if (anim != null) anim.SetTrigger("Attack");
            Debug.Log("✓ Enemy Attack! Knight'a " + 1 + " hasar!");
            playerTarget.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
        }
    }

    void Flip(float dir)
    {
        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
        {
            facingRight = !facingRight;
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    // Hasar Alma Fonksiyonu
    private float lastDamageTime = -99f; 
    public void TakeDamage(int damage)
    {
        if (Time.time < lastDamageTime + 1.0f) return;

        lastDamageTime = Time.time;
        currentHealth -= damage;
        Debug.Log("Düşman Canı: " + currentHealth);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        Debug.Log("Düşman Öldü!");
        isAlive = false;
        gameObject.SetActive(false); 
    }

    // Trigger (Çarpışma ile hasar verme)
    private float touchDamageCooldown = 0f;
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time > touchDamageCooldown)
            {
                touchDamageCooldown = Time.time + 1.0f; 
                other.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
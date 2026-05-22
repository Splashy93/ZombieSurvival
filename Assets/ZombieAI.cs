using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackRate = 1.5f;
    public int attackDamage = 10;
    private float nextAttackTime;

    [Header("Audio Settings")]
    public AudioClip growlSound;
    public AudioClip deathSound;
    private AudioSource audioSource;
    private float nextGrowlTime;

    private Transform playerTarget;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    public bool isDead = false;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        
        // Zombi için ses motoru ekleme ve 3D Ses ayarları
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Sesi uzaktan yakından duyulacak şekilde 3D yapar
        audioSource.maxDistance = 20f; // 20 metre uzaktan duyulmaz
        
        // İlk hırlama sesi için rastgele zaman belirleme
        nextGrowlTime = Time.time + Random.Range(2f, 8f);
        
        // Sahnede 'Player' etiketli objeyi bul ve referansını al
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player etiketli obje sahnede bulunamadı!");
        }
    }

    void Update()
    {
        if (isDead) return; // Öldüyse hareket etmesini engelle

        // Zombi hayattayken rastgele zaman aralıklarında hırlama oynatır
        if (growlSound != null && Time.time >= nextGrowlTime)
        {
            audioSource.PlayOneShot(growlSound);
            nextGrowlTime = Time.time + Random.Range(5f, 15f); // Her 5-15 saniyede bir hırlar
        }

        if (playerTarget != null && navMeshAgent != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Oyuncuya saldırı mesafesindeyse
            if (distanceToPlayer <= attackRange)
            {
                navMeshAgent.isStopped = true; // Dur

                if (Time.time >= nextAttackTime)
                {
                    AttackPlayer();
                }
            }
            else // Değilse oyuncuya git
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(playerTarget.position);
                
                // Uzaklaştığında saldırı animasyonunda kalmasını/tekrarlamasını engelliyoruz
                animator.ResetTrigger("Attack"); 
                
                // Eğer Z_Attack animasyonunda takılı kalıyorsa zorla Z_Walk animasyonuna döndür
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Z_Attack") && stateInfo.normalizedTime >= 0.8f)
                {
                    animator.Play("Z_Walk");
                }
                else if (!stateInfo.IsName("Z_Attack") && !stateInfo.IsName("Z_Walk") && !stateInfo.IsName("Z_FallingBack"))
                {
                     animator.Play("Z_Walk");
                }
            }
        }
    }

    private void AttackPlayer()
    {
        animator.SetTrigger("Attack"); // Z_Attack animasyonu için Animator'da Attack trigger'ı açın
        nextAttackTime = Time.time + attackRate;

        // Burada oyuncuya hasar verme kodu çağrılabilir
        // playerTarget.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
    }

    public void TakeDamage(int damage, bool isHeadshot, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " hasar aldı! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false; // Yerdeyken kaymayı önle
        }
        
        // Ölüm sesini çal
        if (deathSound != null)
        {
            // PlayClipAtPoint kullanıyoruz çünkü objeyi 3 saniye sonra sileceğiz, ses de silinmesin.
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        
        // Ölü zombiye vurulunca kan çıkmaması ve hasar almaması için üzerindeki tüm Collider'ları (Kafa, gövde vb.) kapatıyoruz
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }
        
        animator.SetTrigger("Die"); // Ölme animasyonu (Z_FallingBack vs)

        // 3 saniye sonra cesedi yok et
        Destroy(gameObject, 3f);
    }
}

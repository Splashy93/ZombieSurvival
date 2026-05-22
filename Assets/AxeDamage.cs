using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxeDamage : MonoBehaviour
{
    [Header("Combat Settings")]
    public int damage = 35; // 3 vuruşta öldürmesi için (100 / 35 = 3 vuruş)

    [Header("Effects & Audio")]
    public GameObject destructionEffectPrefab;
    public AudioClip axeCutSound;

    private PlayerController player;
    private List<Collider> hitEnemiesThisSwing = new List<Collider>(); // Aynı zombiye bir vuruşta 2 kere hasar vermemek için

    void Start()
    {
        // Baltanın bağlı olduğu ana objedeki (Player) kontrolcüyü bul
        player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        // Eğer karakter atak modunda değilse hitEnemies listesini temizle (yeni vuruş için hazırla)
        if (player != null && !player.isAttacking)
        {
            hitEnemiesThisSwing.Clear();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. ÖNEMLİ KONTROL: Karakter animasyonla (sol tıkla) saldırı halinde mi? Değilse balta hasar VERMEZ!
        if (player != null && !player.isAttacking)
        {
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("Head"))
        {
            // 2. ÖNEMLİ KONTROL: Bu sağdan sola aynı savuruş esnasında bu zombiye zaten vurduksa tekrar vurma!
            if (hitEnemiesThisSwing.Contains(other)) return;
            
            ZombieAI zombie = other.GetComponentInParent<ZombieAI>();
            
            // Eğer zombi var ve henüz ölmemişse işlemi yap
            if (zombie != null && !zombie.isDead)
            {
                hitEnemiesThisSwing.Add(other); // Vurduklarımız listesine kaydet

                // Kesme sesini çal (Düşmanın üstünde çalar 3D olarak)
                if (axeCutSound != null)
                {
                    AudioSource.PlayClipAtPoint(axeCutSound, zombie.transform.position);
                }

                if (destructionEffectPrefab != null)
                {
                    // Kanı ClosestPoint ile değil zombinin orta-göğüs hizasında çıkartıyoruz ki daha stabil dursun
                    Vector3 fxPos = zombie.transform.position + Vector3.up * 1.3f;
                    GameObject fx = Instantiate(destructionEffectPrefab, fxPos, Quaternion.identity);
                    
                    // Kanın zombiyle beraber ilerlemesini istersen aşağıdaki satırı aktif edebilirsin (Opsiyonel)
                    // fx.transform.SetParent(zombie.transform);

                    Destroy(fx, 1.5f);
                }

                bool isHeadshot = other.CompareTag("Head");
                Vector3 hitDirection = (other.transform.position - transform.position).normalized;
                
                zombie.TakeDamage(damage, isHeadshot, hitDirection);
            }
        }
    }
}

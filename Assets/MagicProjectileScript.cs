using UnityEngine;

public class MagicProjectileScript : MonoBehaviour
{
    public int damage = 40;
    private Rigidbody rb;
    private bool isDestroyed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb != null && !isDestroyed)
        {
            // İleriye doğru kalıca bir küre taraması yap (merminin içinden geçmesini %100 engeller)
            float moveDistance = rb.linearVelocity.magnitude * Time.deltaTime;
            
            // Eğer objede Collider yoksa bile bu spherecast çalışacaktır (0.3f yarıçapında kalın bir lazer atıyor)
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.4f, rb.linearVelocity.normalized, moveDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject != this.gameObject)
                {
                    HitTarget(hit.collider.gameObject);
                    if (isDestroyed) break; // Eğer zaten yok edildiyse döngüden çık
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDestroyed && other.gameObject != this.gameObject)
        {
            HitTarget(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDestroyed && collision.gameObject != this.gameObject)
        {
            HitTarget(collision.gameObject);
        }
    }

    private void HitTarget(GameObject go)
    {
        // Kendi karakterimize çarpmaması için kontroller
        if (go.CompareTag("Player") || go.transform.root.CompareTag("Player")) return;
        if (go.CompareTag("Magic")) return; // Başka büyülere çarparsa yoksay

        // Zombie'ye çarpma denetimi
        if (go.CompareTag("Enemy") || go.CompareTag("Head"))
        {
            ZombieAI zombie = go.GetComponentInParent<ZombieAI>();
            if (zombie != null && !zombie.isDead)
            {
                Vector3 hitDirection = rb != null ? rb.linearVelocity.normalized : transform.forward;
                zombie.TakeDamage(damage, false, hitDirection);
                
                isDestroyed = true;
                Destroy(gameObject);
                return;
            }
        }
        
        // Zemin veya duvara da çarptıysa yok olsun, ama eğer sadece zombilere odaklansın dersen 
        // buradaki Destroy kodunu kaldırabilirsin. Şu an zemin/duvar vs neye değse yok olacak:
        isDestroyed = true;
        Destroy(gameObject);
    }
}

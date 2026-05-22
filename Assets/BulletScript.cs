using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public int damage = 25;
    public GameObject bloodPrefab;

    void OnCollisionEnter(Collision collision)
    {
        // Oyuncuya çarparsa görmezden gel
        if (collision.collider.CompareTag("Player")) return;

        if (collision.collider.CompareTag("Enemy") || collision.collider.CompareTag("Head"))
        {
            ZombieAI zombie = collision.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null && !zombie.isDead)
            {
                if (bloodPrefab != null)
                {
                    Instantiate(bloodPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
                }
                
                bool isHeadshot = collision.collider.CompareTag("Head");
                zombie.TakeDamage(damage, isHeadshot, transform.forward);
            }
        }
        
        // Mermiyi yok et
        Destroy(gameObject);
    }
}
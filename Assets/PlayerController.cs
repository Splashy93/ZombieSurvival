using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 2f; // Fare hassasiyeti için kullanılacak
    
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Vector3 cameraOffset = new Vector3(0.5f, 0.5f, -2.5f); // Daha yakın ve sağ omuz üstü
    public float cameraFollowSpeed = 10f;
    public float minPitch = -40f; // Kameranın aşağı bakma sınırı
    public float maxPitch = 60f;  // Kameranın yukarı bakma sınırı
    private float cameraPitch = 0f;
    
    [Header("Raycast & Effects")]
    public GameObject hitEffectPrefab;
    public float raycastDistance = 100f;
    public int shootDamage = 35;

    [Header("Magic Settings")]
    public GameObject magicPrefab;
    public Transform magicSpawnPoint;
    public float magicSpeed = 20f;
    public float magicCooldown = 1f;
    private float nextMagicTime = 0f;
    
    [Header("Melee Settings")]
    public float meleeRange = 2.5f;
    public int meleeDamage = 35;
    public float attackCooldown = 1.0f; // Attack spamlama engeli
    public AudioClip axeCutSound; // Balta et kesme sesi
    private float nextAttackTime = 0f;
    
    [HideInInspector] public bool isAttacking = false;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        // Fare imlecini kilitle ve gizle (TPS deneyimi için)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleInputs();
        HandleAttack();
        HandleRaycast();
        HandleMagic();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void LateUpdate()
    {
        CameraFollow();
    }

    private void HandleInputs()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Karakterin her zaman kendi baktığı yöne (forward) ve yanlara (right) göre hareket etmesi
        moveDirection = (transform.forward * v + transform.right * h).normalized;

        animator.SetFloat("Speed", moveDirection.magnitude * moveSpeed);
    }

    private void MovePlayer()
    {
        // Farenin sağa sola hareketi ile karakteri (Y ekseninde) döndür
        float mouseX = Input.GetAxis("Mouse X") * turnSpeed;
        Quaternion yRotation = Quaternion.Euler(0f, mouseX, 0f);
        rb.MoveRotation(rb.rotation * yRotation);

        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }

    private void HandleAttack()
    {
        // Sol tık (Left Click) ile Attack tetikleme
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            animator.SetTrigger("Attack");
            
            StartCoroutine(AttackStateRoutine());
        }
    }

    private IEnumerator AttackStateRoutine()
    {
        // Baltayı kaldırırken bekle. (Koşarkenki animasyon geçişi (blend) gecikmesini kapatmak için süre kısaltıldı)
        yield return new WaitForSeconds(0.1f);
        
        // Atak modunu aç (Baltanın AxeDamage scripti artık hasar vermeye başlayacak)
        isAttacking = true;

        // Vuruşun aktif kalacağı süre (Örn: Koşarken geçişleri de kapsasın diye süre uzatıldı)
        yield return new WaitForSeconds(0.7f);
        
        // Atak modunu kapat (Artık balta boşta değdiğinde hasar vermeyecek)
        isAttacking = false;
    }

    private void HandleRaycast()
    {
        // Sağ tık (Right Click) ile ekranın merkezinden raycast
        if (Input.GetMouseButtonDown(1) && cameraTransform != null)
        {
            Camera cam = cameraTransform.GetComponent<Camera>();
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                
                if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
                {
                    if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Head"))
                    {
                        ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                        
                        // Sadece zombi hayattaysa kan efekti oyna ve hasar ver
                        if (zombie != null && !zombie.isDead)
                        {
                            if (hitEffectPrefab != null)
                            {
                                GameObject decal = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                                Destroy(decal, 1.5f); // 1.5 saniye sonra yok et
                            }

                            bool isHeadshot = hit.collider.CompareTag("Head");
                            Vector3 hitDirection = ray.direction;
                            zombie.TakeDamage(shootDamage, isHeadshot, hitDirection);
                        }
                    }
                    else // Düşman değilse (örn. duvara ateş edildiyse)
                    {
                        if (hitEffectPrefab != null)
                        {
                            GameObject decal = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                            Destroy(decal, 1.5f);
                        }
                    }
                }
            }
        }
    }

    private void HandleMagic()
    {
        // 2 tuşu ile büyü atma (sol veya sağ tık ile karışmaması için eklendi)
        if (Input.GetKeyDown(KeyCode.Alpha2) && Time.time >= nextMagicTime)
        {
            if (magicPrefab != null)
            {
                nextMagicTime = Time.time + magicCooldown;

                // Eğer magicSpawnPoint (Karakterin eli) atanmışsa oradan, atanmamışsa karakterin hemen önünden at.
                Vector3 spawnPos = magicSpawnPoint != null ? magicSpawnPoint.position : transform.position + transform.forward * 1.0f + Vector3.up * 1.5f;

                // Atılacak yön direkt kameranın baktığı ileri yön
                Vector3 shootDirection = (cameraTransform != null) ? cameraTransform.forward : transform.forward;

                GameObject magic = Instantiate(magicPrefab, spawnPos, Quaternion.LookRotation(shootDirection));
                
                // Kürenin devasa olmasını engellemek için boyutunu sabitliyoruz.
                magic.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                Rigidbody rbMagic = magic.GetComponent<Rigidbody>();
                if (rbMagic != null)
                {
                    rbMagic.useGravity = false;
                    rbMagic.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rbMagic.linearVelocity = shootDirection * magicSpeed;
                }

                // animator.SetTrigger("Attack"); // Büyü atma hissiyatı için aynı saldırı animasyonunu oynatabilirsiniz. (Kapatıldı)
                Destroy(magic, 5f); // 5 Saniye sonra sil (haritadan dışarı çıkarsa diye)
            }
        }
    }

    private void CameraFollow()
    {
        if (cameraTransform != null)
        {
            // Farenin Yukarı / Aşağı hareketini al
            float mouseY = Input.GetAxis("Mouse Y") * turnSpeed;
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch); // Sınırla

            // Omuz hizası için bir pivot (merkez) belirliyoruz
            Vector3 pivot = transform.position + Vector3.up * 1.5f;

            // Karakterin Y eksenindeki dönüşü ile kameranın X (UpDown) eksenindeki dönüşünü birleştiriyoruz
            Quaternion camRot = transform.rotation * Quaternion.Euler(cameraPitch, 0f, 0f);

            // Yeni kameranın hedef pozisyonunu hesapla
            Vector3 targetCamPos = pivot + camRot * cameraOffset;
            
            // Kamera pozisyonu ve açısını uygula
            cameraTransform.position = targetCamPos;
            cameraTransform.rotation = camRot;
        }
    }

    // Basit bir Crosshair çizen fonksiyon
    void OnGUI()
    {
        // Ekranın tam ortasına "+" işareti koyan basit arayüz çizimi
        float crosshairSize = 20f;
        float x = (Screen.width - crosshairSize) / 2f;
        float y = (Screen.height - crosshairSize) / 2f;

        GUI.color = Color.white;
        // Yatay çizgi
        GUI.DrawTexture(new Rect(x, y + crosshairSize / 2f - 1f, crosshairSize, 2f), Texture2D.whiteTexture);
        // Dikey çizgi
        GUI.DrawTexture(new Rect(x + crosshairSize / 2f - 1f, y, 2f, crosshairSize), Texture2D.whiteTexture);
    }
}

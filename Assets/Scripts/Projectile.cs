using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        // Destroy self after lifetime (default 5 seconds)
        Destroy(gameObject, lifetime);

        // If we have a Rigidbody, use physics for movement (smoother collision)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false; // Bullets usually fly straight
            rb.linearVelocity = transform.forward * speed;
        }
    }

    private void Update()
    {
        // Only manually move if NO Rigidbody
        if (GetComponent<Rigidbody>() == null)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Add collision logic here later (e.g., damage enemy)
        // Destroy(gameObject); 
    }
}

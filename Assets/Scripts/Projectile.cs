using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Add collision logic here later (e.g., damage enemy)
        // Destroy(gameObject); 
    }
}

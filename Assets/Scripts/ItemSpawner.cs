using System.Collections;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Drag the Ground object (with a Collider) here to define the spawn area.")]
    [SerializeField] private Collider spawnArea;
    
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxItems = 10;

    [Header("Positioning")]
    [SerializeField] private float yOffset = 0.5f; // Lift item slightly above ground

    private int _currentItemCount;

    private void Start()
    {
        if (spawnArea == null)
        {
            Debug.LogError("ItemSpawner: No Spawn Area (Ground) assigned!");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("ItemSpawner: No Item Prefab assigned!");
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (_currentItemCount < maxItems)
            {
                SpawnItem();
            }
        }
    }

    private void SpawnItem()
    {
        Vector3 randomPosition = GetRandomPosition();
        
        // Instantiate item and keep hierarchy clean by parenting to this spawner (optional)
        GameObject parsedItem = Instantiate(itemPrefab, randomPosition, Quaternion.identity);
        parsedItem.transform.SetParent(transform);
        
        _currentItemCount++;
    }

    private Vector3 GetRandomPosition()
    {
        Bounds bounds = spawnArea.bounds;

        // Generate random X and Z within bounds
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        // Keep Y fixed to the ground height + offset
        // We use bounds.max.y to be safe in case the pivot is weird, 
        // or bounds.center.y if it's a flat plane. 
        // A safer bet for top-of-surface on a box is usually max.y
        float y = bounds.max.y + yOffset;

        return new Vector3(x, y, z);
    }
}

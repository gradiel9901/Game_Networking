using UnityEngine;

public class ItemVisuals : MonoBehaviour
{
    [Header("Spin Settings")]
    [SerializeField] private float rotationSpeed = 90f; // Degrees per second

    [Header("Sparkle Settings")]
    [SerializeField] private Color sparkleColor = new Color(1f, 0.84f, 0f, 1f); // Gold
    [SerializeField] private int maxParticles = 20;

    private bool _isPickedUp = false;
    private ParticleSystem _particleSystem;

    private void Start()
    {
        CreateSparkleEffect();
    }

    private void Update()
    {
        if (_isPickedUp) return;

        // Spin the item
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Call this when the player picks up the item.
    /// Stops rotation and particle emission.
    /// </summary>
    public void OnPickedUp()
    {
        _isPickedUp = true;

        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void CreateSparkleEffect()
    {
        // check if a particle system already exists (e.g. if added manually)
        if (GetComponentInChildren<ParticleSystem>() != null) return;

        // Create a new GameObject for the particles
        GameObject sparkleObj = new GameObject("GoldenSparkles");
        sparkleObj.transform.SetParent(transform, false);
        sparkleObj.transform.localPosition = Vector3.zero;

        // Add Particle System
        ParticleSystem ps = sparkleObj.AddComponent<ParticleSystem>();
        _particleSystem = ps;
        
        // --- Configure Main Module ---
        var main = ps.main;
        main.startLifetime = 1.0f;
        main.startSpeed = 0.5f;
        main.startSize = 0.2f;
        main.startColor = sparkleColor;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Leave trails in world space

        // --- Configure Emission ---
        var emission = ps.emission;
        emission.rateOverTime = 10f; // 10 particles per second

        // --- Configure Shape ---
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f; // Radius of sparkle emission

        // --- Configure Renderer ---
        var renderer = sparkleObj.GetComponent<ParticleSystemRenderer>();
        
        // Try to load a standard particle shader
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default"); // Fallback
        
        if (shader != null)
        {
             Material defaultMat = new Material(shader);
             renderer.material = defaultMat;
        }

        renderer.minParticleSize = 0.05f;
        renderer.maxParticleSize = 0.5f;
    }
}

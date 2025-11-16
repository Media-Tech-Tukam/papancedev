using UnityEngine;

public class SimpleCollisionTest : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔥 PLAYER COLLISION with {collision.gameObject.name}");
        
        // Buscar ObstacleCollision en el objeto que tocamos
        ObstacleCollision obstacle = collision.gameObject.GetComponent<ObstacleCollision>();
        if (obstacle != null)
        {
            Debug.Log($"💥 Activating obstacle effect: {obstacle.effectType}");
            obstacle.HandlePlayerCollision(gameObject);
        }
        else
        {
            Debug.LogWarning("⚠️ Collided object has no ObstacleCollision component!");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"⚡ PLAYER TRIGGER with {other.gameObject.name}");
    }
    
    void Start()
    {
        // Verificar configuración del jugador
        Debug.Log("=== PLAYER COLLISION TEST ===");
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("❌ Player needs Rigidbody! Set useRigidbody=true in ImprovedSplineFollower");
        }
        else
        {
            Debug.Log($"✅ Rigidbody: gravity={rb.useGravity}, frozen={rb.freezeRotation}");
        }
        
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("❌ Player needs Collider!");
        }
        else
        {
            Debug.Log($"✅ Collider: {col.GetType().Name}, trigger={col.isTrigger}");
        }
        
        if (!CompareTag("Player"))
        {
            Debug.LogError($"❌ Player tag is '{tag}', should be 'Player'");
        }
        else
        {
            Debug.Log("✅ Player tag correct");
        }
    }
}
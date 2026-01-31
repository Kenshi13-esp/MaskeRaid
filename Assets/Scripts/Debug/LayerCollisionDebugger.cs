using UnityEngine;

public class LayerCollisionDebugger : MonoBehaviour
{
    private void Start()
    {
        int bossLayer = LayerMask.NameToLayer("Boss");
        int playerLayer = LayerMask.NameToLayer("Player");
        
        Debug.Log($"=== LAYER COLLISION DEBUG ===");
        Debug.Log($"Boss Layer Index: {bossLayer}");
        Debug.Log($"Player Layer Index: {playerLayer}");
        
        bool canCollide = !Physics2D.GetIgnoreLayerCollision(bossLayer, playerLayer);
        Debug.Log($"Boss <-> Player Collision Enabled: {canCollide}");
        
        GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (bossObj != null)
        {
            Debug.Log($"Boss GameObject found: {bossObj.name}, Layer: {LayerMask.LayerToName(bossObj.layer)}");
            
            BoxCollider2D[] bossColliders = bossObj.GetComponents<BoxCollider2D>();
            Debug.Log($"Boss has {bossColliders.Length} BoxCollider2D components");
            
            foreach (var col in bossColliders)
            {
                Debug.Log($"  - Collider: isTrigger={col.isTrigger}, enabled={col.enabled}, offset={col.offset}, size={col.size}");
            }
            
            Rigidbody2D bossRb = bossObj.GetComponent<Rigidbody2D>();
            if (bossRb != null)
            {
                Debug.Log($"Boss Rigidbody2D: bodyType={bossRb.bodyType}, simulated={bossRb.simulated}");
            }
        }
        else
        {
            Debug.LogWarning("Boss GameObject NOT FOUND!");
        }
        
        if (playerObj != null)
        {
            Debug.Log($"Player GameObject found: {playerObj.name}, Layer: {LayerMask.LayerToName(playerObj.layer)}");
            
            BoxCollider2D playerCol = playerObj.GetComponent<BoxCollider2D>();
            if (playerCol != null)
            {
                Debug.Log($"Player Collider: isTrigger={playerCol.isTrigger}, enabled={playerCol.enabled}");
            }
            
            Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Debug.Log($"Player Rigidbody2D: bodyType={playerRb.bodyType}, simulated={playerRb.simulated}");
            }
        }
        else
        {
            Debug.LogWarning("Player GameObject NOT FOUND!");
        }
        
        Debug.Log($"=== END LAYER COLLISION DEBUG ===");
    }
}

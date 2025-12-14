using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Places instruments on real-world surfaces detected by Quest's Scene API
/// Falls back to floor plane if Scene API not available
/// </summary>
public class ARInstrumentPlacer : MonoBehaviour
{
    [Header("Orchestra Reference")]
    public Transform orchestra;              // Parent object containing all instruments
    
    [Header("Player Reference")]
    public Transform playerHead;             // CenterEyeAnchor
    
    [Header("Placement Settings")]
    public float distanceFromPlayer = 2.5f;  // How far in front of player to place
    public float maxPlacementHeight = 0.3f;  // Max height for floor surfaces
    public bool useSceneAnchors = true;      // Try to use Quest Scene API
    public bool autoPlaceOnStart = false;    // Auto-place when game starts
    
    [Header("Fallback Settings")]
    public float fallbackFloorHeight = 0f;   // Floor height if no surface found
    
    [Header("Debug")]
    public bool showDebugVisuals = false;
    
    private bool isPlaced = false;
    private OVRCameraRig cameraRig;
    private List<OVRSceneAnchor> floorAnchors = new List<OVRSceneAnchor>();
    
    void Start()
    {
        // Find references
        cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig != null && playerHead == null)
            playerHead = cameraRig.centerEyeAnchor;
        
        if (orchestra == null)
            orchestra = GameObject.Find("Orchestra")?.transform;
        
        // Subscribe to scene loading
        if (useSceneAnchors)
        {
            StartCoroutine(WaitForSceneAnchors());
        }
        
        if (autoPlaceOnStart)
        {
            StartCoroutine(DelayedAutoPlace());
        }
    }
    
    IEnumerator DelayedAutoPlace()
    {
        // Wait a moment for scene to load
        yield return new WaitForSeconds(1f);
        PlaceOrchestra();
    }
    
    IEnumerator WaitForSceneAnchors()
    {
        // Wait for OVRSceneManager to load scene
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Find floor anchors
            OVRSceneAnchor[] anchors = FindObjectsOfType<OVRSceneAnchor>();
            floorAnchors.Clear();
            
            foreach (var anchor in anchors)
            {
                // Check if this is a floor or floor-like surface
                var classification = anchor.GetComponent<OVRSemanticClassification>();
                if (classification != null)
                {
                    if (classification.Contains(OVRSceneManager.Classification.Floor) ||
                        classification.Contains(OVRSceneManager.Classification.Table) ||
                        classification.Contains(OVRSceneManager.Classification.Other))
                    {
                        // Only consider surfaces below certain height as potential floors
                        if (anchor.transform.position.y < maxPlacementHeight)
                        {
                            floorAnchors.Add(anchor);
                        }
                    }
                }
            }
            
            if (floorAnchors.Count > 0)
            {
                Debug.Log($"[ARInstrumentPlacer] Found {floorAnchors.Count} floor anchors");
                break;
            }
            
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        if (floorAnchors.Count == 0)
        {
            Debug.Log("[ARInstrumentPlacer] No floor anchors found, will use fallback");
        }
    }
    
    /// <summary>
    /// Place orchestra in front of player on detected surface or fallback floor
    /// </summary>
    public void PlaceOrchestra()
    {
        if (orchestra == null || playerHead == null)
        {
            Debug.LogWarning("[ARInstrumentPlacer] Missing references");
            return;
        }
        
        // Calculate position in front of player
        Vector3 forward = playerHead.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 targetPosition = playerHead.position + forward * distanceFromPlayer;
        
        // Find floor height at target position
        float floorY = FindFloorHeight(targetPosition);
        targetPosition.y = floorY;
        
        // Position orchestra
        orchestra.position = targetPosition;
        
        // Face toward player
        Vector3 lookDir = playerHead.position - targetPosition;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            orchestra.rotation = Quaternion.LookRotation(lookDir);
        
        isPlaced = true;
        
        Debug.Log($"[ARInstrumentPlacer] Placed orchestra at {targetPosition}, floor height: {floorY}");
    }
    
    float FindFloorHeight(Vector3 position)
    {
        // Try scene anchors first
        if (useSceneAnchors && floorAnchors.Count > 0)
        {
            float closestDist = float.MaxValue;
            float closestY = fallbackFloorHeight;
            
            foreach (var anchor in floorAnchors)
            {
                // Check horizontal distance
                Vector3 anchorPos = anchor.transform.position;
                float dist = Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(anchorPos.x, anchorPos.z)
                );
                
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestY = anchorPos.y;
                }
            }
            
            // If we found a reasonably close floor anchor, use it
            if (closestDist < 3f)
            {
                return closestY;
            }
        }
        
        // Try raycast to find floor
        RaycastHit hit;
        Vector3 rayStart = position + Vector3.up * 2f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 5f))
        {
            // Check if hit something reasonable
            if (hit.point.y < maxPlacementHeight)
            {
                return hit.point.y;
            }
        }
        
        // Fallback to configured floor height
        return fallbackFloorHeight;
    }
    
    /// <summary>
    /// Call this when player presses trigger to start - places orchestra and anchors it
    /// </summary>
    public void AnchorOrchestra()
    {
        PlaceOrchestra();
    }
    
    /// <summary>
    /// Reset placement state
    /// </summary>
    public void ResetPlacement()
    {
        isPlaced = false;
    }
    
    public bool IsPlaced => isPlaced;
    
    void OnDrawGizmos()
    {
        if (!showDebugVisuals) return;
        
        // Draw where orchestra would be placed
        if (playerHead != null)
        {
            Vector3 forward = playerHead.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 targetPos = playerHead.position + forward * distanceFromPlayer;
            targetPos.y = fallbackFloorHeight;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
            Gizmos.DrawLine(playerHead.position, targetPos);
        }
    }
}

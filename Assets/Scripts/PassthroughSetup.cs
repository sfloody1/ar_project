using UnityEngine;

/// <summary>
/// Sets up Meta Quest Passthrough for AR experience
/// Enables passthrough so user sees real world with virtual objects overlaid
/// </summary>
public class PassthroughSetup : MonoBehaviour
{
    [Header("Settings")]
    public bool enableOnStart = true;
    [Range(0f, 1f)]
    public float passthroughOpacity = 1f;
    
    private OVRPassthroughLayer passthroughLayer;
    private OVRCameraRig cameraRig;
    
    void Start()
    {
        if (enableOnStart)
        {
            EnablePassthrough();
        }
    }
    
    public void EnablePassthrough()
    {
        // Find or create OVRPassthroughLayer
        cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig == null)
        {
            Debug.LogWarning("[PassthroughSetup] No OVRCameraRig found");
            return;
        }
        
        // Check for existing passthrough layer
        passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
        
        if (passthroughLayer == null)
        {
            // Add passthrough layer to camera rig
            passthroughLayer = cameraRig.gameObject.AddComponent<OVRPassthroughLayer>();
            passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;
        }
        
        // Enable passthrough
        passthroughLayer.enabled = true;
        passthroughLayer.textureOpacity = passthroughOpacity;
        
        // Set camera background to transparent/solid color
        // The passthrough will show through
        Camera centerCam = cameraRig.centerEyeAnchor?.GetComponent<Camera>();
        if (centerCam != null)
        {
            centerCam.clearFlags = CameraClearFlags.SolidColor;
            centerCam.backgroundColor = new Color(0, 0, 0, 0);
        }
        
        // Also set left/right eye cameras
        Camera leftCam = cameraRig.leftEyeAnchor?.GetComponent<Camera>();
        Camera rightCam = cameraRig.rightEyeAnchor?.GetComponent<Camera>();
        
        if (leftCam != null)
        {
            leftCam.clearFlags = CameraClearFlags.SolidColor;
            leftCam.backgroundColor = new Color(0, 0, 0, 0);
        }
        if (rightCam != null)
        {
            rightCam.clearFlags = CameraClearFlags.SolidColor;
            rightCam.backgroundColor = new Color(0, 0, 0, 0);
        }
        
        Debug.Log("[PassthroughSetup] Passthrough enabled");
    }
    
    public void DisablePassthrough()
    {
        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = false;
        }
    }
    
    public void SetOpacity(float opacity)
    {
        passthroughOpacity = Mathf.Clamp01(opacity);
        if (passthroughLayer != null)
        {
            passthroughLayer.textureOpacity = passthroughOpacity;
        }
    }
}

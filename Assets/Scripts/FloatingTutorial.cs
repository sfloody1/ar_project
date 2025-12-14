using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Floating AR-style tutorial window that follows the player's view
/// Positions itself in front of the player and stays visible
/// </summary>
public class FloatingTutorial : MonoBehaviour
{
    [Header("References")]
    public Transform playerHead;           // CenterEyeAnchor
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public TMP_Text skipText;
    public ConductorController conductorController;
    public AudioManager audioManager;
    
    [Header("Floating Settings")]
    public float distanceFromPlayer = 1.5f;  // How far in front of player
    public float heightOffset = 0.0f;        // Offset from eye level
    public float followSpeed = 2f;           // How fast it follows player gaze
    public float lookAtSpeed = 5f;           // How fast it rotates to face player
    
    [Header("Window Size")]
    public float windowWidth = 0.8f;         // Width in meters
    public float windowHeight = 0.45f;       // Height in meters (16:9 aspect)
    
    [Header("Tutorial Settings")]
    public VideoClip tutorialClip;
    public string videoUrl;                  // Alternative: streaming URL
    public bool showOnStart = true;
    public bool allowSkip = true;
    public float skipHoldTime = 1.5f;        // Hold trigger to skip
    
    private float skipHoldTimer = 0f;
    private bool isShowing = false;
    private bool tutorialCompleted = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    // The actual floating panel
    private GameObject floatingPanel;
    private MeshRenderer panelBackground;
    private Canvas worldCanvas;
    
    void Start()
    {
        // Find player head if not assigned
        if (playerHead == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
                playerHead = rig.centerEyeAnchor;
        }
        
        // Create the floating panel
        CreateFloatingPanel();
        
        // Setup video player
        SetupVideoPlayer();
        
        // Initially hide
        if (floatingPanel != null)
            floatingPanel.SetActive(false);
        
        if (showOnStart)
        {
            ShowTutorial();
        }
    }
    
    void CreateFloatingPanel()
    {
        // Create parent object
        floatingPanel = new GameObject("FloatingTutorialPanel");
        floatingPanel.transform.SetParent(transform);
        
        // Create background quad
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "Background";
        bgQuad.transform.SetParent(floatingPanel.transform);
        bgQuad.transform.localPosition = new Vector3(0, 0, 0.01f); // Slightly behind video
        bgQuad.transform.localScale = new Vector3(windowWidth + 0.05f, windowHeight + 0.1f, 1f);
        
        // Dark semi-transparent background
        panelBackground = bgQuad.GetComponent<MeshRenderer>();
        Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        panelBackground.material = bgMat;
        
        // Destroy collider
        Destroy(bgQuad.GetComponent<Collider>());
        
        // Create world space canvas for UI elements
        GameObject canvasObj = new GameObject("TutorialCanvas");
        canvasObj.transform.SetParent(floatingPanel.transform);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localRotation = Quaternion.identity;
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(windowWidth * 1000, windowHeight * 1000); // In canvas units
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // Scale down to world units
        
        // Add RawImage for video
        GameObject videoObj = new GameObject("VideoDisplay");
        videoObj.transform.SetParent(canvasObj.transform);
        videoDisplay = videoObj.AddComponent<RawImage>();
        
        RectTransform videoRect = videoDisplay.GetComponent<RectTransform>();
        videoRect.anchorMin = new Vector2(0.5f, 0.55f);
        videoRect.anchorMax = new Vector2(0.5f, 0.55f);
        videoRect.pivot = new Vector2(0.5f, 0.5f);
        videoRect.sizeDelta = new Vector2(windowWidth * 900, windowHeight * 700);
        videoRect.localPosition = Vector3.zero;
        
        // Add skip text
        GameObject skipObj = new GameObject("SkipText");
        skipObj.transform.SetParent(canvasObj.transform);
        skipText = skipObj.AddComponent<TextMeshProUGUI>();
        skipText.text = "Hold trigger to skip";
        skipText.fontSize = 24;
        skipText.alignment = TextAlignmentOptions.Center;
        skipText.color = Color.white;
        
        RectTransform skipRect = skipText.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.5f, 0.1f);
        skipRect.anchorMax = new Vector2(0.5f, 0.1f);
        skipRect.pivot = new Vector2(0.5f, 0.5f);
        skipRect.sizeDelta = new Vector2(600, 50);
        skipRect.localPosition = new Vector3(0, -windowHeight * 400, 0);
        
        // Add title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform);
        TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Tutorial - Conducting Pattern";
        titleText.fontSize = 32;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.9f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(700, 60);
        titleRect.localPosition = new Vector3(0, windowHeight * 400, 0);
    }
    
    void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = floatingPanel.AddComponent<VideoPlayer>();
        }
        
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Create render texture
        RenderTexture rt = new RenderTexture(1280, 720, 0);
        rt.Create();
        videoPlayer.targetTexture = rt;
        
        if (videoDisplay != null)
            videoDisplay.texture = rt;
        
        // Set video source
        if (tutorialClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = tutorialClip;
        }
        else if (!string.IsNullOrEmpty(videoUrl))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl;
        }
        
        videoPlayer.loopPointReached += OnVideoEnd;
    }
    
    void Update()
    {
        if (!isShowing || playerHead == null) return;
        
        // Calculate target position in front of player
        Vector3 forward = playerHead.forward;
        forward.y = 0; // Keep horizontal
        forward.Normalize();
        
        targetPosition = playerHead.position + forward * distanceFromPlayer;
        targetPosition.y = playerHead.position.y + heightOffset;
        
        // Calculate target rotation to face player
        Vector3 lookDir = playerHead.position - targetPosition;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            targetRotation = Quaternion.LookRotation(-lookDir);
        
        // Smoothly move and rotate
        floatingPanel.transform.position = Vector3.Lerp(
            floatingPanel.transform.position, 
            targetPosition, 
            Time.deltaTime * followSpeed
        );
        
        floatingPanel.transform.rotation = Quaternion.Slerp(
            floatingPanel.transform.rotation,
            targetRotation,
            Time.deltaTime * lookAtSpeed
        );
        
        // Check for skip input
        if (allowSkip)
        {
            bool holding = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) ||
                          OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
            
            if (holding)
            {
                skipHoldTimer += Time.deltaTime;
                float progress = skipHoldTimer / skipHoldTime;
                if (skipText != null)
                    skipText.text = $"Skipping... {Mathf.RoundToInt(progress * 100)}%";
                
                if (skipHoldTimer >= skipHoldTime)
                {
                    SkipTutorial();
                }
            }
            else
            {
                skipHoldTimer = 0f;
                if (skipText != null)
                    skipText.text = "Hold trigger to skip";
            }
        }
    }
    
    public void ShowTutorial()
    {
        if (tutorialCompleted) return;
        
        isShowing = true;
        
        if (floatingPanel != null)
        {
            // Position immediately in front of player first
            if (playerHead != null)
            {
                Vector3 forward = playerHead.forward;
                forward.y = 0;
                forward.Normalize();
                floatingPanel.transform.position = playerHead.position + forward * distanceFromPlayer;
                floatingPanel.transform.position = new Vector3(
                    floatingPanel.transform.position.x,
                    playerHead.position.y + heightOffset,
                    floatingPanel.transform.position.z
                );
                floatingPanel.transform.LookAt(playerHead);
                floatingPanel.transform.Rotate(0, 180, 0);
            }
            
            floatingPanel.SetActive(true);
        }
        
        if (videoPlayer != null && (tutorialClip != null || !string.IsNullOrEmpty(videoUrl)))
        {
            videoPlayer.Play();
        }
        
        // Pause game systems
        if (conductorController != null)
            conductorController.enabled = false;
    }
    
    public void HideTutorial()
    {
        isShowing = false;
        
        if (floatingPanel != null)
            floatingPanel.SetActive(false);
        
        if (videoPlayer != null)
            videoPlayer.Stop();
    }
    
    void SkipTutorial()
    {
        CompleteTutorial();
    }
    
    void OnVideoEnd(VideoPlayer vp)
    {
        // Video finished playing once - could auto-complete or loop
        // Currently set to loop, so this won't trigger unless changed
    }
    
    void CompleteTutorial()
    {
        tutorialCompleted = true;
        HideTutorial();
        
        // Re-enable game systems
        if (conductorController != null)
            conductorController.enabled = true;
    }
    
    public void ReplayTutorial()
    {
        tutorialCompleted = false;
        ShowTutorial();
    }
}

using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Floating AR-style tutorial window that follows the player's view
/// Auto-connects to Tutorial button in pause menu
/// </summary>
public class FloatingTutorial : MonoBehaviour
{


private void OnEnable() { if (videoPlayer == null || videoDisplay == null) { CreateTutorialUI(); } }

    [Header("References")]
    public Transform playerHead;           // CenterEyeAnchor
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public TMP_Text skipText;
    public ConductorController conductorController;
    public AudioManager audioManager;
    public Button tutorialButton;          // Optional: Button in pause menu
    
    [Header("Floating Settings")]
    public float distanceFromPlayer = 1.5f;  
    public float heightOffset = 0.0f;        
    public float followSpeed = 2f;           
    public float lookAtSpeed = 5f;           
    
    [Header("Window Size")]
    public float windowWidth = 0.8f;         
    public float windowHeight = 0.45f;       
    
    [Header("Tutorial Settings")]
    public VideoClip tutorialClip;
    public string videoUrl;                  
    public bool showOnStart = true;
    public bool allowSkip = true;
    public float skipHoldTime = 1.5f;        
    
    private float skipHoldTimer = 0f;
    private bool isShowing = false;
    private bool tutorialCompleted = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private GameObject floatingPanel;
    private MeshRenderer panelBackground;
    private Canvas worldCanvas;
    
    void Start()
    {
        if (playerHead == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
                playerHead = rig.centerEyeAnchor;
        }
        
        CreateFloatingPanel();
        SetupVideoPlayer();
        ConnectTutorialButton();
        
        if (floatingPanel != null)
            floatingPanel.SetActive(false);
        
        if (showOnStart)
        {
            ShowTutorial();
        }
    }
    
    void ConnectTutorialButton()
    {
        if (tutorialButton == null)
        {
            GameObject buttonObj = GameObject.Find("TutorialButton");
            if (buttonObj != null)
                tutorialButton = buttonObj.GetComponent<Button>();
        }
        
        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(OnTutorialButtonPressed);
            Debug.Log("[FloatingTutorial] Connected to Tutorial button");
        }
    }
    
    public void OnTutorialButtonPressed()
    {
        Debug.Log("[FloatingTutorial] Tutorial button pressed");
        
        if (conductorController != null && conductorController.PauseCanvas != null)
        {
            conductorController.PauseCanvas.SetActive(false);
        }
        
        tutorialCompleted = false;
        ShowTutorial();
    }
    
    void CreateFloatingPanel()
    {
        floatingPanel = new GameObject("FloatingTutorialPanel");
        floatingPanel.transform.SetParent(transform);
        
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "Background";
        bgQuad.transform.SetParent(floatingPanel.transform);
        bgQuad.transform.localPosition = new Vector3(0, 0, 0.01f);
        bgQuad.transform.localScale = new Vector3(windowWidth + 0.05f, windowHeight + 0.1f, 1f);
        
        panelBackground = bgQuad.GetComponent<MeshRenderer>();
        Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        panelBackground.material = bgMat;
        Destroy(bgQuad.GetComponent<Collider>());
        
        GameObject canvasObj = new GameObject("TutorialCanvas");
        canvasObj.transform.SetParent(floatingPanel.transform);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localRotation = Quaternion.identity;
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(windowWidth * 1000, windowHeight * 1000);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        
        GameObject videoObj = new GameObject("VideoDisplay");
        videoObj.transform.SetParent(canvasObj.transform);
        videoDisplay = videoObj.AddComponent<RawImage>();
        
        RectTransform videoRect = videoDisplay.GetComponent<RectTransform>();
        videoRect.anchorMin = new Vector2(0.5f, 0.55f);
        videoRect.anchorMax = new Vector2(0.5f, 0.55f);
        videoRect.pivot = new Vector2(0.5f, 0.5f);
        videoRect.sizeDelta = new Vector2(windowWidth * 900, windowHeight * 700);
        videoRect.localPosition = Vector3.zero;
        
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
        
        RenderTexture rt = new RenderTexture(1280, 720, 0);
        rt.Create();
        videoPlayer.targetTexture = rt;
        
        if (videoDisplay != null)
            videoDisplay.texture = rt;
        
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
        
        Vector3 forward = playerHead.forward;
        forward.y = 0;
        forward.Normalize();
        
        targetPosition = playerHead.position + forward * distanceFromPlayer;
        targetPosition.y = playerHead.position.y + heightOffset;
        
        Vector3 lookDir = playerHead.position - targetPosition;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            targetRotation = Quaternion.LookRotation(-lookDir);
        
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
        // Currently loops
    }
    
    void CompleteTutorial()
    {
        tutorialCompleted = true;
        HideTutorial();
        
        if (conductorController != null)
            conductorController.enabled = true;
    }
    
    public void ReplayTutorial()
    {
        tutorialCompleted = false;
        ShowTutorial();
    }


private void CreateTutorialUI() { Debug.Log("Creating Tutorial UI..."); GameObject canvasObj = new GameObject("TutorialCanvas"); canvasObj.transform.SetParent(transform, false); Canvas canvas = canvasObj.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = Camera.main; CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>(); scaler.dynamicPixelsPerUnit = 100; canvasObj.AddComponent<GraphicRaycaster>(); RectTransform canvasRect = canvasObj.GetComponent<RectTransform>(); canvasRect.sizeDelta = new Vector2(800, 450); canvasRect.localPosition = Vector3.zero; canvasRect.localRotation = Quaternion.identity; canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); GameObject panelObj = new GameObject("Panel"); panelObj.transform.SetParent(canvasObj.transform, false); RectTransform panelRect = panelObj.AddComponent<RectTransform>(); panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.sizeDelta = Vector2.zero; panelRect.anchoredPosition = Vector2.zero; Image panelImage = panelObj.AddComponent<Image>(); panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); GameObject videoObj = new GameObject("VideoDisplay"); videoObj.transform.SetParent(panelObj.transform, false); RectTransform videoRect = videoObj.AddComponent<RectTransform>(); videoRect.anchorMin = new Vector2(0.05f, 0.15f); videoRect.anchorMax = new Vector2(0.95f, 0.95f); videoRect.sizeDelta = Vector2.zero; videoRect.anchoredPosition = Vector2.zero; RawImage videoImage = videoObj.AddComponent<RawImage>(); videoImage.color = Color.white; videoDisplay = videoImage; videoPlayer = gameObject.AddComponent<VideoPlayer>(); videoPlayer.playOnAwake = false; videoPlayer.renderMode = VideoRenderMode.RenderTexture; videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct; RenderTexture rt = new RenderTexture(1920, 1080, 0); rt.Create(); videoPlayer.targetTexture = rt; videoImage.texture = rt; if (tutorialClip != null) { videoPlayer.clip = tutorialClip; } else if (!string.IsNullOrEmpty(videoUrl)) { videoPlayer.url = videoUrl; } GameObject skipTextObj = new GameObject("SkipText"); skipTextObj.transform.SetParent(panelObj.transform, false); RectTransform skipRect = skipTextObj.AddComponent<RectTransform>(); skipRect.anchorMin = new Vector2(0.5f, 0.05f); skipRect.anchorMax = new Vector2(0.5f, 0.05f); skipRect.sizeDelta = new Vector2(300, 40); skipRect.anchoredPosition = Vector2.zero; skipText = skipTextObj.AddComponent<TextMeshProUGUI>(); skipText.text = "Hold B to skip"; skipText.fontSize = 24; skipText.alignment = TextAlignmentOptions.Center; skipText.color = Color.white; Debug.Log("Tutorial UI Created Successfully!"); }
}

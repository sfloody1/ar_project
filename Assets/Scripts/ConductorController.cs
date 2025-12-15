using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class ConductorController : MonoBehaviour
{
    Vector3[] RightPattern = {
        new Vector3(0, -.1f, 0),
        new Vector3(-.15f, 0, 0),
        new Vector3(.15f, 0, 0),
        new Vector3(0, .2f, 0)
    };
    Vector3[] LeftPattern = {
        new Vector3(0, -.1f, 0),
        new Vector3(.15f, 0, 0),
        new Vector3(-.15f, 0, 0),
        new Vector3(0, .2f, 0)
    };

    [Header("OVR References")]
    public OVRCameraRig ovrCameraRig;
    public Transform RightControllerTransform;
    public Transform LeftControllerTransform;

    [Header("Baton")]
    public GameObject Baton;

    [Header("UI Elements")]
    public TMP_Text Feedback;
    public TMP_Text ScoreShow;
    public TMP_Text Flat;
    public TMP_Text FinalScore;
    public TMP_Text FinalGrade;
    public TMP_Text StartText;
    public TMP_Text BeatIndicator;
    public TMP_Text NextBeatText;
    public TMP_Text CountDown;

    public TMP_FontAsset feedbackFont;
    public TMP_FontAsset scoreFont;
    public TMP_FontAsset gradeFont;

    [Header("Audio")]
    public AudioManager audioManager;

    [Header("Progress Bar")]
    public Slider ProgressBar;

    [Header("Pause Menu")]
    public GameObject PauseCanvas;
    private bool isPaused = false;


    private Vector3 setRightPos;
    private Quaternion setRightRot;
    private Vector3 setLeftPos;
    private Quaternion setLeftRot;
    private int score;
    private int totalPossibleScore;
    private bool started;
    private bool finished;
    private bool beatChecked = false;
    private bool canRestart = false;
    private bool isCountingDown = false;
    private int CurrentBeat = 0;
    


    private const int samplesPerBeat = 30; 
    private Vector3[] rightHistory = new Vector3[samplesPerBeat];
    private Vector3[] leftHistory  = new Vector3[samplesPerBeat];
    private int historyIndex = 0;

    private Coroutine rightMoveCoroutine;
    private Coroutine leftMoveCoroutine;

    public GameObject RightIndicator; 
    public GameObject LeftIndicator; 
    public Image photoImage;

    private Vector3 prevRightPos;
    private Vector3 prevLeftPos;



    public void TogglePause() {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame() {
        isPaused = true;

        Time.timeScale = 0f;

        if (audioManager != null)
            audioManager.PauseSong();

        if (PauseCanvas != null)
            PauseCanvas.SetActive(true);
    }

    public void ResumeGame() {
        isPaused = false;

        Time.timeScale = 1f;

        if (audioManager != null)
            audioManager.ResumeSong();

        if (PauseCanvas != null)
            PauseCanvas.SetActive(false);
    }

public void PlayTutorial() {
        Debug.Log("Play Tutorial pressed");
        
        // Close pause menu
        if (PauseCanvas != null)
            PauseCanvas.SetActive(false);
        
        // Find and show tutorial
        FloatingTutorial tutorial = FindObjectOfType<FloatingTutorial>();
        if (tutorial != null)
        {
            tutorial.ReplayTutorial();
        }
        else
        {
            Debug.LogWarning("FloatingTutorial not found!");
        }
    }

    public void QuitGame() {
        Debug.Log("Quit Game pressed");

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    void UpdateIndicators(int beat)
    {
        if (RightIndicator == null || LeftIndicator == null) return;

        // These are LOCAL positions relative to VR_UI_Anchor
        Vector3 rightBase = Vector3.right * 0.15f;
        Vector3 leftBase  = Vector3.left  * 0.15f;

        RightIndicator.transform.localPosition =
            rightBase + RightPattern[beat] + new Vector3(.2f, .5f, 1f);

        LeftIndicator.transform.localPosition =
            leftBase + LeftPattern[beat] + new Vector3(-.2f, .5f, 1f);
    }



    void Start()
    {
        if (ovrCameraRig == null) ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            Transform ts = ovrCameraRig.transform.Find("TrackingSpace");
            if (ts != null)
            {
                if (RightControllerTransform == null)
                {
                    Transform rh = ts.Find("RightHandAnchor");
                    if (rh != null) RightControllerTransform = rh.Find("RightControllerAnchor") ?? rh;
                }
                if (LeftControllerTransform == null)
                {
                    Transform lh = ts.Find("LeftHandAnchor");
                    if (lh != null) LeftControllerTransform = lh.Find("LeftControllerAnchor") ?? lh;
                }
            }
        }

        if (Baton == null) Baton = GameObject.Find("Baton");

        if (RightControllerTransform != null)
        {
            setRightPos = RightControllerTransform.position;
            setRightRot = RightControllerTransform.rotation;
        }
        if (LeftControllerTransform != null)
        {
            setLeftPos = LeftControllerTransform.position;
            setLeftRot = LeftControllerTransform.rotation;
        }

        InitializeUI();

        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.OnBeat += OnBeat;
            audioManager.OnSongEnd += OnSongEnd;
        }

        if (RightControllerTransform != null)
        setRightPos = RightControllerTransform.position;

        if (LeftControllerTransform != null)
            setLeftPos = LeftControllerTransform.position;

        prevRightPos = RightControllerTransform.position;
        prevLeftPos = LeftControllerTransform.position;
    }

    void InitializeUI()
    {
        if (Feedback != null){
            Feedback.text = "";
            Feedback.font = feedbackFont;
        } 
        if (FinalGrade != null){
            FinalGrade.text = "";
            FinalGrade.font = gradeFont;
        } 
        if (FinalScore != null){
            FinalScore.text = "";
            FinalScore.font = scoreFont;
        }
        if (Flat != null){
            Flat.text = "";
            Flat.font = feedbackFont;
        } 
        if (ScoreShow != null){
            ScoreShow.text = "Score: 0";
            ScoreShow.font = scoreFont;
        } 
        if (StartText != null){
            StartText.text = "Press trigger to start!";
            StartText.font = scoreFont;
        } 
        if (BeatIndicator != null){
            BeatIndicator.text = "";
            BeatIndicator.font = scoreFont;
        } 
        if (NextBeatText != null){
            NextBeatText.text = "";
            NextBeatText.font = scoreFont;
        }
        if (ProgressBar != null){
            ProgressBar.value = 0f;
        }
        if (CountDown != null) {
            CountDown.text = "";
            CountDown.font = feedbackFont;
            CountDown.gameObject.SetActive(false);
        }
        if (photoImage != null)
            photoImage.gameObject.SetActive(false);


        isCountingDown = false;

    }

    void OnDestroy()
    {
        if (audioManager != null)
        {
            audioManager.OnBeat -= OnBeat;
            audioManager.OnSongEnd -= OnSongEnd;
        }
    }

    void OnBeat(int beat)
    {
        beatChecked = false;
        if (BeatIndicator != null)
        {
            string[] n = { "DOWN", "LEFT", "RIGHT", "UP" };
            Color[] c = { Color.red, Color.yellow, Color.green, Color.cyan };
            BeatIndicator.text = "Beat " + (beat + 1);
            BeatIndicator.color = c[beat];
        }
        if (NextBeatText != null)
        {
            string[] n = { "DOWN", "LEFT", "RIGHT", "UP" };
            NextBeatText.text = "Next: " + n[(beat + 1) % 4];
        }

        UpdateIndicators(beat);

        if (started && !finished && !isCountingDown)
        {
            CheckGesture(beat);
            beatChecked = true;
        }
    }





    void OnSongEnd()
    {
        finished = true;
        ShowFinalScore();
    }


    void Update()
    {
        // X button (left hand) - toggle pause
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            TogglePause();
            return;
        }
        
        // A button (right hand) - quit when paused
        if (isPaused && OVRInput.GetDown(OVRInput.Button.One))
        {
            QuitGame();
            return;
        }
        
        // B button (right hand) - play tutorial when paused
        if (isPaused && OVRInput.GetDown(OVRInput.Button.Two))
        {
            PlayTutorial();
            return;
        }

        if (isPaused) return;
        
        if (RightControllerTransform == null || LeftControllerTransform == null)
            return;

        Vector3 rPos = RightControllerTransform.position;
        Quaternion rRot = RightControllerTransform.rotation;
        Vector3 lPos = LeftControllerTransform.position;
        Quaternion lRot = LeftControllerTransform.rotation;

        bool trigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (!trigger)
        {
            InputDevice rd = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            rd.TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
        }

        if (trigger)
        {
            if (!started && !finished && RightControllerTransform != null && LeftControllerTransform != null)
            {
                setRightPos = rPos;
                setRightRot = rRot;
                setLeftPos = lPos;
                setLeftRot = lRot;

                if (StartText != null) StartText.text = "";
                started = true;
                score = 0;
                totalPossibleScore = 0;

                StartCoroutine(StartCountDown());

                if (photoImage != null)
                    photoImage.gameObject.SetActive(true);
            }
            else if (finished && canRestart)
            {
                RestartGame();
                canRestart = false;
                return;
            }
        }

        if (started && !finished && !isCountingDown)
        {
            CheckFlatness(rRot, lRot);
            //CHANGE HERE
            //Vector3 rDelta = rPos - setRightPos;
            //Vector3 lDelta = lPos - setLeftPos;
            //Vector3 rDelta = rPos - prevRightPos;
            //Vector3 lDelta = lPos - prevLeftPos;

            //rightHistory[historyIndex] = rDelta;
            //leftHistory[historyIndex] = lDelta;
            //historyIndex = (historyIndex + 1) % samplesPerBeat;

            if (!beatChecked && audioManager != null && audioManager.IsPlaying)
            {
                CheckGesture(audioManager.CurrentBeat);
                beatChecked = true;
            }

            if (ScoreShow != null){
                ScoreShow.text = "Score: " + score;
            }
            if (ProgressBar != null && audioManager != null && audioManager.IsPlaying)
            {
                float progress = Mathf.Clamp01(audioManager.SongTime / audioManager.SongLength);
                ProgressBar.value = progress;
            }
        }

    }

    IEnumerator StartCountDown()
    {
        isCountingDown = true;

        if (CountDown != null)
            CountDown.gameObject.SetActive(true);

        string[] steps = { "3", "2", "1", "GO!" };

        foreach (string s in steps)
        {
            if (CountDown != null)
            {
                CountDown.text = s;
                CountDown.color = s == "GO!" ? Color.green : Color.white;
                CountDown.ForceMeshUpdate();
            }

            yield return new WaitForSeconds(1f);
        }

        if (CountDown != null)
            CountDown.gameObject.SetActive(false);

        isCountingDown = false;
        started = true;

        if (audioManager != null) audioManager.StartSong();
        
        // Start photo fade out - show for 4 seconds, then fade over 1.5 seconds
        if (photoImage != null)
            StartCoroutine(FadeOutPhoto(8f, 1.5f));
    }

    IEnumerator FadeOutPhoto(float displayTime, float fadeTime)
    {
        // Show photo for displayTime seconds
        yield return new WaitForSeconds(displayTime);
        
        // Then fade out over fadeTime seconds
        if (photoImage != null)
        {
            Color startColor = photoImage.color;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                photoImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            // Hide completely after fade
            photoImage.gameObject.SetActive(false);
            // Reset alpha for next time
            photoImage.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        }
    }



    void CheckFlatness(Quaternion rRot, Quaternion lRot)
    {
        if (Flat == null) return;

        float ra = Quaternion.Angle(setRightRot, rRot);
        float la = Quaternion.Angle(setLeftRot, lRot);

        if (ra > 30f && la > 30f)
        {
            Flat.text = "Flatten both hands!";
            Flat.color = Color.red;
        }
        else if (ra > 30f)
        {
            Flat.text = "Flatten right hand!";
            Flat.color = Color.yellow;
        }
        else if (la > 30f)
        {
            Flat.text = "Flatten left hand!";
            Flat.color = Color.yellow;
        }
        else Flat.text = "";
    }

    void CheckGesture(int beat)
    {
        // Calculate movement since last beat
        Vector3 rDelta = RightControllerTransform.position - prevRightPos;
        Vector3 lDelta = LeftControllerTransform.position - prevLeftPos;

        // Optional: store last positions for next beat
        prevRightPos = RightControllerTransform.position;
        prevLeftPos = LeftControllerTransform.position;

        // Calculate dot product with expected right hand pattern
        float rDot = Vector3.Dot(rDelta.normalized, RightPattern[beat]);

        // Factor in movement magnitude (need to actually move, not just point)
        float magnitudeFactor = Mathf.Clamp01(rDelta.magnitude / 0.2f);

        // Final score based on direction accuracy and movement magnitude
        float combined = rDot * magnitudeFactor;

        totalPossibleScore += 3;

        if (combined > 0.15f) StartCoroutine(ShowFeedback("Perfect!", Color.blue, 3));
        else if (combined > 0.1f) StartCoroutine(ShowFeedback("Good", Color.green, 2));
        else if (combined > 0.05f) StartCoroutine(ShowFeedback("Okay", Color.yellow, 1));
        else StartCoroutine(ShowFeedback("X", Color.red, 0));

        // Volume based on right hand movement magnitude
        if (audioManager != null)
            audioManager.SetMasterVolume(Mathf.Clamp(rDelta.magnitude * 2f, 0.3f, 1f));

    }

    IEnumerator ShowFeedback(string t, Color c, int p)
    {
        score += p;

        if (Feedback != null)
        {
            Feedback.text = t;
            Feedback.color = c;
        }

        yield return new WaitForSeconds(0.5f);

        if (Feedback != null) Feedback.text = "";
    }

    void ShowFinalScore()
    {
        started = false;
        finished = true;

        StopAllCoroutines();

        clearText();

        if (FinalScore != null){
            StartCoroutine(AnimateFinalScore(score, totalPossibleScore));
        } 

        if (ProgressBar != null) { ProgressBar.value = 1f; }

        if (StartText != null){
            StartText.text = "Press trigger to replay!";
        }

        StartCoroutine(EnableRestart());
        
    }
    IEnumerator EnableRestart()
    {
        yield return new WaitForSeconds(0.2f); 
        canRestart = true;
    }

    void clearText() {
        if (Feedback != null){
            Feedback.text = "";
            Feedback.ForceMeshUpdate();
        } 
        if (Flat != null){
            Flat.text = "";
            Flat.ForceMeshUpdate();
        } 
        if (ScoreShow != null){
            ScoreShow.text = "";
            ScoreShow.ForceMeshUpdate();
        } 
        if (BeatIndicator != null){
            BeatIndicator.text = "";
            BeatIndicator.ForceMeshUpdate();
        } 
        if (NextBeatText != null){
            NextBeatText.text = "";
            NextBeatText.ForceMeshUpdate();
        }
    }

    IEnumerator AnimateFinalScore(int finalScore, int totalPossibleScore)
    {
        if (FinalScore == null) yield break;

        float duration = 1.5f; 
        float elapsed = 0f;
        int displayedScore = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(0, finalScore, t));
            float movingPct = totalPossibleScore > 0 ? (float)displayedScore / totalPossibleScore : 0;

            if (movingPct >= 0.9f) FinalScore.color = Color.blue;
            else if (movingPct >= 0.8f) FinalScore.color = Color.green;
            else if (movingPct >= 0.7f) FinalScore.color = Color.yellow;
            else if (movingPct >= 0.6f) FinalScore.color = new Color(1f, 0.5f, 0f);
            else FinalScore.color = Color.red;

            FinalScore.text = $"Final Score: {displayedScore}/{totalPossibleScore}";

            yield return null;
        }

        FinalScore.text = $"Final Score: {finalScore}/{totalPossibleScore}";
        float pct = totalPossibleScore > 0 ? (float)finalScore / totalPossibleScore : 0;
        if (FinalGrade != null)
        {
            if (pct >= 0.75f) { FinalGrade.text = "A"; FinalGrade.color = Color.blue; }
            else if (pct >= 0.6f) { FinalGrade.text = "B"; FinalGrade.color = Color.green; }
            else if (pct >= 0.45f) { FinalGrade.text = "C"; FinalGrade.color = Color.yellow; }
            else if (pct >= 0.3f) { FinalGrade.text = "D"; FinalGrade.color = new Color(1f, 0.5f, 0f); }
            else { FinalGrade.text = "F"; FinalGrade.color = Color.red; }
        }

    }

    public void ResetGameState()
    {
        InitializeUI();
        finished = false;
        started = false;
        score = 0;
        totalPossibleScore = 0;
        canRestart = false;
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        
        ResetGameState();

        if (RightControllerTransform != null)
        {
            setRightPos = RightControllerTransform.position;
            setRightRot = RightControllerTransform.rotation;
        }
        if (LeftControllerTransform != null)
        {
            setLeftPos = LeftControllerTransform.position;
            setLeftRot = LeftControllerTransform.rotation;
        }

        for (int i = 0; i < samplesPerBeat; i++)
        {
            rightHistory[i] = Vector3.zero;
            leftHistory[i] = Vector3.zero;
        }
        historyIndex = 0;

        if (rightMoveCoroutine != null) StopCoroutine(rightMoveCoroutine);
        if (leftMoveCoroutine != null) StopCoroutine(leftMoveCoroutine);


        StartCoroutine(StartCountDown());
    }


}

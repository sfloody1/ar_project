using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using TMPro;

public class ConductorController : MonoBehaviour
{
    Vector3[] RightPattern = {
        new Vector3(0, -1f, 0),
        new Vector3(-1f, 0, 0),
        new Vector3(1f, 0, 0),
        new Vector3(0, 1f, 0)
    };
    Vector3[] LeftPattern = {
        new Vector3(0, -1f, 0),
        new Vector3(1f, 0, 0),
        new Vector3(-1f, 0, 0),
        new Vector3(0, 1f, 0)
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

    [Header("Audio")]
    public AudioManager audioManager;

    private Vector3 setRightPos;
    private Quaternion setRightRot;
    private Vector3 setLeftPos;
    private Quaternion setLeftRot;
    private int score;
    private int totalPossibleScore;
    private bool started;
    private bool finished;
    private bool beatChecked = false;

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
        if (RightControllerTransform != null) { setRightPos = RightControllerTransform.position; setRightRot = RightControllerTransform.rotation; }
        if (LeftControllerTransform != null) { setLeftPos = LeftControllerTransform.position; setLeftRot = LeftControllerTransform.rotation; }
        InitializeUI();
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null) { audioManager.OnBeat += OnBeat; audioManager.OnSongEnd += OnSongEnd; }
    }

    void InitializeUI()
    {
        if (Feedback != null) Feedback.text = "";
        if (FinalGrade != null) FinalGrade.text = "";
        if (FinalScore != null) FinalScore.text = "";
        if (Flat != null) Flat.text = "";
        if (ScoreShow != null) ScoreShow.text = "Score: 0";
        if (StartText != null) StartText.text = "Press trigger to start!";
        if (BeatIndicator != null) BeatIndicator.text = "";
        if (NextBeatText != null) NextBeatText.text = "";
    }

    void OnDestroy() { if (audioManager != null) { audioManager.OnBeat -= OnBeat; audioManager.OnSongEnd -= OnSongEnd; } }

    void OnBeat(int beat)
    {
        beatChecked = false;
        if (BeatIndicator != null)
        {
            string[] n = { "DOWN", "LEFT", "RIGHT", "UP" };
            Color[] c = { Color.red, Color.yellow, Color.green, Color.cyan };
            BeatIndicator.text = n[beat]; BeatIndicator.color = c[beat];
        }
        if (NextBeatText != null)
        {
            string[] n = { "DOWN", "LEFT", "RIGHT", "UP" };
            NextBeatText.text = "Next: " + n[(beat + 1) % 4];
        }
    }

    void OnSongEnd() { finished = true; ShowFinalScore(); }

    void Update()
    {
        if (RightControllerTransform == null || LeftControllerTransform == null) return;
        Vector3 rPos = RightControllerTransform.position;
        Quaternion rRot = RightControllerTransform.rotation;
        Vector3 lPos = LeftControllerTransform.position;
        Quaternion lRot = LeftControllerTransform.rotation;

        bool trigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (!trigger) { InputDevice rd = InputDevices.GetDeviceAtXRNode(XRNode.RightHand); rd.TryGetFeatureValue(CommonUsages.triggerButton, out trigger); }

        if (trigger)
        {
            if (!started && !finished)
            {
                setRightPos = rPos; setRightRot = rRot; setLeftPos = lPos; setLeftRot = lRot;
                if (StartText != null) StartText.text = "";
                started = true; score = 0; totalPossibleScore = 0;
                if (audioManager != null) audioManager.StartSong();
            }
            else if (finished) RestartGame();
        }

        if (started && !finished)
        {
            CheckFlatness(rRot, lRot);
            if (!beatChecked && audioManager != null && audioManager.IsPlaying) { CheckGesture(audioManager.CurrentBeat, rPos, lPos); beatChecked = true; }
            if (ScoreShow != null) ScoreShow.text = "Score: " + score;
        }
    }

    void CheckFlatness(Quaternion rRot, Quaternion lRot)
    {
        if (Flat == null) return;
        float ra = Quaternion.Angle(setRightRot, rRot);
        float la = Quaternion.Angle(setLeftRot, lRot);
        if (ra > 5f && la > 5f) { Flat.text = "Flatten both hands!"; Flat.color = Color.red; }
        else if (ra > 5f) { Flat.text = "Flatten right hand!"; Flat.color = Color.yellow; }
        else if (la > 5f) { Flat.text = "Flatten left hand!"; Flat.color = Color.yellow; }
        else Flat.text = "";
    }

    void CheckGesture(int beat, Vector3 rPos, Vector3 lPos)
    {
        Vector3 rd = rPos - setRightPos; Vector3 ld = lPos - setLeftPos;
        float rDot = Vector3.Dot(rd.normalized, RightPattern[beat]);
        float lDot = Vector3.Dot(ld.normalized, LeftPattern[beat]);
        float combined = ((rDot + lDot) / 2f) * Mathf.Clamp01((rd.magnitude + ld.magnitude) / 0.4f);
        totalPossibleScore += 5;
        if (combined > 0.7f) StartCoroutine(ShowFeedback("Perfect!", Color.blue, 5));
        else if (combined > 0.5f) StartCoroutine(ShowFeedback("Good", Color.green, 3));
        else if (combined > 0.3f) StartCoroutine(ShowFeedback("Okay", Color.yellow, 1));
        else StartCoroutine(ShowFeedback("X", Color.red, 0));
        if (audioManager != null) audioManager.SetMasterVolume(Mathf.Clamp((rd.magnitude + ld.magnitude) * 1.5f, 0.3f, 1f));
    }

    IEnumerator ShowFeedback(string t, Color c, int p)
    {
        score += p;
        if (Feedback != null) { Feedback.text = t; Feedback.color = c; }
        yield return new WaitForSeconds(0.5f);
        if (Feedback != null) Feedback.text = "";
    }

    void ShowFinalScore()
    {
        started = false;
        float pct = totalPossibleScore > 0 ? (float)score / totalPossibleScore : 0;
        if (FinalScore != null) FinalScore.text = score + "/" + totalPossibleScore;
        if (FinalGrade != null)
        {
            if (pct >= 0.9f) { FinalGrade.text = "A"; FinalGrade.color = Color.blue; }
            else if (pct >= 0.8f) { FinalGrade.text = "B"; FinalGrade.color = Color.green; }
            else if (pct >= 0.7f) { FinalGrade.text = "C"; FinalGrade.color = Color.yellow; }
            else if (pct >= 0.6f) { FinalGrade.text = "D"; FinalGrade.color = new Color(1f, 0.5f, 0f); }
            else { FinalGrade.text = "F"; FinalGrade.color = Color.red; }
        }
        if (StartText != null) StartText.text = "Press trigger to replay!";
    }

    public void RestartGame() { finished = false; started = false; score = 0; totalPossibleScore = 0; InitializeUI(); }
}

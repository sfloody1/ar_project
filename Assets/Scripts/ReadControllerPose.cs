using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ReadControllerPose : MonoBehaviour
{
    Vector3[] RightPattern = {
        new Vector3(0, -1f, 0),   // down
        new Vector3(-1f, 0, 0),    // left
        new Vector3(1f, 0, 0),   // right
        new Vector3(0, 1f, 0)     // up
    };
    Vector3[] LeftPattern = {
        new Vector3(0, -1f, 0),   // down
        new Vector3(1f, 0, 0),   // right
        new Vector3(-1f, 0, 0),    // left
        new Vector3(0, 1f, 0)     // up
    };

    public GameObject Baton;
    private Transform BatonTransform;

    private Vector3 rotationOffsetEuler;
    private Quaternion rotationOffset; 

    public Transform RightControllerTransform;
    public Transform LeftControllerTransform;

    private Vector3 setRightPos;
    private Quaternion setRightRot;
    private Vector3 setLeftPos;
    private Quaternion setLeftRot;

    private int score;
    private int TotalScore;
    private bool started;
    private bool finished;

    public TMP_Text Feedback;
    public TMP_Text ScoreShow;
    public TMP_Text Flat;
    public TMP_Text FinalScore;
    public TMP_Text FinalGrade;
    public TMP_Text StartText;


    IEnumerator Check(int Beat, Vector3 RightControllerPosition, Vector3 LeftControllerPosition, Vector3 setRightPos, Vector3 setLeftPos) {
        float distance = Vector3.Distance(RightControllerPosition, setRightPos + RightPattern[Beat]) + Vector3.Distance(LeftControllerPosition, setLeftPos + LeftPattern[Beat]);
        if (distance < 0.2f) {
            Feedback.text = "Perfect!";
            Feedback.color = Color.blue;
            score = score + 5;
        } else if (distance < .4f) {
            Feedback.text = "Good";
            Feedback.color = Color.green;
            score = score + 3;
        } else if (distance < .6f) {
            Feedback.text = "Okay";
            Feedback.color = Color.yellow;
            score = score + 1;
        } else {
            Feedback.text = "X";
            Feedback.color = Color.red;
        }
        yield return new WaitForSeconds(1);
        Feedback.text = "";
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BatonTransform = Baton.GetComponent<Transform>();
        rotationOffsetEuler = new Vector3(60, 0, 0);
        rotationOffset = Quaternion.Euler(rotationOffsetEuler);
        setRightPos = RightControllerTransform.position;
        setRightRot = RightControllerTransform.rotation;
        setLeftPos = LeftControllerTransform.position;
        setLeftRot = LeftControllerTransform.rotation;

        Feedback.text = "feedback";
        FinalGrade.text = "finalgrade";
        FinalScore.text = "finalscore";
        Flat.text = "flat";
        ScoreShow.text = "Score: 0";
        StartText.text = "Please put hands in start position and press the right trigger to begin!";

        score = 0;
        started = false;
        finished = false;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 RightControllerPosition = RightControllerTransform.position;
        Quaternion RightControllerRotation = RightControllerTransform.rotation;

        Vector3 LeftControllerPosition = LeftControllerTransform.position;
        Quaternion LeftControllerRotation = LeftControllerTransform.rotation;

        BatonTransform.position = RightControllerPosition;
        BatonTransform.rotation = RightControllerTransform.rotation * rotationOffset;

        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
        {
            setRightPos = RightControllerPosition;
            setRightRot = RightControllerRotation;
            setLeftPos = LeftControllerPosition;
            setLeftRot = LeftControllerRotation;

            StartText.text = "";

            //COUNTDOWN TO START SONG
            started = true;
            Debug.Log("Saved position: " + setRightPos);
        }
        
        if (started) {
            float RightAngle = Quaternion.Angle(setRightRot, RightControllerRotation);
            float LeftAngle = Quaternion.Angle(setRightRot, LeftControllerRotation);

            if (RightAngle > 5f && LeftAngle > 5f) {
                Flat.text = "Flatten both hands!";
                Flat.color = Color.red;
            } else if (RightAngle > 5f) {
                Flat.text = "Flatten your right hand!";
                Flat.color = Color.yellow;
            } else if (LeftAngle > 5f) {
                Flat.text = "Flatten your left hand!";
                Flat.color = Color.yellow;
            } else {
                Flat.text = "";
            }


            /*
            // Have beats tagged in song with 0, 1, 2, 3 for the 4 beats
            StartCoroutine(Check(beat, RightControllerPosition, LeftControllerPosition, setRightPos, setLeftPos));
            ScoreShow.text = "Score: " + score;
            // text should come up -- have to fix in unity so that it does
            if (song is finished) {
                finished = true;
            }
            */

        } 
        if (finished) {
            /*
            //i want value for total number of beats in song -- multiply by 5 for total number of points possible
            TotalScore = numBeats * 5;

            //when song is done:
            FinalScore.text = "Final Score: " + score.ToString() + " / " + TotalScore.ToString();
            if (score > (.9 * TotalScore)) {
                FinalGrade.text = "A";
                FinalGrade.color = Color.blue;
            } else if (score > (.8 * TotalScore)) {
                FinalGrade.text = "B";
                FinalGrade.color = Color.green;
            } else if (score > (.7 * TotalScore)) {
                FinalGrade.text = "C";
                FinalGrade.color = Color.yellow;
            } else if (score > (.6 * TotalScore)) {
                FinalGrade.text = "D";
                FinalGrade.color = Color.orange;
            } else {
                FinalGrade.text = "F"
                FinalGrade.color = Color.red;
            }
            started = false;
            finished = false;
            */
        }        
    }
}

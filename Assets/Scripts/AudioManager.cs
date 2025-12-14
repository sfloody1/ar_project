using UnityEngine;
using System;

/// <summary>
/// AudioManager - 管理乐队音频播放和节拍追踪
/// 配合 ReadControllerPose.cs 使用
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource pianoSource;
    public AudioSource violinSource;
    
    [Header("Audio Clips")]
    public AudioClip pianoClip;
    public AudioClip violinClip;

    [Header("Tempo Settings")]
    public float BPM = 92f;
    public int beatsPerBar = 4;

    [Header("Volume Control")]
    [Range(0f, 1f)] public float pianoVolume = 1f;
    [Range(0f, 1f)] public float violinVolume = 1f;
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("State")]
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private float songTime = 0f;
    [SerializeField] private int currentBeat = 0;
    [SerializeField] private int totalBeatsPlayed = 0;
    [SerializeField] private int currentBar = 0;

    public event Action<int> OnBeat;
    public event Action<int> OnBarStart;
    public event Action OnSongStart;
    public event Action OnSongEnd;

    private float beatInterval;
    private float nextBeatTime;
    private float songStartTime;
    private float songDuration;
    private bool songFinished = false;

    public bool IsPlaying => isPlaying;
    public bool IsSongFinished => songFinished;
    public float SongLength => songDuration;
    public float SongTime => songTime;
    public int CurrentBeat => currentBeat;
    public int TotalBeatsPlayed => totalBeatsPlayed;
    public int CurrentBar => currentBar;
    public float BeatInterval => beatInterval;
    public int TotalBeatsInSong => Mathf.FloorToInt(songDuration / beatInterval);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        beatInterval = 60f / BPM;
    }

    void Start()
    {
        SetupAudioSources();
        songDuration = pianoClip != null ? pianoClip.length : (violinClip != null ? violinClip.length : 45f);
        //songDuration = 1f;
        Debug.Log("Song length: " + songDuration);
    }

    void Update()
    {
        if (!isPlaying || songFinished) return;
        songTime = Time.time - songStartTime;
        if (songTime >= songDuration) { EndSong(); return; }
        if (Time.time >= nextBeatTime) { ProcessBeat(); nextBeatTime += beatInterval; }
        UpdateVolumes();
    }

    void SetupAudioSources()
    {
        if (pianoSource == null)
        {
            GameObject obj = new GameObject("PianoAudio");
            obj.transform.SetParent(transform);
            pianoSource = obj.AddComponent<AudioSource>();
            pianoSource.playOnAwake = false;
            pianoSource.spatialBlend = 0f;
        }
        if (violinSource == null)
        {
            GameObject obj = new GameObject("ViolinAudio");
            obj.transform.SetParent(transform);
            violinSource = obj.AddComponent<AudioSource>();
            violinSource.playOnAwake = false;
            violinSource.spatialBlend = 0f;
        }
        if (pianoClip != null) pianoSource.clip = pianoClip;
        if (violinClip != null) violinSource.clip = violinClip;
    }

    void ProcessBeat()
    {
        currentBeat = totalBeatsPlayed % beatsPerBar;
        OnBeat?.Invoke(currentBeat);
        if (currentBeat == 0) { currentBar = totalBeatsPlayed / beatsPerBar; OnBarStart?.Invoke(currentBar); }
        totalBeatsPlayed++;
    }

    void UpdateVolumes()
    {
        if (pianoSource != null) pianoSource.volume = pianoVolume * masterVolume;
        if (violinSource != null) violinSource.volume = violinVolume * masterVolume;
    }

    public void StartSong()
    {
        if (isPlaying) return;
        songTime = 0f; currentBeat = 0; totalBeatsPlayed = 0; currentBar = 0; songFinished = false;
        songStartTime = Time.time;
        // Add extra time before first beat so player can prepare
        // First beat will trigger after 1.5 beat intervals instead of 1
        nextBeatTime = Time.time + beatInterval * 1.5f;
        if (pianoSource != null && pianoSource.clip != null) pianoSource.Play();
        if (violinSource != null && violinSource.clip != null) violinSource.Play();
        isPlaying = true;
        OnSongStart?.Invoke();
        Debug.Log("[AudioManager] Song started! BPM: " + BPM);
    }

    public void StopSong()
    {
        if (!isPlaying) return;
        pianoSource?.Stop(); violinSource?.Stop();
        isPlaying = false;
    }

    private void EndSong()
    {
        isPlaying = false; songFinished = true;
        pianoSource?.Stop(); violinSource?.Stop();
        OnSongEnd?.Invoke();
        Debug.Log("[AudioManager] Song ended! Total beats: " + totalBeatsPlayed);
    }

    public void SetInstrumentVolume(string instrument, float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (instrument.ToLower() == "piano") pianoVolume = volume;
        else if (instrument.ToLower() == "violin") violinVolume = volume;
    }

    public void SetMasterVolume(float volume) { masterVolume = Mathf.Clamp01(volume); }

    public Vector3 GetExpectedDirection(int beat)
    {
        switch (beat % 4)
        {
            case 0: return new Vector3(0, -1f, 0);
            case 1: return new Vector3(-1f, 0, 0);
            case 2: return new Vector3(1f, 0, 0);
            case 3: return new Vector3(0, 1f, 0);
            default: return Vector3.zero;
        }
    }

    public string GetBeatName(int beat)
    {
        switch (beat % 4)
        {
            case 0: return "DOWN";
            case 1: return "LEFT";
            case 2: return "RIGHT";
            case 3: return "UP";
            default: return "";
        }
    }
    public void PauseSong()
    {
        if (pianoSource != null) {
            pianoSource.Pause();
        }
        if (violinSource != null) {
            violinSource.Pause();
        }
    }

    public void ResumeSong()
    {
        if (pianoSource != null) {
            pianoSource.UnPause();
        }
        if (violinSource != null) {
            violinSource.UnPause();
        }
    }

    public float GetTimeToNextBeat() { return nextBeatTime - Time.time; }
    public float GetBeatProgress() { return Mathf.Clamp01((beatInterval - GetTimeToNextBeat()) / beatInterval); }
}

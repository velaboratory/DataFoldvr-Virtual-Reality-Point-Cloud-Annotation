using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.Globalization;

public class TimerWithLogging : MonoBehaviour
{
    public Button startStopButton;
    public TextMeshProUGUI startStopButtonText;
    public TextMeshProUGUI timerText;

    [Tooltip("Whether this takes part in the shared timer state.\nCan't change this at runtime")]
    public bool useGlobalSharedTimer = true;

    public static TimerWithLogging instance;
    // private float timer;

    private readonly Stopwatch timer = new Stopwatch();
    private Stopwatch Timer => useGlobalSharedTimer ? instance.timer : timer;
    public double CurrentTime => Timer.Elapsed.TotalSeconds;

    // Start is called before the first frame update
    private void Awake()
    {
        if (useGlobalSharedTimer && instance == null) instance = this;
        UpdateTimerText();
    }

    private void OnEnable()
    {
        UpdateTimerText();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Timer.IsRunning) UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        timerText.text = Timer.Elapsed.ToString(@"mm\:ss\.fff");
        startStopButtonText.text = Timer.IsRunning ? "Stop" : "Start";
    }

    public void StartStopTimer()
    {
        if (Timer.IsRunning)
        {
            StopTimer();
        }
        else
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        Timer.Start();
        unityutilities.Logger.LogRow("timer", "start", Timer.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        UpdateTimerText();
    }

    public void StopTimer()
    {
        Timer.Stop();
        unityutilities.Logger.LogRow("timer", "stop", Timer.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        UpdateTimerText();
    }

    public void ResetTimer()
    {
        unityutilities.Logger.LogRow("timer", "reset", Timer.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        Timer.Reset();
        UpdateTimerText();
    }
}
using UnityEngine;
using UnityEngine.Events;

public class Timer : MonoBehaviour
{
    public float delay = 0f;
    public float duration = 1f;
    public int repeatCount = 0; // 0 for infinite repeats
    [SerializeField] private bool autoStart = true;
    public UnityEvent onTimerComplete;

    private float elapsed = 0f;
    private bool isRunning = false;
    private int timesCompleted = 0;
    private float nextTriggerTime = 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (autoStart)
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        if (!isRunning)
        {
            isRunning = true;
            nextTriggerTime = Time.time + delay;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTriggerTime)
            {
                onTimerComplete.Invoke();
                timesCompleted++;

                if (repeatCount > 0 && timesCompleted >= repeatCount)
                {
                    isRunning = false;
                }
                else
                {
                    nextTriggerTime += duration;
                }
            }
        }
    }
}

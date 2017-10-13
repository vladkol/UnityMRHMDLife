using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAdaptiveQuality : MonoBehaviour
{
    public float startupTime = 3.0f;
    public float secondsBeforeChangingQuality = 2.0f;

    [Range(60, 90)]
    public float initialTargetFramerate = 90;

    [Range(1, 50)]
    public float maximumTriesToGoUp = 5;

    [Range(1f, 10f)]
    public float downFramerateThreshold = 2.0f;
    [Range(0.05f, 0.9f)]
    public float upFramerateThreshold = 0.5f;

    public event Action<SimpleAdaptiveQuality, int, int> qualityLevelChanged;

    public TextMesh qualityDebugText;

    private bool workingInstance = false;
    private float targetFramerate = 90;
    private float startTime = 0;
    private float rateChangeStart = 0;
    private int previousResult = 0;

    private int minumumLevel = -100;
    private int maximumLevel = 100;
    private int initialLevel = 0;

    private int triedToGoUp = 0;
    private int previousChange = 0;

    private Queue<float> fpsQueue = new Queue<float>();

    private static System.Threading.Mutex _mutex = new System.Threading.Mutex();
    private static SimpleAdaptiveQuality _instance;


    private void OnEnable()
    {
        _mutex.WaitOne();

        try
        {
            if (_instance != null)
            {
                Debug.LogError("There may be only one instance of SimpleAdaptiveQuality");
                workingInstance = false;
                this.enabled = false;
                return;
            }
            else
            {
                _instance = this;
                workingInstance = true;
            }
        }
        finally
        {
            _mutex.ReleaseMutex();
        }

        startTime = Time.unscaledTime;
        targetFramerate = initialTargetFramerate;

        initialLevel = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("MRQualityLevel", initialLevel), true);

#if UNITY_2017_2_OR_NEWER
        bool isXR = UnityEngine.XR.XRSettings.enabled;
#else
        bool isXR = UnityEngine.VR.VRSettings.enabled;
#endif


#if !UNITY_EDITOR && UNITY_WSA && ENABLE_WINMD_SUPPORT
        if(isXR)
        {
            try
            {
                var display = Windows.Graphics.Holographic.HolographicDisplay.GetDefault();
                if (display != null)
                {
                    var displayRate = display.RefreshRate;

                    if (displayRate > 0)
                    {
                        if (displayRate > 59)
                            targetFramerate = (float)displayRate;
                        else
                            targetFramerate = 60;
                    }
                }

            }
            catch { }
        }
#else
        float vrRefreshRate = 0;
        if (isXR)
        {
#if !UNITY_2017_2_OR_NEWER
            vrRefreshRate = UnityEngine.VR.VRDevice.refreshRate;
#else
            vrRefreshRate = UnityEngine.XR.XRDevice.refreshRate;
#endif
        }

        if(vrRefreshRate >= 30)
        {
            targetFramerate = vrRefreshRate;
        }
        else if (Application.targetFrameRate > 0 && Application.targetFrameRate < targetFramerate)
        {
            targetFramerate = Application.targetFrameRate;
        }
#endif
    }

    private void OnDisable()
    {
        _mutex.WaitOne();

        try
        {
            if (!workingInstance)
                return;

            triedToGoUp = 0;
            PlayerPrefs.SetInt("MRQualityLevel", QualitySettings.GetQualityLevel());
            PlayerPrefs.Save();

            _instance = null;
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }

    private void Start()
    {
        if (!workingInstance)
            return;

        startTime = Time.unscaledTime;

        var currentLevel = QualitySettings.GetQualityLevel();
        if(currentLevel != initialLevel && qualityLevelChanged != null)
            qualityLevelChanged(this, initialLevel, currentLevel);
    }


    void LateUpdate ()
    {
        if (!workingInstance)
            return;

        if ((Time.unscaledTime - startTime) < startupTime)
            return;

        float refreshRate = GetFramerate();

        float minimumFramerate = targetFramerate - downFramerateThreshold;
        float maximimFramerate = targetFramerate - upFramerateThreshold;

        int toDo = 0;
        if(refreshRate < minimumFramerate)
        {
            toDo = -1;
        }
        else if(refreshRate > maximimFramerate)
        {
            toDo = 1;
        }

        int currentLevel = QualitySettings.GetQualityLevel();

        if (qualityDebugText != null)
        {
            qualityDebugText.text = string.Format("{0}: {1}({2})fps", currentLevel.ToString(), (int)refreshRate, (int)targetFramerate);
        }


        if (toDo != previousResult)
        {
            rateChangeStart = Time.unscaledTime;
            previousResult = toDo;
        }

        if (0 == toDo)
            return;

        if ((Time.unscaledTime - rateChangeStart) >= secondsBeforeChangingQuality)
        {
            if (toDo < 0 && currentLevel > minumumLevel)
            {
                previousResult = 0;
                rateChangeStart = float.MaxValue;
                QualitySettings.DecreaseLevel(true);

                int newLevel = QualitySettings.GetQualityLevel();
                if (newLevel == currentLevel)
                {
                    minumumLevel = currentLevel;
                }
                else
                {
                    previousChange = -1;

                    if (qualityLevelChanged != null)
                        qualityLevelChanged(this, currentLevel, newLevel);
                }

            }
            else if (toDo > 0 && currentLevel < maximumLevel && triedToGoUp < maximumTriesToGoUp)
            {
                previousResult = 0;
                rateChangeStart = float.MaxValue;

                QualitySettings.IncreaseLevel(true);

                int newLevel = QualitySettings.GetQualityLevel();
                if (newLevel == currentLevel)
                {
                    maximumLevel = currentLevel;
                }
                else 
                {
                    if(previousChange != 1)
                        triedToGoUp++;

                    previousChange = 1;

                    if (qualityLevelChanged != null)
                        qualityLevelChanged(this, currentLevel, newLevel);
                }

            }
        }
    }

    private float GetFramerate()
    {
        float lastValue = 1f / Time.unscaledDeltaTime;

        fpsQueue.Enqueue(lastValue);

        if (fpsQueue.Count == 30)
            fpsQueue.Dequeue();

        if (fpsQueue.Count > 1)
        {
            lastValue = 0;
            foreach (float v in fpsQueue)
            {
                lastValue += v;
            }
            lastValue /= fpsQueue.Count;
        }

        return lastValue;
    }
}

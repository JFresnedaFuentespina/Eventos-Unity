using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool timerEnabled = false;
    public float counter = 0f;
    public int tenths = 0;
    public int seconds = 0;
    public int minutes = 0;

    public Button playPauseButton;
    public Button resetButton;
    public TextMeshProUGUI tenthsText;
    public TextMeshProUGUI secondsText;
    public TextMeshProUGUI minutesText;


    void Start()
    {
        Reset();
        playPauseButton.onClick.AddListener(PlayPause);
        resetButton.onClick.AddListener(Reset);
    }

    // Update is called once per frame
    void Update()
    {
        if (timerEnabled)
        {
            counter += Time.deltaTime;
            if (counter > 0.1f)
            {
                counter = 0f;
                tenths++;
                tenthsText.text = tenths.ToString();
                if (Mathf.FloorToInt(tenths) > 9)
                {
                    tenths = 0;
                    seconds++;
                    secondsText.text = seconds.ToString("00");
                    if (seconds > 59)
                    {
                        seconds = 0;
                        minutes++;
                        minutesText.text = minutes.ToString("00");
                    }
                }
            }
        }

        // if (loadImageProgressEnabled)
        // {
        //     if (httpClient.downloadProgress < 1.0f)
        //     {
        //         Debug.Log(httpClient.downloadProgress * 100 + "% (Bytes downloaded: " + httpClient.downloadedBytes / 1024 + " KB");
        //         loadImageProgressSlider.value = httpClient.downloadProgress;
        //     }
        // }

    }

    public void PlayPause()
    {
        timerEnabled = !timerEnabled;
        if (timerEnabled)
        {
            playPauseButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Pause";
        }
        else
        {
            playPauseButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
        }
    }
    public void Reset()
    {
        tenths = 0;
        seconds = 0;
        minutes = 0;
        counter = 0f;
        timerEnabled = false;
        playPauseButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
        tenthsText.text = tenths.ToString("0");
        secondsText.text = seconds.ToString("00");
        minutesText.text = minutes.ToString("00");
    }
}

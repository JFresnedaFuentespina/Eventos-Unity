using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkImageManager : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Full URL to the texture file on the dimedianetapi (png/jpg/etc).")]
    public string textureUrl = "https://dimedianetapi9.azurewebsites.net/api/Files/457";

    [Header("UI Targets (assign at least one)")]
    public Image targetUIImage;
    [Header("Optional UI")]
    public Button downloadButton;
    public Slider loadImageProgressSlider;
    public GameObject waitLoadSpinner;
    private LoginManager loginManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loginManager = FindFirstObjectByType<LoginManager>();
        downloadButton.onClick.AddListener(LoadImageAsync);
    }
    private UnityWebRequest httpClient;
    private bool loadImageProgressEnabled = false;
    public void ApplyDownloadedBytes(byte[] textureBinary)
    {
        int configuredMax = 16384;

        if (textureBinary == null || textureBinary.Length == 0)
        {
            Debug.LogWarning("No texture bytes provided.");
            return;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(textureBinary))
        {
            Debug.LogError("Failed to load image from bytes.");
            UnityEngine.Object.Destroy(texture);
            return;
        }

        Debug.LogFormat("Loaded texture size: {0}x{1}. SystemInfo.maxTextureSize={2}", texture.width, texture.height, SystemInfo.maxTextureSize);

        int deviceMax = TextureUtils.DefaultMaxDimension;
        int maxDim = (configuredMax <= 0) ? deviceMax : Mathf.Min(configuredMax, deviceMax);

        // Try to downscale to maxDim. If scaling fails internally, TextureUtils will retry with smaller sizes.
        Texture2D safeTex = TextureUtils.EnsureTextureWithinMax(texture, maxDim, destroySourceIfScaled: true);

        Debug.LogFormat("Using texture size: {0}x{1}", safeTex.width, safeTex.height);

        if (safeTex == null)
        {
            Debug.LogError("No texture available after scaling.");
            return;
        }

        var sprite = Sprite.Create(safeTex, new Rect(0f, 0f, safeTex.width, safeTex.height), new Vector2(0.5f, 0.5f), 100f);
        targetUIImage.sprite = sprite;
        targetUIImage.preserveAspect = true;
    }

    public void LoadImageAsync()
    {
        // Cargar imagen con Coroutine
        StartCoroutine(LoadImageCoroutine());
    }

    private IEnumerator LoadImageCoroutine()
    {
        using (httpClient = new UnityWebRequest(textureUrl + "?container=dimedianetblobs"))
        {
            Debug.Log("Getting image...");
            httpClient.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(loginManager.bearerToken))
            {
                httpClient.SetRequestHeader("Authorization", "Bearer " + loginManager.bearerToken);
            }
            httpClient.SetRequestHeader("Accept", "/");
            yield return httpClient.SendWebRequest(); // Devuelve un IEnumerator

            Debug.Log("hpptClient.isDone = " + httpClient.isDone);

            if (httpClient.result == UnityWebRequest.Result.ConnectionError || httpClient.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(httpClient.error);
            }
            else
            {
                byte[] textureBinary = httpClient.downloadHandler.data;
                ApplyDownloadedBytes(textureBinary);
            }
        }
    }

    public void LoadImageBlocking()
    {
        using (httpClient = new UnityWebRequest(textureUrl + "?container=dimedianetblobs"))
        {
            Debug.Log("Getting image...");
            httpClient.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(loginManager.bearerToken))
            {
                httpClient.SetRequestHeader("Authorization", "Bearer " + loginManager.bearerToken);
            }
            httpClient.SetRequestHeader("Accept", "/");
            httpClient.SendWebRequest(); // blocking call

            while (!httpClient.isDone)
            {
                Thread.Sleep(1000);
            }

            Debug.Log("hpptClient.isDone = " + httpClient.isDone);

            if (httpClient.result == UnityWebRequest.Result.ConnectionError || httpClient.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(httpClient.error);
            }
            else
            {
                byte[] textureBinary = httpClient.downloadHandler.data;
                ApplyDownloadedBytes(textureBinary);
            }
        }
    }
}

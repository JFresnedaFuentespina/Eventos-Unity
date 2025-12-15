using System;
using UnityEngine;

/// <summary>
/// Defensive texture utilities: downscale to a configured max dimension while retrying smaller sizes
/// if RenderTexture allocation fails. Always logs sizes and device limits so you can debug "texture too large".
/// </summary>
public static class TextureUtils
{
    // A conservative upper bound to protect very old/fragile GPUs.
    // You can lower this (e.g. 4096) if you want to be more conservative.
    private const int ConservativeHardCap = 8192;

    public static int DefaultMaxDimension
    {
        get
        {
            int max = SystemInfo.maxTextureSize;
            if (max <= 0) max = 4096; // fallback
            // don't let device reported max exceed our conservative cap
            return Math.Min(max, ConservativeHardCap);
        }
    }

    /// <summary>
    /// Ensure texture fits within maxDimension (both width and height <= maxDimension).
    /// If maxDimension <= 0 the DefaultMaxDimension is used.
    /// Returns the original texture if it already fits, otherwise returns a new scaled Texture2D.
    /// If scaling fails, attempts progressively smaller scales until it succeeds or reaches 1x1.
    /// </summary>
    public static Texture2D EnsureTextureWithinMax(Texture2D source, int maxDimension = 0, bool destroySourceIfScaled = false)
    {
        if (source == null) return null;

        if (maxDimension <= 0) maxDimension = DefaultMaxDimension;
        // Enforce hard cap as extra protection
        maxDimension = Math.Min(maxDimension, ConservativeHardCap);

        Debug.LogFormat("TextureUtils: source {0}x{1}, requested maxDim {2}, deviceMax {3}",
            source.width, source.height, maxDimension, SystemInfo.maxTextureSize);

        if (source.width <= maxDimension && source.height <= maxDimension)
            return source;

        float scale = Mathf.Min((float)maxDimension / source.width, (float)maxDimension / source.height);
        int targetW = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
        int targetH = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

        // Try to scale; if it fails, retry halving the target until success.
        Texture2D scaled = SafeScaleRetry(source, targetW, targetH);
        if (scaled != source && destroySourceIfScaled && source != null)
        {
            UnityEngine.Object.Destroy(source);
        }
        return scaled;
    }

    /// <summary>
    /// Attempts to perform a RenderTexture-based scale. If allocation or operations fail, this will
    /// progressively reduce the target by half and retry until success or until minimum size reached.
    /// </summary>
    private static Texture2D SafeScaleRetry(Texture2D source, int startW, int startH)
    {
        int attemptW = startW;
        int attemptH = startH;

        while (attemptW >= 1 && attemptH >= 1)
        {
            try
            {
                Texture2D result = ScaleTextureWithRT(source, attemptW, attemptH);
                if (result != null)
                {
                    Debug.LogFormat("TextureUtils: scaled to {0}x{1}", attemptW, attemptH);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("TextureUtils: scale attempt {0}x{1} failed: {2}", attemptW, attemptH, ex.Message);
                // fall through to reduce size and retry
            }

            // reduce size (halve) and retry
            attemptW = Math.Max(1, attemptW / 2);
            attemptH = Math.Max(1, attemptH / 2);

            // Prevent infinite loops: if both are 1 and still failed, break
            if (attemptW == 1 && attemptH == 1)
                break;
        }

        Debug.LogWarning("TextureUtils: all scale attempts failed; returning original texture (may still be too large).");
        return source;
    }

    /// <summary>
    /// Performs the actual RenderTexture->Texture2D scaling.
    /// Throws exceptions if RenderTexture allocation fails.
    /// </summary>
    private static Texture2D ScaleTextureWithRT(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source == null) return null;

        // Defensive: ensure we don't ask for RT larger than SystemInfo.maxTextureSize
        int deviceMax = SystemInfo.maxTextureSize;
        if (deviceMax > 0)
        {
            if (targetWidth > deviceMax || targetHeight > deviceMax)
                throw new InvalidOperationException($"Requested RT {targetWidth}x{targetHeight} exceeds device max {deviceMax}");
        }

        RenderTexture rt = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            // This can throw or return null on allocation failure
            rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            if (rt == null)
                throw new InvalidOperationException("RenderTexture.GetTemporary returned null");

            // Blit and read back
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            tex.Apply(false, false); // keep readable for UI/Sprite creation
            return tex;
        }
        finally
        {
            RenderTexture.active = previous;
            if (rt != null)
                RenderTexture.ReleaseTemporary(rt);
        }
    }
}
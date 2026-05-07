using UnityEngine;

#if ZXING_AVAILABLE
using ZXing;
using ZXing.QrCode;
#endif

/// <summary>
/// Static helper to generate a QR code Texture2D from any string.
///
/// SETUP: Import ZXing.Net via NuGet or Unity Package Manager:
///   Window > Package Manager > + > Add package from git URL:
///   https://github.com/micjahn/ZXing.Net.git#unity
///
/// OR download ZXing.Net.dll and put it in Assets/Plugins/
/// Then add ZXING_AVAILABLE to Project Settings > Player > Scripting Define Symbols
///
/// If you prefer NO external library, use the FallbackQRTexture() method below
/// which calls a free QR API via UnityWebRequest.
/// </summary>
public static class QRCodeGenerator
{
    /// <summary>
    /// Generate a QR code texture. Uses ZXing if available, else falls back to online API.
    /// Call this from a Coroutine when using the fallback API path.
    /// </summary>
    public static Texture2D Generate(string content, int size = 256)
    {
#if ZXING_AVAILABLE
        return GenerateLocal(content, size);
#else
        return GeneratePlaceholder(size);
#endif
    }

#if ZXING_AVAILABLE
    static Texture2D GenerateLocal(string content, int size)
    {
        var writer = new BarcodeWriterTexture
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width  = size,
                Height = size,
                Margin = 1,
                // Use error correction level H for more reliable scanning
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H
            }
        };

        Texture2D texture = writer.Write(content);

        // Recolour: dark pixels → purple, light pixels → dark bg
        // to match your app's colour scheme
        Color bgColor   = new Color(0.10f, 0.08f, 0.21f, 1f);   // #1a1535
        Color qrColor   = new Color(0.71f, 0.54f, 1.00f, 1f);   // #b48aff purple

        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = pixels[i].r < 0.5f ? qrColor : bgColor;

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
#endif

    /// <summary>
    /// Plain grey checkerboard placeholder shown before ZXing is set up.
    /// Replace with your loading spinner if preferred.
    /// </summary>
    static Texture2D GeneratePlaceholder(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color a = new Color(0.10f, 0.08f, 0.21f, 1f);
        Color b = new Color(0.18f, 0.14f, 0.33f, 1f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, ((x / 16 + y / 16) % 2 == 0) ? a : b);
        tex.Apply();
        return tex;
    }
}


/// <summary>
/// Alternative: fetch QR as PNG from a free API (no ZXing needed).
/// Attach to a MonoBehaviour and call StartCoroutine(FetchQRCode(url, rawImage)).
/// </summary>
public class QRCodeFetcher : MonoBehaviour
{
    /// <summary>
    /// Downloads a QR code image from goqr.me API and assigns it to a RawImage.
    /// Usage: StartCoroutine(FetchQRCode("https://yourapp.com/join?ref=9TEAE", qrRawImage));
    /// </summary>
    public static System.Collections.IEnumerator FetchQRCode(
        string content,
        UnityEngine.UI.RawImage targetImage,
        int size = 200)
    {
        // Free QR API — replace with your own if you have a backend
        string encodedContent = UnityEngine.Networking.UnityWebRequest.EscapeURL(content);
        string apiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size={size}x{size}&data={encodedContent}&color=b48aff&bgcolor=1a1535&format=png";

        using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(apiUrl);
        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            targetImage.texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
        }
        else
        {
            Debug.LogWarning("QR fetch failed: " + req.error);
            // Show placeholder
            targetImage.texture = QRCodeGenerator.Generate(content, size);
        }
    }
}
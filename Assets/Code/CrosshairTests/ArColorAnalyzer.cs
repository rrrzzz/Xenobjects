using System.Collections;
using Code;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

public class ARColorAnalyzer : MonoBehaviour
{
    public ARMovementInteractionDataProvider dataProvider;
    public ARCameraManager cameraManager;
    public TMP_Text _colorText;
    public int regionSize = 30;
    
    [HideInInspector] public Color crossColor;
    [HideInInspector] public bool isProcessingImage = true;
    [HideInInspector] public bool isProcessingSuccess;
    
    private XRCpuImage.ConversionParams _conversionParams;
    
    private void Start()
    {
        dataProvider.SingleTouchEvent.AddListener(TryCaptureImage);
    }
 
    public void TryCaptureImage()
    {
        isProcessingImage = true;
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            StartCoroutine(DetermineColorCpuImage(cpuImage));
        }
        else
        {
            isProcessingImage = false;
            isProcessingSuccess = false;
            Debug.LogError("Failed to acquire latest CPU image.");
        }
    }

    IEnumerator DetermineColorCpuImage(XRCpuImage cpuImage)
    {
        int centerX = cpuImage.width / 2;
        int centerY = cpuImage.height / 2;
        
        RectInt centerRect = new RectInt(
            centerX - regionSize / 2, 
            centerY - regionSize / 2, 
            regionSize, 
            regionSize
        );

        // var conversionParams = new XRCpuImage.ConversionParams
        // {
        //     inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
        //     outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
        //     outputFormat = TextureFormat.RGBA32,
        //     transformation = XRCpuImage.Transformation.None
        // };
        
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = centerRect,
            outputDimensions = new Vector2Int(regionSize, regionSize),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };
        
        // Start async conversion
        var asyncConversion = cpuImage.ConvertAsync(conversionParams);
        // Wait for the conversion to complete or fail
        while (asyncConversion.status is XRCpuImage.AsyncConversionStatus.Processing or 
               XRCpuImage.AsyncConversionStatus.Pending)
        {
            yield return null; // Continue waiting until the conversion is complete
        }

        // Handle conversion failure
        if (asyncConversion.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            isProcessingImage = false;
            isProcessingSuccess = false;
            Debug.LogError("Failed to convert the CPU image.");
            cpuImage.Dispose();
            yield break;
        }

        // Dispose of the CPU image after the conversion is done
        cpuImage.Dispose();

        var data = asyncConversion.GetData<byte>();
        if (!data.IsCreated)
        {
            isProcessingImage = false;
            isProcessingSuccess = false;
            Debug.LogError("Failed to retrieve CPU image data.");
            asyncConversion.Dispose();
            yield break;
        }

        // Analyze the raw pixel data directly
        crossColor = DetermineDominantColorChatGPT(data);
        isProcessingImage = false;
        isProcessingSuccess = true;
        
        asyncConversion.Dispose();
    }
    
    private Color DetermineDominantColorChatGPT(NativeArray<byte> rawImageData)
{
    var colorDifferenceThreshold = 90;    // Threshold to skip near-grey pixels
    var colorDominanceThreshold = 200;     // Threshold to consider a color channel dominant
    var percentageThreshold = 80f;        // Percentage to decide if a color is dominant
    int redPixelsCount = 0;
    int bluePixelsCount = 0;
    int greenPixelsCount = 0;
    int totalPixels = 0;
    
    // Each pixel consists of 4 bytes: RGBA
    for (int i = 0; i < rawImageData.Length; i += 4)
    {
        byte r = rawImageData[i];     // Red channel (0-255)
        byte g = rawImageData[i + 1]; // Green channel (0-255)
        byte b = rawImageData[i + 2]; // Blue channel (0-255)

        totalPixels++;

        // Skip near-grey pixels
        if (Mathf.Abs(r - g) < colorDifferenceThreshold &&  
            Mathf.Abs(r - b) < colorDifferenceThreshold && 
            Mathf.Abs(g - b) < colorDifferenceThreshold)
        {
            continue;
        }

        // Determine if this pixel is close to a solid color
        if (r - g > colorDominanceThreshold && r - b > colorDominanceThreshold)
        {
            redPixelsCount++;
        }
        else if (g - r > colorDominanceThreshold && g - b > colorDominanceThreshold)
        {
            greenPixelsCount++;
        }
        else if (b - r > colorDominanceThreshold && b - g > colorDominanceThreshold)
        {
            bluePixelsCount++;
        }
    }

    // Calculate percentages
    float redPercentage = (float)redPixelsCount / totalPixels * 100f;
    float greenPercentage = (float)greenPixelsCount / totalPixels * 100f;
    float bluePercentage = (float)bluePixelsCount / totalPixels * 100f;

    // Determine the dominant color based on percentage
    string dominantColorName;
    Color dominantColor;
    
    if (redPercentage > percentageThreshold)
    {
        dominantColorName = "Red";
        dominantColor = Color.red;
    }
    else if (greenPercentage > percentageThreshold)
    {
        dominantColorName = "Green";
        dominantColor = Color.green;
    }
    else if (bluePercentage > percentageThreshold)
    {
        dominantColorName = "Blue";
        dominantColor = Color.blue;
    }
    else
    {
        dominantColorName = "Tie or None";
        dominantColor = Color.clear;
    }

    _colorText.text = $"Dominant color is: {dominantColorName}\n" +
                      $"Red Pixels: {redPixelsCount} ({redPercentage:F2}%)\n" +
                      $"Green Pixels: {greenPixelsCount} ({greenPercentage:F2}%)\n" +
                      $"Blue Pixels: {bluePixelsCount} ({bluePercentage:F2}%)";
    Debug.Log("Dominant color is: " + dominantColorName);
    
    return dominantColor;
}
    
    private Color DetermineDominantColor(NativeArray<byte> rawImageData)
    {
        var colorDifferenceThreshold = 90;

        float maxRed = 0, maxBlue = 0, maxGreen = 0;
        
        // Each pixel consists of 4 bytes: RGBA
        for (int i = 0; i < rawImageData.Length; i += 4)
        {
            byte r = rawImageData[i];     // Red channel (0-255)
            byte g = rawImageData[i + 1]; // Green channel (0-255)
            byte b = rawImageData[i + 2]; // Blue channel (0-255)

            if (Mathf.Abs(r - g) < colorDifferenceThreshold &&  
                Mathf.Abs(r - b) < colorDifferenceThreshold && 
                Mathf.Abs(g - b) < colorDifferenceThreshold)
            {
                // If the difference between the channels is small, skip this pixel
                continue;
            }

            maxRed = Mathf.Max(r, maxRed);
            maxBlue = Mathf.Max(b, maxBlue);
            maxGreen = Mathf.Max(g, maxGreen);
        }

        // Determine the dominant color
        string dominantColorName;
        Color dominantColor;
        
        if (maxRed > maxBlue && maxRed > maxGreen)
        {
            dominantColorName = "Red";
            dominantColor = Color.red;
        }
        else if (maxBlue > maxRed && maxBlue > maxGreen)
        {
            dominantColorName = "Blue";
            dominantColor = Color.blue;
        }
        else if (maxGreen > maxRed && maxGreen > maxBlue)
        {
            dominantColorName = "Green";
            dominantColor = Color.green;
        }
        else
        {
            dominantColorName = "Tie or None";
            dominantColor = Color.black;
        }
        
        _colorText.text = $"Dominant color is: {dominantColorName}\n" +
                         $"Max Red: {maxRed}\n" +
                         $"Max Blue: {maxBlue}\n" +
                         $"Max Green: {maxGreen}";
        Debug.Log("Dominant color is: " + dominantColorName);
        
        return dominantColor;
    }

    // Color analysis methods using 0-255 range
    bool IsRed(byte r, byte g, byte b)
    {
        // A pixel is considered red if the red component is significantly higher than green and blue
        return r > 20 && r > g && r > b;
    }

    bool IsBlue(byte r, byte g, byte b)
    {
        // A pixel is considered blue if the blue component is significantly higher than red and green
        return b > 20 && b > r && b > g;
    }

    bool IsGreen(byte r, byte g, byte b)
    {
        return g > 20 && g > b && g > r;
    }
}
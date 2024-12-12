using System.Collections;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

public class ARColorAnalyzer : MonoBehaviour
{
    public const int NearGreyThreshold = 30;
    public const int ColorDominanceThreshold = 10;
    public const float PercentagePixelCountThreshold = 10;
    
    public ARCameraManager cameraManager;
    public TMP_Text colorText;
    public int regionSize = 30;
    
    [HideInInspector] public Color dominantColor;
    [HideInInspector] public bool isProcessingImage;
    [HideInInspector] public bool isProcessingSuccess;
    
    private XRCpuImage.ConversionParams _conversionParams;

    private void Start()
    {
        // dataProvider.SingleTouchEvent.AddListener(TryCaptureImage);
    }
    
    public void TryCaptureImage()
    {
        if (isProcessingImage)
        {
            return;
        }
        
        isProcessingImage = true;
        isProcessingSuccess = false;
        
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            StartCoroutine(DetermineColorCpuImage(cpuImage));
        }
        else
        {
            isProcessingImage = false;
            Debug.LogError("Failed to acquire latest CPU image.");
        }
    }

    private IEnumerator DetermineColorCpuImage(XRCpuImage cpuImage)
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
            Debug.LogError("Failed to retrieve CPU image data.");
            asyncConversion.Dispose();
            yield break;
        }

        // Analyze the raw pixel data directly
        dominantColor = DetermineDominantColorChatGpt(data);
        isProcessingSuccess = true;
        isProcessingImage = false;
        
        asyncConversion.Dispose();
    }
    
    private Color DetermineDominantColorChatGpt(NativeArray<byte> rawImageData)
    {
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
    
            // Skip near-grey pixels
            if (Mathf.Abs(r - g) < NearGreyThreshold &&  
                Mathf.Abs(r - b) < NearGreyThreshold && 
                Mathf.Abs(g - b) < NearGreyThreshold)
            {
                continue;
            }

            var isColorPixel = true;


            if (r - g > ColorDominanceThreshold && r - b > ColorDominanceThreshold)
            {
                redPixelsCount++;
            }
            else if (g - r > ColorDominanceThreshold && g - b > ColorDominanceThreshold)
            {
                greenPixelsCount++;
            }
            else if (b - r > ColorDominanceThreshold && b - g > ColorDominanceThreshold)
            {
                bluePixelsCount++;
            }
            else
            {
                isColorPixel = false;
            }
            
            // No color dominance threshold
            // {
            //     var rMinDif = Mathf.Min(r - g, r - b);
            //     var gMinDif = Mathf.Min(g - r, g - b);
            //     var bMinDif = Mathf.Min(b - r, b - g);
            //     
            //     if (rMinDif > gMinDif && rMinDif > bMinDif)
            //     {
            //         redPixelsCount++;
            //     }
            //     else if (gMinDif > rMinDif && gMinDif > bMinDif)
            //     {
            //         greenPixelsCount++;
            //     }
            //     else if (bMinDif > rMinDif && bMinDif > gMinDif)
            //     {
            //         bluePixelsCount++;
            //     }
            // }
            
            if (isColorPixel)
            {
                totalPixels++;
            }
        }
    
        totalPixels = totalPixels == 0 ? 1 : totalPixels;
        
        // Calculate percentages
        float redPercentage = (float)redPixelsCount / totalPixels * 100f;
        float greenPercentage = (float)greenPixelsCount / totalPixels * 100f;
        float bluePercentage = (float)bluePixelsCount / totalPixels * 100f;
    
        // Determine the dominant color based on percentage
        string dominantColorName;
        Color dominantColor;
        
        if (redPercentage > greenPercentage && redPercentage > bluePercentage && redPercentage > PercentagePixelCountThreshold)
        {
            dominantColorName = "Red";
            dominantColor = Color.red;
        }
        else if (greenPercentage > redPercentage && greenPercentage > bluePercentage && greenPercentage > PercentagePixelCountThreshold)
        {
            dominantColorName = "Green";
            dominantColor = Color.green;
        }
        else if (bluePercentage > greenPercentage && bluePercentage > redPercentage && bluePercentage > PercentagePixelCountThreshold)
        {
            dominantColorName = "Blue";
            dominantColor = Color.blue;
        }
        else
        {
            dominantColorName = "Tie or None";
            dominantColor = Color.clear;
        }
    
        if (colorText)
        {
            colorText.text = $"Dominant color is: {dominantColorName}\n" +
                              $"Red Pixels: {redPixelsCount} ({redPercentage:F2}%)\n" +
                              $"Green Pixels: {greenPixelsCount} ({greenPercentage:F2}%)\n" +
                              $"Blue Pixels: {bluePixelsCount} ({bluePercentage:F2}%)";
        }
    
        Debug.Log("Dominant color is: " + dominantColorName);
        
        return dominantColor;
    }
}
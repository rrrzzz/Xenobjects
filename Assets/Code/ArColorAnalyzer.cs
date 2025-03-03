using System.Collections;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

public class ARColorAnalyzer : MonoBehaviour
{
    public int nearGreyThreshold = 20;
    public int colorDominanceThreshold = 20;
    public float percentagePixelCountThreshold = 30;
    public float  dominantPercentageCrossCenteredCheck = 5;
    public float crossHorVertErrorMargin = 4;
    public ARCameraManager cameraManager;
    public TMP_Text colorText;
    public float regionSizeWidthPercentage = .5f;
    
    [HideInInspector] public Color dominantColor;
    [HideInInspector] public bool isProcessingImage;
    [HideInInspector] public bool isProcessingSuccess;
    [HideInInspector] public bool isRewritingColorOnCrossCheck = true;
    
    private XRCpuImage.ConversionParams _conversionParams;
    private string _currentSuccessLine;
    private int _regionSize;

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
    
    public void TryDetermineCenteredAtCross()
    {
        if (isProcessingImage)
        {
            return;
        }
        
        isProcessingImage = true;
        isProcessingSuccess = false;
        
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            StartCoroutine(DetermineCrossCenterImage(cpuImage));
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
        
        _regionSize = Mathf.RoundToInt(cpuImage.width * regionSizeWidthPercentage);
        
        RectInt centerRect = new RectInt(
            centerX - _regionSize / 2, 
            centerY - _regionSize / 2, 
            _regionSize, 
            _regionSize
        );
        
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = centerRect,
            outputDimensions = new Vector2Int(_regionSize, _regionSize),
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
    
    private IEnumerator DetermineCrossCenterImage(XRCpuImage cpuImage)
    {
        int centerX = cpuImage.width / 2;
        int centerY = cpuImage.height / 2;
        
        _regionSize = Mathf.RoundToInt(cpuImage.width * regionSizeWidthPercentage);
        
        RectInt centerRect = new RectInt(
            centerX - _regionSize / 2, 
            centerY - _regionSize / 2, 
            _regionSize, 
            _regionSize
        );
        
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = centerRect,
            outputDimensions = new Vector2Int(_regionSize, _regionSize),
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
        var isCentered = CheckIfCenteredOnCross(data);
        isProcessingSuccess = true;
        isProcessingImage = false;
        
        if (!isCentered && isRewritingColorOnCrossCheck)
        { 
            dominantColor = Color.clear;
        }
        
        asyncConversion.Dispose();
    }
    
    private bool IsCrossPixel(byte r, byte g, byte b)
    {
        if (Mathf.Abs(r - g) < nearGreyThreshold &&  
            Mathf.Abs(r - b) < nearGreyThreshold && 
            Mathf.Abs(g - b) < nearGreyThreshold)
        {
            return false;
        }
        
        Color currentPixel;

        if (r - g >= colorDominanceThreshold && r - b >= colorDominanceThreshold)
        {
            currentPixel = Color.red;
        }
        else if (g - r >= colorDominanceThreshold && g - b >= colorDominanceThreshold)
        {
            currentPixel = Color.green;
        }
        else if (b - r >= colorDominanceThreshold && b - g >= colorDominanceThreshold)
        {
            currentPixel = Color.blue;
        }
        else
        {
            currentPixel = Color.clear;
        }

        return currentPixel == dominantColor;
    }
    
    private int CountPixelsInDirection(
        NativeArray<byte> imageData,
        int startX, int startY, int dx, int dy)
    {
        int count = 0;
        int x = startX;
        int y = startY;
        while (x >= 0 && x < _regionSize && y >= 0 && y < _regionSize)
        {
            int index = (y * _regionSize + x) * 4; // RGBA
            byte r = imageData[index + 0];
            byte g = imageData[index + 1];
            byte b = imageData[index + 2];
            
            if (!IsCrossPixel(r, g, b)) 
                break;
            
            count++;
            x += dx;
            y += dy;
        }
        return count;
    }
    
    private bool CheckIfCenteredOnCross(NativeArray<byte> imageData)
    {
        // Center of the image
        int centerX = _regionSize / 2;
        int centerY = _regionSize / 2;
    
        // 1) Check if center pixel is the cross color
        int centerIndex = (centerY * _regionSize + centerX) * 4;
        byte rc = imageData[centerIndex + 0];
        byte gc = imageData[centerIndex + 1];
        byte bc = imageData[centerIndex + 2];
    
        if (!IsCrossPixel(rc, gc, bc))
        {
            PrintDebugWithSuccessLineIntact($"Center pixel is not cross color; " +
                                            "Pixel color: (" + rc + ", " + gc + ", " + bc + "), " +
                                            $"Dominant color: {dominantColor}");
            Debug.Log("Center pixel is not cross color; cross isn't centered here.");
            return false;
        }
    
        // 2) Count cross-colored pixels in each direction
        int upCount    = CountPixelsInDirection(imageData, centerX, centerY, 0, -1);
        int downCount  = CountPixelsInDirection(imageData, centerX, centerY, 0,  1);
        int leftCount  = CountPixelsInDirection(imageData, centerX, centerY, -1, 0);
        int rightCount = CountPixelsInDirection(imageData, centerX, centerY,  1, 0);
    
        // Because we start counting from the center each time, each direction includes the center pixel.
        // If you want to exclude the center pixel from each direction’s length, just subtract 1 from these counts.
    
        // 3) Decide on a tolerance for “equal” lengths (in case of minor variations)
        int verticalDifference = Mathf.Abs(upCount - downCount);
        int horizontalDifference = Mathf.Abs(leftCount - rightCount);
    
        Debug.Log($"Up: {upCount}, Down: {downCount}, Left: {leftCount}, Right: {rightCount}");
    
        if (verticalDifference <= crossHorVertErrorMargin && horizontalDifference <= crossHorVertErrorMargin)
        {
            Debug.Log("Camera is looking directly at the cross center!");
        }
        else
        {
            Debug.Log("Camera is off-center from the cross.");
        }
        
        var isCenteredAtCross = verticalDifference <= crossHorVertErrorMargin && 
                         horizontalDifference <= crossHorVertErrorMargin;
        
        PrintDebugWithSuccessLineIntact($"Is centered at cross : {isCenteredAtCross}, " +
                                        $"vertical: {verticalDifference}, horizontal: {horizontalDifference}"); 

        if (!isCenteredAtCross) return false;
        
        var percentage = DetermineDominantColorPercentage(imageData);

        if (Mathf.Approximately(percentage, -1))
        {
            PrintDebugWithSuccessLineIntact($"Is centered at cross : true, percentage: {percentage}, " +
                                            $"vertical: {verticalDifference}, horizontal: {horizontalDifference}"); 
            return false;

        }

        if (percentage <= dominantPercentageCrossCenteredCheck)
        {
            _currentSuccessLine = "Is lines aligned : true; dominant percentage: " + percentage + "% at " + Time.realtimeSinceStartup;
            colorText.text = _currentSuccessLine;
            return true;
        }
        
        PrintDebugWithSuccessLineIntact("Is lines aligned : false; dominant percentage: " + percentage + "%");
        return false;
    }

    private void PrintDebugWithSuccessLineIntact(string newLine)
    {
        const string successLine = "Is lines aligned : true; dominant percentage:";
        
        var isTextContainsSuccessLine = colorText.text.Contains(successLine);
        if (!isTextContainsSuccessLine)
        {
            colorText.text = newLine;
            return;
        }
        
        colorText.text = _currentSuccessLine + "\n" + newLine;
    }
    
    private float DetermineDominantColorPercentage(NativeArray<byte> rawImageData)
    {
        int redPixelsCount = 0;
        int bluePixelsCount = 0;
        int greenPixelsCount = 0;
        int totalColoredPixels = 0;
        int totalPixels = 0;
        
        // Each pixel consists of 4 bytes: RGBA
        for (int i = 0; i < rawImageData.Length; i += 4)
        {
            byte r = rawImageData[i];     // Red channel (0-255)
            byte g = rawImageData[i + 1]; // Green channel (0-255)
            byte b = rawImageData[i + 2]; // Blue channel (0-255)

            totalPixels++;
            
            if (Mathf.Abs(r - g) < nearGreyThreshold &&  
                Mathf.Abs(r - b) < nearGreyThreshold && 
                Mathf.Abs(g - b) < nearGreyThreshold)
            {
                continue;
            }
            
            var isColorPixel = true;

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
            else
            {
                isColorPixel = false;
            }

            if (isColorPixel)
            {
                totalColoredPixels++;
            }
        }
    
        totalColoredPixels = totalColoredPixels == 0 ? 1 : totalColoredPixels;
        
        // Calculate percentages
        float redPercentage = (float)redPixelsCount / totalColoredPixels * 100f;
        float greenPercentage = (float)greenPixelsCount / totalColoredPixels * 100f;
        float bluePercentage = (float)bluePixelsCount / totalColoredPixels * 100f;
        
        if (redPercentage > greenPercentage && redPercentage > bluePercentage && redPercentage > percentagePixelCountThreshold)
        {
            if (dominantColor != Color.red || totalPixels < 1)
            {
                return -1;
            }

            return (float)redPixelsCount / totalPixels * 100f;
        }
        
        if (greenPercentage > redPercentage && greenPercentage > bluePercentage && greenPercentage > percentagePixelCountThreshold)
        {
            if (dominantColor != Color.green || totalPixels < 1)
            {
                return -1;
            }
            
            return (float)greenPixelsCount / totalPixels * 100f;
        } 
        
        if (bluePercentage > greenPercentage && bluePercentage > redPercentage && bluePercentage > percentagePixelCountThreshold)
        {
            if (dominantColor != Color.blue || totalPixels < 1)
            {
                return -1;
            }

            return (float)bluePixelsCount / totalPixels * 100f;
        }
        
        return -1;
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

            if (Mathf.Abs(r - g) < nearGreyThreshold &&  
                Mathf.Abs(r - b) < nearGreyThreshold && 
                Mathf.Abs(g - b) < nearGreyThreshold)
            {
                continue;
            }
            
            var isColorPixel = true;

            if (r - g >= colorDominanceThreshold && r - b >= colorDominanceThreshold)
            {
                redPixelsCount++;
            }
            else if (g - r >= colorDominanceThreshold && g - b >= colorDominanceThreshold)
            {
                greenPixelsCount++;
            }
            else if (b - r >= colorDominanceThreshold && b - g >= colorDominanceThreshold)
            {
                bluePixelsCount++;
            }
            else
            {
                isColorPixel = false;
            }

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
        Color foundDominantColor;
        
        if (redPercentage > greenPercentage && redPercentage > bluePercentage && redPercentage > percentagePixelCountThreshold)
        {
            dominantColorName = "Red";
            foundDominantColor = Color.red;
        }
        else if (greenPercentage > redPercentage && greenPercentage > bluePercentage && greenPercentage > percentagePixelCountThreshold)
        {
            dominantColorName = "Green";
            foundDominantColor = Color.green;
        }
        else if (bluePercentage > greenPercentage && bluePercentage > redPercentage && bluePercentage > percentagePixelCountThreshold)
        {
            dominantColorName = "Blue";
            foundDominantColor = Color.blue;
        }
        else
        {
            dominantColorName = "Tie or None";
            foundDominantColor = Color.clear;
        }
    
        if (colorText)
        {
            var text = $"Dominant color is: {dominantColorName}\n" +
                       $"Red Pixels: {redPixelsCount} ({redPercentage:F2}%)\n" +
                       $"Green Pixels: {greenPixelsCount} ({greenPercentage:F2}%)\n" +
                       $"Blue Pixels: {bluePixelsCount} ({bluePercentage:F2}%)";
            PrintDebugWithSuccessLineIntact(text);
        }
    
        Debug.Log("Dominant color is: " + dominantColorName);
        
        return foundDominantColor;
    }
}
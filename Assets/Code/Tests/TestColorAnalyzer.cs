using System.Collections;
using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;

public class TestColorAnalyzer : MonoBehaviour
{
    public const int NearGreyThreshold = 30;
    public const int ColorDominanceThreshold = 10;
    public const float PercentagePixelCountThreshold = 10;
    public const int CheckRegion = 50;
    
    public TMP_Text colorText;
    public float sidePixelsCrossErrorThreshold = 2;
    [HideInInspector] public Color dominantColor;
    [HideInInspector] public bool isProcessingImage;
    [HideInInspector] public bool isProcessingSuccess;
    
    public void TryCaptureImage()
    {
        if (isProcessingImage)
        {
            return;
        }
        
        isProcessingImage = true;
        isProcessingSuccess = false;
        
        StartCoroutine(DetermineColorScreenshot());
    }
    
    public void TryDetermineCenteredAtCross()
    {
        if (isProcessingImage)
        {
            return;
        }
        
        isProcessingImage = true;
        isProcessingSuccess = false;
        
        StartCoroutine(DetermineCrossCenterScreenshot());
    }

    private IEnumerator DetermineColorScreenshot()
    {
        dominantColor = DetermineDominantColorChatGpt();
        isProcessingSuccess = true;
        isProcessingImage = false;
        yield return null;
    }
    
    private IEnumerator DetermineCrossCenterScreenshot()
    {
        // Capture the screen as a Texture2D
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        // Check if the camera is centered on a cross of (for example) red color
        var isSeeingCross = CheckIfCenteredOnCross(texture);

        if (colorText != null)
        {
            colorText.text = isSeeingCross 
                ? "Camera is centered on the cross!" 
                : "Camera is off-center from the cross.";
        }

        if (!isSeeingCross)
        {
            dominantColor = Color.clear;
        }
        
        Debug.Log("Is cross centered: " + isSeeingCross);
        
        isProcessingImage = false;
        isProcessingSuccess = true;
        
        yield return null;
    }

    /// <summary>
    /// Determines if the center of the screenshot is looking at a red cross
    /// by verifying the center pixel is red, then counting how far we can go
    /// in each direction with red pixels.
    /// </summary>
    private bool CheckIfCenteredOnCross(Texture2D tex)
    {
        // Get all pixels once, for speed
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;
        
        // Find center pixel
        int centerX = width / 2;
        int centerY = height / 2;

        // Get index of the center pixel in the 1D array
        int centerIndex = (centerY * width + centerX);

        // Is the center pixel red?
        if (!IsCrossPixel(pixels[centerIndex])) 
        {
            return false;
        }

        // Count consecutive cross pixels going up, down, left, and right.
        int upCount    = CountPixelsInDirection(pixels, width, height, centerX, centerY,  0, -1);
        int downCount  = CountPixelsInDirection(pixels, width, height, centerX, centerY,  0,  1);
        int leftCount  = CountPixelsInDirection(pixels, width, height, centerX, centerY, -1,  0);
        int rightCount = CountPixelsInDirection(pixels, width, height, centerX, centerY,  1,  0);
        //
        // int upCount    = CountPixelsInDirection(pixels, CheckRegion, CheckRegion, centerX, centerY,  0, -1);
        // int downCount  = CountPixelsInDirection(pixels, CheckRegion, CheckRegion, centerX, centerY,  0,  1);
        // int leftCount  = CountPixelsInDirection(pixels, CheckRegion, CheckRegion, centerX, centerY, -1,  0);
        // int rightCount = CountPixelsInDirection(pixels, CheckRegion, CheckRegion, centerX, centerY,  1,  0);

        // Each direction includes the center pixel, so the total length
        // of each arm is count - 1 if you want to exclude center pixel from each direction.
        
        Debug.Log($"Up: {upCount}, Down: {downCount}, Left: {leftCount}, Right: {rightCount}");

        // Decide how close up/down must match and left/right must match
        int verticalDifference = Mathf.Abs(upCount - downCount);
        int horizontalDifference = Mathf.Abs(leftCount - rightCount);

        // If they're nearly equal, assume the cross is centered
        bool isCentered = verticalDifference <= sidePixelsCrossErrorThreshold && 
                          horizontalDifference <= sidePixelsCrossErrorThreshold;
        return isCentered;
    }

    /// <summary>
    /// Counts how many cross-colored pixels we can move through in (dx, dy)
    /// from the starting point (startX, startY), including the starting pixel.
    /// </summary>
    private int CountPixelsInDirection(Color32[] pixels, int width, int height,
                                       int startX, int startY, int dx, int dy)
    {
        int count = 0;
        int x = startX;
        int y = startY;
        var steps = 0;
        
        while (x >= 0 && x < width && y >= 0 && y < height && steps < CheckRegion)
        {
            int idx = y * width + x;
            if (!IsCrossPixel(pixels[idx])) break;

            count++;
            x += dx;
            y += dy;
            steps++;
        }
        return count;
    }
    
    private bool IsCrossPixel(Color32 c)
    {
        float r = c.r; // Red channel (0-255)
        float g = c.g; // Green channel (0-255)
        float b = c.b; // Blue channel (0-255)
    
        // Skip near-grey pixels
        if (IsNearGrey(c))
        {
            return false;
        }

        Color currentPixel;
        if (r - g > ColorDominanceThreshold && r - b > ColorDominanceThreshold)
        {
            currentPixel = Color.red;
        }
        else if (g - r > ColorDominanceThreshold && g - b > ColorDominanceThreshold)
        {
            currentPixel = Color.green;
        }
        else if (b - r > ColorDominanceThreshold && b - g > ColorDominanceThreshold)
        {
            currentPixel = Color.blue;
        }
        else
        {
            currentPixel = Color.clear;
        }

        return currentPixel == dominantColor;
    }

    /// <summary>
    /// Optional helper to skip near-grey pixels.
    /// </summary>
    private bool IsNearGrey(Color32 c)
    {
        return Mathf.Abs(c.r - c.g) < NearGreyThreshold &&
               Mathf.Abs(c.r - c.b) < NearGreyThreshold &&
               Mathf.Abs(c.g - c.b) < NearGreyThreshold;
    }
    
    private Color DetermineDominantColorChatGpt()
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        
        int redPixelsCount = 0;
        int bluePixelsCount = 0;
        int greenPixelsCount = 0;
        int totalPixels = 0;
        
        // Each pixel consists of 4 bytes: RGBA
        foreach (var pixel in texture.GetPixels32())
        {
            float r = pixel.r;     // Red channel (0-255)
            float g = pixel.g; // Green channel (0-255)
            float b = pixel.b; // Blue channel (0-255)
    
            // Skip near-grey pixels
            if (IsNearGrey(pixel))
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
using UnityEngine;

public class CrosshairColorDetector : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            var blackPixelsCount = 0;
            var bluePixelsCount = 0;
            var redPixelsCount = 0;
            foreach (var pixel in tex.GetPixels())
            {
                if (IsRed(pixel))
                    redPixelsCount++;
                else if (IsBlue(pixel))
                    bluePixelsCount++;
                else if (IsBlack(pixel))
                    blackPixelsCount++;
            }
            
            string dominantColor;
            if (redPixelsCount > bluePixelsCount && redPixelsCount > blackPixelsCount)
                dominantColor = "Red";
            else if (bluePixelsCount > redPixelsCount && bluePixelsCount > blackPixelsCount)
                dominantColor = "Blue";
            else if (blackPixelsCount > redPixelsCount && blackPixelsCount > bluePixelsCount)
                dominantColor = "Black";
            else
                dominantColor = "Tie or None";

            Debug.Log("Dominant color is: " + dominantColor);
        }
    }
    
    private bool IsRed(Color color)
    {
        // A pixel is considered red if the red component is significantly higher than green and blue
        return color.r > 0.5f && color.g < 0.3f && color.b < 0.3f;
    }

    private bool IsBlue(Color color)
    {
        // A pixel is considered blue if the blue component is significantly higher than red and green
        return color.b > 0.5f && color.r < 0.3f && color.g < 0.3f;
    }

    private bool IsBlack(Color color)
    {
        // A pixel is considered black if all RGB components are low
        return color.r < 0.1f && color.g < 0.1f && color.b < 0.1f;
    }
}

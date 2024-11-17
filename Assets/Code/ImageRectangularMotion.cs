using System;
using UnityEngine;

public class ImageRectangularMotion : MonoBehaviour
{
    private const float YtoXRatio = 1.423f;
    private const float XOffsetRatio = 0.232f;
    private const float XSpeedRatio = 0.634f;
    private const float ScreenPercentageImageWidth = 0.3f;
    
    private RectTransform _imageRect; // Assign the image RectTransform in the Inspector
    private float _speed; // Movement speed in units per second
    private Vector2 _centerOffset; // Distance from the screen center
    private Vector2[] _rectangleCorners; // The corners of the rectangle
    private int _currentCorner; // Tracks the current corner to move to
    private bool _movingToUpperLeft = true; // Indicates initial movement to upper-left corner
    private Vector2 _centerPosition;

    void Awake()
    {
        _imageRect = GetComponent<RectTransform>();
        _centerOffset = Vector2.one * (Screen.width * XOffsetRatio);
        _speed = Screen.width * XSpeedRatio;
        
        Vector2 imageSize = new Vector2(Screen.width * ScreenPercentageImageWidth, 
            Screen.width * YtoXRatio * ScreenPercentageImageWidth);
        _imageRect.sizeDelta = imageSize;

        _rectangleCorners = new []
        {
            new Vector2(_centerPosition.x - _centerOffset.x, _centerPosition.y + _centerOffset.y), // Upper-left
            new Vector2(_centerPosition.x + _centerOffset.x, _centerPosition.y + _centerOffset.y), // Upper-right
            new Vector2(_centerPosition.x + _centerOffset.x, _centerPosition.y - _centerOffset.y), // Bottom-right
            new Vector2(_centerPosition.x - _centerOffset.x, _centerPosition.y - _centerOffset.y)  // Bottom-left
        };
    }

    private void OnEnable()
    {
        _movingToUpperLeft = true;
        _centerPosition = Vector2.zero;
        _imageRect.anchoredPosition = _centerPosition;
    }

    void Update()
    {
        if (_movingToUpperLeft)
        {
            // Move towards the upper-left corner
            MoveTowards(_rectangleCorners[0]);

            if (HasReachedTarget(_rectangleCorners[0]))
            {
                _movingToUpperLeft = false; // Switch to rectangle motion
                _currentCorner = 1; // Start at the upper-right corner
            }
        }
        else
        {
            // Move along the rectangle corners
            MoveTowards(_rectangleCorners[_currentCorner]);

            if (HasReachedTarget(_rectangleCorners[_currentCorner]))
            {
                // Proceed to the next corner
                _currentCorner = (_currentCorner + 1) % _rectangleCorners.Length;
            }
        }
    }

    private void MoveTowards(Vector2 target)
    {
        // Smoothly move the image towards the target
        _imageRect.anchoredPosition = Vector2.MoveTowards(
            _imageRect.anchoredPosition, 
            target, 
            _speed * Time.deltaTime
        );
    }

    private bool HasReachedTarget(Vector2 target)
    {
        // Check if the image is close enough to the target
        return Vector2.Distance(_imageRect.anchoredPosition, target) < 0.1f;
    }
    
    // private bool TryGetParamValue(out float[] val)
    // {
    //     val = new float[]{0,0,0,0,0};
    //
    //     if (valuesField)
    //     {
    //         // Split the string by commas
    //         string[] parts = valuesField.text.Split(',');
    //         if (string.IsNullOrEmpty(parts[0]))
    //         {
    //             return false;
    //         }
    //         for (int i = 0; i < parts.Length; i++)
    //         {
    //             if (string.IsNullOrEmpty(parts[i]))
    //             {
    //                 continue;
    //             }
    //             val[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
    //         }
    //             
    //         return true;
    //     }
    //         
    //     return false;
    // }
}
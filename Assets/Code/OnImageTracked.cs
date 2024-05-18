using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class OnImageTracked : MonoBehaviour
{
    public float tiltThreshold = 20f;
    public float movementThreshold = 0.2f;
    public float movementTrackingInterval = 1f;
    public TMP_Text cameraPosRotTxt;
    public TMP_Text distanceToObjTxt;
    public TMP_Text objPosTxt;
    public TMP_Text imagePosRotTxt;
    public TMP_Text titlTxt;
    public TMP_Text movedTxt;
    public ARTrackedImageManager trackedImgManager;
    public GameObject contentPrefab;
    
    private GameObject _spawnedContent;
    private ARTrackedImage _trackedImg;
    private Transform _camTr;
    private Vector3 _prevPosition;
    private float _prevTime;

    void Start()
    {
        // Ensure the gyroscope is enabled
        Input.gyro.enabled = true;
        _camTr = Camera.main.transform;
        _prevPosition = _camTr.position;
    }
    
    private void OnEnable() => trackedImgManager.trackedImagesChanged += OnChanged;

    private void OnDisable() => trackedImgManager.trackedImagesChanged -= OnChanged;
    
    private void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        if (eventArgs.added.Count != 0)
        {
            _trackedImg = eventArgs.added[0];
            _spawnedContent = Instantiate(contentPrefab, _trackedImg.transform);
        }

        if (eventArgs.updated.Count == 0) return;
        
        _spawnedContent.transform.position = _trackedImg.transform.position;
        _spawnedContent.transform.rotation = _trackedImg.transform.rotation;
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup - _prevTime > movementTrackingInterval)
        {
            var movementDist = Vector3.Distance(_camTr.position, _prevPosition);
            movedTxt.text = movementDist > movementThreshold ? "KEEP RUNNING FROM YOURSELF" : "MOVE YOUR ASS, BOI!";
            _prevPosition = _camTr.position;
            _prevTime = Time.realtimeSinceStartup;
        }
        
        var camRot = _camTr.rotation.eulerAngles;
        var rotNormalized = NormalizeAngle(camRot.z) * -1;
        if (rotNormalized > tiltThreshold || rotNormalized < -tiltThreshold)
        {
            titlTxt.text = "Phone tilted!";
        }
        else
        {
            titlTxt.text = "Not tilted";
        }
        
        cameraPosRotTxt.text = $"Phone pos: {_camTr.position}\nPhone rot: {camRot}\n";

        if (_spawnedContent)
        {
            objPosTxt.text = "obj pos: " +  _spawnedContent.transform.position;
            distanceToObjTxt.text = "Dist to obj: " + Vector3.Distance(_camTr.position, 
                _spawnedContent.transform.position);
        }
        
        if (_trackedImg)
        {
            imagePosRotTxt.text = "img pos: " +  _trackedImg.transform.position +"\nimg rot:" +  
                               _trackedImg.transform.rotation;
        }
        
        // if (Input.gyro.enabled)
        // {
        //     // Get the device rotation
        //     Quaternion deviceRotation = GyroToUnity(Input.gyro.attitude);
        //     Vector3 deviceEulerAngles = deviceRotation.eulerAngles;
        //
        //     // Detect tilt around the Z-axis
        //     float zTilt = NormalizeAngle(deviceEulerAngles.z);
        //
        //     if (Mathf.Abs(zTilt) > tiltThreshold)
        //     {
        //         // titlTxt.text = "Phone tilt is: " + zTilt;
        //         // Add your tilt handling logic here
        //     }
        // }
    }
    
    // Convert the gyro rotation to Unity's coordinate system
    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    // Normalize the angle to the range of -180 to 180 degrees
    private static float NormalizeAngle(float angle)
    {
        if (angle > 180)
            angle -= 360;
        return angle;
    }

    // private IEnumerator ExplodeAfterDelay()
    // {
    //     yield return new WaitForSeconds(3);
    //     spawnAndExplode.SetActive(true);
    // }
}

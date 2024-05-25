using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float zoomSpeed = 2f;
    public float moveSpeed = 10f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Confined; // Ограничить курсор в пределах окна игры
    }

    void Update()
    {
        HandleMouseLook();
        HandleZoom();
        HandleCameraMove();
    }

    void HandleMouseLook()
    {
        if (Input.GetMouseButton(0)) // Left mouse button
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            yRotation += mouseX;

            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }

    void HandleZoom()
    {
        if (cam != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            cam.fieldOfView -= scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 15f, 90f);
        }
    }

    void HandleCameraMove()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button (wheel button)
        {
            float moveX = Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float moveY = Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;

            cam.transform.Translate(-moveX, -moveY, 0, Space.Self);
        }
    }
}

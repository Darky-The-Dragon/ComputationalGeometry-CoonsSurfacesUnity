using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Movement")] public float moveSpeed = 10f;

    public float fastMoveMultiplier = 3f;
    public float verticalSpeed = 6f;

    [Header("Mouse Look")] public float mouseSensitivity = 2f;

    public float webGLSensitivityMultiplier = 0.35f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public bool invertY;

    [Header("Zoom")] public float zoomSpeed = 20f;

    [Header("Mode Toggle")] public KeyCode toggleCameraModeKey = KeyCode.C;

    public bool startInCameraMode;

    [Header("State (Read Only)")] [SerializeField]
    private bool cameraInputEnabled;

    private float pitch;
    private float runtimeMouseSensitivity;
    private float yaw;

    private void Start()
    {
        var angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        runtimeMouseSensitivity = mouseSensitivity;

#if UNITY_WEBGL && !UNITY_EDITOR
        runtimeMouseSensitivity *= webGLSensitivityMultiplier;
#endif

        SetCameraInputEnabled(startInCameraMode);
    }

    private void Update()
    {
        HandleModeToggle();

        if (!cameraInputEnabled)
            return;

        HandleMouseLook();
        HandleMovement();
        HandleZoom();
    }

    private void HandleModeToggle()
    {
        if (Input.GetKeyDown(toggleCameraModeKey))
            SetCameraInputEnabled(!cameraInputEnabled);
    }

    private void SetCameraInputEnabled(bool enabled)
    {
        cameraInputEnabled = enabled;

        if (cameraInputEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleMouseLook()
    {
        var mouseX = Input.GetAxisRaw("Mouse X") * runtimeMouseSensitivity;
        var mouseY = Input.GetAxisRaw("Mouse Y") * runtimeMouseSensitivity;

        yaw += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        var speed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMoveMultiplier;

        var forward = transform.forward;
        var right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        var move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.A)) move -= right;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position += move * speed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.0001f)
            transform.position += transform.forward * scroll * zoomSpeed;
    }

    public bool IsCameraInputEnabled()
    {
        return cameraInputEnabled;
    }

    public void EnableCameraInput()
    {
        SetCameraInputEnabled(true);
    }

    public void DisableCameraInput()
    {
        SetCameraInputEnabled(false);
    }

    public void ToggleCameraInput()
    {
        SetCameraInputEnabled(!cameraInputEnabled);
    }
}
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMoveMultiplier = 3f;
    public float verticalSpeed = 6f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public bool invertY = false;

    [Header("Zoom")]
    public float zoomSpeed = 20f;

    [Header("Mode Toggle")]
    public KeyCode toggleCameraModeKey = KeyCode.C;
    public KeyCode forceUIModeKey = KeyCode.Escape;
    public bool startInCameraMode = false;

    [Header("State (Read Only)")]
    [SerializeField] private bool cameraInputEnabled = false;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

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
        {
            SetCameraInputEnabled(!cameraInputEnabled);
        }

        if (Input.GetKeyDown(forceUIModeKey))
        {
            SetCameraInputEnabled(false);
        }
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
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        float speed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMoveMultiplier;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = Vector3.zero;

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
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.0001f)
        {
            transform.position += transform.forward * scroll * zoomSpeed;
        }
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
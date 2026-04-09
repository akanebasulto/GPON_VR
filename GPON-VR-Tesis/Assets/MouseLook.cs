using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad del ratón")]
    public float sensitivityX = 3.0f;
    public float sensitivityY = 3.0f;

    [Header("Referencias")]
    public Transform playerBody;
    public Transform cameraTransform;

    [Header("Límite vertical")]
    public float minVertical = -80f;
    public float maxVertical = 80f;

    private float rotationX = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null)
            playerBody = transform.root;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // ✅ New Input System — reemplaza Input.GetAxis
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * sensitivityX * Time.deltaTime;
        float mouseY = mouseDelta.y * sensitivityY * Time.deltaTime;

        // Rotación horizontal → XR Origin
        playerBody.Rotate(Vector3.up * mouseX);

        // Rotación vertical → solo la cámara
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVertical, maxVertical);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }
}
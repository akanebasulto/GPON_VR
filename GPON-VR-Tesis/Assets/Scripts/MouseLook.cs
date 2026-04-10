using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad del ratón")]
    public float sensitivityX = 3.0f;
    public float sensitivityY = 3.0f;

    [Header("Referencias")]
    public Transform playerBody;      // XR Origin
    public Transform cameraOffset;    // Camera Offset 

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

        // Busca Camera Offset automáticamente si no se asigna
        if (cameraOffset == null)
        {
            GameObject co = GameObject.Find("Camera Offset");
            if (co != null) cameraOffset = co.transform;
        }
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * sensitivityX * Time.deltaTime;
        float mouseY = mouseDelta.y * sensitivityY * Time.deltaTime;

        // Rotación horizontal → XR Origin
        playerBody.Rotate(Vector3.up * mouseX);

        // Rotación vertical → Camera Offset (no Main Camera)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVertical, maxVertical);
        cameraOffset.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }
}
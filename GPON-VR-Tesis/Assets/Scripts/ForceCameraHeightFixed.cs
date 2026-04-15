using UnityEngine;

public class ForceCameraHeightFixed : MonoBehaviour
{
    [Header("Altura de los ojos (0.1 = persona de pie)")]
    public float eyeHeight = 0.1f;

    void LateUpdate()
    {
        // Forzamos la altura cada frame (el Mock Runtime intenta resetearla)
        Vector3 pos = transform.localPosition;
        pos.y = eyeHeight;
        transform.localPosition = pos;
    }
}
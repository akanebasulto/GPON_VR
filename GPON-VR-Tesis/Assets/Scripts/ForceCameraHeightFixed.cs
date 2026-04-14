using UnityEngine;

public class ForceCameraHeightFixed : MonoBehaviour
{
    [Header("Altura de los ojos (1.6 = persona de pie)")]
    public float eyeHeight = 1.6f;

    void LateUpdate()
    {
        // Forzamos la altura cada frame (el Mock Runtime intenta resetearla)
        Vector3 pos = transform.localPosition;
        pos.y = eyeHeight;
        transform.localPosition = pos;
    }
}
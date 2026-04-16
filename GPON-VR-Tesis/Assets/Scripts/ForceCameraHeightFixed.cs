using UnityEngine;

public class ForceCameraHeightFixed : MonoBehaviour
{
    [Header("Altura de los ojos (1.1 = persona de pie)")]
    public float eyeHeight = 1.1f;

    void LateUpdate()
    {
        // Forzamos la altura cada frame (el Mock Runtime intenta resetearla)
        Vector3 pos = transform.localPosition;
        pos.y = eyeHeight;
        transform.localPosition = pos;
    }
}
using UnityEngine;

public class ForceCameraHeightFixed : MonoBehaviour
{
    [Header("Altura de los ojos (2.6 = persona de pie)")]
    public float eyeHeight = 2.6f;

    void LateUpdate()
    {
        // Forzamos la altura cada frame (el Mock Runtime intenta resetearla)
        Vector3 pos = transform.localPosition;
        pos.y = eyeHeight;
        transform.localPosition = pos;
    }
}
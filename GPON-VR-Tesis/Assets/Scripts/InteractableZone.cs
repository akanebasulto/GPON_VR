using UnityEngine;

public class InteractableZone : MonoBehaviour
{
    [Header("Información de la zona")]
    public string nombreLegible = "Inspeccionar";
    public bool tieneInteraccionFisica = false;
    public string textoAccionE = "Interactuar";

    [Header("Controller — arrastra solo el de este objeto")]
    public GestionPCController controlPC;
    public OLTController controlOLT;
    public ODFController controlODF;
    public NAPController controlNAP;
    public ONTController controlONT;

    public void InteractuarConPanel()
    {
        if (controlPC != null) { controlPC.TogglePanel(); return; }
        if (controlOLT != null) { controlOLT.TogglePanel(); return; }
        if (controlODF != null) { controlODF.TogglePanel(); return; }
        if (controlNAP != null) { controlNAP.TogglePanel(); return; }
        if (controlONT != null) { controlONT.TogglePanel(); return; }
    }

    public void InteractuarFisicamente()
    {
        // Solo la NAP tiene interacción física por ahora (tapa)
        // Si en el futuro el ONT necesita agarrarse, se añade aquí
        if (controlNAP != null) { controlNAP.AbrirCerrar(); return; }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Va en: ODF_Chasis (Zona 1 — rack)
// Panel de solo lectura.
// Se actualiza automaticamente cuando el OLT cambia potencia.
// El panel permanece abierto al alejarse; se cierra con F o Escape.

public class ODFController : MonoBehaviour
{
    [Header("Panel educativo")]
    public GameObject panelODF;
    public TMP_Text textoPanel;

    [Header("Referencia al OLT")]
    public OLTController controladorOLT;

    // ── Constantes tecnicas reales ──
    private const float PERDIDA_ADAPTADOR = 0.30f; // dB por adaptador SC/APC
    private const float PERDIDA_TOTAL_ODF = 0.60f; // entrada + salida

    // DISTANCIA_CIERRE eliminada — el panel ya no se cierra
    // por distancia. Se cierra con F (TogglePanel) o con Escape.

    // ── Estado interno ──
    private float potenciaDesdeOLT = 2.0f;
    private bool panelAbierto = false;

    void Start()
    {
        if (panelODF != null)
            panelODF.SetActive(false);

        if (controladorOLT != null)
            controladorOLT.OnParameterChanged.AddListener(
                OnPotenciaOLTCambiada);
    }

    void OnDestroy()
    {
        if (controladorOLT != null)
            controladorOLT.OnParameterChanged.RemoveListener(
                OnPotenciaOLTCambiada);
    }

    void Update()
    {
        // Escape cierra el panel desde cualquier distancia
        if (panelAbierto
            && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CerrarPanel();
        }
    }

    void OnPotenciaOLTCambiada(float nuevaPotencia)
    {
        potenciaDesdeOLT = nuevaPotencia;
        if (panelAbierto) RefrescarPanel();
    }

    // ── Métodos de panel ──

    public void TogglePanel()
    {
        if (panelAbierto) CerrarPanel();
        else AbrirPanel();
    }

    public void AbrirPanel()
    {
        panelAbierto = true;
        if (panelODF != null) panelODF.SetActive(true);
        RefrescarPanel();
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelODF != null) panelODF.SetActive(false);
    }

    // ── Contenido del panel ──

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        float pSalida = potenciaDesdeOLT - PERDIDA_TOTAL_ODF;

        string estadoSenal;
        if (pSalida >= 1f) estadoSenal = "[OK] Senal optima hacia NAP";
        else if (pSalida >= -1f) estadoSenal = "[!!] Senal ajustada";
        else estadoSenal = "[--] Senal debil";

        textoPanel.text =
            "<size=115%><b>ODF — OPTICAL DISTRIB. FRAME</b></size>\n" +
            "<color=#AAAAAA>──────────────────────────────</color>\n\n" +
            "<b>¿Que es?</b>\n" +
            "Componente <color=#FFB347>PASIVO</color> (sin electronica).\n" +
            "Organiza y protege las fibras\n" +
            "dentro del rack.\n\n" +
            "<b>Funcion en la red:</b>\n" +
            "Interconecta el OLT con el cable\n" +
            "ADSS exterior que viaja hasta\n" +
            "la Caja NAP.\n\n" +
            "<b>CONECTOR: SC/APC</b>\n" +
            "Verde = APC (pulido en angulo 8°)\n" +
            "Reduce reflexiones hacia el laser\n" +
            "del OLT.\n\n" +
            "<b>PERDIDAS EN ESTE PUNTO:</b>\n" +
            "Adaptador entrada: <color=#FF8C00>" +
                PERDIDA_ADAPTADOR.ToString("F2") + " dB</color>\n" +
            "Adaptador salida:  <color=#FF8C00>" +
                PERDIDA_ADAPTADOR.ToString("F2") + " dB</color>\n" +
            "Total ODF:         <color=#FF8C00>" +
                PERDIDA_TOTAL_ODF.ToString("F2") + " dB</color>\n\n" +
            "<b>PRESUPUESTO OPTICO:</b>\n" +
            "Entrada (OLT): <color=#00FF88>" +
                potenciaDesdeOLT.ToString("F1") + " dBm</color>\n" +
            "Salida (NAP):  <color=#00FF88>" +
                pSalida.ToString("F1") + " dBm</color>\n\n" +
            "<color=#AAAAAA>" + estadoSenal + "</color>\n\n" +
            "<b>Estandar:</b> <color=#AAAAAA>IEC 61753-1</color>\n\n" +
            "<color=#555555>[F] Cerrar   [Esc] Cerrar</color>";
    }
}
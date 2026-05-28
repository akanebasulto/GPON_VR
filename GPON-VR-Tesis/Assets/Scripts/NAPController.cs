using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Va en: CajaNAP (Zona 2)
// E: abre/cierra la tapa (animacion Lerp)
// F: muestra/oculta el panel educativo
// Escape: cierra el panel si está abierto

public class NAPController : MonoBehaviour
{
    [Header("Tapa de la caja")]
    public Transform tapaPivot;
    public float velocidadApertura = 2.5f;

    [Header("Panel educativo")]
    public GameObject panelNAP;
    public TMP_Text textoPanel;

    [Header("Referencia al OLT")]
    public OLTController controladorOLT;

    // ── Constantes tecnicas ──
    private const float PERDIDA_ODF = 0.60f;
    private const float PERDIDA_CABLE_FEEDER = 1.00f;
    private const float PERDIDA_SPLITTER = 10.5f;
    private const float PERDIDA_CABLE_DROP = 0.50f;
    private const float SENS_MINIMA_ONT = -27.0f;
    // DISTANCIA_CIERRE eliminada — el panel ya no se cierra por distancia.
    // Se cierra con F (TogglePanel) o con Escape.

    // ── Estado interno ──
    private bool estaAbierta = false;
    private bool panelAbierto = false;
    private float potenciaOLT = 2.0f;

    // Propiedad publica para que InteractableZone
    // pueda actualizar el texto del prompt E
    public bool EstaAbierta => estaAbierta;

    void Start()
    {
        if (panelNAP != null)
            panelNAP.SetActive(false);

        if (tapaPivot != null)
            tapaPivot.localRotation = Quaternion.Euler(0f, 0f, 0f);

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
        AnimarTapa();

        // Escape cierra el panel desde cualquier distancia
        if (panelAbierto
            && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CerrarPanel();
        }

        // ── BLOQUE ELIMINADO ──
        // Ya no existe la comprobación de DISTANCIA_CIERRE.
        // El panel permanece abierto mientras el jugador
        // se aleja para leerlo; solo se cierra con F o Escape.
    }

    // Animacion suave de la tapa con Lerp
    void AnimarTapa()
    {
        if (tapaPivot == null) return;

        Quaternion objetivo = estaAbierta
            ? Quaternion.Euler(0f, 0f, 160f)
            : Quaternion.Euler(0f, 0f, 0f);

        tapaPivot.localRotation = Quaternion.Lerp(
            tapaPivot.localRotation,
            objetivo,
            Time.deltaTime * velocidadApertura);
    }

    // ── Llamado por InteractableZone con tecla E ──

    public void AbrirCerrar()
    {
        estaAbierta = !estaAbierta;
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
        if (panelNAP != null) panelNAP.SetActive(true);
        RefrescarPanel();
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelNAP != null) panelNAP.SetActive(false);
    }

    void OnPotenciaOLTCambiada(float nuevaPotencia)
    {
        potenciaOLT = nuevaPotencia;
        if (panelAbierto) RefrescarPanel();
    }

    // ── Calculos de presupuesto optico ──

    float PotenciaEntradaNAP()
        => potenciaOLT - PERDIDA_ODF - PERDIDA_CABLE_FEEDER;

    float PotenciaSalidaSplitter()
        => PotenciaEntradaNAP() - PERDIDA_SPLITTER;

    float PotenciaEnONT()
        => PotenciaSalidaSplitter() - PERDIDA_CABLE_DROP;

    // ── Contenido del panel ──

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        float pEntrada = PotenciaEntradaNAP();
        float pSplit = PotenciaSalidaSplitter();
        float pONT = PotenciaEnONT();
        float margen = pONT - SENS_MINIMA_ONT;

        string estadoTapa = estaAbierta
            ? "Tapa: ABIERTA — splitter visible"
            : "Tapa: CERRADA — [E] para abrir";

        string estadoRed;
        if (margen >= 10f) estadoRed = "[OK] Excelente — Margen: +" + margen.ToString("F1") + " dB";
        else if (margen >= 5f) estadoRed = "[OK] Bueno     — Margen: +" + margen.ToString("F1") + " dB";
        else if (margen >= 0f) estadoRed = "[!!] Ajustado  — Margen: +" + margen.ToString("F1") + " dB";
        else estadoRed = "[--] Insuf.    — Deficit: " + margen.ToString("F1") + " dB";

        textoPanel.text =
            "<size=115%><b>CAJA NAP — NODO DE ACCESO</b></size>\n" +
            "<color=#AAAAAA>──────────────────────────────</color>\n\n" +
            "  " + estadoTapa + "\n\n" +
            "<b>¿Que es?</b>\n" +
            "Caja <color=#FFB347>pasiva</color> exterior. Contiene el\n" +
            "splitter optico 1x8 y distribuye\n" +
            "la senal a los clientes.\n\n" +
            "<b>SPLITTER 1x8 (interior):</b>\n" +
            "Divide 1 fibra en 8 senales iguales.\n" +
            "Perdida teorica: 10 log10(8) = 9.03 dB\n" +
            "Perdida real:   10.50 dB\n" +
            "(sin electronica — completamente pasivo)\n\n" +
            "<b>PRESUPUESTO OPTICO COMPLETO:</b>\n" +
            "Potencia OLT:    <color=#00FF88>" + potenciaOLT.ToString("F1") + " dBm</color>\n" +
            "  - ODF:         <color=#FF8C00>-0.60 dB</color>\n" +
            "  - Cable feeder:<color=#FF8C00>-1.00 dB</color>\n" +
            "Entrada NAP:     <color=#00FF88>" + pEntrada.ToString("F1") + " dBm</color>\n" +
            "  - Splitter 1x8:<color=#FF8C00>-10.50 dB</color>\n" +
            "Salida splitter: <color=#00FF88>" + pSplit.ToString("F1") + " dBm</color>\n" +
            "  - Cable drop:  <color=#FF8C00>-0.50 dB</color>\n" +
            "En ONTs:         <color=#00FF88>" + pONT.ToString("F1") + " dBm</color>\n" +
            "Minimo ONT:      <color=#AAAAAA>-27.0 dBm</color>\n\n" +
            "<color=#AAAAAA>" + estadoRed + "</color>\n\n" +
            "<b>Estandar:</b> <color=#AAAAAA>ITU-T G.671</color>\n\n" +
            "<color=#555555>[F] Cerrar   [Esc] Cerrar</color>";
    }
}
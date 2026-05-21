using UnityEngine;
using UnityEngine.Events;
using TMPro;

// ══════════════════════════════════════════════════════════
// NAPController.cs
// Se asigna al GameObject: CajaNAP
//
// Gestiona la tapa (animación) y el panel informativo.
// Las teclas E y F las recibe desde ProximityDetector
// → InteractableZone → aquí. Este script NO maneja
// input directamente.
// ══════════════════════════════════════════════════════════

public class NAPController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────
    // REFERENCIAS
    // ──────────────────────────────────────────────────────

    [Header("Tapa de la caja NAP")]
    public Transform tapaPivot;
    public float velocidadApertura = 2.5f;

    [Header("Panel informativo")]
    public GameObject panelNAP;
    public TMP_Text textoPanel;

    [Header("Referencia al OLT")]
    public OLTController controladorOLT;

    // ──────────────────────────────────────────────────────
    // CONSTANTES TÉCNICAS
    // ──────────────────────────────────────────────────────

    private const float PERDIDA_ODF = 0.60f;
    private const float PERDIDA_CABLE_FEEDER = 1.00f;
    private const float PERDIDA_SPLITTER = 10.50f;
    private const float PERDIDA_CABLE_DROP = 0.50f;
    private const int DROPS_ACTIVOS = 4;
    private const int DROPS_TOTALES = 8;
    private const float DISTANCIA_CIERRE = 4.0f;

    // ──────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ──────────────────────────────────────────────────────

    private bool estaAbierta = false;
    private bool panelAbierto = false;
    private float potenciaOLT = 2.0f;

    // Propiedad pública — InteractableZone la lee para
    // actualizar el texto del prompt E
    public bool EstaAbierta => estaAbierta;

    // ──────────────────────────────────────────────────────
    // INICIO
    // ──────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────
    // UPDATE — solo animación de tapa y cierre por distancia
    // ──────────────────────────────────────────────────────

    void Update()
    {
        AnimarTapa();

        if (panelAbierto)
            VerificarDistanciaPanel();
    }

    void AnimarTapa()
    {
        if (tapaPivot == null) return;

        Quaternion objetivo = estaAbierta
            ? Quaternion.Euler(0f, 90f, 0f)
            : Quaternion.Euler(0f, 0f, 0f);

        tapaPivot.localRotation = Quaternion.Lerp(
            tapaPivot.localRotation,
            objetivo,
            Time.deltaTime * velocidadApertura
        );
    }

    void VerificarDistanciaPanel()
    {
        if (Camera.main == null) return;

        float dist = Vector3.Distance(
            transform.position,
            Camera.main.transform.position
        );

        if (dist > DISTANCIA_CIERRE)
            CerrarPanel();
    }

    // ──────────────────────────────────────────────────────
    // INTERACCIÓN FÍSICA — llamada desde InteractableZone
    // ──────────────────────────────────────────────────────

    public void AbrirCerrar()
    {
        estaAbierta = !estaAbierta;
    }

    // ──────────────────────────────────────────────────────
    // PANEL — llamados desde InteractableZone y PanelManager
    // ──────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────
    // EVENTO DEL OLT
    // ──────────────────────────────────────────────────────

    void OnPotenciaOLTCambiada(float nuevaPotencia)
    {
        potenciaOLT = nuevaPotencia;
        if (panelAbierto) RefrescarPanel();
    }

    // ──────────────────────────────────────────────────────
    // CÁLCULOS ÓPTICOS
    // ──────────────────────────────────────────────────────

    float PotenciaEntradaNAP()
        => potenciaOLT - PERDIDA_ODF - PERDIDA_CABLE_FEEDER;

    float PotenciaSalidaSplitter()
        => PotenciaEntradaNAP() - PERDIDA_SPLITTER;

    float PotenciaEnONT()
        => PotenciaSalidaSplitter() - PERDIDA_CABLE_DROP;

    // ──────────────────────────────────────────────────────
    // CONTENIDO DEL PANEL
    // ──────────────────────────────────────────────────────

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        float pIn = PotenciaEntradaNAP();
        float pSplit = PotenciaSalidaSplitter();
        float pONT = PotenciaEnONT();
        float margen = pONT - (-27.0f);

        string estadoTapa = estaAbierta
            ? "  Tapa: ABIERTA — splitter visible"
            : "  Tapa: CERRADA — [E] para abrir";

        string estadoRed;
        if (margen >= 10f) estadoRed = "✓ EXCELENTE — Margen: +" + margen.ToString("F1") + " dB";
        else if (margen >= 5f) estadoRed = "✓ BUENO     — Margen: +" + margen.ToString("F1") + " dB";
        else if (margen >= 0f) estadoRed = "⚠ AJUSTADO  — Margen: +" + margen.ToString("F1") + " dB";
        else estadoRed = "✗ INSUFICIENTE  — Déficit: " + margen.ToString("F1") + " dB";

        textoPanel.text =
            "╔═══ CAJA NAP — NODO DE ACCESO ══════╗\n\n" +
            "  Splitter interno: 1×8 pasivo\n" +
            "  Drops activos: " + DROPS_ACTIVOS + " / " + DROPS_TOTALES + "\n\n" +
            estadoTapa + "\n\n" +
            "  PRESUPUESTO ÓPTICO:\n" +
            "  OLT:        " + potenciaOLT.ToString("F1") + " dBm\n" +
            "  – ODF:     -" + PERDIDA_ODF.ToString("F2") + " dB\n" +
            "  – Feeder:  -" + PERDIDA_CABLE_FEEDER.ToString("F2") + " dB\n" +
            "  Entrada NAP: " + pIn.ToString("F1") + " dBm\n" +
            "  – Splitter:-" + PERDIDA_SPLITTER.ToString("F2") + " dB\n" +
            "  Salida:      " + pSplit.ToString("F1") + " dBm\n" +
            "  – Drop:    -" + PERDIDA_CABLE_DROP.ToString("F2") + " dB\n" +
            "  ─────────────────────────\n" +
            "  En ONTs:     " + pONT.ToString("F1") + " dBm\n" +
            "  Mínimo ONT: -27.0 dBm\n\n" +
            "  " + estadoRed + "\n\n" +
            "  [E] Abrir/Cerrar  [F] Cerrar\n" +
            "╚════════════════════════════════════╝";
    }

} // ← único cierre de la clase. Nada va después de esta llave.
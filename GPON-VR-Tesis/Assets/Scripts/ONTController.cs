using UnityEngine;
using TMPro;

// ══════════════════════════════════════════════════════════
// ONTController.cs — Versión sin física
// Se asigna a: ONT_1, ONT_2, ONT_3 y ONT_4
//
// El mismo script para las 4 ONTs.
// El campo 'numeroCliente' personaliza cada instancia.
//
// Interacción: solo F para abrir panel informativo.
// Las teclas las gestiona ProximityDetector → InteractableZone.
// Este script NO maneja input directamente.
// ══════════════════════════════════════════════════════════

public class ONTController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────
    // IDENTIFICACIÓN
    // ──────────────────────────────────────────────────────

    [Header("Identificación del cliente")]
    [Range(1, 4)]
    public int numeroCliente = 1;

    // ──────────────────────────────────────────────────────
    // LEDs — referencias directas, sin usar Tags
    // ──────────────────────────────────────────────────────

    [Header("LEDs (arrastra cada Sphere desde Hierarchy)")]
    public Renderer ledPower;
    public Renderer ledPON;
    public Renderer ledLAN;
    public Renderer ledWiFi;

    [Header("Materiales de LEDs")]
    public Material matVerde;
    public Material matNaranja;
    public Material matRojo;
    public Material matGris;

    // ──────────────────────────────────────────────────────
    // PANEL
    // ──────────────────────────────────────────────────────

    [Header("Panel informativo")]
    public GameObject panelONT;
    public TMP_Text textoPanel;

    // ──────────────────────────────────────────────────────
    // REFERENCIA AL OLT
    // ──────────────────────────────────────────────────────

    [Header("Referencia al OLT")]
    public OLTController controladorOLT;

    // ──────────────────────────────────────────────────────
    // CONSTANTES
    // ──────────────────────────────────────────────────────

    private const float PERDIDA_TOTAL = 12.6f;   // dB (ODF+feeder+splitter+drop)
    private const float SENS_MINIMA = -27.0f;  // dBm (sensibilidad mínima ONT)
    private const float DISTANCIA_CIERRE = 3.0f;  // metros

    // ──────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ──────────────────────────────────────────────────────

    private float potenciaOLT = 2.0f;
    private bool panelAbierto = false;

    // Datos del cliente — fijos por ahora
    private string ssid;
    private int velocidadMbps = 100;
    private int clientesWiFi;

    // ──────────────────────────────────────────────────────
    // INICIO
    // ──────────────────────────────────────────────────────

    void Start()
    {
        // Datos simulados personalizados por número de cliente
        ssid = "Red_GPON_" + numeroCliente;
        clientesWiFi = numeroCliente + 1;

        if (panelONT != null)
            panelONT.SetActive(false);

        // Estado inicial de LEDs
        ActualizarLEDs(potenciaOLT);

        // Suscripción al evento del OLT
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
    // UPDATE — solo cierre por distancia
    // ──────────────────────────────────────────────────────

    void Update()
    {
        if (panelAbierto)
            VerificarDistanciaPanel();
    }

    void VerificarDistanciaPanel()
    {
        if (Camera.main == null) return;

        float dist = Vector3.Distance(
            transform.position,
            Camera.main.transform.position);

        if (dist > DISTANCIA_CIERRE)
            CerrarPanel();
    }

    // ──────────────────────────────────────────────────────
    // PANEL — llamados desde InteractableZone
    // ──────────────────────────────────────────────────────

    public void TogglePanel()
    {
        if (panelAbierto) CerrarPanel();
        else AbrirPanel();
    }

    public void AbrirPanel()
    {
        panelAbierto = true;
        if (panelONT != null) panelONT.SetActive(true);
        RefrescarPanel();
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelONT != null) panelONT.SetActive(false);
    }

    // ──────────────────────────────────────────────────────
    // EVENTO DEL OLT
    // ──────────────────────────────────────────────────────

    void OnPotenciaOLTCambiada(float nuevaPotencia)
    {
        potenciaOLT = nuevaPotencia;
        ActualizarLEDs(potenciaOLT);
        if (panelAbierto) RefrescarPanel();
    }

    // ──────────────────────────────────────────────────────
    // LEDs
    // ──────────────────────────────────────────────────────

    void ActualizarLEDs(float potencia)
    {
        float potenciaEnONT = potencia - PERDIDA_TOTAL;
        bool haySenal = potenciaEnONT >= SENS_MINIMA;

        // POWER: siempre verde (el equipo está encendido)
        if (ledPower != null)
            ledPower.material = matVerde;

        // PON: verde = sincronizado, rojo = sin señal óptica
        if (ledPON != null)
            ledPON.material = haySenal ? matVerde : matRojo;

        // LAN: verde si hay señal (PC conectada)
        if (ledLAN != null)
            ledLAN.material = haySenal ? matVerde : matGris;

        // WiFi: verde con clientes, naranja sin clientes, gris sin señal
        if (ledWiFi != null)
        {
            if (!haySenal) ledWiFi.material = matGris;
            else if (clientesWiFi > 0) ledWiFi.material = matVerde;
            else ledWiFi.material = matNaranja;
        }
    }

    // ──────────────────────────────────────────────────────
    // CONTENIDO DEL PANEL
    // ──────────────────────────────────────────────────────

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        float potONT = potenciaOLT - PERDIDA_TOTAL;
        float margen = potONT - SENS_MINIMA;
        bool haySenal = potONT >= SENS_MINIMA;

        string estadoPON = haySenal
            ? "● SINCRONIZADO con OLT"
            : "○ SIN SEÑAL ÓPTICA (LOS)";

        string estadoMargen;
        if (margen >= 10f) estadoMargen = "✓ EXCELENTE — +" + margen.ToString("F1") + " dB";
        else if (margen >= 5f) estadoMargen = "✓ BUENO     — +" + margen.ToString("F1") + " dB";
        else if (margen >= 0f) estadoMargen = "⚠ AJUSTADO  — +" + margen.ToString("F1") + " dB";
        else estadoMargen = "✗ SIN SEÑAL — " + margen.ToString("F1") + " dB";

        textoPanel.text =
            "╔═══ ONT — CLIENTE " + numeroCliente + " ══════════════╗\n\n" +
            "  Puerto GPON:  " + numeroCliente + " / 8\n" +
            "  ID ONT:       ONU-000" + numeroCliente + "\n\n" +
            "  SEÑAL ÓPTICA:\n" +
            "  Potencia:     " + potONT.ToString("F1") + " dBm\n" +
            "  Mínimo ONT:   " + SENS_MINIMA + " dBm\n" +
            "  " + estadoMargen + "\n\n" +
            "  ESTADO PON:   " + estadoPON + "\n\n" +
            "  RED WiFi:\n" +
            "  SSID:         " + ssid + "\n" +
            "  Velocidad:    " + velocidadMbps + " Mbps\n" +
            "  Clientes:     " + clientesWiFi + " dispositivos\n\n" +
            "  λ recepción:  1490 nm (downstream)\n" +
            "  λ emisión:    1310 nm (upstream)\n\n" +
            "  [F] Cerrar\n" +
            "╚════════════════════════════════════╝";
    }

} // ← único cierre de la clase. Nada va después.
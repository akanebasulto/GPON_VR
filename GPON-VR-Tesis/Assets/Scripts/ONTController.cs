using UnityEngine;
using TMPro;

// Va en: ONT_1, ONT_2, ONT_3, ONT_4 (Zona 3)
//
// COMO PERSONALIZAR EL NOMBRE DE CADA CLIENTE:
// Selecciona ONT_1 en el Hierarchy.
// En el Inspector, campo "Nombre Cliente":
//   cambia "Cliente" por el nombre real, ej: "Oficina 1"
// Repite para ONT_2 → "Recepcion", ONT_3 → "Sala Reuniones", etc.

public class ONTController : MonoBehaviour
{
    [Header("Identificacion del cliente")]
    [Range(1, 4)]
    [Tooltip("Numero de puerto GPON (1 a 4)")]
    public int numeroCliente = 1;

    [Tooltip("Nombre personalizado. Ejemplos:\n" +
             "Oficina 1 | Recepcion | Sala Reuniones | Almacen")]
    public string nombreCliente = "Cliente";

    [Header("Panel educativo")]
    public GameObject panelONT;
    public TMP_Text textoPanel;

    [Header("LEDs del panel frontal")]
    public Renderer ledPower;
    public Renderer ledPON;
    public Renderer ledLAN;
    public Renderer ledWiFi;

    [Header("Materiales de LEDs")]
    public Material matVerde;
    public Material matNaranja;
    public Material matRojo;
    public Material matGris;

    [Header("Referencia al OLT")]
    public OLTController controladorOLT;

    // ── Constantes ──
    private const float PERDIDA_TOTAL = 12.6f;   // ODF+feeder+splitter+drop
    private const float SENS_MINIMA = -27.0f;
    private const float DISTANCIA_CIERRE = 3.0f;

    // ── Estado interno ──
    private float potenciaOLT = 2.0f;
    private bool panelAbierto = false;

    // Datos del cliente (fijos en esta version)
    private int velocidadMbps = 100;
    private int clientesWiFi;
    private string ssid;

    void Start()
    {
        // Genera datos simulados distintos por cliente
        clientesWiFi = numeroCliente + 1;
        ssid = "GPON_" + nombreCliente.Replace(" ", "_");

        if (panelONT != null)
            panelONT.SetActive(false);

        ActualizarLEDs(potenciaOLT);

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
        if (!panelAbierto) return;
        if (Camera.main == null) return;

        float dist = Vector3.Distance(
            transform.position, Camera.main.transform.position);
        if (dist > DISTANCIA_CIERRE) CerrarPanel();
    }

    // ── LEDs ──

    void ActualizarLEDs(float potencia)
    {
        float pONT = potencia - PERDIDA_TOTAL;
        bool haySenal = pONT >= SENS_MINIMA;

        if (ledPower != null) ledPower.material = matVerde;

        if (ledPON != null)
            ledPON.material = haySenal ? matVerde : matRojo;

        if (ledLAN != null)
            ledLAN.material = haySenal ? matVerde : matGris;

        if (ledWiFi != null)
        {
            if (!haySenal) ledWiFi.material = matGris;
            else if (clientesWiFi > 0) ledWiFi.material = matVerde;
            else ledWiFi.material = matNaranja;
        }
    }

    void OnPotenciaOLTCambiada(float nuevaPotencia)
    {
        potenciaOLT = nuevaPotencia;
        ActualizarLEDs(potenciaOLT);
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
        if (panelONT != null) panelONT.SetActive(true);
        RefrescarPanel();
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelONT != null) panelONT.SetActive(false);
    }

    // ── Contenido del panel ──

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        float pONT = potenciaOLT - PERDIDA_TOTAL;
        float margen = pONT - SENS_MINIMA;
        bool senal = pONT >= SENS_MINIMA;

        string estadoPON = senal
            ? "[OK] SINCRONIZADO con OLT"
            : "[--] SIN SENAL OPTICA (LOS)";

        string estadoMargen;
        if (margen >= 10f) estadoMargen = "[OK] Excelente +" + margen.ToString("F1") + " dB";
        else if (margen >= 5f) estadoMargen = "[OK] Bueno     +" + margen.ToString("F1") + " dB";
        else if (margen >= 0f) estadoMargen = "[!!] Ajustado  +" + margen.ToString("F1") + " dB";
        else estadoMargen = "[--] Sin senal  " + margen.ToString("F1") + " dB";

        textoPanel.text =
            "<size=115%><b>ONT — " + nombreCliente.ToUpper() + "</b></size>\n" +
            "<color=#AAAAAA>──────────────────────────────</color>\n\n" +
            "<b>Puerto GPON:</b>  " + numeroCliente + " / 8\n" +
            "<b>ID ONT:</b>       ONU-000" + numeroCliente + "\n\n" +
            "<b>¿Que es?</b>\n" +
            "Equipo en el lado del cliente.\n" +
            "Convierte senal optica en Ethernet\n" +
            "y WiFi para los dispositivos.\n\n" +
            "<b>MODULO BOSA:</b>\n" +
            "Recibe: <color=#FFB347>1490 nm</color> (desde OLT)\n" +
            "Envia:  <color=#87CEEB>1310 nm</color> (hacia OLT)\n\n" +
            "<b>SENAL OPTICA RECIBIDA:</b>\n" +
            "Potencia:  <color=#00FF88>" + pONT.ToString("F1") + " dBm</color>\n" +
            "Minimo:    <color=#AAAAAA>" + SENS_MINIMA + " dBm</color>\n" +
            "Estado:    <color=#AAAAAA>" + estadoMargen + "</color>\n\n" +
            "<b>ESTADO PON:</b>\n" +
            "<color=#AAAAAA>" + estadoPON + "</color>\n\n" +
            "<b>RED WiFi:</b>\n" +
            "SSID:      <color=#00FF88>" + ssid + "</color>\n" +
            "Velocidad: <color=#00FF88>" + velocidadMbps + " Mbps</color>\n" +
            "Clientes:  <color=#00FF88>" + clientesWiFi + " dispositivos</color>\n\n" +
            "<b>Estandar:</b> <color=#AAAAAA>ITU-T G.984.5</color>\n\n" +
            "<color=#555555>[F] Cerrar</color>";
    }
}
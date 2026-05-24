using UnityEngine;
using UnityEngine.Events;
using TMPro;

// ══════════════════════════════════════════════════════════
// OLTController.cs
// Va en: OLT_Chasis
//
// Panel de SOLO LECTURA — el OLT muestra su estado.
// La configuración se hace desde GestionPCController (la PC).
//
// CAMPOS DEL INSPECTOR:
//   Panel OLT     → el Canvas "Panel_OLT"
//   Texto Panel   → el TextMeshPro "Texto_OLT" dentro del Canvas
//   4 materiales  → para cambiar el color de los LEDs
// ══════════════════════════════════════════════════════════

public class OLTController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────
    // PANEL — los dos únicos campos de UI que necesitas
    // ──────────────────────────────────────────────────────

    [Header("Panel educativo")]
    [Tooltip("Arrastra aquí el Canvas 'Panel_OLT'")]
    public GameObject panelOLT;

    [Tooltip("Arrastra aquí el TextMeshPro 'Texto_OLT'")]
    public TMP_Text textoPanel;

    // ──────────────────────────────────────────────────────
    // LEDs — materiales para cambiar el color de las esferas
    // ──────────────────────────────────────────────────────

    [Header("Materiales de LEDs")]
    public Material matLedVerde;
    public Material matLedNaranja;
    public Material matLedRojo;
    public Material matLedGris;

    // ──────────────────────────────────────────────────────
    // EVENTO — el ODF y la NAP se suscriben aquí
    // Cuando la PC cambia la potencia, este evento avisa
    // a todos los componentes suscritos automáticamente
    // ──────────────────────────────────────────────────────

    [Header("Evento para otros componentes")]
    public UnityEvent<float> OnParameterChanged;

    // ──────────────────────────────────────────────────────
    // ESTADO INTERNO
    // Estos valores los actualiza GestionPCController
    // ──────────────────────────────────────────────────────

    private float potenciaActual = 2.0f;
    private int velocidadActual = 100;
    private bool panelAbierto = false;

    // Los LEDs se buscan por Tag una sola vez en Start()
    private GameObject[] ledsOLT;

    // Tabla de velocidades disponibles
    private int[] velocidades = { 10, 50, 100, 300, 600 };

    // ──────────────────────────────────────────────────────
    // INICIO
    // ──────────────────────────────────────────────────────

    void Start()
    {
        // Busca los LEDs UNA SOLA VEZ — tienen Tag "LED_OLT"
        ledsOLT = GameObject.FindGameObjectsWithTag("LED_OLT");

        // El panel empieza siempre cerrado
        if (panelOLT != null)
            panelOLT.SetActive(false);

        // Estado inicial: todos los LEDs en verde
        ActualizarLEDs(potenciaActual);
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
        if (panelOLT != null) panelOLT.SetActive(true);
        RefrescarPanel();
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelOLT != null) panelOLT.SetActive(false);
    }

    // ──────────────────────────────────────────────────────
    // RECIBIR CONFIGURACIÓN DESDE LA PC
    // GestionPCController llama a este método cuando
    // el estudiante cambia algo en la consola NMS
    // ──────────────────────────────────────────────────────

    public void ActualizarEstado(float nuevaPotencia, int indiceVelocidad)
    {
        potenciaActual = nuevaPotencia;

        int idx = Mathf.Clamp(indiceVelocidad, 0, velocidades.Length - 1);
        velocidadActual = velocidades[idx];

        ActualizarLEDs(potenciaActual);

        if (panelAbierto) RefrescarPanel();

        // Avisa a ODF, NAP y ONTs del cambio
        OnParameterChanged?.Invoke(potenciaActual);
    }

    // ──────────────────────────────────────────────────────
    // LEDs — cambia el material (color) de cada esfera LED
    // ──────────────────────────────────────────────────────

    void ActualizarLEDs(float potencia)
    {
        if (ledsOLT == null) return;

        for (int i = 0; i < ledsOLT.Length; i++)
        {
            Renderer r = ledsOLT[i].GetComponent<Renderer>();
            if (r == null) continue;

            // Los primeros 4 son puertos con ONT conectada
            // Los últimos 4 son puertos libres (grises)
            bool conONT = (i < 4);

            if (!conONT)
            {
                r.material = matLedGris;
            }
            else if (potencia >= 0f)
            {
                r.material = matLedVerde;
            }
            else if (potencia >= -2f)
            {
                r.material = (i < 2) ? matLedVerde : matLedNaranja;
            }
            else
            {
                r.material = (i < 2) ? matLedVerde : matLedRojo;
            }
        }
    }

    // ──────────────────────────────────────────────────────
    // CONTENIDO DEL PANEL
    //
    // Aquí es donde el script "escribe en el papel".
    // Construye una cadena de texto con todos los datos
    // y se la asigna al TextMeshPro.
    //
    // ¿Por qué un solo texto y no varios campos?
    // Porque es mucho más sencillo gestionar el layout
    // con saltos de línea que mover objetos en el Canvas.
    // Para cambiar el contenido, solo cambias este método.
    // ──────────────────────────────────────────────────────

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        // Calcula cuántos puertos están activos
        // según la potencia configurada
        int puertosActivos;
        if (potenciaActual >= 0f) puertosActivos = 4;
        else if (potenciaActual >= -2f) puertosActivos = 2;
        else puertosActivos = 0;

        // Estado legible de la red
        string estadoRed;
        if (puertosActivos == 4) estadoRed = "[OK] TODOS LOS CLIENTES ACTIVOS";
        else if (puertosActivos > 0) estadoRed = "[!!] CLIENTES 3 y 4 SIN SEÑAL";
        else estadoRed = "[--] RED INOPERATIVA";

        // El script construye este texto y lo mete en textoPanel
        textoPanel.text =
            "<size=115%><b>OLT — OPTICAL LINE TERMINAL</b></size>\n" +
            "<color=#AAAAAA>─────────────────────────────</color>\n\n" +
            "<b>¿QUÉ ES?</b>\n" +
            "Equipo activo en la central del operador.\n" +
            "Genera y gestiona la señal óptica para\n" +
            "todos los clientes de la red GPON.\n\n" +
            "<b>FUNCIÓN EN LA RED:</b>\n" +
            "Downstream (→ clientes): <color=#FFB347>1490 nm</color>\n" +
            "Upstream   (← clientes): <color=#87CEEB>1310 nm</color>\n" +
            "Velocidad máx: 2.488 Gbps compartidos\n\n" +
            "<b>PARÁMETROS ACTUALES:</b>\n" +
            "Potencia TX:    <color=#00FF88>" + potenciaActual.ToString("F1") + " dBm</color>\n" +
            "Velocidad DS:   <color=#00FF88>" + velocidadActual + " Mbps</color>\n" +
            "Puertos activos: <color=#00FF88>" + puertosActivos + " / 8</color>\n\n" +
            "<color=#888888>" + estadoRed + "</color>\n\n" +
            "<b>ESTÁNDAR:</b> <color=#AAAAAA>ITU-T G.984.2 (GPON)</color>\n\n" +
            "<color=#555555>[F] Cerrar</color>";
    }
}
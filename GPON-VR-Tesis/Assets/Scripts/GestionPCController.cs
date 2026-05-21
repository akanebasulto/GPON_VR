using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// GestionPCController.cs
// Controla el panel NMS de la PC de gestión.
public class GestionPCController : MonoBehaviour
{
    [Header("Panel visual del Canvas")]
    public GameObject panelNMS;

    [Header("Textos del panel (TextMeshPro)")]
    public TMP_Text textoContenido;

    [Header("Referencia al OLT para enviarle configuraciones")]
    public OLTController controladorOLT;

    [Header("Referencia al slider de potencia del OLT")]
    public UnityEngine.UI.Slider sliderPotenciaOLT;

    [Header("Referencia al slider de velocidad del OLT")]
    public UnityEngine.UI.Slider sliderVelocidadOLT;

    private bool panelAbierto = false;
    private bool ignorarFCerrarHastaSoltar = false;
    private float ultimoCambioPanel = -1f;

    [Header("Antirebote de interacción")]
    [Tooltip("Tiempo mínimo entre abrir/cerrar para evitar doble disparo de F")]
    public float cooldownToggle = 0.2f;

    private float potenciaActual = 2.0f;
    private int indiceVelocidad = 2;

    private readonly int[] velocidades = { 10, 50, 100, 300, 600 };

    void Start()
    {
        if (panelNMS != null)
            panelNMS.SetActive(false);

        RefrescarTextoPanel();
    }

    void Update()
    {
        if (!panelAbierto)
            return;

        ProcesarInputPanel();
    }

    public void TogglePanel()
    {
        // Para evitar el comportamiento invertido con interactores externos,
        // este método solo abre si está cerrado (idempotente cuando está abierto).
        if (panelAbierto)
            return;

        if (Time.time - ultimoCambioPanel < cooldownToggle)
            return;

        AbrirPanel();
    }

    // Método explícito para cerrar desde otros scripts (si se necesita).
    public void CerrarPanelExterno()
    {
        if (!panelAbierto)
            return;

        CerrarPanel();
    }

    public void AbrirPanel()
    {
        panelAbierto = true;
        ultimoCambioPanel = Time.time;

        if (panelNMS != null)
            panelNMS.SetActive(true);

        // Evita que la misma pulsación de F (usada para abrir)
        // cierre el panel inmediatamente.
        ignorarFCerrarHastaSoltar = true;
        RefrescarTextoPanel();
    }

    void ProcesarInputPanel()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            potenciaActual = Mathf.Clamp(potenciaActual + 0.5f, -3f, 5f);
            AplicarConfiguracion();
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            potenciaActual = Mathf.Clamp(potenciaActual - 0.5f, -3f, 5f);
            AplicarConfiguracion();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            indiceVelocidad = Mathf.Clamp(indiceVelocidad + 1, 0, velocidades.Length - 1);
            AplicarConfiguracion();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            indiceVelocidad = Mathf.Clamp(indiceVelocidad - 1, 0, velocidades.Length - 1);
            AplicarConfiguracion();
        }

        // Permite cerrar con F después de soltar la tecla usada al abrir.
        if (ignorarFCerrarHastaSoltar && !Keyboard.current.fKey.isPressed)
            ignorarFCerrarHastaSoltar = false;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CerrarPanel();
        }
    }

    void CerrarPanel()
    {
        panelAbierto = false;
        ultimoCambioPanel = Time.time;

        if (panelNMS != null)
            panelNMS.SetActive(false);
    }

    void AplicarConfiguracion()
    {
        if (sliderPotenciaOLT != null)
            sliderPotenciaOLT.value = potenciaActual;

        if (sliderVelocidadOLT != null)
            sliderVelocidadOLT.value = indiceVelocidad;

        RefrescarTextoPanel();
    }

    void RefrescarTextoPanel()
    {
        if (textoContenido == null) return;

        string estadoRed = EvaluarEstadoRed(potenciaActual);

        string contenido =
            "=== CONSOLA NMS - RED GPON ===\n" +
            "\n" +
            "  CONFIGURACION ACTIVA:\n" +
            "  - Potencia TX:    " + potenciaActual.ToString("F1") + " dBm\n" +
            "     (Up/Down para cambiar)\n" +
            "\n" +
            "  - Velocidad/puerto: " + velocidades[indiceVelocidad] + " Mbps\n" +
            "     (Left/Right para cambiar)\n" +
            "\n" +
            "  ESTADO DE LA RED:\n" +
            "  " + estadoRed + "\n" +
            "\n" +
            "  TOPOLOGIA:\n" +
            "  PC -> OLT -> ODF -> Feeder\n" +
            "              -> NAP -> ONT x4\n" +
            "\n" +
            "  Presupuesto optico maximo: 28 dB\n" +
            "  (Estandar GPON ITU-T G.984)\n" +
            "\n" +
            "  [Esc] para cerrar\n" +
            "==============================";

        textoContenido.text = contenido;
    }

    string EvaluarEstadoRed(float potencia)
    {
        if (potencia >= 1f)
            return "OPTIMO - Todos los ONTs activos";
        else if (potencia >= -1f)
            return "DEGRADADO - ONTs 3 y 4 con alerta";
        else
            return "CRITICO - ONTs 3 y 4 sin senal";
    }
}


using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Va en: PC_Gestion (Zona 1)
// Panel configurable: el usuario cambia potencia y velocidad
// con las flechas del teclado cuando el panel está abierto.
// El panel permanece abierto al alejarse; se cierra con F o Escape.

public class GestionPCController : MonoBehaviour
{
    [Header("Panel educativo")]
    public GameObject panelPC;
    public TMP_Text textoPanel;

    [Header("Referencia al OLT")]
    public OLTController controladorOLT;

    // ── Estado interno ──
    private float potenciaActual = 2.0f;
    private int indiceVelocidad = 2;        // índice 2 = 100 Mbps
    private bool panelAbierto = false;

    // DISTANCIA_CIERRE eliminada — el panel ya no se cierra
    // por distancia. Se cierra con F (TogglePanel) o con Escape.

    private int[] velocidades = { 10, 50, 100, 300, 600 };

    void Start()
    {
        if (panelPC != null)
            panelPC.SetActive(false);
    }

    void Update()
    {
        if (!panelAbierto) return;
        if (Keyboard.current == null) return;

        bool cambio = false;

        // Potencia TX (Arriba / Abajo)
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            potenciaActual = Mathf.Clamp(potenciaActual + 0.5f, -3f, 5f);
            cambio = true;
        }
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            potenciaActual = Mathf.Clamp(potenciaActual - 0.5f, -3f, 5f);
            cambio = true;
        }

        // Velocidad contratada (Izquierda / Derecha)
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            indiceVelocidad = Mathf.Clamp(
                indiceVelocidad + 1, 0, velocidades.Length - 1);
            cambio = true;
        }
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            indiceVelocidad = Mathf.Clamp(
                indiceVelocidad - 1, 0, velocidades.Length - 1);
            cambio = true;
        }

        if (cambio)
        {
            AplicarConfiguracion();
            RefrescarPanel();
        }

        // Escape cierra el panel desde cualquier distancia
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CerrarPanel();
    }

    void AplicarConfiguracion()
    {
        if (controladorOLT != null)
            controladorOLT.ActualizarEstado(potenciaActual, indiceVelocidad);
    }

    // ── Métodos de panel (llamados por InteractableZone) ──

    public void TogglePanel()
    {
        if (panelAbierto) CerrarPanel();
        else AbrirPanel();
    }

    public void AbrirPanel()
    {
        panelAbierto = true;
        if (panelPC != null) panelPC.SetActive(true);
        RefrescarPanel();
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelPC != null) panelPC.SetActive(false);
    }

    // ── Contenido del panel ──

    void RefrescarPanel()
    {
        if (textoPanel == null) return;

        string estadoRed;
        int puertos;

        if (potenciaActual >= 0f)
        {
            estadoRed = "[OK] Red completamente operativa";
            puertos = 4;
        }
        else if (potenciaActual >= -2f)
        {
            estadoRed = "[!!] Clientes 3 y 4 con senal degradada";
            puertos = 2;
        }
        else
        {
            estadoRed = "[--] Clientes 3 y 4 sin senal";
            puertos = 0;
        }

        textoPanel.text =
            "<size=115%><b>CONSOLA NMS — PC DE GESTION</b></size>\n" +
            "<color=#AAAAAA>──────────────────────────────</color>\n\n" +
            "<b>Funcion:</b>\n" +
            "Estacion desde la que el tecnico\n" +
            "configura y monitorea la red GPON.\n" +
            "Conectada al OLT via Ethernet.\n\n" +
            "<b>CONFIGURACION ACTIVA:</b>\n" +
            "Potencia TX:  <color=#00FF88>" +
                potenciaActual.ToString("F1") + " dBm</color>\n" +
            "  [Flecha Arriba = +0.5 | Abajo = -0.5]\n\n" +
            "Velocidad DS: <color=#00FF88>" +
                velocidades[indiceVelocidad] + " Mbps</color>\n" +
            "  [Flecha Der = subir | Izq = bajar]\n\n" +
            "<b>TOPOLOGIA:</b>\n" +
            "PC --> OLT --> ODF --> Feeder\n" +
            "               --> NAP 1x8\n" +
            "                   --> ONT x4\n\n" +
            "<b>ESTADO DE LA RED:</b>\n" +
            "<color=#AAAAAA>" + estadoRed + "</color>\n" +
            "Puertos activos: <color=#00FF88>" +
                puertos + " / 8</color>\n\n" +
            "<b>Estandar:</b> <color=#AAAAAA>ITU-T G.984 (GPON)</color>\n\n" +
            "<color=#555555>[F] Cerrar   [Esc] Cerrar</color>";
    }
}

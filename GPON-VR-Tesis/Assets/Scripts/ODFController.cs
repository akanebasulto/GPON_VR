using UnityEngine;
using TMPro;

// Este script va en el GameObject ODF_Panel
public class ODFController : MonoBehaviour
{
    [Header("Panel de UI")]
    public GameObject panelODF;

    [Header("Textos del panel")]
    public TMP_Text textoPotenciaEntrada;   // Recibe del OLT
    public TMP_Text textoPotenciaSalida;    // Calcula y muestra
    public TMP_Text textoFibrasActivas;

    [Header("Referencia al OLT (para leer potencia)")]
    public OLTController oltController;     // Se arrastra en el Inspector

    [Header("Slider del OLT (para leer el valor)")]
    public UnityEngine.UI.Slider sliderPotenciaOLT;

    // Pérdida fija del ODF — valor estándar real
    private const float PERDIDA_ODF = 0.6f; // dB

    private bool panelAbierto = false;

    void Start()
    {
        if (panelODF != null)
            panelODF.SetActive(false);

        // Se suscribe al evento del OLT
        // Cuando el OLT cambie parámetros, este script se entera
        if (oltController != null)
            oltController.OnParameterChanged.AddListener(ActualizarDatos);
    }

    public void TogglePanel()
    {
        panelAbierto = !panelAbierto;
        panelODF.SetActive(panelAbierto);

        // Al abrir el panel, actualiza los datos inmediatamente
        if (panelAbierto && sliderPotenciaOLT != null)
            ActualizarDatos(sliderPotenciaOLT.value);
    }

    // Se llama automáticamente cuando el OLT dispara su evento
    // potenciaOLT es el valor actual del slider de potencia
    void ActualizarDatos(float potenciaOLT)
    {
        float potenciaSalida = potenciaOLT - PERDIDA_ODF;

        if (textoPotenciaEntrada != null)
            textoPotenciaEntrada.text =
                "Potencia desde OLT: " + potenciaOLT.ToString("F1") + " dBm";

        if (textoPotenciaSalida != null)
            textoPotenciaSalida.text =
                "Potencia tras ODF:  " + potenciaSalida.ToString("F1") + " dBm";
    }
}
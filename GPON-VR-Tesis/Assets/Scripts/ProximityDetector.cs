using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class ProximityDetector : MonoBehaviour
{
    [Header("Textos del HUD")]
    public TMP_Text textoPromptF;
    public TMP_Text textoPromptE;
    public TMP_Text textoSeleccion;   // nuevo — para el alternador Tab

    [Header("Contenedor visual del HUD")]
    [Tooltip("El Panel (Image) que agrupa todos los prompts")]
    public GameObject contenedorPrompts;

    [Header("Ángulo de visión")]
    [Range(0f, 1f)]
    public float umbralDireccion = 0.4f;

    // Lista de todas las zonas donde está el jugador
    private List<InteractableZone> zonasDisponibles
        = new List<InteractableZone>();

    // Índice del componente seleccionado con Tab
    private int indiceSeleccionado = 0;

    // ──────────────────────────────────────────────────────
    // DETECCIÓN DE PROXIMIDAD
    // ──────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        InteractableZone zona = other.GetComponent<InteractableZone>();
        if (zona == null) return;
        if (!zonasDisponibles.Contains(zona))
            zonasDisponibles.Add(zona);
    }

    void OnTriggerExit(Collider other)
    {
        InteractableZone zona = other.GetComponent<InteractableZone>();
        if (zona == null) return;

        int idx = zonasDisponibles.IndexOf(zona);
        zonasDisponibles.Remove(zona);

        // Ajusta el índice si la zona que salió estaba
        // antes o en la posición seleccionada
        if (idx <= indiceSeleccionado && indiceSeleccionado > 0)
            indiceSeleccionado--;
    }

    // ──────────────────────────────────────────────────────
    // UPDATE
    // ──────────────────────────────────────────────────────

    void Update()
    {
        // Limpia referencias nulas por si algún objeto
        // fue desactivado sin salir del trigger
        zonasDisponibles.RemoveAll(z => z == null);

        if (zonasDisponibles.Count == 0)
        {
            OcultarTodo();
            return;
        }

        // Verifica que el jugador está mirando hacia
        // al menos una de las zonas disponibles
        if (!HayZonaEnfrente())
        {
            OcultarTodo();
            return;
        }

        // Mantiene el índice dentro del rango válido
        indiceSeleccionado = Mathf.Clamp(
            indiceSeleccionado, 0, zonasDisponibles.Count - 1);

        ProcesarTeclas();

        InteractableZone zonaActiva = zonasDisponibles[indiceSeleccionado];
        ActualizarHUD(zonaActiva);
    }

    // ──────────────────────────────────────────────────────
    // VERIFICACIÓN DE DIRECCIÓN
    // ──────────────────────────────────────────────────────

    bool HayZonaEnfrente()
    {
        if (Camera.main == null) return false;

        foreach (InteractableZone zona in zonasDisponibles)
        {
            if (zona == null) continue;

            Vector3 dir = (zona.transform.position
                - Camera.main.transform.position).normalized;

            float dot = Vector3.Dot(
                Camera.main.transform.forward, dir);

            if (dot > umbralDireccion) return true;
        }
        return false;
    }

    // ──────────────────────────────────────────────────────
    // TECLAS
    // ──────────────────────────────────────────────────────

    void ProcesarTeclas()
    {
        if (Keyboard.current == null) return;

        // Tab — alternar entre componentes (solo si hay más de uno)
        if (zonasDisponibles.Count > 1 &&
            Keyboard.current.tabKey.wasPressedThisFrame)
        {
            indiceSeleccionado =
                (indiceSeleccionado + 1) % zonasDisponibles.Count;
        }

        InteractableZone zonaActiva = zonasDisponibles[indiceSeleccionado];

        // F — abrir panel del componente seleccionado
        if (Keyboard.current.fKey.wasPressedThisFrame)
            zonaActiva.InteractuarConPanel();

        // E — interacción física del componente seleccionado
        if (Keyboard.current.eKey.wasPressedThisFrame)
            zonaActiva.InteractuarFisicamente();
    }

    // ──────────────────────────────────────────────────────
    // HUD
    // ──────────────────────────────────────────────────────

    void ActualizarHUD(InteractableZone zonaActiva)
    {
        if (contenedorPrompts != null)
            contenedorPrompts.SetActive(true);

        // ── Prompt F ──
        if (textoPromptF != null)
            textoPromptF.text = "[F]  " + zonaActiva.nombreLegible;

        // ── Prompt E ──
        if (textoPromptE != null)
        {
            if (zonaActiva.tieneInteraccionFisica)
            {
                textoPromptE.text = "[E]  " + zonaActiva.textoAccionE;
                textoPromptE.gameObject.SetActive(true);
            }
            else
            {
                textoPromptE.gameObject.SetActive(false);
            }
        }

        // ── Alternador Tab (solo cuando hay más de una zona) ──
        if (textoSeleccion != null)
        {
            if (zonasDisponibles.Count > 1)
            {
                string contenido = "<size=85%><color=#AAAAAA>[Tab] Alternar:</color></size>\n";

                for (int i = 0; i < zonasDisponibles.Count; i++)
                {
                    if (zonasDisponibles[i] == null) continue;

                    if (i == indiceSeleccionado)
                        // Seleccionado: texto blanco con flecha
                        contenido += "<color=#FFFFFF>->  " + zonasDisponibles[i].nombreLegible + "</color>";
                    else
                        // No seleccionado: texto gris sin flecha
                        contenido += "<color=#888888>    " +
                            zonasDisponibles[i].nombreLegible +
                            "</color>";

                    if (i < zonasDisponibles.Count - 1)
                        contenido += "\n";
                }

                textoSeleccion.text = contenido;
                textoSeleccion.gameObject.SetActive(true);
            }
            else
            {
                textoSeleccion.gameObject.SetActive(false);
            }
        }
    }

    void OcultarTodo()
    {
        if (contenedorPrompts != null)
            contenedorPrompts.SetActive(false);
    }

    public void RefrescarPromptE()
    {
        if (zonasDisponibles.Count > 0)
        {
            InteractableZone zonaActiva =
                zonasDisponibles[indiceSeleccionado];
            if (zonaActiva != null)
                ActualizarHUD(zonaActiva);
        }
    }
}
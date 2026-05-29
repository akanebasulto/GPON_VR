using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class ProximityDetector : MonoBehaviour
{
    [Header("Textos del HUD")]
    public TMP_Text textoPromptF;
    public TMP_Text textoPromptE;
    public TMP_Text textoSeleccion;

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

    // ── OPTIMIZACIÓN: caché de Camera.main ──
    // Camera.main llama FindGameObjectWithTag internamente cada vez
    // que se accede. Cacheándola en Start() esa búsqueda ocurre
    // una sola vez en lugar de 120 veces por segundo.
    private Camera camaraCache;

    // ── OPTIMIZACIÓN: control de cambios en el HUD ──
    // ActualizarHUD solo se ejecuta cuando algo cambia,
    // no en cada frame aunque el estado sea idéntico.
    private InteractableZone zonaActivaAnterior = null;
    private int indiceAnterior = -1;
    private bool hudVisible = false;

    // ── OPTIMIZACIÓN: limpieza de nulos con timer ──
    // RemoveAll con lambda genera basura para el GC cada frame.
    // Con un timer se ejecuta solo 2 veces por segundo.
    private float timerLimpieza = 0f;
    private const float INTERVALO_LIMPIEZA = 0.5f;

    // ──────────────────────────────────────────────────────
    // INICIO
    // ──────────────────────────────────────────────────────

    void Start()
    {
        // Cachear Camera.main una sola vez
        camaraCache = Camera.main;
    }

    // ──────────────────────────────────────────────────────
    // DETECCIÓN DE PROXIMIDAD
    // ──────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        InteractableZone zona = other.GetComponent<InteractableZone>();
        if (zona == null) return;
        if (!zonasDisponibles.Contains(zona))
        {
            zonasDisponibles.Add(zona);
            ForzarRefrescoHUD(); // nueva zona: forzar redibujado
        }
    }

    void OnTriggerExit(Collider other)
    {
        InteractableZone zona = other.GetComponent<InteractableZone>();
        if (zona == null) return;

        int idx = zonasDisponibles.IndexOf(zona);
        zonasDisponibles.Remove(zona);

        if (idx <= indiceSeleccionado && indiceSeleccionado > 0)
            indiceSeleccionado--;

        ForzarRefrescoHUD(); // zona salió: forzar redibujado
    }

    // ──────────────────────────────────────────────────────
    // UPDATE
    // ──────────────────────────────────────────────────────

    void Update()
    {
        // Limpieza de nulos: solo cada INTERVALO_LIMPIEZA segundos
        timerLimpieza += Time.deltaTime;
        if (timerLimpieza >= INTERVALO_LIMPIEZA)
        {
            timerLimpieza = 0f;
            int antes = zonasDisponibles.Count;
            zonasDisponibles.RemoveAll(z => z == null);
            if (zonasDisponibles.Count != antes) ForzarRefrescoHUD();
        }

        if (zonasDisponibles.Count == 0)
        {
            if (hudVisible) OcultarTodo();
            return;
        }

        bool hayZona = HayZonaEnfrente();

        if (!hayZona)
        {
            if (hudVisible) OcultarTodo();
            return;
        }

        // Mantiene el índice dentro del rango válido
        indiceSeleccionado = Mathf.Clamp(
            indiceSeleccionado, 0, zonasDisponibles.Count - 1);

        ProcesarTeclas();

        // ── Solo actualizar el HUD si algo cambió ──
        InteractableZone zonaActiva = zonasDisponibles[indiceSeleccionado];
        bool cambio = (zonaActiva != zonaActivaAnterior)
                   || (indiceSeleccionado != indiceAnterior)
                   || !hudVisible;

        if (cambio)
        {
            zonaActivaAnterior = zonaActiva;
            indiceAnterior = indiceSeleccionado;
            ActualizarHUD(zonaActiva);
        }
    }

    // ──────────────────────────────────────────────────────
    // VERIFICACIÓN DE DIRECCIÓN
    // ──────────────────────────────────────────────────────

    bool HayZonaEnfrente()
    {
        // Usa camaraCache en lugar de Camera.main
        if (camaraCache == null)
        {
            camaraCache = Camera.main; // recupera si fue nula
            if (camaraCache == null) return false;
        }

        Vector3 forward = camaraCache.transform.forward;
        Vector3 camPos = camaraCache.transform.position;

        foreach (InteractableZone zona in zonasDisponibles)
        {
            if (zona == null) continue;

            Vector3 dir = (zona.transform.position - camPos).normalized;
            if (Vector3.Dot(forward, dir) > umbralDireccion) return true;
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
            ForzarRefrescoHUD();
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

        hudVisible = true;

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
                string contenido =
                    "<size=85%><color=#AAAAAA>[Tab] Alternar:</color></size>\n";

                for (int i = 0; i < zonasDisponibles.Count; i++)
                {
                    if (zonasDisponibles[i] == null) continue;

                    if (i == indiceSeleccionado)
                        contenido += "<color=#FFFFFF>->  "
                            + zonasDisponibles[i].nombreLegible
                            + "</color>";
                    else
                        contenido += "<color=#888888>    "
                            + zonasDisponibles[i].nombreLegible
                            + "</color>";

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
        hudVisible = false;
        zonaActivaAnterior = null;
        indiceAnterior = -1;

        if (contenedorPrompts != null)
            contenedorPrompts.SetActive(false);
    }

    // Fuerza que el próximo frame redibuje el HUD aunque
    // la zona activa sea la misma (p.ej. al entrar/salir una zona)
    void ForzarRefrescoHUD()
    {
        zonaActivaAnterior = null;
        indiceAnterior = -1;
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

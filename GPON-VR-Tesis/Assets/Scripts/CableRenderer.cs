using System.Collections.Generic;
using UnityEngine;

// Requiere un LineRenderer en el mismo objeto.
[RequireComponent(typeof(LineRenderer))]
// Funciona en edición y en ejecución.
[ExecuteAlways]
public class CableRenderer : MonoBehaviour
{
    [Header("Puntos de conexión")]
    public Transform puntoInicio;
    public Transform puntoFin;

    [Header("Puntos manuales (opcionales)")]
    public Transform[] puntosIntermedios;

    [Header("Apariencia")]
    [Range(0f, 0.2f)]
    public float curvatura = 0.04f;

    [Range(0.0005f, 0.02f)]
    public float grosor = 0.004f;

    [Range(1, 80)]
    public int subdivisiones = 24;

    [Header("Bordeo de collider")]
    [Range(0.002f, 0.08f)]
    // Distancia mínima al borde del collider.
    public float separacionColision = 0.015f;

    [Range(0.01f, 0.3f)]
    // Cuánto rodea lateralmente para no atravesar el volumen.
    public float radioBordeo = 0.08f;

    // Capas sólidas que el cable debe evitar.
    public LayerMask mascaraColision = ~0;

    [Header("Pulso")]
    public bool animarPulso = false;
    public Color colorPulso = new Color(1f, 0.53f, 0f);
    public float velocidadPulso = 1.5f;

    private LineRenderer lr;
    private float offsetPulso;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        ConfigurarLineRenderer();
    }

    private void Update()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr == null || puntoInicio == null || puntoFin == null) return;

        ConfigurarLineRenderer();
        ActualizarCable();

        if (animarPulso && Application.isPlaying) AnimarPulso();
    }

    private void OnValidate()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        ConfigurarLineRenderer();
        if (puntoInicio != null && puntoFin != null) ActualizarCable();
    }

    private void ConfigurarLineRenderer()
    {
        if (lr == null) return;

        lr.startWidth = grosor;
        lr.endWidth = grosor;
        lr.useWorldSpace = true;
        lr.numCornerVertices = 8;
        lr.numCapVertices = 8;
        lr.textureMode = animarPulso ? LineTextureMode.Tile : LineTextureMode.Stretch;

        if (animarPulso && lr.material != null)
            lr.material.SetColor("_EmissionColor", colorPulso * 1.2f);
    }

    private void ActualizarCable()
    {
        // 1) Controles base.
        List<Vector3> controles = new List<Vector3> { puntoInicio.position };

        if (puntosIntermedios != null)
        {
            for (int i = 0; i < puntosIntermedios.Length; i++)
                if (puntosIntermedios[i] != null) controles.Add(puntosIntermedios[i].position);
        }

        controles.Add(puntoFin.position);

        // 2) Si no hay puntos manuales, genera subdivisiones inteligentes para bordear collider.
        if ((puntosIntermedios == null || puntosIntermedios.Length == 0) && controles.Count == 2)
            InsertarSubdivisionesDeBordeo(controles);

        // 3) Si hay puntos manuales, respetarlos exactamente.
        bool hayPuntosManualeValidos = HayPuntosIntermediosValidos();
        List<Vector3> puntos = hayPuntosManualeValidos
            ? GenerarPuntosRespetandoControles(controles)
            : GenerarPuntosPorLongitudTotal(controles);

        // 4) Curvatura global (solo cuando no hay puntos manuales).
        if (!hayPuntosManualeValidos) AplicarCurvaturaGlobal(puntos);

        // 5) Copia al LineRenderer.
        lr.positionCount = puntos.Count;
        for (int i = 0; i < puntos.Count; i++) lr.SetPosition(i, puntos[i]);
    }

    private void InsertarSubdivisionesDeBordeo(List<Vector3> controles)
    {
        Vector3 inicio = controles[0];
        Vector3 fin = controles[1];
        Vector3 dir = fin - inicio;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;
        dir /= dist;

        float radio = Mathf.Max(grosor * 0.5f, 0.001f);

        // Detecta primer impacto en el camino directo.
        if (!Physics.SphereCast(inicio, radio, dir, out RaycastHit hitEntrada, dist, mascaraColision, QueryTriggerInteraction.Ignore))
            return;

        // Detecta punto de salida aproximado casteando desde el final hacia atrás.
        Vector3 dirBack = -dir;
        bool hitBack = Physics.SphereCast(fin, radio, dirBack, out RaycastHit hitSalida, dist, mascaraColision, QueryTriggerInteraction.Ignore);

        // Punto en borde de entrada del collider.
        Vector3 bordeEntrada = hitEntrada.point + hitEntrada.normal * (separacionColision + radio);

        // Si no se detecta salida, usamos el mismo impacto para mantener robustez.
        Vector3 bordeSalida = hitBack
            ? hitSalida.point + hitSalida.normal * (separacionColision + radio)
            : bordeEntrada + dir * (radioBordeo * 0.5f);

        // Vector lateral para bordear el objeto.
        Vector3 lateral = Vector3.Cross(dir, Vector3.up);
        if (lateral.sqrMagnitude < 0.0001f) lateral = Vector3.Cross(dir, Vector3.right);
        lateral.Normalize();

        // Dos opciones de rodeo: lateral positivo o negativo.
        Vector3 rodeoA = (bordeEntrada + bordeSalida) * 0.5f + lateral * radioBordeo;
        Vector3 rodeoB = (bordeEntrada + bordeSalida) * 0.5f - lateral * radioBordeo;

        // Elegimos la opción con menor longitud total (ruta más recta posible).
        float rutaA = Vector3.Distance(inicio, bordeEntrada) + Vector3.Distance(bordeEntrada, rodeoA) + Vector3.Distance(rodeoA, bordeSalida) + Vector3.Distance(bordeSalida, fin);
        float rutaB = Vector3.Distance(inicio, bordeEntrada) + Vector3.Distance(bordeEntrada, rodeoB) + Vector3.Distance(rodeoB, bordeSalida) + Vector3.Distance(bordeSalida, fin);

        Vector3 rodeo = rutaA <= rutaB ? rodeoA : rodeoB;

        // Inserta subdivisiones de ruta: borde entrada -> rodeo -> borde salida.
        controles.Insert(1, bordeEntrada);
        controles.Insert(2, rodeo);
        controles.Insert(3, bordeSalida);
    }


    private bool HayPuntosIntermediosValidos()
    {
        if (puntosIntermedios == null || puntosIntermedios.Length == 0) return false;

        for (int i = 0; i < puntosIntermedios.Length; i++)
            if (puntosIntermedios[i] != null) return true;

        return false;
    }

    private List<Vector3> GenerarPuntosRespetandoControles(List<Vector3> controles)
    {
        // Esta ruta garantiza pasar por cada punto de control exacto.
        List<Vector3> salida = new List<Vector3>(subdivisiones + controles.Count + 2);
        if (controles.Count == 0) return salida;

        salida.Add(controles[0]);

        int tramos = Mathf.Max(1, controles.Count - 1);
        int subPorTramo = Mathf.Max(1, subdivisiones / tramos);

        for (int t = 0; t < tramos; t++)
        {
            Vector3 a = controles[t];
            Vector3 b = controles[t + 1];

            for (int i = 1; i <= subPorTramo; i++)
            {
                float u = i / (float)subPorTramo;
                Vector3 p = Vector3.Lerp(a, b, u);

                // Curvatura local ligera para que no se vea totalmente rígido.
                float arco = 4f * u * (1f - u);
                p += Vector3.down * ((curvatura * 0.35f) * arco);

                salida.Add(p);
            }

            // Fuerza exactitud del control al final de cada tramo.
            salida[salida.Count - 1] = b;
        }

        return salida;
    }
    private List<Vector3> GenerarPuntosPorLongitudTotal(List<Vector3> controles)
    {
        List<Vector3> salida = new List<Vector3>(subdivisiones + 2);
        if (controles.Count < 2)
        {
            salida.AddRange(controles);
            return salida;
        }

        float total = 0f;
        for (int i = 0; i < controles.Count - 1; i++) total += Vector3.Distance(controles[i], controles[i + 1]);
        if (total < 0.0001f)
        {
            salida.Add(controles[0]);
            return salida;
        }

        int muestras = Mathf.Max(1, subdivisiones);
        for (int s = 0; s <= muestras; s++)
        {
            float t = s / (float)muestras;
            float objetivo = t * total;
            salida.Add(PuntoEnPolilinea(controles, objetivo));
        }

        return salida;
    }

    private Vector3 PuntoEnPolilinea(List<Vector3> controles, float distanciaObjetivo)
    {
        float acumulado = 0f;

        for (int i = 0; i < controles.Count - 1; i++)
        {
            Vector3 a = controles[i];
            Vector3 b = controles[i + 1];
            float d = Vector3.Distance(a, b);
            if (d < 0.0001f) continue;

            if (acumulado + d >= distanciaObjetivo)
            {
                float local = (distanciaObjetivo - acumulado) / d;
                return Vector3.Lerp(a, b, local);
            }

            acumulado += d;
        }

        return controles[controles.Count - 1];
    }

    private void AplicarCurvaturaGlobal(List<Vector3> puntos)
    {
        if (puntos.Count < 3 || curvatura <= 0f) return;

        int ultimo = puntos.Count - 1;
        for (int i = 1; i < ultimo; i++)
        {
            float t = i / (float)ultimo;
            float arco = 4f * t * (1f - t);
            puntos[i] += Vector3.down * (curvatura * arco);
        }
    }

    private void AnimarPulso()
    {
        if (lr == null || lr.material == null) return;

        offsetPulso += Time.deltaTime * velocidadPulso;
        if (offsetPulso > 1f) offsetPulso -= 1f;
        lr.material.mainTextureOffset = new Vector2(offsetPulso, 0f);
    }
}


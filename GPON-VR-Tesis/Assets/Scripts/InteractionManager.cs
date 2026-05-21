using UnityEngine;
using UnityEngine.InputSystem;   // ← Importante
using TMPro;

public class InteractionManager : MonoBehaviour
{
    [Header("Configuración del Raycast")]
    public float distanciaMaxima = 2.5f;
    public LayerMask capaInteractable;

    [Header("Prompt visual")]
    public TMP_Text textoPrompt;

    [Header("Input System - Acciones")]
    public InputActionReference interactAction;   // Asignar la acción "F" aquí

    private GameObject objetoActual = null;

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void Update()
    {
        DetectarObjetoMirado();

        // Nueva forma recomendada con Input System
        if (interactAction != null && interactAction.action.WasPressedThisFrame() && objetoActual != null)
        {
            InteractuarConObjeto(objetoActual);
        }
    }

    void DetectarObjetoMirado()

    {

        Ray rayo = Camera.main.ScreenPointToRay(

            new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)

        );

        if (Physics.Raycast(rayo, out RaycastHit golpe, distanciaMaxima, capaInteractable))

        {

            // El Raycast chocó con algo — ahora buscamos hacia arriba

            // en la jerarquía hasta encontrar un objeto con Tag Interactable

            GameObject interactable = BuscarInteractable(golpe.collider.gameObject);

            if (interactable != null)

            {

                objetoActual = interactable;

                MostrarPrompt(interactable.name);

            }

            else

            {

                LimpiarPrompt();

            }

        }

        else

        {

            LimpiarPrompt();

        }

    }

    // Sube por la jerarquía del objeto golpeado buscando

    // el primer GameObject que tenga Tag "Interactable"

    GameObject BuscarInteractable(GameObject objetoGolpeado)
    {
        // Primero revisa el objeto mismo
        if (objetoGolpeado.CompareTag("Interactable"))
            return objetoGolpeado;

        // Luego sube nivel por nivel hacia la raíz
        Transform padre = objetoGolpeado.transform.parent;
        while (padre != null)
        {
            if (padre.CompareTag("Interactable"))
                return padre.gameObject;

            padre = padre.parent;
        }

        // No encontró ningún Interactable en la jerarquía
        return null;
    }

    void InteractuarConObjeto(GameObject obj)
    {
        // Gestión PC
        GestionPCController pc = obj.GetComponent<GestionPCController>() ?? obj.GetComponentInParent<GestionPCController>();
        if (pc != null) { pc.TogglePanel(); return; }

        // OLT
        OLTController olt = obj.GetComponent<OLTController>() ?? obj.GetComponentInParent<OLTController>();
        if (olt != null) { olt.TogglePanel(); return; }

        // ODF
        ODFController odf = obj.GetComponent<ODFController>() ?? obj.GetComponentInParent<ODFController>();
        if (odf != null) { odf.TogglePanel(); return; }

        // Puedes añadir más controladores aquí (NAP, ONT, etc.)
    }

    void MostrarPrompt(string nombreObjeto)
    {
        if (textoPrompt != null)
            textoPrompt.text = "[F] Inspeccionar " + nombreObjeto;
    }

    void LimpiarPrompt()
    {
        objetoActual = null;
        if (textoPrompt != null)
            textoPrompt.text = "";
    }
}
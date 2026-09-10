using UnityEngine;

/// <summary>
/// Zona invisible que detecta al jugador y le avisa a la puerta.
/// Va en un objeto hijo de la puerta, con un BoxCollider en Is Trigger = ON.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaPuerta : MonoBehaviour
{
    [Tooltip("La puerta que se abre. Normalmente es el objeto padre.")]
    public PuertaTrigger puerta;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        puerta = GetComponentInParent<PuertaTrigger>();
    }

    void OnTriggerEnter(Collider otro)
    {
        if (puerta == null) return;
        if (otro.GetComponentInParent<PlayerController>() == null) return;
        puerta.Abrir();
    }

    void OnTriggerExit(Collider otro)
    {
        if (puerta == null) return;
        if (otro.GetComponentInParent<PlayerController>() == null) return;
        puerta.Cerrar();
    }
}

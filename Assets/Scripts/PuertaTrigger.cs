using UnityEngine;

/// <summary>
/// Puerta corrediza. Va en el objeto "puerta".
/// Necesita DOS colliders: el solido que bloquea el paso y una zona
/// de deteccion (Is Trigger = ON) mas grande a su alrededor.
/// El configurador crea esa zona automaticamente como objeto hijo.
/// </summary>
public class PuertaTrigger : MonoBehaviour
{
    [Header("Movimiento de apertura")]
    [Tooltip("Hacia donde se desliza la puerta, en espacio local. (1,0,0) = a su derecha.")]
    public Vector3 direccionApertura = Vector3.right;

    [Tooltip("Cuantos metros se desliza al abrirse.")]
    public float distanciaApertura = 4f;

    [Tooltip("Que tan rapido se abre y se cierra.")]
    public float velocidadApertura = 3f;

    [Header("Comportamiento")]
    [Tooltip("Si esta activo, la puerta se vuelve a cerrar cuando el jugador se aleja.")]
    public bool cerrarAlSalir = false;

    [Header("Audio (opcional)")]
    public AudioClip sonidoApertura;

    private Vector3 posicionCerrada;
    private Vector3 posicionAbierta;
    private bool abierta;
    private bool sonoYa;
    private AudioSource audioSource;

    void Start()
    {
        posicionCerrada = transform.position;
        posicionAbierta = posicionCerrada + transform.TransformDirection(direccionApertura.normalized) * distanciaApertura;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        Vector3 objetivo = abierta ? posicionAbierta : posicionCerrada;

        // MoveTowards da un desplazamiento constante y llega exacto al destino.
        transform.position = Vector3.MoveTowards(
            transform.position,
            objetivo,
            velocidadApertura * Time.deltaTime);
    }

    /// <summary>Llamado por la zona de deteccion hija.</summary>
    public void Abrir()
    {
        if (abierta) return;
        abierta = true;

        if (!sonoYa && sonidoApertura != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoApertura);
            sonoYa = true;
        }

        Debug.Log("[Puerta] Abriendo");
    }

    public void Cerrar()
    {
        if (!cerrarAlSalir) return;
        abierta = false;
        Debug.Log("[Puerta] Cerrando");
    }

    void OnDrawGizmosSelected()
    {
        Vector3 desde = Application.isPlaying ? posicionCerrada : transform.position;
        Vector3 hasta = desde + transform.TransformDirection(direccionApertura.normalized) * distanciaApertura;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(desde, hasta);
        Gizmos.DrawWireSphere(hasta, 0.3f);
    }
}

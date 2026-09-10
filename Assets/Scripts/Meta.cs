using UnityEngine;

/// <summary>
/// Meta del nivel. Va en el cubo final. El collider queda SOLIDO: la esfera
/// aterriza encima del cubo y ahi se dispara el mensaje y el sonido.
/// Muestra un mensaje distinto segun si juntaste todas las botellas o no.
/// </summary>
public class Meta : MonoBehaviour
{
    [Header("Mensajes")]
    [TextArea]
    public string mensajeIncompleto = "Llegaste a la meta!";

    [TextArea]
    public string mensajeCompleto = "Llegaste a la meta... eres un alcoholico!";

    [Header("Comportamiento")]
    [Tooltip("Si esta activo, la meta solo se puede activar una vez.")]
    public bool soloUnaVez = true;

    [Tooltip("Si esta activo, el jugador deja de moverse al llegar a la meta.")]
    public bool congelarJugador = true;

    [Header("Audio (opcional)")]
    public AudioClip sonidoMeta;

    private bool yaLlego;

    void ReproducirSonido()
    {
        if (sonidoMeta == null)
        {
            Debug.LogWarning("[Meta] No hay sonido asignado en el campo Sonido Meta.");
            return;
        }

        // Con un AudioSource propio en 2D se oye parejo sin importar que tan
        // lejos este la camara del cubo de meta.
        var fuente = GetComponent<AudioSource>();
        if (fuente != null)
        {
            fuente.spatialBlend = 0f;
            fuente.PlayOneShot(sonidoMeta);
            Debug.Log($"[Meta] Sonando '{sonidoMeta.name}' ({sonidoMeta.length:0.0} s).");
        }
        else
        {
            AudioSource.PlayClipAtPoint(sonidoMeta, Camera.main != null
                ? Camera.main.transform.position
                : transform.position);
        }
    }

    // Funciona de las dos formas: si el cubo es solido, la esfera lo toca y
    // entra por OnCollisionEnter; si lo pones en trigger, entra por OnTriggerEnter.
    void OnCollisionEnter(Collision colision)
    {
        Llegar(colision.collider);
    }

    void OnTriggerEnter(Collider otro)
    {
        Llegar(otro);
    }

    void Llegar(Collider otro)
    {
        if (soloUnaVez && yaLlego) return;

        var jugador = otro.GetComponentInParent<PlayerController>();
        if (jugador == null) return;

        yaLlego = true;

        bool completo = GameManager.Instancia != null && GameManager.Instancia.TodasRecolectadas;
        string mensaje = completo ? mensajeCompleto : mensajeIncompleto;

        if (GameManager.Instancia != null)
            GameManager.Instancia.MostrarMensajeFinal(mensaje);
        else
            Debug.Log(mensaje);

        ReproducirSonido();

        if (congelarJugador)
        {
            jugador.enabled = false;
            var rb = jugador.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
    }
}

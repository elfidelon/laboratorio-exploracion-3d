using UnityEngine;

/// <summary>
/// Botella recolectable. Va en cada objeto "corona".
/// Al tocarla el jugador, suma 1 al contador y el objeto desaparece.
/// </summary>
public class Coleccionable : MonoBehaviour
{
    [Header("Comportamiento")]
    [Tooltip("Si esta activo, la botella gira sobre si misma para que se note.")]
    public bool girar = true;

    [Tooltip("Grados por segundo que gira la botella.")]
    public float velocidadGiro = 90f;

    [Tooltip("Si esta activo, la botella flota suavemente arriba y abajo.")]
    public bool flotar = true;

    public float alturaFlote = 0.15f;
    public float velocidadFlote = 2f;

    [Header("Efectos (opcional)")]
    public AudioClip sonidoRecoleccion;
    public GameObject efectoAlRecolectar;

    private Vector3 posicionInicial;
    private bool yaRecolectada;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        if (girar)
            transform.Rotate(Vector3.up * velocidadGiro * Time.deltaTime, Space.World);

        if (flotar)
        {
            float desfase = Mathf.Sin(Time.time * velocidadFlote) * alturaFlote;
            transform.position = posicionInicial + Vector3.up * desfase;
        }
    }

    void OnTriggerEnter(Collider otro)
    {
        Recolectar(otro);
    }

    // Por si la botella termina con un collider solido en vez de trigger,
    // tambien respondemos a la colision fisica.
    void OnCollisionEnter(Collision colision)
    {
        Recolectar(colision.collider);
    }

    void Recolectar(Collider otro)
    {
        if (yaRecolectada) return;

        // Solo cuenta si quien toca la botella es el jugador.
        if (otro.GetComponentInParent<PlayerController>() == null) return;

        yaRecolectada = true;

        if (sonidoRecoleccion != null)
            AudioSource.PlayClipAtPoint(sonidoRecoleccion, transform.position);

        if (efectoAlRecolectar != null)
            Instantiate(efectoAlRecolectar, transform.position, Quaternion.identity);

        if (GameManager.Instancia != null)
            GameManager.Instancia.SumarBotella();

        gameObject.SetActive(false);
    }
}

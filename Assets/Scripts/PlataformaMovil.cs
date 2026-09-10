using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Plataforma que va y viene de izquierda a derecha sobre el eje X.
/// Ademas lleva consigo al jugador cuando esta parado encima.
/// </summary>
public class PlataformaMovil : MonoBehaviour
{
    [Header("Recorrido")]
    [Tooltip("Cuantos metros se mueve hacia cada lado desde su punto de inicio.")]
    public float distancia = 4f;

    [Tooltip("Velocidad del recorrido en m/s.")]
    public float velocidad = 2f;

    [Tooltip("Espera en segundos al llegar a cada extremo. Dejalo en 0 para que no pare.")]
    public float esperaEnExtremos = 0.5f;

    [Tooltip("Si esta activo, arranca moviendose hacia la izquierda.")]
    public bool empezarHaciaLaIzquierda = false;

    private Vector3 puntoIzquierdo;
    private Vector3 puntoDerecho;
    private Vector3 destino;
    private float esperaRestante;

    // Quien va montado en la plataforma en este momento.
    private readonly HashSet<Transform> pasajeros = new HashSet<Transform>();

    void Start()
    {
        Vector3 inicio = transform.position;
        puntoIzquierdo = inicio + Vector3.left * distancia;
        puntoDerecho = inicio + Vector3.right * distancia;
        destino = empezarHaciaLaIzquierda ? puntoIzquierdo : puntoDerecho;
    }

    void FixedUpdate()
    {
        if (esperaRestante > 0f)
        {
            esperaRestante -= Time.fixedDeltaTime;
            return;
        }

        Vector3 anterior = transform.position;

        transform.position = Vector3.MoveTowards(
            anterior, destino, velocidad * Time.fixedDeltaTime);

        // Cuanto se movio realmente en este paso de fisicas.
        Vector3 delta = transform.position - anterior;

        // Al jugador le sumamos ese mismo desplazamiento; si no, la plataforma
        // se le iria de abajo de los pies porque su script fija la velocidad
        // cada FixedUpdate y borraria cualquier arrastre por friccion.
        foreach (var pasajero in pasajeros)
        {
            if (pasajero != null) pasajero.position += delta;
        }

        if (Vector3.Distance(transform.position, destino) < 0.001f)
        {
            destino = (destino == puntoDerecho) ? puntoIzquierdo : puntoDerecho;
            esperaRestante = esperaEnExtremos;
        }
    }

    void OnCollisionEnter(Collision colision)
    {
        var jugador = colision.collider.GetComponentInParent<PlayerController>();
        if (jugador != null) pasajeros.Add(jugador.transform);
    }

    void OnCollisionExit(Collision colision)
    {
        var jugador = colision.collider.GetComponentInParent<PlayerController>();
        if (jugador != null) pasajeros.Remove(jugador.transform);
    }

    // Dibuja el recorrido en la vista Scene para poder ajustarlo a ojo.
    void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying
            ? (puntoIzquierdo + puntoDerecho) * 0.5f
            : transform.position;

        Vector3 izq = centro + Vector3.left * distancia;
        Vector3 der = centro + Vector3.right * distancia;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(izq, der);
        Gizmos.DrawWireCube(izq, transform.localScale);
        Gizmos.DrawWireCube(der, transform.localScale);
    }
}

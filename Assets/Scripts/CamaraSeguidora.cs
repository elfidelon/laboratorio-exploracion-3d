using UnityEngine;

/// <summary>
/// Camara en tercera persona con seguimiento suave.
/// Va en la Main Camera; arrastra la esfera al campo "objetivo".
/// </summary>
public class CamaraSeguidora : MonoBehaviour
{
    [Tooltip("El jugador al que sigue la camara.")]
    public Transform objetivo;

    [Tooltip("Posicion de la camara respecto al jugador.")]
    public Vector3 desplazamiento = new Vector3(0f, 6f, -9f);

    [Tooltip("Que tan suave sigue la camara. Valores chicos = mas suave.")]
    public float suavizado = 5f;

    [Tooltip("Si esta activo, la camara siempre mira hacia el jugador.")]
    public bool mirarAlObjetivo = true;

    void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 destino = objetivo.position + desplazamiento;

        transform.position = Vector3.Lerp(
            transform.position,
            destino,
            1f - Mathf.Exp(-suavizado * Time.deltaTime));

        if (mirarAlObjetivo)
            transform.LookAt(objetivo.position + Vector3.up * 0.5f);
    }
}

using UnityEngine;
using TMPro;

/// <summary>
/// Lleva la cuenta de botellas recolectadas y actualiza la interfaz.
/// Va en un GameObject vacio llamado "GameManager".
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Interfaz")]
    [Tooltip("El Text (TMP) que esta dentro del Canvas.")]
    public TMP_Text textoContador;

    [Tooltip("Texto grande que aparece al llegar a la meta. Puede quedar vacio.")]
    public TMP_Text textoMensajeFinal;

    [Header("Textos")]
    public string formatoContador = "Botellas: {0}/{1}";

    [Header("Audio (opcional)")]
    public AudioClip sonidoRecolectar;

    private int recolectadas;
    private int total;
    private AudioSource audioSource;

    public int Recolectadas => recolectadas;
    public int Total => total;
    public bool TodasRecolectadas => total > 0 && recolectadas >= total;

    void Awake()
    {
        Instancia = this;
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Contamos cuantos coleccionables hay en la escena al arrancar,
        // asi no hay que actualizar un numero a mano si agregas botellas.
        total = FindObjectsByType<Coleccionable>(FindObjectsInactive.Exclude).Length;

        if (textoMensajeFinal != null)
            textoMensajeFinal.gameObject.SetActive(false);

        ActualizarInterfaz();
        Debug.Log($"[GameManager] Botellas en el nivel: {total}");
    }

    /// <summary>Llamado por cada Coleccionable al ser tocado por el jugador.</summary>
    public void SumarBotella()
    {
        recolectadas++;
        ActualizarInterfaz();

        if (sonidoRecolectar != null && audioSource != null)
            audioSource.PlayOneShot(sonidoRecolectar);

        Debug.Log($"[GameManager] Recolectadas {recolectadas} de {total}");
    }

    void ActualizarInterfaz()
    {
        if (textoContador != null)
            textoContador.text = string.Format(formatoContador, recolectadas, total);
    }

    /// <summary>Llamado por la Meta para mostrar el mensaje final en pantalla.</summary>
    public void MostrarMensajeFinal(string mensaje)
    {
        if (textoMensajeFinal != null)
        {
            textoMensajeFinal.gameObject.SetActive(true);
            textoMensajeFinal.text = mensaje;
        }
        else if (textoContador != null)
        {
            textoContador.text = mensaje;
        }

        Debug.Log($"[GameManager] {mensaje}");
    }
}

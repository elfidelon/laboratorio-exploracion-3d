using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento y salto de la esfera del jugador.
/// Va en el GameObject "Sphere". Requiere Rigidbody y un Collider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad de desplazamiento en m/s. El GDD sugiere 4-6.")]
    public float velocidad = 5f;

    [Tooltip("Fuerza que se aplica al saltar. Sube este valor si la esfera salta muy poco.")]
    public float fuerzaSalto = 5f;

    [Tooltip("Si esta activo, el movimiento es relativo a hacia donde mira la camara.")]
    public bool movimientoRelativoACamara = true;

    [Tooltip("El nivel esta armado en linea recta sobre el eje X. Con esto activo la esfera no puede salirse hacia el fondo ni hacia el frente.")]
    public bool bloquearEjeZ = true;

    [Tooltip("Gravedad extra al caer. 1 = gravedad normal. Subelo si el salto se siente flotante.")]
    public float multiplicadorGravedad = 2f;

    [Header("Deteccion de suelo")]
    [Tooltip("Radio de la esfera invisible que revisa si hay suelo debajo.")]
    public float radioChequeoSuelo = 0.3f;

    [Tooltip("Que tan abajo del centro se hace el chequeo. Para una esfera de escala 1, usa 0.5.")]
    public float distanciaChequeoSuelo = 0.5f;

    [Tooltip("Capas que cuentan como suelo. Dejalo en Everything si no usas capas.")]
    public LayerMask capaSuelo = ~0;

    [Header("Audio (opcional)")]
    public AudioClip sonidoSalto;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 direccion;
    private bool enSuelo;
    private bool saltoPedido;
    private readonly Collider[] colisionesSuelo = new Collider[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Evita que la esfera se quede girando sola y se sienta resbalosa.
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (bloquearEjeZ)
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
    }

    void Update()
    {
        // La entrada se LEE en Update (cada frame) para no perder pulsaciones,
        // pero se APLICA en FixedUpdate porque estamos usando fisicas.
        LeerEntrada();
    }

    void LeerEntrada()
    {
        float x = 0f;
        float z = 0f;

        var teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) x -= 1f;
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) x += 1f;
            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) z -= 1f;
            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) z += 1f;

            if (teclado.spaceKey.wasPressedThisFrame) saltoPedido = true;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f)
            {
                x = stick.x;
                z = stick.y;
            }
            if (gamepad.buttonSouth.wasPressedThisFrame) saltoPedido = true;
        }

        direccion = new Vector3(x, 0f, z);

        if (movimientoRelativoACamara && Camera.main != null)
        {
            // Proyectamos los ejes de la camara sobre el plano XZ para que
            // "adelante" sea siempre hacia donde apunta la vista.
            Transform cam = Camera.main.transform;
            Vector3 adelante = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 derecha = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            direccion = adelante * z + derecha * x;
        }

        if (bloquearEjeZ) direccion.z = 0f;

        // Normalizamos para que moverse en diagonal no sea mas rapido.
        if (direccion.sqrMagnitude > 1f) direccion.Normalize();
    }

    void FixedUpdate()
    {
        enSuelo = HaySueloDebajo();

        // Movemos conservando la velocidad vertical actual (la gravedad y el salto).
        Vector3 objetivo = direccion * velocidad;
        objetivo.y = rb.linearVelocity.y;
        rb.linearVelocity = objetivo;

        // Gravedad extra: hace el salto mas corto en tiempo sin bajarle altura,
        // asi el personaje no se siente flotando en la luna.
        if (multiplicadorGravedad > 1f)
        {
            rb.AddForce(
                Physics.gravity * (multiplicadorGravedad - 1f),
                ForceMode.Acceleration);
        }

        if (saltoPedido)
        {
            saltoPedido = false;

            if (enSuelo)
            {
                // Se limpia la velocidad vertical para que el salto sea siempre igual.
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);

                if (sonidoSalto != null && audioSource != null)
                    audioSource.PlayOneShot(sonidoSalto);
            }
        }
    }

    /// <summary>
    /// Revisa si hay piso debajo, ignorando el propio collider de la esfera
    /// (si no lo ignoraramos, siempre creeria estar en el suelo).
    /// </summary>
    bool HaySueloDebajo()
    {
        Vector3 punto = transform.position + Vector3.down * distanciaChequeoSuelo;

        int n = Physics.OverlapSphereNonAlloc(
            punto, radioChequeoSuelo, colisionesSuelo, capaSuelo, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            Collider c = colisionesSuelo[i];
            if (c == null) continue;
            if (c.transform == transform) continue;
            if (c.transform.IsChildOf(transform)) continue;
            return true;
        }

        return false;
    }

    // Dibuja la esfera de chequeo de suelo en la vista Scene para poder ajustarla.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * distanciaChequeoSuelo,
            radioChequeoSuelo);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento en 3a persona relativo a la camara + giro del personaje.
// Requiere: Rigidbody + PlayerInput (con una accion "Move" de tipo Vector2).
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad de caminado en metros por segundo (recomendado 3.5 - 6).")]
    [Range(1f, 12f)] [SerializeField] float speed = 4.5f;
    [Tooltip("Que tan rapido gira el personaje hacia donde camina.")]
    [SerializeField] float giroSuavizado = 12f;
    [Tooltip("Altura maxima que el personaje sube sola (escalones, plataforma de la estacion).")]
    [SerializeField] float alturaEscalon = 0.65f;

    Rigidbody rb;
    CapsuleCollider capsula;
    PlayerInput playerInput;
    InputAction moveAction;
    Transform cam;

    // Velocidad horizontal actual (la usa AnimacionJugador).
    public float VelocidadActual { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        rb.freezeRotation = true;                              // la fisica no lo voltea; rotamos a mano
        rb.interpolation = RigidbodyInterpolation.Interpolate; // movimiento suave para la camara
        speed = Mathf.Clamp(speed, 1f, 12f);                   // proteccion ante valores viejos (p. ej. 500)
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void FixedUpdate()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (ControlesTactiles.Activos)   // joystick en pantalla (celular / tablet)
            input = Vector2.ClampMagnitude(input + ControlesTactiles.Movimiento, 1f);

        if (input.sqrMagnitude > 0.01f)
        {
            // direcciones de la camara proyectadas al plano del piso
            Vector3 fwd   = cam != null ? cam.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.right   : Vector3.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();

            Vector3 move = Vector3.ClampMagnitude(fwd * input.y + right * input.x, 1f);

            // conserva la velocidad vertical (gravedad) y aplica la horizontal
            rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);

            SubirEscalon(move.normalized);

            // gira el personaje hacia donde camina
            Quaternion objetivo = Quaternion.LookRotation(move.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, objetivo, giroSuavizado * Time.fixedDeltaTime));
        }
        else
        {
            // frena en horizontal, mantiene la gravedad
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        VelocidadActual = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
    }

    // Si hay un obstaculo bajo adelante (escalon) y libre por encima, sube al personaje.
    void SubirEscalon(Vector3 dir)
    {
        if (capsula == null) return;
        float escala = transform.lossyScale.y;
        float pie = transform.position.y + (capsula.center.y - capsula.height * 0.5f) * escala;
        float radio = capsula.radius * transform.lossyScale.x;
        float alcance = radio + 0.2f;
        Vector3 bajo = new Vector3(transform.position.x, pie + 0.05f, transform.position.z);

        if (!Physics.Raycast(bajo, dir, alcance, ~0, QueryTriggerInteraction.Ignore)) return;     // nada adelante
        Vector3 alto = bajo + Vector3.up * alturaEscalon;
        if (Physics.Raycast(alto, dir, alcance + 0.1f, ~0, QueryTriggerInteraction.Ignore)) return; // es una pared
        RaycastHit hit;
        if (Physics.Raycast(alto + dir * alcance, Vector3.down, out hit, alturaEscalon, ~0, QueryTriggerInteraction.Ignore))
        {
            float subir = hit.point.y - pie + 0.02f;
            if (subir > 0.01f && subir <= alturaEscalon) rb.position += Vector3.up * subir;
        }
    }
}

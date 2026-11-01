using UnityEngine;

// Conecta el movimiento del jugador con su Animator.
// Pon este script en el objeto "player" (junto al PlayerController).
// El Animator del modelo debe tener un parametro float "Velocidad".
public class AnimacionJugador : MonoBehaviour
{
    [SerializeField] Animator animator;
    [Tooltip("Suavizado del cambio entre idle y caminar.")]
    [SerializeField] float suavizado = 10f;

    PlayerController controller;
    float valor;
    static readonly int VelocidadHash = Animator.StringToHash("Velocidad");

    void Start()
    {
        controller = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || controller == null) return;
        valor = Mathf.Lerp(valor, controller.VelocidadActual, suavizado * Time.deltaTime);
        animator.SetFloat(VelocidadHash, valor);
    }
}

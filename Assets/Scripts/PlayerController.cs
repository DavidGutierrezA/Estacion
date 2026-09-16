using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    PlayerInput playerinput;
    [SerializeField] int speed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerinput = GetComponent<PlayerInput>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        Vector2 input = playerinput.actions["Move"].ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0f, input.y);
        rb.AddForce(move * speed);

        if (!playerinput.actions["Move"].IsPressed())
        {
            rb.linearVelocity = new Vector3(0f, 0f, 0f);
        }
    }
}

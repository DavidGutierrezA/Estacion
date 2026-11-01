using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// Camara orbital en 3a persona (GDD: "orbitar con clic derecho / mover el raton, zoom con la rueda").
// Va en el objeto CinemachineCamera, junto a CinemachineOrbitalFollow.
//  - Arrastrar con clic DERECHO (o IZQUIERDO sobre espacio vacio, no sobre la UI): gira la camara.
//  - Rueda del raton: acerca / aleja.
//  - No hace nada mientras un panel de POI pausa el juego (Time.timeScale = 0).
[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class CamaraOrbital : MonoBehaviour
{
    [Header("Giro")]
    [Tooltip("Grados por pixel de movimiento del raton.")]
    [SerializeField] float sensibilidad = 0.2f;
    [SerializeField] bool invertirVertical = false;

    [Header("Zoom")]
    [Tooltip("Cuanto cambia el zoom por cada paso de la rueda.")]
    [SerializeField] float pasoZoom = 0.1f;

    CinemachineOrbitalFollow orbital;
    bool arrastrando;

    void Awake() { orbital = GetComponent<CinemachineOrbitalFollow>(); }

    void Update()
    {
        if (Time.timeScale == 0f) { arrastrando = false; return; }
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Empieza a arrastrar solo si el clic NO cae sobre la interfaz (botones de epoca, etc.)
        bool clicNuevo = mouse.rightButton.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame;
        if (clicNuevo)
            arrastrando = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        if (!mouse.rightButton.isPressed && !mouse.leftButton.isPressed) arrastrando = false;

        if (arrastrando)
        {
            Vector2 d = mouse.delta.ReadValue() * sensibilidad;

            var h = orbital.HorizontalAxis;
            h.Value += d.x;
            if (h.Wrap) h.Value = Mathf.Repeat(h.Value - h.Range.x, h.Range.y - h.Range.x) + h.Range.x;
            orbital.HorizontalAxis = h;

            var v = orbital.VerticalAxis;
            v.Value = Mathf.Clamp(v.Value + (invertirVertical ? d.y : -d.y), v.Range.x, v.Range.y);
            orbital.VerticalAxis = v;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            var r = orbital.RadialAxis;
            r.Value = Mathf.Clamp(r.Value - Mathf.Sign(scroll) * pasoZoom, r.Range.x, r.Range.y);
            orbital.RadialAxis = r;
        }
    }
}

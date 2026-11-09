using UnityEngine;
using UnityEngine.EventSystems;

// Joystick en pantalla: se arrastra la perilla con el dedo y escribe la
// direccion en ControlesTactiles.Movimiento (-1..1 en X y en Y).
// Va en la zona tactil (un area mas grande que el circulo, para que sea
// facil de agarrar); "fondo" es el circulo y "perilla" el punto que se mueve.
public class JoystickVirtual : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform fondo;
    [SerializeField] RectTransform perilla;
    [Tooltip("Por debajo de este valor no se mueve (evita temblores).")]
    [SerializeField] float zonaMuerta = 0.12f;

    public void OnPointerDown(PointerEventData e) { Mover(e); }
    public void OnDrag(PointerEventData e) { Mover(e); }
    public void OnPointerUp(PointerEventData e) { Soltar(); }
    void OnDisable() { Soltar(); }

    void Mover(PointerEventData e)
    {
        if (fondo == null) return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(fondo, e.position, e.pressEventCamera, out local)) return;
        float radio = fondo.rect.width * 0.5f;
        Vector2 v = Vector2.ClampMagnitude(local / radio, 1f);
        if (perilla != null) perilla.anchoredPosition = v * radio;
        ControlesTactiles.Movimiento = v.magnitude < zonaMuerta ? Vector2.zero : v;
    }

    void Soltar()
    {
        if (perilla != null) perilla.anchoredPosition = Vector2.zero;
        ControlesTactiles.Movimiento = Vector2.zero;
    }
}

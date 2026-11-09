using System.Collections.Generic;
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

    [Header("Táctil (celular)")]
    [Tooltip("Grados por pixel al deslizar un dedo.")]
    [SerializeField] float sensibilidadTactil = 0.18f;
    [Tooltip("Cuanto zoom por pixel al pellizcar.")]
    [SerializeField] float zoomPellizco = 0.004f;

    CinemachineOrbitalFollow orbital;
    bool arrastrando;
    readonly HashSet<int> dedosSobreUI = new HashSet<int>();
    readonly List<UnityEngine.InputSystem.Controls.TouchControl> dedos = new List<UnityEngine.InputSystem.Controls.TouchControl>();

    void Awake() { orbital = GetComponent<CinemachineOrbitalFollow>(); }

    void Update()
    {
        if (Time.timeScale == 0f) { arrastrando = false; dedosSobreUI.Clear(); return; }
        if (ControlesTactiles.Activos && Touchscreen.current != null) { Tactil(Touchscreen.current); return; }
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
            Zoom(-Mathf.Sign(scroll) * pasoZoom);
    }

    // Un dedo: girar. Dos dedos: pellizcar para zoom.
    // Los dedos que empiezan sobre la interfaz (joystick, botones) se ignoran.
    void Tactil(Touchscreen pantalla)
    {
        dedos.Clear();
        foreach (var t in pantalla.touches)
        {
            var fase = t.phase.ReadValue();
            int id = t.touchId.ReadValue();
            if (fase == UnityEngine.InputSystem.TouchPhase.Began && SobreUI(t.position.ReadValue())) dedosSobreUI.Add(id);
            if (fase == UnityEngine.InputSystem.TouchPhase.Ended || fase == UnityEngine.InputSystem.TouchPhase.Canceled) { dedosSobreUI.Remove(id); continue; }
            if (fase == UnityEngine.InputSystem.TouchPhase.None) continue;
            if (!dedosSobreUI.Contains(id)) dedos.Add(t);
        }

        if (dedos.Count == 1)
        {
            Girar(dedos[0].delta.ReadValue() * sensibilidadTactil);
        }
        else if (dedos.Count >= 2)
        {
            Vector2 a = dedos[0].position.ReadValue(), b = dedos[1].position.ReadValue();
            Vector2 aAntes = a - dedos[0].delta.ReadValue(), bAntes = b - dedos[1].delta.ReadValue();
            float cambio = Vector2.Distance(a, b) - Vector2.Distance(aAntes, bAntes);
            Zoom(-cambio * zoomPellizco);   // separar los dedos = acercar
        }
    }

    static bool SobreUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;
        var datos = new PointerEventData(EventSystem.current) { position = pos };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(datos, hits);
        return hits.Count > 0;
    }

    void Girar(Vector2 d)
    {
        var h = orbital.HorizontalAxis;
        h.Value += d.x;
        if (h.Wrap) h.Value = Mathf.Repeat(h.Value - h.Range.x, h.Range.y - h.Range.x) + h.Range.x;
        orbital.HorizontalAxis = h;

        var v = orbital.VerticalAxis;
        v.Value = Mathf.Clamp(v.Value + (invertirVertical ? d.y : -d.y), v.Range.x, v.Range.y);
        orbital.VerticalAxis = v;
    }

    void Zoom(float delta)
    {
        var r = orbital.RadialAxis;
        r.Value = Mathf.Clamp(r.Value + delta, r.Range.x, r.Range.y);
        orbital.RadialAxis = r;
    }
}

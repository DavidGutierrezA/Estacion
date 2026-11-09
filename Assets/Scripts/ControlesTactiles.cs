using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Controles para celular / tablet.
//  - Se activan solos si el juego se abre desde un celular o tablet
//    (Application.isMobilePlatform tambien funciona en WebGL: lee el navegador).
//  - El boton "Táctil" (arriba a la derecha) los prende o apaga a mano; la
//    eleccion se recuerda en ese navegador (PlayerPrefs).
//  - Con los controles activos: joystick abajo a la izquierda para caminar,
//    deslizar el dedo en la pantalla para girar la camara, pellizcar para zoom,
//    y tocar el aviso dorado para leer la historia de un punto.
public class ControlesTactiles : MonoBehaviour
{
    public static bool Activos { get; private set; }
    /// Direccion del joystick (-1..1). La lee el PlayerController.
    public static Vector2 Movimiento { get; set; }
    public static event Action<bool> AlCambiar;

    [SerializeField] GameObject joystick;
    [SerializeField] TMP_Text textoBoton;
    [Tooltip("Texto del aviso de los POIs (cambia segun el modo).")]
    [SerializeField] TMP_Text textoAviso;
    [Tooltip("Cuadrito con la letra E del aviso (se oculta en modo tactil).")]
    [SerializeField] GameObject teclaAviso;
    [Tooltip("Pista 'T · cambiar de época' (se oculta en modo tactil).")]
    [SerializeField] GameObject pistaTeclado;

    const string PREF = "controles_tactiles";   // -1 = automatico, 0 = no, 1 = si

    void Start()
    {
        int guardado = PlayerPrefs.GetInt(PREF, -1);
        Aplicar(guardado < 0 ? EsDispositivoMovil() : guardado == 1);
    }

    public static bool EsDispositivoMovil()
    {
        if (Application.isMobilePlatform) return true;                 // celular o tablet (tambien en navegador)
        if (SystemInfo.deviceType == DeviceType.Handheld) return true;
        return Touchscreen.current != null && Mouse.current == null;   // pantalla tactil sin raton
    }

    // Lo llama el boton "Táctil".
    public void Alternar()
    {
        Aplicar(!Activos);
        PlayerPrefs.SetInt(PREF, Activos ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Aplicar(bool activar)
    {
        Activos = activar;
        Movimiento = Vector2.zero;
        if (joystick != null) joystick.SetActive(activar);
        if (textoBoton != null) textoBoton.text = activar ? "Táctil: SÍ" : "Táctil: NO";
        if (textoAviso != null) textoAviso.text = activar ? "Toca aquí para descubrir la historia" : "Descubre la historia de este lugar";
        if (teclaAviso != null) teclaAviso.SetActive(!activar);
        if (pistaTeclado != null) pistaTeclado.SetActive(!activar);
        if (AlCambiar != null) AlCambiar(activar);
    }

    void OnDisable() { Movimiento = Vector2.zero; }
}

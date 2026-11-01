using UnityEngine;
using UnityEngine.InputSystem;

// Pantalla de bienvenida / instrucciones.
// Se muestra al iniciar (pausa el juego). Se cierra con el boton "Comenzar",
// Enter, Espacio o Esc. Se vuelve a abrir con la tecla H o el boton "?".
public class PantallaInstrucciones : MonoBehaviour
{
    public static bool Abierta { get; private set; }

    [SerializeField] GameObject panel;
    [SerializeField] bool mostrarAlInicio = true;

    void Start()
    {
        if (mostrarAlInicio) Abrir(); else Cerrar();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (Abierta && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            Cerrar();
        else if (!Abierta && kb.hKey.wasPressedThisFrame)
            Abrir();
    }

    public void Abrir()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            var creditos = panel.transform.Find("PanelCreditos");   // siempre abre en las instrucciones
            if (creditos != null) creditos.gameObject.SetActive(false);
        }
        Abierta = true;
        Time.timeScale = 0f;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        Abierta = false;
        Time.timeScale = 1f;
    }

    void OnDisable() { Abierta = false; }
}

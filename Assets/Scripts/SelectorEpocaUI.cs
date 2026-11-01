using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Selector de epoca (GDD: mecanica central).
//  - Botones de la barra inferior: viajan a la epoca indicada (Ir(indice)).
//  - Tecla T: pasa a la siguiente epoca.
//  - Transicion: destello calido + nombre de la epoca en el centro (sensacion de "viaje en el tiempo").
//  - Resalta el boton de la epoca activa.
public class SelectorEpocaUI : MonoBehaviour
{
    [SerializeField] EpocaManager manager;
    [Tooltip("Botones en el orden de las epocas del EpocaManager (0, 1, ...).")]
    [SerializeField] RectTransform[] botones;
    [Tooltip("Etiqueta con el nombre de cada epoca (mismo orden). Se pinta de color cuando esta activa.")]
    [SerializeField] Image[] etiquetas;
    [SerializeField] Color colorActiva = new Color(0.86f, 0.47f, 0.29f, 0.95f);
    [SerializeField] Color colorInactiva = new Color(0.08f, 0.11f, 0.18f, 0.80f);
    [Tooltip("Año que se muestra para cada epoca (mismo orden). Ej: 2026, 1950")]
    [SerializeField] string[] anios;
    [Tooltip("Nombre corto de cada epoca (mismo orden). Ej: Actualidad, La Viuda de Blanco")]
    [SerializeField] string[] subtitulos;
    [SerializeField] Image destello;
    [SerializeField] TMP_Text tituloEpoca;
    [SerializeField] float duracion = 0.9f;

    bool enTransicion;
    Vector3[] escalaBase;

    void Start()
    {
        if (botones != null)
        {
            escalaBase = new Vector3[botones.Length];
            for (int i = 0; i < botones.Length; i++) escalaBase[i] = botones[i] != null ? botones[i].localScale : Vector3.one;
        }
        // etiquetas con el mismo formato para todas las epocas: año + nombre
        if (etiquetas != null)
            for (int i = 0; i < etiquetas.Length; i++)
            {
                if (etiquetas[i] == null) continue;
                var t = etiquetas[i].GetComponentInChildren<TMP_Text>(true);
                if (t != null) t.text = "<b>" + Anio(i) + "</b>\n<size=75%>" + Sub(i) + "</size>";
            }
        if (destello != null) { var c = destello.color; c.a = 0f; destello.color = c; destello.gameObject.SetActive(false); }
        if (tituloEpoca != null) tituloEpoca.gameObject.SetActive(false);
        Resaltar(manager != null ? manager.Actual : 0);
    }

    void Update()
    {
        if (manager == null || PantallaInstrucciones.Abierta || Time.timeScale == 0f) return;
        var kb = Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame) Ir((manager.Actual + 1) % Mathf.Max(1, manager.Cantidad));
    }

    public void Ir(int indice)
    {
        if (manager == null || enTransicion || PantallaInstrucciones.Abierta) return;
        if (indice == manager.Actual) { Resaltar(indice); return; }
        StartCoroutine(Transicion(indice));
    }

    IEnumerator Transicion(int indice)
    {
        enTransicion = true;
        float mitad = duracion * 0.45f;
        if (destello != null) destello.gameObject.SetActive(true);
        if (tituloEpoca != null)
        {
            tituloEpoca.text = "<size=170%>" + Anio(indice) + "</size>\n<size=70%>" + Sub(indice) + "</size>";
            tituloEpoca.gameObject.SetActive(true);
        }

        for (float t = 0; t < mitad; t += Time.unscaledDeltaTime) { Alfa(t / mitad); yield return null; }
        Alfa(1f);
        manager.AplicarEpoca(indice);
        Resaltar(indice);
        yield return new WaitForSecondsRealtime(duracion * 0.25f);
        for (float t = 0; t < mitad * 1.4f; t += Time.unscaledDeltaTime) { Alfa(1f - t / (mitad * 1.4f)); yield return null; }
        Alfa(0f);

        if (destello != null) destello.gameObject.SetActive(false);
        if (tituloEpoca != null) tituloEpoca.gameObject.SetActive(false);
        enTransicion = false;
    }

    string Anio(int i) { return anios != null && i < anios.Length && !string.IsNullOrEmpty(anios[i]) ? anios[i] : ""; }
    string Sub(int i) { return subtitulos != null && i < subtitulos.Length && !string.IsNullOrEmpty(subtitulos[i]) ? subtitulos[i] : (manager != null ? manager.NombreDe(i) : ""); }

    void Alfa(float a)
    {
        a = Mathf.Clamp01(a);
        if (destello != null) { var c = destello.color; c.a = a * 0.92f; destello.color = c; }
        if (tituloEpoca != null) { var c = tituloEpoca.color; c.a = a; tituloEpoca.color = c; }
    }

    void Resaltar(int activo)
    {
        if (botones != null && escalaBase != null)
            for (int i = 0; i < botones.Length; i++)
                if (botones[i] != null) botones[i].localScale = escalaBase[i] * (i == activo ? 1.12f : 1f);   // respeta la escala original
        if (etiquetas != null)
            for (int i = 0; i < etiquetas.Length; i++)
                if (etiquetas[i] != null) etiquetas[i].color = i == activo ? colorActiva : colorInactiva;
    }
}

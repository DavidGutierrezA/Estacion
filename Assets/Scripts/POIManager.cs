using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class POIManager : MonoBehaviour
{
    public static POIManager Instance { get; private set; }

    [Header("Indicador")]
    public GameObject indicator;

    [Header("Panel de información")]
    public GameObject panel;
    public TMP_Text yearText;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text sourceText;
    public Image photoImage;
    [Tooltip("Se muestra cuando el POI todavia no tiene foto asignada.")]
    public GameObject fotoPendiente;

    [Header("Opciones")]
    public bool pauseWhileOpen = true;

    private POI current;
    private bool panelOpen;

    private void Awake()
    {
        Instance = this;
        if (indicator != null) indicator.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (PantallaInstrucciones.Abierta) return;   // no abrir POIs encima de las instrucciones

        if (!panelOpen && current != null && kb.eKey.wasPressedThisFrame)
            OpenPanel(current);
        else if (panelOpen && kb.escapeKey.wasPressedThisFrame)
            ClosePanel();
    }

    // Tocar / hacer clic en el aviso dorado (celular y PC): abre el POI cercano.
    public void Interactuar()
    {
        if (!panelOpen && current != null && !PantallaInstrucciones.Abierta) OpenPanel(current);
    }

    public void PlayerEntered(POI poi)
    {
        current = poi;
        if (!panelOpen && indicator != null) indicator.SetActive(true);
    }

    public void PlayerExited(POI poi)
    {
        if (current != poi) return;
        current = null;
        if (indicator != null) indicator.SetActive(false);
    }

    public void OpenPanel(POI poi)
    {
        if (yearText != null) yearText.text = poi.year;
        if (titleText != null) titleText.text = poi.title;
        if (descriptionText != null) descriptionText.text = poi.description;
        if (sourceText != null) sourceText.text = poi.source;
        bool hayFoto = poi.photo != null;
        if (photoImage != null)
        {
            photoImage.sprite = poi.photo;
            photoImage.preserveAspect = true;
            photoImage.gameObject.SetActive(hayFoto);
        }
        if (fotoPendiente != null) fotoPendiente.SetActive(!hayFoto);

        if (panel != null) panel.SetActive(true);
        if (indicator != null) indicator.SetActive(false);
        panelOpen = true;

        if (pauseWhileOpen) Time.timeScale = 0f;
    }

    public void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
        panelOpen = false;

        if (pauseWhileOpen) Time.timeScale = 1f;

        if (current != null && indicator != null) indicator.SetActive(true);
    }
}

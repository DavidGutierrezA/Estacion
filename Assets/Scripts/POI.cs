using UnityEngine;


[RequireComponent(typeof(Collider))]
public class POI : MonoBehaviour
{
    [Header("Datos del punto")]
    public string year = "1926";
    public string title = "Fachada principal de la Estación";
    [TextArea(3, 6)]
    public string description =
        "Construida en 1926, fue el corazón de la vida cajiqueña y punto de " +
        "encuentro del comercio de la Sabana.";
    public string source = "Fuente: archivo del Instituto";
    public Sprite photo;

    [Header("Detección")]
    public string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            POIManager.Instance.PlayerEntered(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            POIManager.Instance.PlayerExited(this);
    }
}

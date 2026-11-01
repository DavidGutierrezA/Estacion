using UnityEngine;

// Hace que el marcador dorado de un POI flote y gire suavemente.
public class MarcadorPOI : MonoBehaviour
{
    [SerializeField] float amplitud = 0.18f;
    [SerializeField] float velocidadFlote = 2f;
    [SerializeField] float velocidadGiro = 60f;

    Vector3 inicio;
    float fase;

    void Start()
    {
        inicio = transform.localPosition;
        fase = Random.value * 10f;
    }

    void Update()
    {
        transform.localPosition = inicio + Vector3.up * Mathf.Sin((Time.time + fase) * velocidadFlote) * amplitud;
        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
    }
}

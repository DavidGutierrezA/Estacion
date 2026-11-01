using UnityEngine;

// Pasos del personaje: suena un paso cada cierta distancia recorrida en el piso,
// con tono y volumen un poco distintos cada vez para que no se oiga repetitivo.
// Va en el objeto "player" (junto al PlayerController).
[RequireComponent(typeof(AudioSource))]
public class SonidoPasos : MonoBehaviour
{
    [SerializeField] AudioClip[] pasos;
    [Tooltip("Metros entre un paso y el siguiente.")]
    [SerializeField] float distanciaPaso = 1.1f;
    [SerializeField] Vector2 volumen = new Vector2(0.12f, 0.2f);
    [SerializeField] Vector2 tono = new Vector2(0.9f, 1.1f);
    [Tooltip("Por debajo de esta velocidad no suenan pasos.")]
    [SerializeField] float velocidadMinima = 0.4f;

    PlayerController pc;
    AudioSource fuente;
    Vector3 ultimaPos;
    float recorrido;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;   // la camara siempre sigue al jugador: sonido 2D
        ultimaPos = transform.position;
    }

    void Update()
    {
        Vector3 p = transform.position;
        Vector3 d = p - ultimaPos; d.y = 0f;
        ultimaPos = p;
        float vel = pc != null ? pc.VelocidadActual : d.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
        if (Time.timeScale == 0f || vel < velocidadMinima || !EnElPiso()) { recorrido = distanciaPaso * 0.6f; return; }   // el primer paso suena pronto al arrancar
        recorrido += d.magnitude;
        if (recorrido >= distanciaPaso)
        {
            recorrido = 0f;
            Sonar();
        }
    }

    bool EnElPiso()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
    }

    void Sonar()
    {
        if (pasos == null || pasos.Length == 0) return;
        var clip = pasos[Random.Range(0, pasos.Length)];
        if (clip == null) return;
        fuente.pitch = Random.Range(tono.x, tono.y);
        fuente.PlayOneShot(clip, Random.Range(volumen.x, volumen.y));
    }
}

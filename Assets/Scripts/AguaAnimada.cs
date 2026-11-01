using UnityEngine;

// Mueve suavemente las ondas del agua (desplaza la textura de normales).
// Usa un MaterialPropertyBlock: no modifica el material del proyecto.
[RequireComponent(typeof(Renderer))]
public class AguaAnimada : MonoBehaviour
{
    [SerializeField] Vector2 velocidad = new Vector2(0.035f, 0.022f);
    [SerializeField] Vector2 repeticion = new Vector2(3f, 3f);

    Renderer rend;
    MaterialPropertyBlock bloque;
    Vector2 desplazamiento;
    static readonly int ST = Shader.PropertyToID("_BaseMap_ST");

    void Start()
    {
        rend = GetComponent<Renderer>();
        bloque = new MaterialPropertyBlock();
    }

    void Update()
    {
        desplazamiento += velocidad * Time.deltaTime;
        desplazamiento.x %= 1f; desplazamiento.y %= 1f;
        rend.GetPropertyBlock(bloque);
        bloque.SetVector(ST, new Vector4(repeticion.x, repeticion.y, desplazamiento.x, desplazamiento.y));
        rend.SetPropertyBlock(bloque);
    }
}

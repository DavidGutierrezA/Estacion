using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NieblaAnimada : MonoBehaviour
{
    [Header("Desplazamiento de la textura")]
    public float velocidad = 0.005f;

    Renderer rend;
    MaterialPropertyBlock bloque;
    Vector4 st = new Vector4(1f, 1f, 0f, 0f);
    static readonly int idST = Shader.PropertyToID("_BaseMap_ST");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        bloque = new MaterialPropertyBlock();
        if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(idST))
            st = rend.sharedMaterial.GetVector(idST);
    }

    void Update()
    {
        st.z = Mathf.Repeat(st.z + velocidad * Time.deltaTime, 1f);
        rend.GetPropertyBlock(bloque);
        bloque.SetVector(idST, st);
        rend.SetPropertyBlock(bloque);
    }
}

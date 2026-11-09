using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light))]
public class SombrasNubes : MonoBehaviour
{
    [Header("Nubes")]
    [SerializeField, Range(0f, 1f)] float oscuridad = 0.35f;
    [SerializeField, Range(0.1f, 0.9f)] float cobertura = 0.45f;
    [SerializeField, Range(0.02f, 0.5f)] float suavidad = 0.18f;
    [SerializeField] float tamano = 140f;
    [SerializeField] Vector2 viento = new Vector2(1.6f, 0.7f);
    [SerializeField] int resolucion = 256;
    [SerializeField] int semilla = 7;

    Light luz;
    UniversalAdditionalLightData datos;
    Texture2D textura;
    Texture cookiePrevio;
    Vector2 tamanoPrevio;
    Vector2 offsetPrevio;
    Vector2 offset;

    void OnEnable()
    {
        luz = GetComponent<Light>();
        datos = GetComponent<UniversalAdditionalLightData>();
        if (textura == null) textura = Generar();
        cookiePrevio = luz.cookie;
        luz.cookie = textura;
        if (datos != null)
        {
            tamanoPrevio = datos.lightCookieSize;
            offsetPrevio = datos.lightCookieOffset;
            datos.lightCookieSize = new Vector2(tamano, tamano);
        }
    }

    void OnDisable()
    {
        if (luz != null) luz.cookie = cookiePrevio;
        if (datos != null)
        {
            datos.lightCookieSize = tamanoPrevio;
            datos.lightCookieOffset = offsetPrevio;
        }
    }

    void OnDestroy()
    {
        if (textura != null) Destroy(textura);
    }

    void Update()
    {
        if (datos == null) return;
        offset += viento * Time.deltaTime;
        offset.x = Mathf.Repeat(offset.x, tamano);
        offset.y = Mathf.Repeat(offset.y, tamano);
        datos.lightCookieOffset = offset;
    }

    Texture2D Generar()
    {
        int n = Mathf.Clamp(resolucion, 32, 1024);
        var px = new Color32[n * n];
        var rnd = new System.Random(semilla);
        float ox = (float)rnd.NextDouble() * 1000f, oy = (float)rnd.NextDouble() * 1000f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (float)x / n, v = (float)y / n;
                float f = Fbm(u, v, ox, oy);
                float nube = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(cobertura - suavidad, cobertura + suavidad, f));
                byte b = (byte)Mathf.RoundToInt(255f * (1f - oscuridad * nube));
                px[y * n + x] = new Color32(b, b, b, b);
            }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
        tex.name = "Nubes_Cookie";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }

    static float Fbm(float u, float v, float ox, float oy)
    {
        float suma = 0f, amp = 0.5f, total = 0f;
        int frecuencia = 3;
        for (int o = 0; o < 4; o++)
        {
            suma += amp * Periodico(u, v, frecuencia, ox + o * 17.3f, oy + o * 31.7f);
            total += amp;
            amp *= 0.5f;
            frecuencia *= 2;
        }
        return suma / total;
    }

    static float Periodico(float u, float v, int f, float ox, float oy)
    {
        float x = u * f, y = v * f;
        float a = Mathf.PerlinNoise(ox + x, oy + y);
        float b = Mathf.PerlinNoise(ox + x - f, oy + y);
        float c = Mathf.PerlinNoise(ox + x, oy + y - f);
        float d = Mathf.PerlinNoise(ox + x - f, oy + y - f);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BurbujaNiebla
{
    const string GEN_DIR = "Assets/Art/Generado/Niebla";
    const string ROOT = "COMUN_Niebla";

    public static Vector2 Centro = new Vector2(17f, 47.5f);
    public static float SemiX = 72f;
    public static float SemiZ = 25f;
    public static float Exponente = 6f;
    public static float RetiroParedes = 3f;
    public static Color ColorNiebla = new Color(0.84f, 0.87f, 0.90f);
    public static float NieblaInicio = 40f;
    public static float NieblaFin = 140f;

    static readonly string[] FondoLejano =
    {
        "COMUN_Entorno/Casas_Fondo",
        "EPOCA_2026_Props/Edificio_Moderno",
        "EPOCA_2026_Props/Jardin_Moderno"
    };

    [MenuItem("Estacion/Niebla/Crear burbuja (todo)")]
    public static void MenuTodo() { QuitarFondo(); Construir(); NieblaCamara(); Sucia(); Debug.Log("<b>[Niebla]</b> Burbuja creada."); }

    [MenuItem("Estacion/Niebla/1 - Quitar fondo lejano")]
    public static void MenuFondo() { QuitarFondo(); Sucia(); }

    [MenuItem("Estacion/Niebla/2 - Cortinas y paredes")]
    public static void MenuCortinas() { Construir(); Sucia(); }

    [MenuItem("Estacion/Niebla/3 - Niebla de camara")]
    public static void MenuCamara() { NieblaCamara(); Sucia(); }

    [MenuItem("Estacion/Niebla/Borrar burbuja")]
    public static void MenuBorrar()
    {
        var r = Raiz(ROOT);
        if (r != null) Undo.DestroyObjectImmediate(r);
        Sucia();
    }

    static void QuitarFondo()
    {
        foreach (var ruta in FondoLejano)
        {
            var go = BuscarRuta(ruta);
            if (go == null) { Debug.LogWarning("[Niebla] No encontrado: " + ruta); continue; }
            Undo.RecordObject(go, "Quitar fondo lejano");
            go.SetActive(false);
        }
    }

    static void Construir()
    {
        var vieja = Raiz(ROOT);
        if (vieja != null) Undo.DestroyObjectImmediate(vieja);

        CrearCarpeta(GEN_DIR);
        var tex = TexturaRuido();
        var mat = MaterialNiebla(tex);

        var root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, "Crear burbuja de niebla");

        Cortina(root.transform, "Cortina_Interior", mat, -1.5f, 10f, 0.45f, 0.006f);
        Cortina(root.transform, "Cortina_Principal", mat, 0f, 18f, 0.9f, -0.004f);
        Cortina(root.transform, "Cortina_Exterior", mat, 7f, 28f, 1f, 0.003f);
        Paredes(root.transform);
    }

    static void Cortina(Transform padre, string nombre, Material mat, float desfase, float alto, float alfa, float velocidad)
    {
        var malla = MallaAnillo(nombre, SemiX + desfase, SemiZ + desfase, alto, alfa, 10);
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        go.AddComponent<NieblaAnimada>().velocidad = velocidad;
    }

    static void Paredes(Transform padre)
    {
        var g = new GameObject("Paredes_Invisibles");
        g.transform.SetParent(padre, false);
        float per;
        var pts = Contorno(SemiX - RetiroParedes, SemiZ - RetiroParedes, 4f, out per);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            Vector3 dir = b - a;
            float largo = dir.magnitude;
            if (largo < 0.01f) continue;
            var w = new GameObject("Pared_" + i.ToString("00"));
            w.transform.SetParent(g.transform, false);
            w.transform.position = (a + b) * 0.5f + Vector3.up * 2f;
            w.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            var bc = w.AddComponent<BoxCollider>();
            bc.size = new Vector3(1f, 4f, largo + 0.6f);
            w.isStatic = true;
        }
    }

    static List<Vector3> Contorno(float a, float b, float paso, out float perimetro)
    {
        const int denso = 4000;
        var d = new List<Vector3>(denso + 1);
        for (int i = 0; i <= denso; i++)
        {
            float t = i * Mathf.PI * 2f / denso;
            float c = Mathf.Cos(t), s = Mathf.Sin(t);
            float x = a * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / Exponente);
            float z = b * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / Exponente);
            d.Add(new Vector3(Centro.x + x, 0f, Centro.y + z));
        }
        var acum = new float[d.Count];
        for (int i = 1; i < d.Count; i++) acum[i] = acum[i - 1] + Vector3.Distance(d[i - 1], d[i]);
        perimetro = acum[d.Count - 1];
        int n = Mathf.Max(16, Mathf.RoundToInt(perimetro / paso));
        var res = new List<Vector3>(n + 1);
        int k = 0;
        for (int i = 0; i <= n; i++)
        {
            float obj = perimetro * i / n;
            while (k < d.Count - 2 && acum[k + 1] < obj) k++;
            float seg = acum[k + 1] - acum[k];
            float f = seg > 0f ? Mathf.Clamp01((obj - acum[k]) / seg) : 0f;
            res.Add(Vector3.Lerp(d[k], d[k + 1], f));
        }
        res[n] = res[0];
        return res;
    }

    static Mesh MallaAnillo(string nombre, float a, float b, float alto, float alfa, int filas)
    {
        float per;
        var pts = Contorno(a, b, 2f, out per);
        int cols = pts.Count;
        int rep = Mathf.Max(1, Mathf.RoundToInt(per / 32f));

        var verts = new Vector3[cols * (filas + 1)];
        var uvs = new Vector2[verts.Length];
        var colores = new Color[verts.Length];
        float recorrido = 0f;
        for (int i = 0; i < cols; i++)
        {
            if (i > 0) recorrido += Vector3.Distance(pts[i - 1], pts[i]);
            float u = recorrido / per * rep;
            for (int j = 0; j <= filas; j++)
            {
                float t = (float)j / filas;
                int idx = j * cols + i;
                verts[idx] = new Vector3(pts[i].x, -0.3f + (alto + 0.3f) * t, pts[i].z);
                uvs[idx] = new Vector2(u, t);
                colores[idx] = new Color(1f, 1f, 1f, alfa * (1f - Mathf.SmoothStep(0.25f, 1f, t)));
            }
        }

        var tris = new List<int>(filas * (cols - 1) * 6);
        for (int j = 0; j < filas; j++)
            for (int i = 0; i < cols - 1; i++)
            {
                int v0 = j * cols + i, v1 = v0 + 1, v2 = v0 + cols, v3 = v2 + 1;
                tris.Add(v0); tris.Add(v2); tris.Add(v1);
                tris.Add(v1); tris.Add(v2); tris.Add(v3);
            }

        string ruta = GEN_DIR + "/" + nombre + ".asset";
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        bool nueva = m == null;
        if (nueva) m = new Mesh(); else m.Clear();
        m.name = nombre;
        m.indexFormat = verts.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        m.vertices = verts;
        m.uv = uvs;
        m.colors = colores;
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        if (nueva) AssetDatabase.CreateAsset(m, ruta); else EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D TexturaRuido()
    {
        string ruta = GEN_DIR + "/Niebla_Ruido.png";
        const int w = 512, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Fbm((float)x / w, (float)y / h);
                n = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.7f, n));
                float c = Mathf.Lerp(0.9f, 1f, n);
                px[y * w + x] = new Color(c, c, c, Mathf.Lerp(0.7f, 1f, n));
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.wrapModeU = TextureWrapMode.Repeat;
        imp.wrapModeV = TextureWrapMode.Clamp;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = true;
        imp.maxTextureSize = 512;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static float Fbm(float u, float v)
    {
        float suma = 0f, amp = 0.5f, total = 0f, f = 1f;
        for (int o = 0; o < 4; o++)
        {
            suma += amp * Repetible(u, v, 6f * f, 4f * f, o * 17.3f);
            total += amp;
            amp *= 0.5f;
            f *= 2f;
        }
        return Mathf.Clamp01(suma / total);
    }

    static float Repetible(float u, float v, float fx, float fy, float semilla)
    {
        float a = Mathf.PerlinNoise(u * fx + 13.1f + semilla, v * fy + 7.3f);
        float b = Mathf.PerlinNoise((u - 1f) * fx + 13.1f + semilla, v * fy + 7.3f);
        return Mathf.Lerp(a, b, u);
    }

    static Material MaterialNiebla(Texture2D tex)
    {
        string ruta = GEN_DIR + "/Niebla_Cortina.mat";
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, ruta); }
        mat.shader = sh;
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", new Color(ColorNiebla.r, ColorNiebla.g, ColorNiebla.b, 1f));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_Cull", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_ColorMode", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_SOFTPARTICLES_ON");
        float cerca = 3f, lejos = 15f;
        mat.SetFloat("_CameraFadingEnabled", 1f);
        mat.SetFloat("_CameraNearFadeDistance", cerca);
        mat.SetFloat("_CameraFarFadeDistance", lejos);
        mat.SetVector("_CameraFadeParams", new Vector4(cerca, 1f / (lejos - cerca), 0f, 0f));
        mat.EnableKeyword("_FADING_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void NieblaCamara()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = NieblaInicio;
        RenderSettings.fogEndDistance = NieblaFin;
        RenderSettings.fogColor = ColorNiebla;
        var sky = RenderSettings.skybox;
        if (sky != null && sky.HasProperty("_GroundColor"))
        {
            Undo.RecordObject(sky, "Horizonte niebla");
            sky.SetColor("_GroundColor", ColorNiebla);
            EditorUtility.SetDirty(sky);
        }
        DynamicGI.UpdateEnvironment();
    }

    static GameObject Raiz(string nombre)
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == nombre) return go;
        return null;
    }

    static GameObject BuscarRuta(string ruta)
    {
        int corte = ruta.IndexOf('/');
        var r = Raiz(corte < 0 ? ruta : ruta.Substring(0, corte));
        if (r == null || corte < 0) return r;
        var t = r.transform.Find(ruta.Substring(corte + 1));
        return t != null ? t.gameObject : null;
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        int corte = ruta.LastIndexOf('/');
        string padre = ruta.Substring(0, corte);
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, ruta.Substring(corte + 1));
    }

    static void Sucia()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
}
#endif

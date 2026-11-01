#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// =============================================================================
//  FUENTES FUNCIONANDO    Menu: Estacion > Juego > 7 - Fuentes con agua
//  - Agua con ondas en movimiento (normal map generado + AguaAnimada).
//  - Fuentes clasicas (2026 y Viuda): chorro desde el remate + cortinas que caen
//    desde el borde de los dos platos.
//  - Canales de 2026 (Fuentes rectas): chorros verticales a lo largo del agua.
//  Los chorros son hijos de cada fuente, asi que aparecen/desaparecen con su epoca.
//  Se puede volver a correr (reemplaza los chorros anteriores).
// =============================================================================
public static class FuentesAgua
{
    const string GEN = "Assets/Art/Generado";
    const bool CANALES_CON_CHORROS = false;

    // Copia de "Agua" sin mapa de normales: superficie lisa y quieta.
    static Material MaterialAguaQuieta(Material baseAgua)
    {
        if (baseAgua == null) return null;
        string path = "Assets/Art/Materiales/Agua_Canal.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(baseAgua); AssetDatabase.CreateAsset(m, path); }
        else { m.shader = baseAgua.shader; m.CopyPropertiesFromMaterial(baseAgua); }
        m.renderQueue = baseAgua.renderQueue;
        m.SetTexture("_BumpMap", null);
        m.DisableKeyword("_NORMALMAP");
        m.SetFloat("_Smoothness", 0.85f);
        EditorUtility.SetDirty(m);
        return m;
    }

    [MenuItem("Estacion/Juego/7 - Fuentes con agua")]
    public static void Construir()
    {
        var normal = GenerarNormalAgua();
        var gota = MaterialGotas();
        int chorros = 0, superficies = 0;

        // ---- Superficies de agua con ondas ----
        // "Agua 1" (canal derecho) usaba otro modo de brillo y se veia vacio: se iguala a "Agua".
        var agua0 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Agua.mat");
        var agua1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Agua 1.mat");
        if (agua0 != null && agua1 != null) { agua1.shader = agua0.shader; agua1.CopyPropertiesFromMaterial(agua0); agua1.renderQueue = agua0.renderQueue; }
        foreach (var path in new[] { "Assets/Art/Materiales/Agua.mat", "Assets/Art/Materiales/Agua 1.mat" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) continue;
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 0.55f);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", 0.92f);
            EditorUtility.SetDirty(m);
        }
        foreach (var raiz in Raices())
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                var mat = r.sharedMaterial;
                if (mat == null || !(mat.name == "Agua" || mat.name == "Agua 1")) continue;
                if (r.GetComponent<AguaAnimada>() == null) r.gameObject.AddComponent<AguaAnimada>();
                superficies++;
            }

        // ---- Fuentes clasicas ----
        foreach (var nombre in new[] { "Fuente 2026", "Fuente viuda" })
        {
            var f = Raiz(nombre);
            if (f == null) continue;
            Limpiar(f.transform);
            Renderer bola = null, plato1 = null, plato2 = null;
            foreach (var r in f.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Remate_bola") bola = r;
                if (r.name == "Plato1_borde") plato1 = r;
                if (r.name == "Plato2_borde") plato2 = r;
            }
            if (bola != null) { Chorro(f.transform, "Agua_Chorro_Remate", bola.bounds.center + Vector3.up * (bola.bounds.extents.y + 0.03f), gota); chorros++; }
            if (plato2 != null) { Cortina(f.transform, "Agua_Cortina_Plato2", plato2.bounds.center + Vector3.up * 0.04f, plato2.bounds.extents.x * 0.98f, 90f, gota); chorros++; }
            if (plato1 != null) { Cortina(f.transform, "Agua_Cortina_Plato1", plato1.bounds.center + Vector3.up * 0.04f, plato1.bounds.extents.x * 0.98f, 140f, gota); chorros++; }
        }

        // ---- Canales de 2026 (Fuentes rectas) ----
        // David prefiere el agua QUIETA aqui: material propio sin ondas, sin animacion.
        // Pon CANALES_CON_CHORROS = true si algun dia se quieren chorritos.
        var canales = Raiz("Fuentes rectas");
        if (canales != null)
        {
            Limpiar(canales.transform);
            var quieta = MaterialAguaQuieta(agua0);
            foreach (var r in canales.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("placeholder agua")) continue;
                var anim = r.GetComponent<AguaAnimada>();
                if (anim != null) { Object.DestroyImmediate(anim); superficies--; }
                if (quieta != null) r.sharedMaterial = quieta;
                if (!CANALES_CON_CHORROS) continue;
                var b = r.bounds;
                for (float x = b.min.x + 1.6f; x <= b.max.x - 1.5f; x += 3.2f)
                {
                    var go = Emisor(canales.transform, "Agua_Chorro_Canal", new Vector3(x, b.max.y + 0.02f, b.center.z), Quaternion.Euler(-90f, 0f, 0f), gota);
                    var ps = go.GetComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3.6f, 4.2f);
                    main.startLifetime = 0.95f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
                    var em = ps.emission; em.rateOverTime = 28f;
                    var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 4f; sh.radius = 0.03f;
                    chorros++;
                }
            }
        }

        // ---- Fuente de piso de 2026 (FuentesPiso): rejilla de chorros que salen de vez en cuando ----
        var piso = Raiz("FuentesPiso");
        if (piso != null)
        {
            Limpiar(piso.transform);
            var viejas = new List<GameObject>();
            foreach (Transform t in piso.transform) if (t.name == "Boquillas") viejas.Add(t.gameObject);
            foreach (var v in viejas) Object.DestroyImmediate(v);
            Renderer plano = null;
            foreach (var r in piso.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer)) { plano = r; break; }
            if (plano != null)
            {
                var b = plano.bounds;
                var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Agua_Boquilla.mat");
                if (metal == null) { metal = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(metal, "Assets/Art/Materiales/Agua_Boquilla.mat"); }
                metal.SetColor("_BaseColor", new Color(0.22f, 0.23f, 0.24f)); metal.SetFloat("_Metallic", 0.6f); metal.SetFloat("_Smoothness", 0.5f);
                var boq = new GameObject("Boquillas"); boq.transform.SetParent(piso.transform, false);
                int n = 4; float paso = Mathf.Min(b.size.x, b.size.z) / (n + 0.5f);
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                    {
                        var p = new Vector3(b.center.x + (i - (n - 1) / 2f) * paso, b.max.y, b.center.z + (j - (n - 1) / 2f) * paso);
                        var tapa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        Object.DestroyImmediate(tapa.GetComponent<Collider>());
                        tapa.name = "Boquilla"; tapa.transform.SetParent(boq.transform, false);
                        tapa.transform.position = p + Vector3.up * 0.005f;
                        tapa.transform.localScale = new Vector3(0.28f, 0.01f, 0.28f);
                        tapa.GetComponent<MeshRenderer>().sharedMaterial = metal;
                        tapa.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        var go = Emisor(piso.transform, "Agua_ChorroPiso", p + Vector3.up * 0.02f, Quaternion.Euler(-90f, 0f, 0f), gota);
                        var ps = go.GetComponent<ParticleSystem>();
                        var main = ps.main;
                        main.playOnAwake = false; main.loop = true;
                        main.startSpeed = new ParticleSystem.MinMaxCurve(5.2f, 5.8f);   // ~1.6 m de alto
                        main.startLifetime = 1.15f;
                        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.13f);
                        var em = ps.emission; em.rateOverTime = 45f;
                        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 3f; sh.radius = 0.03f;
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        chorros++;
                    }
                if (piso.GetComponent<ChorrosPiso>() == null) piso.AddComponent<ChorrosPiso>();
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Fuentes]</b> Agua animada en " + superficies + " superficies y " + chorros + " chorros/cortinas.");
    }

    static void Chorro(Transform parent, string name, Vector3 pos, Material m)
    {
        var go = Emisor(parent, name, pos, Quaternion.Euler(-90f, 0f, 0f), m);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 3.5f);
        main.startLifetime = 0.95f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
        var em = ps.emission; em.rateOverTime = 120f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.03f;
    }

    // Anillo de gotas que salen hacia afuera del borde del plato y caen por gravedad.
    static void Cortina(Transform parent, string name, Vector3 pos, float radio, float cantidad, Material m)
    {
        var go = Emisor(parent, name, pos, Quaternion.Euler(90f, 0f, 0f), m);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startLifetime = 0.55f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.11f);
        var em = ps.emission; em.rateOverTime = cantidad * 2.2f;
        main.maxParticles = 400;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radio; sh.radiusThickness = 0f; sh.arc = 360f;
    }

    // Sistema de particulas base (gotas de agua con gravedad), compensando la escala del padre.
    static GameObject Emisor(Transform parent, string name, Vector3 pos, Quaternion rot, Material m)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        var ls = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1f;
        main.maxParticles = 220;
        main.startColor = new Color(0.92f, 0.97f, 1f, 0.95f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = m;
        // gotas estiradas segun su velocidad: se leen como hilos de agua
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.velocityScale = 0.09f;
        rend.lengthScale = 1.2f;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static void Limpiar(Transform t)
    {
        var borrar = new List<GameObject>();
        foreach (var ps in t.GetComponentsInChildren<ParticleSystem>(true)) if (ps.name.StartsWith("Agua_")) borrar.Add(ps.gameObject);
        foreach (var g in borrar) Object.DestroyImmediate(g);
    }

    static Material MaterialGotas()
    {
        string path = "Assets/Art/Materiales/Agua_Gotas.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, path); }
        m.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        m.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f, 1f));
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Mapa de normales de ondas, repetible (suma de senos con frecuencias enteras).
    static Texture2D GenerarNormalAgua()
    {
        string path = GEN + "/Agua_Normal.png";
        var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (ex != null) return ex;
        const int S = 256;
        int[,] ondas = { { 1, 2 }, { 3, -1 }, { -2, 3 }, { 4, 1 }, { 1, -5 }, { 6, 3 } };
        float[] fases = { 0.3f, 1.7f, 2.9f, 4.1f, 5.3f, 0.9f };
        float[] amp = { 1f, 0.7f, 0.6f, 0.4f, 0.35f, 0.25f };
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S, dx = 0f, dy = 0f;
                for (int k = 0; k < amp.Length; k++)
                {
                    float a = 2f * Mathf.PI * (ondas[k, 0] * u + ondas[k, 1] * v) + fases[k];
                    float c = Mathf.Cos(a) * amp[k] * 2f * Mathf.PI;
                    dx += c * ondas[k, 0]; dy += c * ondas[k, 1];
                }
                var n = new Vector3(-dx * 0.04f, -dy * 0.04f, 1f).normalized;
                px[y * S + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
        var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.NormalMap; imp.wrapMode = TextureWrapMode.Repeat;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static GameObject[] Raices() { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(); }
    static GameObject Raiz(string n) { foreach (var g in Raices()) if (g.name == n) return g; return null; }
}
#endif

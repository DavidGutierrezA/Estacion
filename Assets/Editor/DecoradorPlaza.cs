#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// =============================================================================
//  DECORADOR DE LA PLAZA — La Estacion en el Tiempo
//  Menu:  Estacion > Plaza > ...
//
//  Construye la ambientacion de la epoca ACTUALIDAD (2026) usando la foto aerea
//  como guia. Todo queda en dos contenedores de escena:
//    COMUN_Entorno     -> presente en todas las epocas (terreno, pasto, vias,
//                         arboles del frente, casas de fondo)
//    EPOCA_2026_Props  -> solo en 2026 (calle, faroles, astas, cafe, bancas,
//                         edificio moderno, jardin). Se registra solo en el
//                         EpocaManager (activar en 2026 / desactivar en otras).
//  Volver a correr un paso BORRA y RECONSTRUYE su contenedor.
//  Las posiciones estan en las listas de cada seccion: editalas a gusto.
// =============================================================================
public static class DecoradorPlaza
{
    const string MAT_DIR = "Assets/Art/Materiales/Plaza";
    const string GEN_DIR = "Assets/Art/Generado";
    const string ROOT_COMUN = "COMUN_Entorno";
    const string ROOT_2026 = "EPOCA_2026_Props";

    static System.Random rng;

    // ------------------------------------------------------------------ MENU
    [MenuItem("Estacion/Plaza/1 - Ambiente (cielo, sol, postproceso)")]
    public static void MenuAmbiente() { Ambiente(); Sucia(); }

    [MenuItem("Estacion/Plaza/2 - Piso de adoquin")]
    public static void MenuPiso() { Piso(); Sucia(); }

    [MenuItem("Estacion/Plaza/3 - Props de la plaza (2026)")]
    public static void MenuProps() { Props(); Sucia(); }

    [MenuItem("Estacion/Plaza/4 - Estacion como la foto (colores por epoca)")]
    public static void MenuEstacion() { Estacion(); Sucia(); }

    [MenuItem("Estacion/Plaza/5 - Camara menos cenital")]
    public static void MenuCamara() { Camara(new Vector3(0f, 6.5f, -10f), 28f); Sucia(); }

    [MenuItem("Estacion/Plaza/Hacer todo (1 a 4)")]
    public static void MenuTodo()
    {
        Ambiente(); Piso(); Props(); Estacion(); Sucia();
        Debug.Log("<b>[Plaza]</b> Ambientacion 2026 completa.");
    }

    static void Sucia()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------- AMBIENTE
    static void Ambiente()
    {
        EnsureDir(MAT_DIR);
        string skyPath = MAT_DIR + "/Cielo_2026.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, skyPath); }
        sky.SetFloat("_SunSize", 0.035f);
        sky.SetFloat("_SunSizeConvergence", 5f);
        sky.SetFloat("_AtmosphereThickness", 0.9f);
        sky.SetColor("_SkyTint", new Color(0.42f, 0.58f, 0.85f));
        sky.SetColor("_GroundColor", new Color(0.42f, 0.42f, 0.40f));
        sky.SetFloat("_Exposure", 1.25f);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;

        Light sol = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            sol.transform.rotation = Quaternion.Euler(42f, 32f, 0f);   // ilumina la fachada principal
            sol.color = new Color(1f, 0.95f, 0.86f);
            sol.intensity = 1.7f;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.85f;
            RenderSettings.sun = sol;
            EditorUtility.SetDirty(sol);
        }

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.05f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 320f;
        RenderSettings.fogColor = new Color(0.74f, 0.81f, 0.90f);
        DynamicGI.UpdateEnvironment();

        var camGO = FindRoot("Camera");
        if (camGO != null)
        {
            camGO.tag = "MainCamera";   // necesario para el movimiento relativo a camara
            var ud = camGO.GetComponent<UniversalAdditionalCameraData>();
            if (ud != null) ud.renderPostProcessing = true;
            var cam = camGO.GetComponent<Camera>();
            if (cam != null) cam.farClipPlane = 600f;
            EditorUtility.SetDirty(camGO);
        }

        var vol = Object.FindFirstObjectByType<Volume>();
        if (vol != null && vol.sharedProfile != null)
        {
            var prof = vol.sharedProfile;
            var ca = Efecto<ColorAdjustments>(prof);
            ca.postExposure.Override(0.1f);
            ca.contrast.Override(10f);
            ca.saturation.Override(6f);
            var vg = Efecto<Vignette>(prof);
            vg.intensity.Override(0.2f);
            vg.smoothness.Override(0.4f);
            Bloom bl;
            if (prof.TryGet(out bl)) { bl.active = true; bl.threshold.Override(0.95f); bl.intensity.Override(0.25f); }
            EditorUtility.SetDirty(prof);
        }

        // Agua con menos reflejo metalico (evita el destello blanco)
        foreach (var p in new[] { "Assets/Art/Materiales/Agua.mat", "Assets/Art/Materiales/Agua 1.mat" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) continue;
            m.SetColor("_BaseColor", new Color(0.24f, 0.46f, 0.56f, 0.78f));
            m.SetFloat("_Metallic", 0.05f);
            m.SetFloat("_Smoothness", 0.82f);
            EditorUtility.SetDirty(m);
        }
    }

    static T Efecto<T>(VolumeProfile prof) where T : VolumeComponent
    {
        T c;
        if (!prof.TryGet(out c))
        {
            c = prof.Add<T>(true);
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, prof);
        }
        c.active = true;
        return c;
    }

    // ------------------------------------------------------------------ PISO
    static void Piso()
    {
        var tex = GenerarAdoquin();
        var m = Mat("Plaza_Adoquin", Color.white, 0.12f);
        var tiling = new Vector2(20.5f, 16.6f);   // ~3.2 m por repeticion (8 adoquines de 40 cm)
        m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling);
        m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", tiling);
        EditorUtility.SetDirty(m);
        foreach (var n in new[] { "Suelo", "Suelo (1)" })
        {
            var go = FindRoot(n);
            if (go == null) continue;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            EditorUtility.SetDirty(go);
        }
    }

    static Texture2D GenerarAdoquin()
    {
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Adoquin_Plaza.png";
        const int S = 512, N = 8, cell = S / N;
        var r = new System.Random(11);
        var tone = new float[N, N];
        for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) tone[i, j] = 0.55f + (float)r.NextDouble() * 0.11f;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int cx = x / cell, cy = y / cell;
                int lx = x % cell, ly = y % cell;
                int edge = Mathf.Min(Mathf.Min(lx, cell - 1 - lx), Mathf.Min(ly, cell - 1 - ly));
                float n = ((float)r.NextDouble() - 0.5f) * 0.035f;
                Color c;
                if (edge < 2) c = new Color(0.36f, 0.35f, 0.33f);                     // junta
                else
                {
                    float t = tone[cx, cy] + n;
                    if (edge < 5) t -= 0.05f * (5 - edge) / 3f;                         // bisel
                    c = new Color(t, t * 0.985f, t * 0.95f);
                }
                px[y * S + x] = c;
            }
        var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 4;
        imp.maxTextureSize = 512;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ----------------------------------------------------------------- PROPS
    static void Props()
    {
        rng = new System.Random(2026);

        var tronco   = Mat("Arbol_Tronco",      new Color(0.36f, 0.25f, 0.16f), 0.1f);
        var h1       = Mat("Arbol_Hoja_Oscura", new Color(0.17f, 0.32f, 0.13f), 0.1f);
        var h2       = Mat("Arbol_Hoja_Media",  new Color(0.25f, 0.42f, 0.17f), 0.1f);
        var tierra   = Mat("Alcorque_Tierra",   new Color(0.24f, 0.20f, 0.15f), 0.05f);
        var arbusto  = Mat("Arbusto_Topiario",  new Color(0.20f, 0.38f, 0.15f), 0.1f);
        var metal    = Mat("Metal_Oscuro",      new Color(0.13f, 0.14f, 0.15f), 0.45f, 0.6f);
        var metalCl  = Mat("Metal_Claro",       new Color(0.72f, 0.74f, 0.76f), 0.5f, 0.7f);
        var luz      = Mat("Farol_Luz",         new Color(1f, 0.95f, 0.8f), 0.3f, 0f, new Color(1.2f, 1.05f, 0.8f));
        var amarillo = Mat("Bandera_Amarillo",  new Color(0.98f, 0.80f, 0.10f));
        var azul     = Mat("Bandera_Azul",      new Color(0.05f, 0.20f, 0.60f));
        var rojo     = Mat("Bandera_Rojo",      new Color(0.80f, 0.10f, 0.12f));
        var verde    = Mat("Bandera_Verde",     new Color(0.10f, 0.50f, 0.22f));
        var blanco   = Mat("Bandera_Blanco",    new Color(0.95f, 0.95f, 0.93f));
        var lona     = Mat("Sombrilla_Azul",    new Color(0.12f, 0.20f, 0.62f), 0.25f);
        var tablero  = Mat("Mesa_Tablero",      new Color(0.16f, 0.15f, 0.14f), 0.3f);
        var madera   = Mat("Mesa_Madera",       new Color(0.62f, 0.36f, 0.18f), 0.3f);
        var bancaMat = Mat("Banca_Piedra",      new Color(0.86f, 0.85f, 0.81f), 0.2f);
        var balasto  = Mat("Via_Balasto",       new Color(0.45f, 0.42f, 0.38f), 0.05f);
        var riel     = Mat("Via_Riel",          new Color(0.42f, 0.40f, 0.38f), 0.6f, 0.8f);
        var durm     = Mat("Via_Durmiente",     new Color(0.30f, 0.22f, 0.15f), 0.1f);
        var cesped   = Mat("Cesped",            new Color(0.30f, 0.45f, 0.20f), 0.05f);
        var terreno  = Mat("Terreno_Base",      new Color(0.36f, 0.43f, 0.29f), 0.05f);
        var asfalto  = Mat("Asfalto",           new Color(0.22f, 0.22f, 0.23f), 0.15f);
        var linea    = Mat("Linea_Amarilla",    new Color(0.95f, 0.75f, 0.10f), 0.2f);
        var anden    = Mat("Anden_Gris",        new Color(0.62f, 0.61f, 0.58f), 0.1f);
        var crema    = Mat("Moderno_Crema",     new Color(0.86f, 0.80f, 0.62f), 0.15f);
        var techoMod = Mat("Moderno_Techo",     new Color(0.78f, 0.80f, 0.80f), 0.3f);
        var bordeMod = Mat("Moderno_Borde",     new Color(0.62f, 0.72f, 0.62f), 0.3f);
        var vent     = Mat("Moderno_Ventana",   new Color(0.12f, 0.14f, 0.17f), 0.6f);
        var tejaCasa = Mat("Casa_Teja",         new Color(0.60f, 0.25f, 0.15f), 0.15f);
        Material[] murosCasa = {
            Mat("Casa_Salmon",   new Color(0.86f, 0.56f, 0.42f)),
            Mat("Casa_Amarilla", new Color(0.90f, 0.78f, 0.46f)),
            Mat("Casa_Blanca",   new Color(0.90f, 0.88f, 0.83f)),
            Mat("Casa_Azul",     new Color(0.56f, 0.68f, 0.78f)),
            Mat("Casa_Ladrillo", new Color(0.66f, 0.36f, 0.26f)) };

        // ===================== COMUN (todas las epocas) =====================
        var comun = Raiz(ROOT_COMUN);
        Prim(PrimitiveType.Cube, "Terreno_Base", comun, new Vector3(20f, -0.09f, 60f), new Vector3(700f, 0.1f, 700f), terreno, default, true);
        Prim(PrimitiveType.Cube, "Cesped_Fondo", comun, new Vector3(20f, 0f, 83f), new Vector3(260f, 0.06f, 34f), cesped, default, true);
        Via(comun, 71f, -110f, 150f, balasto, riel, durm);
        Via(comun, 75f, -110f, 150f, balasto, riel, durm);

        var arboles = Grupo("Arboles_Frente", comun, Vector3.zero);
        for (float x = -40f; x <= 84f; x += 9.5f)
            Arbol(arboles, new Vector3(x, 0f, 28.5f), 0.85f + (float)rng.NextDouble() * 0.2f, tronco, h1, h2, tierra);

        var casas = Grupo("Casas_Fondo", comun, Vector3.zero);
        Vector3[] cp = {
            new Vector3(-66, 0, 90), new Vector3(-54, 0, 94), new Vector3(-42, 0, 88), new Vector3(-30, 0, 97),
            new Vector3(-72, 0, 104), new Vector3(-58, 0, 106), new Vector3(88, 0, 92), new Vector3(100, 0, 98), new Vector3(112, 0, 90) };
        Vector3[] cs = {
            new Vector3(9, 6, 8), new Vector3(8, 7.5f, 9), new Vector3(10, 5.5f, 8), new Vector3(8, 6.5f, 8),
            new Vector3(9, 8, 9), new Vector3(10, 6, 8), new Vector3(9, 6, 9), new Vector3(10, 7, 8), new Vector3(8, 6, 8) };
        for (int i = 0; i < cp.Length; i++) Casa(casas, cp[i], cs[i], murosCasa[i % murosCasa.Length], tejaCasa);

        // ========================= SOLO EPOCA 2026 ==========================
        var p26 = Raiz(ROOT_2026);

        var calle = Grupo("Calle", p26, Vector3.zero);
        Prim(PrimitiveType.Cube, "Asfalto", calle, new Vector3(20f, 0f, 19.5f), new Vector3(260f, 0.06f, 11f), asfalto, default, true);
        Prim(PrimitiveType.Cube, "Sardinel", calle, new Vector3(20f, 0.08f, 25f), new Vector3(260f, 0.16f, 0.8f), anden, default, true);
        Prim(PrimitiveType.Cube, "Anden_Opuesto", calle, new Vector3(20f, 0.06f, 12f), new Vector3(260f, 0.12f, 4f), anden, default, true);
        var dashes = new List<Matrix4x4>();
        for (float x = -108f; x < 148f; x += 6f)
            dashes.Add(Matrix4x4.TRS(new Vector3(x, 0.04f, 19.5f), Quaternion.identity, new Vector3(3f, 0.02f, 0.16f)));
        Combinado("Linea_Calle", calle, dashes, linea);

        var faroles = Grupo("Faroles", p26, Vector3.zero);
        Vector3[] fp = {
            new Vector3(-24, 0, 52), new Vector3(24, 0, 52), new Vector3(-31, 0, 35), new Vector3(29, 0, 34),
            new Vector3(50, 0, 41), new Vector3(-12, 0, 31), new Vector3(12, 0, 31) };
        foreach (var f in fp) Farol(faroles, f, 0f, metal, luz);

        var astas = Grupo("Astas_Banderas", p26, Vector3.zero);
        Asta(astas, new Vector3(24.5f, 0f, 63.5f), metalCl, new[] { amarillo, rojo }, new[] { 1f, 1f });
        Asta(astas, new Vector3(27f, 0f, 63.5f), metalCl, new[] { amarillo, azul, rojo }, new[] { 2f, 1f, 1f });
        Asta(astas, new Vector3(29.5f, 0f, 63.5f), metalCl, new[] { verde, blanco, amarillo }, new[] { 1f, 1f, 1f });

        var cafe = Grupo("Cafe_Sombrillas", p26, Vector3.zero);
        foreach (float x in new[] { -34f, -28f })
            foreach (float z in new[] { 49f, 54.5f, 60f })
                SetCafe(cafe, new Vector3(x, 0f, z), 0f, tablero, madera, metal, lona);

        var bancas = Grupo("Bancas", p26, Vector3.zero);
        for (float x = -35.25f; x <= 80f; x += 19f) Banca(bancas, new Vector3(x, 0f, 28.5f), 0f, bancaMat);

        EdificioModerno(p26, new Vector3(16f, 0f, 104f), crema, techoMod, vent, bordeMod);
        var jardin = Grupo("Jardin_Moderno", p26, Vector3.zero);
        for (float x = -6f; x <= 40f; x += 3.3f) Arbusto(jardin, new Vector3(x, 0f, 85.5f), 0.85f, arbusto);
        for (float x = -4f; x <= 38f; x += 6.6f) Arbol(jardin, new Vector3(x, 0f, 89f), 0.8f, tronco, h1, h2, tierra);

        RegistrarEnEpocas();
        Debug.Log("<b>[Plaza]</b> Props construidos.");
    }

    // ------------------------------------------------------------- ESTACION
    static void Estacion()
    {
        AgregarTag("Techo");
        var est = FindRoot("Estacion");
        if (est != null)
            foreach (var t in est.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Cubierta")) t.gameObject.tag = "Techo";

        var muro = Mat("Estacion_Muro_2026", new Color(0.80f, 0.45f, 0.30f), 0.12f);   // terracota de la foto
        var teja = Mat("Estacion_Teja_2026", new Color(0.62f, 0.27f, 0.17f), 0.2f);    // teja de barro
        if (est != null) PanelesParapeto(est, muro);

        // Plataforma / escalones: beige piedra en 2026; las otras epocas conservan el material original del FBX.
        var plataforma = Mat("Estacion_Plataforma_2026", new Color(0.84f, 0.78f, 0.67f), 0.1f);
        Material plataformaOriginal = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/Estacion.fbx"))
            if (a is Material && a.name == "Suelo_Plaza.002") { plataformaOriginal = (Material)a; break; }
        if (est != null)
            foreach (var t in est.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Portico_piso") || t.name.StartsWith("Escalon")) t.gameObject.tag = "SueloEstacion";

        var em = FindRoot("EpocaManager");
        if (em == null) { Debug.LogWarning("[Plaza] No encontre EpocaManager"); return; }
        var comp = em.GetComponent("EpocaManager");
        var so = new SerializedObject(comp);
        var list = so.FindProperty("epocas");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            bool es2026 = e.FindPropertyRelative("nombre").stringValue.Contains("2026");
            var mats = e.FindPropertyRelative("materiales");
            Material estructuraOtra = null;
            for (int j = 0; j < mats.arraySize; j++)
            {
                var x = mats.GetArrayElementAtIndex(j);
                if (x.FindPropertyRelative("tag").stringValue != "Estructura") continue;
                if (es2026) x.FindPropertyRelative("material").objectReferenceValue = muro;
                else estructuraOtra = x.FindPropertyRelative("material").objectReferenceValue as Material;
            }
            // 2026: teja propia. Otras epocas: el techo conserva el material que ya tenia (el de "Estructura").
            SetTagMat(mats, "Techo", es2026 ? teja : estructuraOtra);
            SetTagMat(mats, "SueloEstacion", es2026 ? plataforma : plataformaOriginal);
        }
        so.ApplyModifiedProperties();
        RegistrarEnEpocas();
        AplicarEpocaActual();
    }

    static void RegistrarEnEpocas()
    {
        var em = FindRoot("EpocaManager");
        var props = FindRoot(ROOT_2026);
        if (em == null || props == null) return;
        var so = new SerializedObject(em.GetComponent("EpocaManager"));
        var list = so.FindProperty("epocas");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            bool es2026 = e.FindPropertyRelative("nombre").stringValue.Contains("2026");
            LimpiarNulos(e.FindPropertyRelative("activar"));
            LimpiarNulos(e.FindPropertyRelative("desactivar"));
            AgregarObjeto(e.FindPropertyRelative(es2026 ? "activar" : "desactivar"), props);
        }
        so.ApplyModifiedProperties();
    }

    static void AplicarEpocaActual()
    {
        var em = FindRoot("EpocaManager");
        if (em == null) return;
        var comp = em.GetComponent("EpocaManager");
        int actual = new SerializedObject(comp).FindProperty("epocaActual").intValue;
        var mi = comp.GetType().GetMethod("AplicarEpoca");
        if (mi != null) mi.Invoke(comp, new object[] { actual });
    }

    // Paneles ovalados del parapeto (marco blanco + centro terracota), como en la fachada real.
    // Usan los tags "Fachada" y "Estructura" para que el EpocaManager les cambie el color por epoca.
    static void PanelesParapeto(GameObject est, Material muro)
    {
        var prev = est.transform.Find("Detalles_Parapeto");
        if (prev != null) Object.DestroyImmediate(prev.gameObject);
        Material blanco = null;
        foreach (var r in est.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Cornisa") { blanco = r.sharedMaterial; break; }
        if (blanco == null) blanco = Mat("Estacion_Moldura_Blanca", new Color(0.94f, 0.93f, 0.89f));

        var g = new GameObject("Detalles_Parapeto").transform;   // se crea en coordenadas de mundo
        // Caras reales del parapeto (se leen del modelo para que los paneles queden al ras)
        float zFrente = 53.36f, xDer = 13.46f, xIzq = -13.61f;
        foreach (var r in est.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "Parapeto") zFrente = r.bounds.min.z;
            else if (r.name == "Parapeto.001") xDer = r.bounds.max.x;
            else if (r.name == "Parapeto.002") xIzq = r.bounds.min.x;
        }
        const float y = 5.7f;
        foreach (float ax in new[] { 2.5f, 4.5f, 6.5f, 8.5f, 10.5f, 12.3f })
            foreach (int s in new[] { -1, 1 })
            {
                float x = ax * s;
                Panel(g, new Vector3(x, y, zFrente - 0.012f), new Vector3(0, 0, 90), new Vector3(0.44f, 0.56f, 0.03f), blanco, "Fachada");
                Panel(g, new Vector3(x, y, zFrente - 0.03f), new Vector3(0, 0, 90), new Vector3(0.30f, 0.46f, 0.02f), muro, "Estructura");
            }
        foreach (float z in new[] { 55.6f, 57.6f, 59.6f, 61.6f })
            foreach (int s in new[] { -1, 1 })
            {
                float xo = s > 0 ? xDer : xIzq;
                Panel(g, new Vector3(xo + 0.012f * s, y, z), new Vector3(90, 0, 0), new Vector3(0.03f, 0.56f, 0.44f), blanco, "Fachada");
                Panel(g, new Vector3(xo + 0.03f * s, y, z), new Vector3(90, 0, 0), new Vector3(0.02f, 0.46f, 0.30f), muro, "Estructura");
            }
        g.SetParent(est.transform, true);   // queda dentro de la estacion, conservando posicion
    }

    static void Panel(Transform parent, Vector3 worldPos, Vector3 rot, Vector3 scale, Material m, string tag)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Panel_Ovalado";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.eulerAngles = rot;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // sin sombra = no parecen flotar
        go.tag = tag;
        go.isStatic = true;
    }

    // --------------------------------------------------------------- CAMARA
    // offset = posicion de la camara respecto al jugador; pitch = inclinacion hacia abajo (grados).
    // Valores originales de David: offset (0, 12, -4.66), pitch 44.5.
    static void Camara(Vector3 offset, float pitch)
    {
        var cm = FindRoot("CinemachineCamera");
        if (cm == null) return;
        Undo.RecordObject(cm.transform, "Camara");
        cm.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        var follow = cm.GetComponent("CinemachineFollow");
        if (follow == null) return;
        var so = new SerializedObject(follow);
        var p = so.FindProperty("FollowOffset");
        if (p != null) { p.vector3Value = offset; so.ApplyModifiedProperties(); }
    }

    // ===================================================== CONSTRUCTORES
    static void Arbol(Transform p, Vector3 pos, float s, Material tronco, Material h1, Material h2, Material tierra)
    {
        var g = Grupo("Arbol", p, pos, (float)rng.NextDouble() * 360f);
        Prim(PrimitiveType.Cube, "Alcorque", g, new Vector3(0, 0.03f, 0), new Vector3(1.6f, 0.06f, 1.6f), tierra);
        Prim(PrimitiveType.Cylinder, "Tronco", g, new Vector3(0, 1.3f * s, 0), new Vector3(0.32f * s, 1.3f * s, 0.32f * s), tronco, default, true);
        Prim(PrimitiveType.Sphere, "Copa_1", g, new Vector3(0, 3.7f * s, 0), Vector3.one * 3.2f * s, h1);
        Prim(PrimitiveType.Sphere, "Copa_2", g, new Vector3(0.9f * s, 3.2f * s, 0.3f * s), Vector3.one * 2.2f * s, h2);
        Prim(PrimitiveType.Sphere, "Copa_3", g, new Vector3(-0.8f * s, 3.4f * s, -0.4f * s), Vector3.one * 2.4f * s, h2);
        Prim(PrimitiveType.Sphere, "Copa_4", g, new Vector3(0.1f * s, 4.6f * s, 0.2f * s), Vector3.one * 2.0f * s, h1);
    }

    static void Arbusto(Transform p, Vector3 pos, float s, Material m)
    {
        Prim(PrimitiveType.Sphere, "Arbusto", p, pos + new Vector3(0, 0.75f * s, 0), new Vector3(1.6f, 1.5f, 1.6f) * s, m, default, true);
    }

    static void Farol(Transform p, Vector3 pos, float rotY, Material metal, Material luz)
    {
        var g = Grupo("Farol", p, pos, rotY);
        Prim(PrimitiveType.Cylinder, "Base", g, new Vector3(0, 0.25f, 0), new Vector3(0.45f, 0.25f, 0.45f), metal, default, true);
        Prim(PrimitiveType.Cylinder, "Poste", g, new Vector3(0, 4.0f, 0), new Vector3(0.18f, 3.6f, 0.18f), metal, default, true);
        Prim(PrimitiveType.Sphere, "Remate", g, new Vector3(0, 7.7f, 0), Vector3.one * 0.25f, metal);
        foreach (int sx in new[] { -1, 1 })
        {
            Prim(PrimitiveType.Cube, "Brazo", g, new Vector3(0.75f * sx, 7.3f, 0), new Vector3(1.5f, 0.09f, 0.09f), metal);
            Prim(PrimitiveType.Cube, "Luminaria", g, new Vector3(1.45f * sx, 7.2f, 0), new Vector3(0.7f, 0.16f, 0.32f), metal);
            Prim(PrimitiveType.Cube, "Luz", g, new Vector3(1.45f * sx, 7.11f, 0), new Vector3(0.6f, 0.04f, 0.26f), luz);
        }
    }

    static void Asta(Transform p, Vector3 pos, Material metal, Material[] franjas, float[] prop)
    {
        var g = Grupo("Asta", p, pos);
        Prim(PrimitiveType.Cylinder, "Base", g, new Vector3(0, 0.15f, 0), new Vector3(0.45f, 0.15f, 0.45f), metal, default, true);
        Prim(PrimitiveType.Cylinder, "Mastil", g, new Vector3(0, 4.8f, 0), new Vector3(0.11f, 4.5f, 0.11f), metal, default, true);
        Prim(PrimitiveType.Sphere, "Bola", g, new Vector3(0, 9.4f, 0), Vector3.one * 0.24f, metal);
        float alto = 1.4f, ancho = 2.2f, y = 9.1f, total = 0f;
        foreach (var v in prop) total += v;
        for (int i = 0; i < franjas.Length; i++)
        {
            float hh = alto * prop[i] / total;
            Prim(PrimitiveType.Cube, "Bandera_" + i, g, new Vector3(0.06f + ancho / 2f, y - hh / 2f, 0), new Vector3(ancho, hh, 0.03f), franjas[i]);
            y -= hh;
        }
    }

    static void SetCafe(Transform p, Vector3 pos, float rotY, Material tablero, Material madera, Material metal, Material lona)
    {
        var g = Grupo("SetCafe", p, pos, rotY);
        Prim(PrimitiveType.Cube, "Mesa", g, new Vector3(0, 0.76f, 0), new Vector3(1.9f, 0.06f, 0.8f), tablero, default, true);
        Prim(PrimitiveType.Cube, "Mesa_Borde", g, new Vector3(0, 0.72f, 0), new Vector3(1.94f, 0.04f, 0.84f), madera);
        foreach (int sz in new[] { -1, 1 })
        {
            Prim(PrimitiveType.Cube, "Banca", g, new Vector3(0, 0.46f, 0.68f * sz), new Vector3(1.9f, 0.05f, 0.32f), tablero, default, true);
            Prim(PrimitiveType.Cube, "Banca_Borde", g, new Vector3(0, 0.43f, 0.68f * sz), new Vector3(1.94f, 0.03f, 0.36f), madera);
        }
        foreach (int sx in new[] { -1, 1 })
        {
            Prim(PrimitiveType.Cube, "Pata_Mesa", g, new Vector3(0.82f * sx, 0.38f, 0), new Vector3(0.07f, 0.76f, 0.07f), metal);
            Prim(PrimitiveType.Cube, "Travesano", g, new Vector3(0.82f * sx, 0.40f, 0), new Vector3(0.07f, 0.07f, 1.5f), metal);
            foreach (int sz in new[] { -1, 1 })
                Prim(PrimitiveType.Cube, "Pata_Banca", g, new Vector3(0.82f * sx, 0.22f, 0.68f * sz), new Vector3(0.07f, 0.44f, 0.07f), metal);
        }
        Prim(PrimitiveType.Cylinder, "Sombrilla_Palo", g, new Vector3(0, 1.4f, 0), new Vector3(0.06f, 1.4f, 0.06f), metal);
        var lonaGo = new GameObject("Sombrilla_Lona");
        lonaGo.transform.SetParent(g, false);
        lonaGo.transform.localPosition = new Vector3(0, 2.65f, 0);
        lonaGo.AddComponent<MeshFilter>().sharedMesh = ConoSombrilla();
        lonaGo.AddComponent<MeshRenderer>().sharedMaterial = lona;
        lonaGo.isStatic = true;
        Prim(PrimitiveType.Sphere, "Sombrilla_Punta", g, new Vector3(0, 3.22f, 0), Vector3.one * 0.12f, lona);
    }

    static void Banca(Transform p, Vector3 pos, float rotY, Material piedra)
    {
        var g = Grupo("Banca", p, pos, rotY);
        Prim(PrimitiveType.Cube, "Cuerpo", g, new Vector3(0, 0.24f, 0), new Vector3(2.2f, 0.48f, 0.62f), piedra, default, true);
        Prim(PrimitiveType.Cube, "Asiento", g, new Vector3(0, 0.5f, 0), new Vector3(2.3f, 0.06f, 0.7f), piedra);
    }

    static void Via(Transform p, float z, float x0, float x1, Material balasto, Material riel, Material durmiente)
    {
        var g = Grupo("Via_z" + Mathf.RoundToInt(z), p, new Vector3(0, 0, z));
        float L = x1 - x0, cx = (x0 + x1) / 2f;
        Prim(PrimitiveType.Cube, "Balasto", g, new Vector3(cx, 0.12f, 0), new Vector3(L, 0.2f, 3.0f), balasto, default, true);
        Prim(PrimitiveType.Cube, "Riel_A", g, new Vector3(cx, 0.33f, -0.72f), new Vector3(L, 0.12f, 0.08f), riel);
        Prim(PrimitiveType.Cube, "Riel_B", g, new Vector3(cx, 0.33f, 0.72f), new Vector3(L, 0.12f, 0.08f), riel);
        var list = new List<Matrix4x4>();
        for (float x = x0; x <= x1; x += 0.7f)
            list.Add(Matrix4x4.TRS(new Vector3(x, 0.25f, 0), Quaternion.identity, new Vector3(0.22f, 0.1f, 2.4f)));
        Combinado("Durmientes_z" + Mathf.RoundToInt(z), g, list, durmiente);
    }

    static void Casa(Transform p, Vector3 pos, Vector3 size, Material muro, Material teja)
    {
        var g = Grupo("Casa", p, pos);
        Prim(PrimitiveType.Cube, "Muros", g, new Vector3(0, size.y / 2f, 0), size, muro, default, true);
        float w = size.z * 0.75f;
        Prim(PrimitiveType.Cube, "Techo", g, new Vector3(0, size.y, 0), new Vector3(size.x + 0.6f, w, w), teja, new Vector3(45f, 0, 0));
    }

    static void EdificioModerno(Transform p, Vector3 pos, Material crema, Material techo, Material vent, Material borde)
    {
        var g = Grupo("Edificio_Moderno", p, pos);
        const float W = 44f, D = 22f, H = 12f;
        Prim(PrimitiveType.Cube, "Cuerpo", g, new Vector3(0, H / 2f, 0), new Vector3(W, H, D), crema, default, true);
        Prim(PrimitiveType.Cube, "Techo", g, new Vector3(0, H + 0.5f, -3f), new Vector3(W + 8f, 0.9f, D + 10f), techo);
        Prim(PrimitiveType.Cube, "Techo_Borde", g, new Vector3(0, H + 0.3f, -3f - (D + 10f) / 2f + 0.3f), new Vector3(W + 8f, 1.3f, 0.6f), borde);
        var list = new List<Matrix4x4>();
        var r = new System.Random(7);
        for (float x = -W / 2f + 2.5f; x < W / 2f - 2f; x += 2.6f)
            for (float y = 2.5f; y < H - 1.5f; y += 2.4f)
                if (r.NextDouble() < 0.35)
                    list.Add(Matrix4x4.TRS(new Vector3(x, y, -D / 2f - 0.05f), Quaternion.identity, new Vector3(1f, 1f, 0.12f)));
        Combinado("Ventanas_Edificio_Moderno", g, list, vent);
    }

    // ============================================================ UTILIDADES
    static Material Mat(string name, Color c, float smooth = 0.15f, float metal = 0f, Color? emis = null)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        m.enableInstancing = true;
        if (emis.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emis.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 rot = default, bool col = false)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    static Transform Grupo(string name, Transform parent, Vector3 pos, float rotY = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = new Vector3(0, rotY, 0);
        go.isStatic = true;
        return go.transform;
    }

    static Transform Raiz(string name)
    {
        var old = FindRoot(name);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        go.isStatic = true;
        return go.transform;
    }

    static GameObject FindRoot(string name)
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    static Mesh cubo;
    static Mesh Cubo()
    {
        if (cubo == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo = t.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(t);
        }
        return cubo;
    }

    // Une muchos cubos en una sola malla (1 draw call) y la guarda como asset.
    static GameObject Combinado(string name, Transform parent, List<Matrix4x4> mats, Material m)
    {
        var ci = new CombineInstance[mats.Count];
        for (int i = 0; i < mats.Count; i++) { ci[i].mesh = Cubo(); ci[i].transform = mats[i]; }
        var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(ci, true, true);
        mesh.RecalculateBounds();
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        go.isStatic = true;
        return go;
    }

    static Mesh ConoSombrilla()
    {
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Sombrilla_Cono.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int n = 8; const float r = 1.5f, h = 0.55f;
        var v = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            var p0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
            var p1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
            var top = new Vector3(0, h, 0);
            int b = v.Count; v.Add(top); v.Add(p1); v.Add(p0); tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);   // cara de arriba
            int c = v.Count; v.Add(top); v.Add(p0); v.Add(p1); tris.Add(c); tris.Add(c + 1); tris.Add(c + 2);   // cara de abajo
        }
        var mesh = new Mesh { name = "Sombrilla_Cono" };
        mesh.SetVertices(v); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void AgregarTag(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    static void SetTagMat(SerializedProperty mats, string tag, Material m)
    {
        if (m == null) return;
        for (int j = 0; j < mats.arraySize; j++)
        {
            var x = mats.GetArrayElementAtIndex(j);
            if (x.FindPropertyRelative("tag").stringValue == tag) { x.FindPropertyRelative("material").objectReferenceValue = m; return; }
        }
        mats.InsertArrayElementAtIndex(mats.arraySize);
        var nx = mats.GetArrayElementAtIndex(mats.arraySize - 1);
        nx.FindPropertyRelative("tag").stringValue = tag;
        nx.FindPropertyRelative("material").objectReferenceValue = m;
    }

    static void LimpiarNulos(SerializedProperty arr)
    {
        for (int j = arr.arraySize - 1; j >= 0; j--)
            if (arr.GetArrayElementAtIndex(j).objectReferenceValue == null) arr.DeleteArrayElementAtIndex(j);
    }

    static void AgregarObjeto(SerializedProperty arr, GameObject go)
    {
        for (int j = 0; j < arr.arraySize; j++) if (arr.GetArrayElementAtIndex(j).objectReferenceValue == go) return;
        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = go;
    }
}
#endif

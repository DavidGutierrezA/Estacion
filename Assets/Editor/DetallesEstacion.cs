#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DetallesEstacion
{
    const string ROOT = "COMUN_Detalles_Estacion";
    const string MAT_DIR = "Assets/Art/Materiales/Estacion";
    const string GEN_DIR = "Assets/Art/Generado/Estacion";

    const float Y_PISO_PORTICO = 0.6f;
    const float Y_TECHO_PORTICO = 4.75f;
    const float ALTO_ARCO = 0.8f;
    const float PASO_LISTON = 0.17f;

    [MenuItem("Estacion/Detalles/Construir detalles de la fachada")]
    public static void Construir()
    {
        var est = GameObject.Find("Estacion");
        if (est == null) { Debug.LogWarning("[Detalles] No encontre Estacion"); return; }
        foreach (var go in EscenaUtil.Raices())
            if (go.name == ROOT) Undo.DestroyObjectImmediate(go);
        var rootGo = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(rootGo, "Detalles estacion");
        rootGo.isStatic = true;
        var root = rootGo.transform;
        Carpeta(GEN_DIR);

        var blanco = Buscar("Molduras_Blanco") ?? NuevoMat("Detalle_Blanco", new Color(0.95f, 0.94f, 0.91f), 0.15f);
        var estructura = Buscar("Estructura") ?? blanco;
        var madera = Buscar("Madera_Cafe");
        Color cMadera = madera != null && madera.HasProperty("_BaseColor") ? madera.GetColor("_BaseColor") : new Color(0.45f, 0.28f, 0.16f);
        var maderaDet = NuevoMat("Madera_Detalle", cMadera * 0.72f + new Color(0, 0, 0, 0.28f), 0.2f);
        var vidrio = NuevoMat("Vidrio_Oscuro", new Color(0.13f, 0.17f, 0.22f), 0.85f);

        var renders = est.GetComponentsInChildren<MeshRenderer>(true);

        var cols = Grupo("Columnas", root, Vector3.zero);
        foreach (var r in renders)
        {
            if (!r.name.StartsWith("Columna")) continue;
            var b = r.bounds;
            float rad = Mathf.Min(b.size.x, b.size.z) / 2f;
            var g = Grupo(r.name + "_Detalle", cols, new Vector3(b.center.x, 0f, b.center.z));
            Caja("Plinto", g, new Vector3(0f, Y_PISO_PORTICO + 0.06f, 0f), new Vector3(rad * 2f + 0.24f, 0.12f, rad * 2f + 0.24f), estructura).tag = "Estructura";
            Cil("Toro", g, new Vector3(0f, Y_PISO_PORTICO + 0.16f, 0f), rad + 0.07f, 0.08f, estructura).tag = "Estructura";
            Cil("Collarino", g, new Vector3(0f, Y_TECHO_PORTICO - 0.42f, 0f), rad + 0.03f, 0.05f, estructura).tag = "Estructura";
            Cil("Equino", g, new Vector3(0f, Y_TECHO_PORTICO - 0.2f, 0f), rad + 0.11f, 0.12f, estructura).tag = "Estructura";
            Caja("Abaco", g, new Vector3(0f, Y_TECHO_PORTICO - 0.07f, 0f), new Vector3(rad * 2f + 0.32f, 0.12f, rad * 2f + 0.32f), estructura).tag = "Estructura";
        }

        Bounds puerta = new Bounds(); bool hayPuerta = false;
        foreach (var r in renders)
        {
            if (!r.name.StartsWith("Frente_R_puerta_0.5_hoja")) continue;
            if (!hayPuerta) { puerta = r.bounds; hayPuerta = true; } else puerta.Encapsulate(r.bounds);
        }
        if (hayPuerta)
        {
            var g = Grupo("Arco_Puerta_Principal", root, new Vector3(puerta.center.x, puerta.max.y, puerta.min.z));
            float a = puerta.size.x / 2f + 0.05f, h = ALTO_ARCO;
            Caja("Travesano", g, new Vector3(0f, 0.06f, -0.04f), new Vector3(a * 2f + 0.3f, 0.12f, 0.08f), blanco);
            MallaObj("Vidrio", g, MediaElipse("Arco_Vidrio", a, h, 0.12f, -0.02f), vidrio);
            MallaObj("Marco", g, AnilloElipse("Arco_Marco", a, h, 0.14f, 0.12f, -0.06f), blanco);
            var radios = new List<CombineInstance>();
            var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            for (int i = 1; i < 6; i++)
            {
                float t = Mathf.PI * i / 6f;
                Vector2 fin = new Vector2(Mathf.Cos(t) * a, Mathf.Sin(t) * h);
                Vector2 mitad = fin * 0.5f;
                float ang = Mathf.Atan2(fin.y, fin.x) * Mathf.Rad2Deg;
                radios.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(new Vector3(mitad.x, 0.12f + mitad.y, -0.05f), Quaternion.Euler(0f, 0f, ang), new Vector3(fin.magnitude, 0.05f, 0.04f)) });
            }
            radios.Add(new CombineInstance { mesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"), transform = Matrix4x4.TRS(new Vector3(0f, 0.12f, -0.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 0.03f, 0.18f)) });
            var mr = new Mesh { name = "Arco_Radios" };
            mr.CombineMeshes(radios.ToArray(), true, true);
            Guardar(mr);
            MallaObj("Radios", g, mr, blanco);
            Caja("Clave", g, new Vector3(0f, 0.12f + h + 0.08f, -0.08f), new Vector3(0.26f, 0.32f, 0.1f), blanco);
        }

        var listones = new List<CombineInstance>();
        var tableros = new List<CombineInstance>();
        var cub = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        foreach (var r in renders)
        {
            if (!r.name.Contains("_hoja")) continue;
            if (r.name.StartsWith("Frente_R_puerta_0.5_hoja")) { Tableros(r.bounds, tableros, cub, 2); continue; }
            var b = r.bounds;
            if (b.min.y > 1.2f) Persiana(b, listones, cub);
            else Tableros(b, tableros, cub, 2);
        }
        var det = Grupo("Carpinteria", root, Vector3.zero);
        var ml = new Mesh { name = "Persianas", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        ml.CombineMeshes(listones.ToArray(), true, true);
        Guardar(ml);
        MallaObj("Persianas", det, ml, maderaDet);
        var mt = new Mesh { name = "Tableros_Puertas", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mt.CombineMeshes(tableros.ToArray(), true, true);
        Guardar(mt);
        MallaObj("Tableros", det, mt, maderaDet);

        var em = GameObject.Find("EpocaManager");
        if (em != null)
        {
            var comp = em.GetComponent("EpocaManager");
            var mi = comp != null ? comp.GetType().GetMethod("AplicarEpoca") : null;
            if (mi != null) mi.Invoke(comp, new object[] { new SerializedObject(comp).FindProperty("epocaActual").intValue });
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Detalles]</b> Detalles de la fachada construidos (" + ROOT + ").");
    }

    static void Persiana(Bounds b, List<CombineInstance> lista, Mesh cub)
    {
        bool enX = b.size.x >= b.size.z;
        float ancho = (enX ? b.size.x : b.size.z) - 0.14f, grueso = (enX ? b.size.z : b.size.x) + 0.05f;
        int n = Mathf.Max(3, Mathf.FloorToInt((b.size.y - 0.16f) / PASO_LISTON));
        float paso = (b.size.y - 0.16f) / n;
        for (int i = 0; i < n; i++)
        {
            float y = b.min.y + 0.08f + paso * (i + 0.5f);
            var esc = enX ? new Vector3(ancho, paso * 0.45f, grueso) : new Vector3(grueso, paso * 0.45f, ancho);
            lista.Add(new CombineInstance { mesh = cub, transform = Matrix4x4.TRS(new Vector3(b.center.x, y, b.center.z), Quaternion.identity, esc) });
        }
        var marco = enX ? new Vector3(0.07f, b.size.y, grueso) : new Vector3(grueso, b.size.y, 0.07f);
        Vector3 lado = enX ? new Vector3(b.size.x / 2f - 0.035f, 0, 0) : new Vector3(0, 0, b.size.z / 2f - 0.035f);
        lista.Add(new CombineInstance { mesh = cub, transform = Matrix4x4.TRS(b.center + lado, Quaternion.identity, marco) });
        lista.Add(new CombineInstance { mesh = cub, transform = Matrix4x4.TRS(b.center - lado, Quaternion.identity, marco) });
        lista.Add(new CombineInstance { mesh = cub, transform = Matrix4x4.TRS(b.center, Quaternion.identity, enX ? new Vector3(0.05f, b.size.y, grueso) : new Vector3(grueso, b.size.y, 0.05f)) });
    }

    static void Tableros(Bounds b, List<CombineInstance> lista, Mesh cub, int porHoja)
    {
        bool enX = b.size.x >= b.size.z;
        float ancho = (enX ? b.size.x : b.size.z), grueso = (enX ? b.size.z : b.size.x) + 0.04f;
        float alto = b.size.y;
        float[] centros = { 0.27f, 0.7f };
        float[] altos = { 0.34f, 0.42f };
        for (int i = 0; i < 2; i++)
        {
            float y = b.min.y + alto * centros[i];
            float w = ancho - 0.3f, hgt = alto * altos[i];
            var esc = enX ? new Vector3(w, hgt, grueso) : new Vector3(grueso, hgt, w);
            lista.Add(new CombineInstance { mesh = cub, transform = Matrix4x4.TRS(new Vector3(b.center.x, y, b.center.z), Quaternion.identity, esc) });
        }
    }

    static Mesh MediaElipse(string name, float a, float h, float y0, float z)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        const int N = 32;
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N;
            Vector3 c = new Vector3(0f, y0, z);
            Vector3 p0 = new Vector3(Mathf.Cos(t0) * a, y0 + Mathf.Sin(t0) * h, z);
            Vector3 p1 = new Vector3(Mathf.Cos(t1) * a, y0 + Mathf.Sin(t1) * h, z);
            DobleTri(v, t, c, p0, p1);
        }
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        Guardar(m);
        return m;
    }

    static Mesh AnilloElipse(string name, float a, float h, float g, float y0, float z)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        const int N = 32;
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N;
            Vector3 i0 = new Vector3(Mathf.Cos(t0) * a, y0 + Mathf.Sin(t0) * h, z), i1 = new Vector3(Mathf.Cos(t1) * a, y0 + Mathf.Sin(t1) * h, z);
            Vector3 o0 = new Vector3(Mathf.Cos(t0) * (a + g), y0 + Mathf.Sin(t0) * (h + g), z), o1 = new Vector3(Mathf.Cos(t1) * (a + g), y0 + Mathf.Sin(t1) * (h + g), z);
            DobleTri(v, t, i0, o0, o1);
            DobleTri(v, t, i0, o1, i1);
        }
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        Guardar(m);
        return m;
    }

    static void DobleTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        i = v.Count;
        v.Add(a); v.Add(c); v.Add(b);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    static GameObject Caja(string name, Transform parent, Vector3 pos, Vector3 escala, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    static GameObject Cil(string name, Transform parent, Vector3 pos, float radio, float alto, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(radio * 2f, alto / 2f, radio * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    static GameObject MallaObj(string name, Transform parent, Mesh m, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.isStatic = true;
        return go;
    }

    static Transform Grupo(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.isStatic = true;
        return go.transform;
    }

    static void Guardar(Mesh m)
    {
        string ruta = GEN_DIR + "/" + m.name + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
    }

    static Material Buscar(string name)
    {
        foreach (var g in AssetDatabase.FindAssets(name + " t:Material"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m != null && m.name == name) return m;
        }
        return null;
    }

    static Material NuevoMat(string name, Color c, float suave)
    {
        Carpeta(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        c.a = 1f;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", suave);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Carpeta(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        Carpeta(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }
}
#endif

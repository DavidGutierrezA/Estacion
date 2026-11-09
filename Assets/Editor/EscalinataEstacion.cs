#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EscalinataEstacion
{
    const string ROOT = "COMUN_Escalinata";
    const string MAT_DIR = "Assets/Art/Materiales/Escalinata";
    const string GEN_DIR = "Assets/Art/Generado/Escalinata";

    const float CENTRO_X = 0f;
    const float Z_PLATAFORMA = 51.14f;
    const float Y_SUELO = 0.045f;
    const float Y_PLATAFORMA = 0.6f;
    const int CONTRAHUELLAS = 4;
    const float HUELLA = 0.4f;

    const float ANCHO_PASO = 5.8f;
    const float RADIO_ARCO = 1.0f;
    const float MARGEN_FRENTE = 0.25f;
    const float GROSOR = 0.42f;
    const float ALTO_BASE = 0.35f;
    const float ALTO_BALAUSTRE = 0.6f;
    const float PASO_BALAUSTRE = 0.24f;
    const float PEDESTAL = 0.5f;

    [MenuItem("Estacion/Escalinata/Construir escalinata")]
    public static void Construir()
    {
        foreach (var go in EscenaUtil.Raices())
            if (go.name == ROOT) Undo.DestroyObjectImmediate(go);
        var rootGo = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(rootGo, "Construir escalinata");
        rootGo.isStatic = true;
        var root = rootGo.transform;
        root.position = new Vector3(CENTRO_X, 0f, Z_PLATAFORMA);
        Carpeta(GEN_DIR);

        var suelo = Buscar("Suelo_Plaza.002") ?? NuevoMat("Escalinata_Piedra", new Color(0.66f, 0.65f, 0.62f));
        var blanco = Buscar("Molduras_Blanco") ?? NuevoMat("Escalinata_Blanco", new Color(0.95f, 0.94f, 0.91f));
        var crema = NuevoMat("Escalinata_Balaustre", new Color(0.91f, 0.86f, 0.73f));

        float anchoEscalones = ANCHO_PASO + 2f * (RADIO_ARCO + MARGEN_FRENTE + 0.35f);
        float sube = (Y_PLATAFORMA - Y_SUELO) / CONTRAHUELLAS;
        float zFrente = -(CONTRAHUELLAS - 1) * HUELLA;
        var escalones = Grupo("Escalones", root, Vector3.zero);
        for (int k = 0; k < CONTRAHUELLAS - 1; k++)
        {
            float top = Y_SUELO + sube * (k + 1);
            float z0 = -(CONTRAHUELLAS - 1 - k) * HUELLA, z1 = 0.1f;
            float ancho = anchoEscalones + (CONTRAHUELLAS - 2 - k) * 0.3f;
            Caja("Escalon_" + (k + 1), escalones, new Vector3(0f, top / 2f, (z0 + z1) / 2f), new Vector3(ancho, top, z1 - z0), suelo, false).tag = "SueloEstacion";
        }

        float dz = -zFrente, dy = Y_PLATAFORMA - Y_SUELO, largo = Mathf.Sqrt(dz * dz + dy * dy), ang = Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
        var rampa = new GameObject("Rampa_Colision");
        rampa.transform.SetParent(root, false);
        rampa.transform.localPosition = new Vector3(0f, (Y_SUELO + Y_PLATAFORMA) / 2f - 0.06f, zFrente / 2f);
        rampa.transform.localEulerAngles = new Vector3(-ang, 0f, 0f);
        rampa.AddComponent<BoxCollider>().size = new Vector3(anchoEscalones + 0.6f, 0.1f, largo);
        rampa.isStatic = true;

        var cil = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        var esf = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        var cub = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        float yBase = Y_PLATAFORMA + ALTO_BASE, yRiel = yBase + ALTO_BALAUSTRE;
        foreach (int lado in new[] { -1, 1 })
        {
            string nombre = lado < 0 ? "Izquierda" : "Derecha";
            var centro = new Vector2(lado * ANCHO_PASO / 2f, MARGEN_FRENTE + RADIO_ARCO);
            var g = Grupo("Baranda_" + nombre, root, new Vector3(centro.x, 0f, centro.y));

            var arco = new List<Vector2>();
            const int N = 28;
            for (int i = 0; i <= N; i++)
            {
                float a = (270f + lado * 90f * i / N) * Mathf.Deg2Rad;
                arco.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RADIO_ARCO);
            }

            MallaObj("Base", g, Barrido("Baranda_Base_" + nombre, arco, GROSOR, Y_PLATAFORMA - 0.02f, yBase), blanco).tag = "Fachada";
            MallaObj("Pasamanos", g, Barrido("Baranda_Pasamanos_" + nombre, arco, GROSOR - 0.06f, yRiel, yRiel + 0.1f), crema);
            var col = new GameObject("Colision");
            col.transform.SetParent(g, false);
            col.AddComponent<MeshCollider>().sharedMesh = Barrido("Baranda_Colision_" + nombre, arco, GROSOR, Y_PLATAFORMA - 0.02f, yRiel + 0.1f);
            col.isStatic = true;

            foreach (int extremo in new[] { 0, N })
            {
                Vector2 p = arco[extremo];
                Vector2 d = extremo == 0 ? arco[1] - arco[0] : arco[N] - arco[N - 1];
                float giro = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                string n = extremo == 0 ? "Pedestal_Frente" : "Pedestal_Fondo";
                var ped = Caja(n, g, new Vector3(p.x, (Y_PLATAFORMA + yRiel + 0.1f) / 2f, p.y), new Vector3(PEDESTAL, yRiel + 0.1f - Y_PLATAFORMA, PEDESTAL), blanco, true);
                ped.transform.localEulerAngles = new Vector3(0f, giro, 0f);
                ped.tag = "Fachada";
                var rem = Caja(n + "_Remate", g, new Vector3(p.x, yRiel + 0.15f, p.y), new Vector3(PEDESTAL + 0.12f, 0.1f, PEDESTAL + 0.12f), blanco, false);
                rem.transform.localEulerAngles = new Vector3(0f, giro, 0f);
                rem.tag = "Fachada";
            }

            float largoArco = Mathf.PI * 0.5f * RADIO_ARCO, libre = largoArco - PEDESTAL - 0.1f;
            int cant = Mathf.Max(1, Mathf.FloorToInt(libre / PASO_BALAUSTRE));
            var piezas = new List<CombineInstance>();
            for (int i = 0; i < cant; i++)
            {
                float s = (PEDESTAL / 2f + 0.05f + (i + 0.5f) * libre / cant) / largoArco;
                float a = (270f + lado * 90f * s) * Mathf.Deg2Rad;
                var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RADIO_ARCO;
                Vector3 b = new Vector3(p.x, yBase, p.y);
                piezas.Add(Pieza(cub, b + new Vector3(0f, 0.03f, 0f), new Vector3(0.17f, 0.06f, 0.17f)));
                piezas.Add(Pieza(cil, b + new Vector3(0f, ALTO_BALAUSTRE / 2f, 0f), new Vector3(0.09f, ALTO_BALAUSTRE / 2f, 0.09f)));
                piezas.Add(Pieza(esf, b + new Vector3(0f, ALTO_BALAUSTRE * 0.33f, 0f), new Vector3(0.17f, 0.26f, 0.17f)));
                piezas.Add(Pieza(esf, b + new Vector3(0f, ALTO_BALAUSTRE * 0.78f, 0f), new Vector3(0.12f, 0.1f, 0.12f)));
                piezas.Add(Pieza(cub, b + new Vector3(0f, ALTO_BALAUSTRE - 0.03f, 0f), new Vector3(0.15f, 0.06f, 0.15f)));
            }
            var malla = new Mesh { name = "Balaustres_" + nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            malla.CombineMeshes(piezas.ToArray(), true, true);
            malla.RecalculateBounds();
            Guardar(malla);
            MallaObj("Balaustres", g, malla, crema);
        }

        OcultarEscalonesViejos();
        AplicarEpocaActual();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Escalinata]</b> Escalinata construida (" + ROOT + ").");
    }

    static Mesh Barrido(string name, List<Vector2> camino, float ancho, float y0, float y1)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        int n = camino.Count;
        var izq = new Vector2[n]; var der = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 d = (camino[Mathf.Min(i + 1, n - 1)] - camino[Mathf.Max(i - 1, 0)]).normalized;
            Vector2 nn = new Vector2(-d.y, d.x) * (ancho / 2f);
            izq[i] = camino[i] + nn; der[i] = camino[i] - nn;
        }
        for (int i = 0; i < n - 1; i++)
        {
            Quad(v, t, V(izq[i], y1), V(izq[i + 1], y1), V(der[i + 1], y1), V(der[i], y1));
            Quad(v, t, V(izq[i], y0), V(izq[i + 1], y0), V(izq[i + 1], y1), V(izq[i], y1));
            Quad(v, t, V(der[i + 1], y0), V(der[i], y0), V(der[i], y1), V(der[i + 1], y1));
            Quad(v, t, V(der[i], y0), V(der[i + 1], y0), V(izq[i + 1], y0), V(izq[i], y0));
        }
        Quad(v, t, V(der[0], y0), V(izq[0], y0), V(izq[0], y1), V(der[0], y1));
        Quad(v, t, V(izq[n - 1], y0), V(der[n - 1], y0), V(der[n - 1], y1), V(izq[n - 1], y1));
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        Guardar(m);
        return m;
    }

    static Vector3 V(Vector2 p, float y) { return new Vector3(p.x, y, p.y); }

    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        t.Add(i); t.Add(i + 2); t.Add(i + 3);
    }

    static void Guardar(Mesh m)
    {
        string ruta = GEN_DIR + "/" + m.name + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
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

    static void OcultarEscalonesViejos()
    {
        var est = GameObject.Find("Estacion");
        if (est == null) return;
        foreach (var t in est.GetComponentsInChildren<Transform>(true))
            if (t.name == "Escalon" || t.name == "Portico_piso")
            {
                Undo.RecordObject(t.gameObject, "Ocultar escalon viejo");
                t.gameObject.SetActive(false);
            }
    }

    static void AplicarEpocaActual()
    {
        var em = GameObject.Find("EpocaManager");
        if (em == null) return;
        var comp = em.GetComponent("EpocaManager");
        if (comp == null) return;
        int actual = new SerializedObject(comp).FindProperty("epocaActual").intValue;
        var mi = comp.GetType().GetMethod("AplicarEpoca");
        if (mi != null) mi.Invoke(comp, new object[] { actual });
    }

    static CombineInstance Pieza(Mesh m, Vector3 pos, Vector3 escala)
    {
        return new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, Quaternion.identity, escala) };
    }

    static Transform Grupo(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.isStatic = true;
        return go.transform;
    }

    static GameObject Caja(string name, Transform parent, Vector3 pos, Vector3 escala, Material m, bool col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
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

    static Material NuevoMat(string name, Color c)
    {
        Carpeta(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.15f);
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

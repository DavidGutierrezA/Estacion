#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SantuarioVirgenViuda
{
    const string ROOT = "EPOCA_Viuda_Santuario_Virgen";
    const string EMPTY = "Ubicacion virgen viuda";
    const string MAT_DIR = "Assets/Art/Materiales/Plaza";
    const string GEN_DIR = "Assets/Art/Generado/Plaza";

    [Header("Medidas (locales: +z = frente)")]
    const float PED_ABAJO_X = 1.2f, PED_ABAJO_Z = 1.0f, PED_ARRIBA_X = 0.9f, PED_ARRIBA_Z = 0.75f, PED_ALTO = 1.35f;
    const float ALTO_ESTATUA = 1.75f;
    const float POSTE_X = 2.38f, POSTE_Z = 1.13f, POSTE_ALTO = 4.0f;
    const float TECHO_ANCHO = 5.4f, TECHO_FLECHA = 0.75f, TECHO_Z0 = -1.5f, TECHO_Z1 = 1.8f;

    static Mesh cubo, esfera;

    [MenuItem("Estacion/Plaza/11 - Santuario de la Virgen (Viuda)")]
    public static void Construir()
    {
        cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        esfera = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        var empty = EscenaUtil.Raiz(EMPTY);
        if (empty == null) { Debug.LogError("[Virgen Viuda] No encuentro '" + EMPTY + "'"); return; }
        foreach (var go in EscenaUtil.Raices()) if (go.name == ROOT) Undo.DestroyObjectImmediate(go);

        var root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, "Santuario Virgen");
        root.transform.position = new Vector3(empty.transform.position.x, 0f, empty.transform.position.z);
        root.transform.rotation = Quaternion.Euler(0f, empty.transform.eulerAngles.y, 0f);

        var rosado = Mat("Santuario_Pedestal", Color.white, 0.15f, 0f);
        rosado.SetTexture("_BaseMap", TexBloques());
        var nubeMat = Mat("Santuario_Nube", new Color(0.18f, 0.48f, 0.88f), 0.45f, 0f);
        var piel = Mat("Santuario_Angel_Piel", new Color(0.93f, 0.76f, 0.62f), 0.3f, 0f);
        var pelo = Mat("Santuario_Angel_Pelo", new Color(0.42f, 0.24f, 0.12f), 0.2f, 0f);
        var metal = Mat("Santuario_Metal", new Color(0.33f, 0.35f, 0.36f), 0.4f, 0.6f);
        var lamina = Mat("Santuario_Lamina", new Color(0.86f, 0.84f, 0.76f), 0.35f, 0f);

        Pieza(root.transform, "Pedestal", Frustum(), rosado, true, new Vector3(0f, PED_ALTO / 2f, 0f));

        var nube = new List<CombineInstance>();
        float y = PED_ALTO;
        Esf(nube, new Vector3(0f, y + 0.18f, 0.02f), new Vector3(0.82f, 0.36f, 0.66f));
        Esf(nube, new Vector3(-0.22f, y + 0.24f, 0.08f), new Vector3(0.42f, 0.32f, 0.42f));
        Esf(nube, new Vector3(0.23f, y + 0.22f, 0.06f), new Vector3(0.4f, 0.3f, 0.4f));
        Pieza(root.transform, "Nube", Combinar(nube), nubeMat, true, Vector3.zero);

        var caras = new List<CombineInstance>();
        var pelos = new List<CombineInstance>();
        foreach (var sx in new[] { -0.2f, 0.2f })
        {
            var c = new Vector3(sx, y + 0.2f, 0.36f);
            Esf(caras, c, new Vector3(0.17f, 0.2f, 0.12f));
            Esf(pelos, c + new Vector3(0f, 0.06f, -0.02f), new Vector3(0.2f, 0.16f, 0.13f));
        }
        Pieza(root.transform, "Angeles", Combinar(caras), piel, false, Vector3.zero);
        Pieza(root.transform, "Angeles_Pelo", Combinar(pelos), pelo, false, Vector3.zero);

        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Virgen/SantaMaria_1k.fbx");
        var piedra = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Juego/Virgen_Piedra.mat");
        if (piedra == null) foreach (var g in AssetDatabase.FindAssets("Virgen_Piedra t:Material")) { piedra = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)); break; }
        if (modelo != null)
        {
            var est = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            est.name = "Estatua";
            est.transform.SetParent(root.transform, false);
            est.transform.localRotation = Quaternion.identity;
            if (piedra != null) foreach (var r in est.GetComponentsInChildren<Renderer>()) r.sharedMaterial = piedra;
            var rs = est.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = Limites(rs);
                est.transform.localScale = Vector3.one * (ALTO_ESTATUA / Mathf.Max(0.01f, b.size.y));
                b = Limites(rs);
                var obj = root.transform.TransformPoint(new Vector3(0f, y + 0.36f, 0.02f));
                est.transform.position += new Vector3(obj.x - b.center.x, obj.y - b.min.y, obj.z - b.center.z);
            }
        }
        else Debug.LogWarning("[Virgen Viuda] No encontre Assets/Art/Virgen/SantaMaria_1k.fbx");

        var postes = new List<CombineInstance>();
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
                Celosia(postes, new Vector3(sx * POSTE_X, 0f, sz * POSTE_Z), POSTE_ALTO);
        Pieza(root.transform, "Columnas", Combinar(postes), metal, true, Vector3.zero);

        Pieza(root.transform, "Techo", Boveda(), lamina, true, Vector3.zero);

        var costillas = new List<CombineInstance>();
        int n = 14;
        foreach (var z in new[] { TECHO_Z0 + 0.05f, -0.4f, 0.7f, TECHO_Z1 - 0.05f })
        {
            Vector3 prev = PuntoArco(0, n, z) - Vector3.up * 0.04f;
            for (int i = 1; i <= n; i++)
            {
                var q = PuntoArco(i, n, z) - Vector3.up * 0.04f;
                Barra(costillas, prev, q, 0.05f);
                prev = q;
            }
        }
        foreach (var sx in new[] { -1f, 1f })
            Barra(costillas, new Vector3(sx * TECHO_ANCHO / 2f, POSTE_ALTO - 0.02f, TECHO_Z0), new Vector3(sx * TECHO_ANCHO / 2f, POSTE_ALTO - 0.02f, TECHO_Z1), 0.07f);
        Pieza(root.transform, "Estructura_Techo", Combinar(costillas), metal, false, Vector3.zero);

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 1.2f, 0f);
        col.size = new Vector3(PED_ABAJO_X, 2.4f, PED_ABAJO_Z);

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;

        foreach (var go in EscenaUtil.Raices())
            if (go.name == "EPOCA_Viuda_Jardines")
            {
                var gruta = go.transform.Find("Gruta_Virgen");
                if (gruta != null) { Undo.RecordObject(gruta.gameObject, "Ocultar gruta"); gruta.gameObject.SetActive(false); }
            }

        Registrar(root);
        EscenaUtil.Ordenar(false);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Virgen Viuda]</b> Santuario construido. La gruta vieja quedo desactivada.");
    }

    static Vector3 PuntoArco(int i, int n, float z)
    {
        float t = i / (float)n;
        float x = -TECHO_ANCHO / 2f + TECHO_ANCHO * t;
        float yy = POSTE_ALTO + TECHO_FLECHA * Mathf.Sin(Mathf.PI * t);
        return new Vector3(x, yy, z);
    }

    static Mesh Boveda()
    {
        const int n = 16;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        for (int cara = 0; cara < 2; cara++)
        {
            int b = v.Count;
            for (int i = 0; i <= n; i++)
            {
                var p = PuntoArco(i, n, 0f);
                v.Add(new Vector3(p.x, p.y, TECHO_Z0));
                v.Add(new Vector3(p.x, p.y, TECHO_Z1));
                uv.Add(new Vector2(i / (float)n * 4f, 0f));
                uv.Add(new Vector2(i / (float)n * 4f, 2f));
            }
            for (int i = 0; i < n; i++)
            {
                int a = b + i * 2;
                var centro = (v[a] + v[a + 3]) / 2f;
                var fuera = centro - new Vector3(0f, POSTE_ALTO - 2f, centro.z);
                if (cara == 1) fuera = -fuera;
                Tri(t, v, a, a + 1, a + 3, fuera);
                Tri(t, v, a, a + 3, a + 2, fuera);
            }
        }
        var m = new Mesh { name = "Santuario_Techo" };
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static Mesh Frustum()
    {
        float bx = PED_ABAJO_X / 2f, bz = PED_ABAJO_Z / 2f, tx = PED_ARRIBA_X / 2f, tz = PED_ARRIBA_Z / 2f, h = PED_ALTO / 2f;
        var abajo = new[] { new Vector3(-bx, -h, -bz), new Vector3(bx, -h, -bz), new Vector3(bx, -h, bz), new Vector3(-bx, -h, bz) };
        var arriba = new[] { new Vector3(-tx, h, -tz), new Vector3(tx, h, -tz), new Vector3(tx, h, tz), new Vector3(-tx, h, tz) };
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4, k = v.Count;
            v.Add(abajo[i]); v.Add(abajo[j]); v.Add(arriba[j]); v.Add(arriba[i]);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
            var fuera = (abajo[i] + abajo[j]) / 2f;
            fuera.y = 0f;
            Tri(t, v, k, k + 1, k + 2, fuera);
            Tri(t, v, k, k + 2, k + 3, fuera);
        }
        int s = v.Count;
        foreach (var p in arriba) { v.Add(p); uv.Add(new Vector2(p.x + 0.5f, p.z + 0.5f)); }
        Tri(t, v, s, s + 1, s + 2, Vector3.up);
        Tri(t, v, s, s + 2, s + 3, Vector3.up);
        var m = new Mesh { name = "Santuario_Pedestal" };
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void Celosia(List<CombineInstance> l, Vector3 c, float alto)
    {
        const float a = 0.09f, g = 0.03f, paso = 0.32f;
        var esquinas = new[] { new Vector3(-a, 0f, -a), new Vector3(a, 0f, -a), new Vector3(a, 0f, a), new Vector3(-a, 0f, a) };
        foreach (var e in esquinas) Barra(l, c + e, c + e + Vector3.up * alto, g);
        for (int lado = 0; lado < 4; lado++)
        {
            var p0 = esquinas[lado];
            var p1 = esquinas[(lado + 1) % 4];
            bool alt = false;
            for (float yy = 0f; yy + paso <= alto; yy += paso)
            {
                var a0 = c + (alt ? p1 : p0) + Vector3.up * yy;
                var a1 = c + (alt ? p0 : p1) + Vector3.up * (yy + paso);
                Barra(l, a0, a1, g * 0.6f);
                alt = !alt;
            }
        }
    }

    static void Barra(List<CombineInstance> l, Vector3 a, Vector3 b, float g)
    {
        var d = b - a;
        if (d.sqrMagnitude < 1e-8f) return;
        var up = Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        l.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS((a + b) / 2f, Quaternion.LookRotation(d.normalized, up), new Vector3(g, g, d.magnitude)) });
    }

    static void Esf(List<CombineInstance> l, Vector3 c, Vector3 s)
    {
        l.Add(new CombineInstance { mesh = esfera, transform = Matrix4x4.TRS(c, Quaternion.identity, s) });
    }

    static Mesh Combinar(List<CombineInstance> partes)
    {
        var m = new Mesh { indexFormat = IndexFormat.UInt32 };
        m.CombineMeshes(partes.ToArray(), true, true);
        m.RecalculateBounds();
        return m;
    }

    static void Tri(List<int> t, List<Vector3> v, int a, int b, int c, Vector3 fuera)
    {
        var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, fuera) < 0f) { t.Add(a); t.Add(c); t.Add(b); }
        else { t.Add(a); t.Add(b); t.Add(c); }
    }

    static void Pieza(Transform root, string nombre, Mesh mesh, Material mat, bool sombra, Vector3 offset)
    {
        var centro = mesh.bounds.center;
        var v = mesh.vertices;
        for (int i = 0; i < v.Length; i++) v[i] -= centro;
        mesh.vertices = v;
        mesh.RecalculateBounds();
        mesh.name = "Santuario_" + nombre;
        var guardada = Guardar(mesh, GEN_DIR + "/Santuario_" + nombre + ".asset");
        var go = new GameObject(nombre);
        go.transform.SetParent(root, false);
        go.transform.localPosition = centro + offset;
        go.transform.localRotation = Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh = guardada;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }

    static Mesh Guardar(Mesh mesh, string path)
    {
        EnsureDir(GEN_DIR);
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existente == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        existente.Clear();
        existente.indexFormat = mesh.indexFormat;
        existente.SetVertices(mesh.vertices);
        existente.SetNormals(mesh.normals);
        if (mesh.uv.Length > 0) existente.SetUVs(0, mesh.uv);
        existente.SetTriangles(mesh.triangles, 0);
        existente.RecalculateBounds();
        existente.name = mesh.name;
        EditorUtility.SetDirty(existente);
        Object.DestroyImmediate(mesh);
        return existente;
    }

    static Bounds Limites(Renderer[] rs)
    {
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static Material Mat(string nombre, Color c, float suavidad, float metal)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", suavidad);
        m.SetFloat("_Metallic", metal);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D TexBloques()
    {
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Santuario_Bloques.png";
        var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (ex != null) return ex;
        const int W = 128, H = 128;
        var px = new Color[W * H];
        for (int yy = 0; yy < H; yy++)
            for (int x = 0; x < W; x++)
            {
                int fila = yy / 43, xx = x + (fila % 2) * 32;
                bool junta = (yy % 43) < 2 || (xx % 64) < 2;
                float r = Mathf.PerlinNoise(x * 0.08f, yy * 0.08f) * 0.05f;
                px[yy * W + x] = junta ? new Color(0.72f, 0.45f, 0.4f) : new Color(0.88f + r, 0.58f + r, 0.52f + r);
            }
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Registrar(GameObject obj)
    {
        var em = EscenaUtil.Raiz("EpocaManager");
        if (em == null) return;
        var comp = em.GetComponent("EpocaManager");
        var so = new SerializedObject(comp);
        var list = so.FindProperty("epocas");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            bool esViuda = e.FindPropertyRelative("nombre").stringValue.ToLower().Contains("viuda");
            var arr = e.FindPropertyRelative(esViuda ? "activar" : "desactivar");
            for (int j = arr.arraySize - 1; j >= 0; j--)
                if (arr.GetArrayElementAtIndex(j).objectReferenceValue == null) arr.DeleteArrayElementAtIndex(j);
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = obj;
        }
        so.ApplyModifiedProperties();
        var mi = comp.GetType().GetMethod("AplicarEpoca");
        if (mi != null) mi.Invoke(comp, new object[] { so.FindProperty("epocaActual").intValue });
    }

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif

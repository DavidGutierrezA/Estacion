#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// =============================================================================
//  MONUMENTO A LA PAZ Y LA RECONCILIACION (2025)  —  solo epoca 2026
//  Menu: Estacion > Esculturas > 1 - Monumento a la paz
//
//  Version low-poly segun las fotos de David: pedestal negro de losas (2 bloques),
//  roca de bronce con armas depuestas y careta triste, paloma, una pata y una mano
//  que sostienen dos alas abiertas con la careta alegre entre ellas, placa dorada.
//  Se ubica en el empty "Ubicacion monumento paz" (se crea si no existe):
//  mueve/rota ese empty y vuelve a correr el menu. El frente mira al +Z del empty.
//  Todo el bronce queda en UNA malla y el granito en otra (ligero para WebGL).
// =============================================================================
public static class MonumentoPaz
{
    const string ROOT = "Monumento_Paz";
    const string EMPTY = "Ubicacion monumento paz";
    static readonly Vector3 POS_DEFECTO = new Vector3(-38f, 0f, 66.5f);   // anden, junto a la via, a la izquierda de la estacion
    const string MAT_DIR = "Assets/Art/Materiales/Esculturas";
    const string GEN_DIR = "Assets/Art/Generado/Esculturas";

    // listas de piezas por material
    static List<CombineInstance> bronce, bronceOscuro, granito, granitoSup, placa, negro;
    static Mesh cubo, esfera, cilindro, capsula;

    [MenuItem("Estacion/Esculturas/1 - Monumento a la paz")]
    public static void Construir()
    {
        cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        esfera = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        cilindro = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        capsula = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        bronce = new List<CombineInstance>(); bronceOscuro = new List<CombineInstance>();
        granito = new List<CombineInstance>(); granitoSup = new List<CombineInstance>();
        placa = new List<CombineInstance>(); negro = new List<CombineInstance>();

        // ---------- ubicacion ----------
        GameObject empty = null;
        foreach (var go in Raices()) if (go.name == EMPTY) empty = go;
        if (empty == null)
        {
            empty = new GameObject(EMPTY);
            empty.transform.position = POS_DEFECTO;
            empty.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // frente hacia la plaza (-Z del mundo)
            Undo.RegisterCreatedObjectUndo(empty, "Empty monumento");
        }
        foreach (var go in Raices()) if (go.name == ROOT) Undo.DestroyObjectImmediate(go);
        var root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, "Monumento paz");
        root.transform.position = new Vector3(empty.transform.position.x, 0f, empty.transform.position.z);
        root.transform.rotation = Quaternion.Euler(0f, empty.transform.eulerAngles.y, 0f);

        // ---------- materiales ----------
        var mBronce = Mat("Bronce", new Color(0.32f, 0.27f, 0.16f), 0.85f, 0.55f);
        var mBronceOsc = Mat("Bronce_Patina", new Color(0.17f, 0.16f, 0.11f), 0.55f, 0.35f);
        var texLosas = TexturaLosas();
        var mGranito = Mat("Granito_Negro", Color.white, 0.05f, 0.30f);
        mGranito.SetTexture("_BaseMap", texLosas); mGranito.SetTextureScale("_BaseMap", Vector2.one);
        var mGranitoSup = Mat("Granito_Negro_Superior", Color.white, 0.05f, 0.30f);
        mGranitoSup.SetTexture("_BaseMap", texLosas); mGranitoSup.SetTextureScale("_BaseMap", new Vector2(1f, 0.29f));
        var mPlaca = Mat("Placa_Dorada", new Color(0.86f, 0.77f, 0.58f), 0.2f, 0.45f);
        var mNegro = Mat("Bronce_Sombra", new Color(0.05f, 0.05f, 0.04f), 0.2f, 0.2f);

        // ---------- pedestal (medidas a ojo de las fotos) ----------
        const float hBase = 2.2f, hSup = 0.55f;
        Add(granito, cubo, new Vector3(0, hBase / 2f, 0), Vector3.zero, new Vector3(1.3f, hBase, 0.8f));
        Add(granitoSup, cubo, new Vector3(0, hBase + hSup / 2f, 0), Vector3.zero, new Vector3(0.95f, hSup, 0.62f));
        Add(placa, cubo, new Vector3(0, 1.45f, 0.405f), Vector3.zero, new Vector3(0.44f, 0.58f, 0.012f));
        float y0 = hBase + hSup;   // tope del pedestal (2.75 m)

        // ---------- roca (version simple) ----------
        Add(bronceOscuro, cubo, new Vector3(0, y0 + 0.11f, 0), Vector3.zero, new Vector3(0.86f, 0.22f, 0.52f));
        Add(bronceOscuro, esfera, new Vector3(-0.2f, y0 + 0.22f, 0f), new Vector3(0, -20, 0), new Vector3(0.45f, 0.22f, 0.45f));
        Add(bronceOscuro, esfera, new Vector3(0.22f, y0 + 0.2f, 0.02f), new Vector3(0, 25, 0), new Vector3(0.45f, 0.2f, 0.42f));
        // careta triste en el frente de la roca
        Careta(new Vector3(0.0f, y0 + 0.12f, 0.27f), 0.85f, false);

        // ---------- dos soportes sencillos que sostienen las alas ----------
        Hueso(bronce, new Vector3(-0.2f, y0 + 0.25f, 0f), new Vector3(-0.12f, y0 + 0.8f, 0f), 0.055f);
        Hueso(bronce, new Vector3(0.22f, y0 + 0.25f, 0f), new Vector3(0.16f, y0 + 0.9f, 0f), 0.065f);

        // ---------- alas ----------
        Ala(new Vector3(-0.08f, y0 + 0.72f, 0f), -1f, 1.12f, 14f);   // izquierda, un poco mas alta
        Ala(new Vector3(0.12f, y0 + 0.84f, 0f), 1f, 0.95f, -14f);    // derecha, sostenida por la mano
        // careta alegre entre las alas
        Careta(new Vector3(0.02f, y0 + 1.02f, 0.06f), 1f, true);

        // ---------- mallas finales ----------
        Combinar(root.transform, "Pedestal", granito, mGranito, true);
        Combinar(root.transform, "Pedestal_Superior", granitoSup, mGranitoSup, false);
        Combinar(root.transform, "Placa", placa, mPlaca, false);
        Combinar(root.transform, "Escultura_Bronce", bronce, mBronce, false);
        Combinar(root.transform, "Escultura_Patina", bronceOscuro, mBronceOsc, false);
        Combinar(root.transform, "Escultura_Detalles", negro, mNegro, false);

        // colision del pedestal
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, hBase / 2f, 0); col.size = new Vector3(1.3f, hBase, 0.8f);

        // texto de la placa (resumen, no la placa textual)
        var t = new GameObject("Texto_Placa");
        t.transform.SetParent(root.transform, false);
        t.transform.localPosition = new Vector3(0, 1.45f, 0.413f);
        t.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        var tmp = t.AddComponent<TextMeshPro>();
        tmp.text = "<b>MONUMENTO A LA PAZ\nY LA RECONCILIACIÓN</b>\n\n<size=75%>Las alas: esperanza y libertad.\nLas armas depuestas: diálogo\ny tolerancia. Las caretas: el arte\nque transforma el dolor en unión.</size>\n\n<size=80%><b>Alcaldía de Cajicá · 2025</b></size>";
        tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.1f; tmp.fontSizeMax = 0.32f; tmp.color = new Color(0.16f, 0.13f, 0.08f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(0.4f, 0.54f);

        foreach (var tr in root.GetComponentsInChildren<Transform>(true)) tr.gameObject.isStatic = true;
        Registrar(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Esculturas]</b> Monumento a la paz en " + root.transform.position + " (solo 2026).");
    }

    // ------------------------------------------------------------------ piezas
    // Careta: ovalo con ojos y boca (alegre = boca hacia arriba).
    static void Careta(Vector3 c, float s, bool alegre)
    {
        Add(bronce, esfera, c, Vector3.zero, new Vector3(0.15f, 0.19f, 0.07f) * s);
        Add(negro, esfera, c + new Vector3(-0.035f, 0.035f, 0.03f) * s, new Vector3(0, 0, alegre ? 15 : -15), new Vector3(0.04f, alegre ? 0.02f : 0.03f, 0.02f) * s);
        Add(negro, esfera, c + new Vector3(0.035f, 0.035f, 0.03f) * s, new Vector3(0, 0, alegre ? -15 : 15), new Vector3(0.04f, alegre ? 0.02f : 0.03f, 0.02f) * s);
        // boca: dos trazos en V (sonrisa) o V invertida (tristeza)
        float dy = alegre ? -0.045f : -0.06f, ang = alegre ? 22f : -22f;
        Add(negro, cubo, c + new Vector3(-0.022f, dy, 0.032f) * s, new Vector3(0, 0, -ang), new Vector3(0.05f, 0.012f, 0.01f) * s);
        Add(negro, cubo, c + new Vector3(0.022f, dy, 0.032f) * s, new Vector3(0, 0, ang), new Vector3(0.05f, 0.012f, 0.01f) * s);
    }

    static void Hueso(List<CombineInstance> lista, Vector3 a, Vector3 b, float r)
    {
        var d = b - a;
        var rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
        lista.Add(new CombineInstance { mesh = capsula, transform = Matrix4x4.TRS((a + b) / 2f, rot, new Vector3(r * 2f, d.magnitude / 2f + r, r * 2f)) });
    }

    // Ala abierta hacia arriba: lamina con la silueta + plumas en hileras por delante y por detras.
    // lado = -1 izquierda, +1 derecha. inclinacion en grados (giro en Z).
    static void Ala(Vector3 raiz, float lado, float escala, float inclinacion)
    {
        // silueta del ala derecha (u hacia afuera, v hacia arriba), en metros
        var sil = new List<Vector2> {
            new Vector2(-0.06f, 0.00f), new Vector2(0.15f, -0.08f), new Vector2(0.38f, 0.10f), new Vector2(0.55f, 0.42f),
            new Vector2(0.65f, 0.80f), new Vector2(0.70f, 1.30f), new Vector2(0.58f, 1.18f), new Vector2(0.45f, 1.05f),
            new Vector2(0.30f, 0.90f), new Vector2(0.15f, 0.70f), new Vector2(0.02f, 0.48f), new Vector2(-0.06f, 0.25f) };
        var m = Matrix4x4.TRS(raiz, Quaternion.Euler(0, 0, inclinacion), Vector3.one * escala);

        // lamina (dos caras + canto), malla propia
        var v = new List<Vector3>(); var t = new List<int>();
        var p3 = new List<Vector3>();
        foreach (var q in sil) p3.Add(new Vector3(q.x * lado, q.y, 0));
        var tris = Orejas(sil);
        const float e = 0.03f;
        for (int k = 0; k < tris.Count; k += 3)
        {
            Vector3 a = p3[tris[k]], b = p3[tris[k + 1]], c = p3[tris[k + 2]];
            Tri(v, t, a + Vector3.forward * e, b + Vector3.forward * e, c + Vector3.forward * e, Vector3.forward);
            Tri(v, t, a - Vector3.forward * e, b - Vector3.forward * e, c - Vector3.forward * e, Vector3.back);
        }
        for (int i = 0; i < p3.Count; i++)
        {
            Vector3 a = p3[i], b = p3[(i + 1) % p3.Count];
            Vector3 n = Vector3.Cross(b - a, Vector3.forward).normalized;
            Vector3 cen = Vector3.zero; foreach (var q in p3) cen += q; cen /= p3.Count;
            if (Vector3.Dot(n, (a + b) / 2f - cen) < 0) n = -n;
            Tri(v, t, a + Vector3.forward * e, b + Vector3.forward * e, b - Vector3.forward * e, n);
            Tri(v, t, a + Vector3.forward * e, b - Vector3.forward * e, a - Vector3.forward * e, n);
        }
        var lamina = new Mesh { name = "Ala_Lamina" };
        lamina.SetVertices(v); lamina.SetTriangles(t, 0); lamina.RecalculateNormals();
        bronce.Add(new CombineInstance { mesh = lamina, transform = m });

        // plumas: tarjetas alargadas que apuntan hacia la punta, en hileras escalonadas
        float angPluma = Mathf.Atan2(0.38f, 1f) * Mathf.Rad2Deg;   // inclinadas hacia afuera
        int fila = 0;
        for (float vv = 0.02f; vv < 1.22f; vv += 0.1f, fila++)
            for (float uu = -0.04f + (fila % 2) * 0.035f; uu < 0.72f; uu += 0.07f)
            {
                var pt = new Vector2(uu, vv);
                if (!Dentro(pt, sil)) continue;
                float largo = Mathf.Lerp(0.16f, 0.3f, vv / 1.2f);
                for (int cara = -1; cara <= 1; cara += 2)
                {
                    var pos = new Vector3(uu * lado, vv + largo * 0.35f, cara * (e + 0.008f + fila * 0.0015f));
                    var rot = Quaternion.Euler(cara * -10f, 0, -angPluma * lado);
                    bronce.Add(new CombineInstance { mesh = esfera, transform = m * Matrix4x4.TRS(pos, rot, new Vector3(0.085f, largo, 0.035f)) });
                }
            }
        // plumas largas del borde exterior (el fleco del ala)
        for (int i = 1; i <= 5; i++)
        {
            Vector2 a = sil[i], b = sil[i + 1];
            for (int s = 0; s < 3; s++)
            {
                var q = Vector2.Lerp(a, b, s / 3f);
                var pos = new Vector3(q.x * lado, q.y + 0.08f, 0);
                var rot = Quaternion.Euler(0, 0, -(angPluma + 8f) * lado);
                bronce.Add(new CombineInstance { mesh = esfera, transform = m * Matrix4x4.TRS(pos, rot, new Vector3(0.09f, 0.34f, 0.035f)) });
            }
        }
    }

    // ------------------------------------------------------------------ utilidades
    static void Add(List<CombineInstance> lista, Mesh mesh, Vector3 pos, Vector3 rot, Vector3 esc)
    { lista.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, Quaternion.Euler(rot), esc) }); }

    static void Combinar(Transform root, string name, List<CombineInstance> lista, Material mat, bool conCollider)
    {
        if (lista.Count == 0) return;
        var mesh = new Mesh { name = "Monumento_" + name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(lista.ToArray(), true, true);
        mesh.RecalculateBounds();
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Monumento_" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color c, float metal, float smooth)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Losas de piedra negra con juntas: 2 columnas x 7 filas (como el pedestal real).
    static Texture2D TexturaLosas()
    {
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Losas_Granito.png";
        var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (ex != null) return ex;
        const int W = 256, H = 512;
        var r = new System.Random(25);
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int fila = y * 7 / H, col = x * 2 / W;
                int yy = y - fila * H / 7, xx = x - col * W / 2;
                bool junta = yy < 3 || xx < 3;
                float ruido = (float)r.NextDouble() * 0.035f + Mathf.PerlinNoise(x * 0.05f + col * 7f, y * 0.05f + fila * 3f) * 0.05f;
                float t = junta ? 0.07f : 0.15f + ruido;
                px[y * W + x] = new Color(t, t, t * 1.05f);
            }
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) { var tmp = b; b = c; c = tmp; }
        int k = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(k); t.Add(k + 1); t.Add(k + 2);
    }

    static bool Dentro(Vector2 pt, List<Vector2> p)
    {
        bool c = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            if (((p[i].y > pt.y) != (p[j].y > pt.y)) && (pt.x < (p[j].x - p[i].x) * (pt.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)) c = !c;
        return c;
    }

    static List<int> Orejas(List<Vector2> pIn)
    {
        var p = new List<Vector2>(pIn);
        float area = 0f; for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; area += a.x * b.y - b.x * a.y; }
        var idx = new List<int>(); for (int i = 0; i < p.Count; i++) idx.Add(i);
        if (area < 0) idx.Reverse();
        var tris = new List<int>();
        int guard = 0;
        while (idx.Count > 3 && guard++ < 5000)
        {
            bool corto = false;
            for (int k = 0; k < idx.Count; k++)
            {
                int ia = idx[(k - 1 + idx.Count) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                Vector2 a = p[ia], b = p[ib], c = p[ic];
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) <= 1e-6f) continue;
                bool vacia = true;
                foreach (int j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    if (EnTri(p[j], a, b, c)) { vacia = false; break; }
                }
                if (!vacia) continue;
                tris.Add(ia); tris.Add(ib); tris.Add(ic); idx.RemoveAt(k); corto = true; break;
            }
            if (!corto) break;
        }
        if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
        return tris;
    }

    static bool EnTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0), pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(neg && pos);
    }

    static GameObject[] Raices() { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(); }

    // Solo en 2026: activo en 2026, desactivado en las demas epocas.
    static void Registrar(GameObject obj)
    {
        GameObject em = null;
        foreach (var go in Raices()) if (go.name == "EpocaManager") em = go;
        if (em == null) return;
        var comp = em.GetComponent("EpocaManager");
        var so = new SerializedObject(comp);
        var list = so.FindProperty("epocas");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            bool es2026 = e.FindPropertyRelative("nombre").stringValue.Contains("2026");
            var arr = e.FindPropertyRelative(es2026 ? "activar" : "desactivar");
            for (int j = arr.arraySize - 1; j >= 0; j--) if (arr.GetArrayElementAtIndex(j).objectReferenceValue == null) arr.DeleteArrayElementAtIndex(j);
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

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// =============================================================================
//  ESCULTURA DEL TREN (relieve + dos senoras sentadas)  —  solo epoca 2026
//  Menu: Estacion > Esculturas > 2 - Relieve del tren y las senoras
//
//  Version simple: el muro lleva las FOTOS de David como textura
//  (Assets/Art/Esculturas/Relieve_Frente.png y Relieve_Espalda.png, recortadas
//  y enderezadas), sobre una base de roca de bronce. Las senoras son un
//  marcador sencillo (roca + dos figuras sentadas sin detalle).
//  Usa el marcador de David: raiz "Estatua" -> "Cube" (se DESACTIVA) y
//  "Riel estatua" (se conserva). Frente hacia -Z (la plaza).
// =============================================================================
public static class RelieveTren
{
    const string MAT_DIR = "Assets/Art/Materiales/Esculturas";
    const string GEN_DIR = "Assets/Art/Generado/Esculturas";
    const string TEX_DIR = "Assets/Art/Esculturas";
    const string GRUPO = "Escultura_Tren";

    static Dictionary<Material, List<CombineInstance>> piezas;
    static Mesh cubo, esfera, capsula, quad;

    [MenuItem("Estacion/Esculturas/2 - Relieve del tren y las senoras")]
    public static void Construir()
    {
        cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        esfera = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        capsula = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        piezas = new Dictionary<Material, List<CombineInstance>>();

        GameObject raiz = null;
        foreach (var go in Raices()) if (go.name == "Estatua") raiz = go;
        if (raiz == null) { Debug.LogError("[Esculturas] No encuentro el objeto raiz 'Estatua'"); return; }
        var viejo = raiz.transform.Find(GRUPO);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        Vector3 centro = raiz.transform.position + new Vector3(1.67f, 0f, 7.17f);
        float W = 6.4f;
        var cube = raiz.transform.Find("Cube");
        if (cube != null)
        {
            var b = cube.GetComponent<Renderer>().bounds;
            centro = new Vector3(b.center.x, 0f, b.center.z);
            W = Mathf.Min(6.6f, b.size.x - 0.2f);
            Undo.RecordObject(cube.gameObject, "Ocultar marcador");
            cube.gameObject.SetActive(false);
        }

        var g = new GameObject(GRUPO);
        Undo.RegisterCreatedObjectUndo(g, "Relieve tren");
        g.transform.SetParent(raiz.transform, false);
        g.transform.position = centro;
        g.transform.rotation = Quaternion.identity;

        // ---------- materiales ----------
        var bronce = Mat("Bronce_Relieve", new Color(0.46f, 0.35f, 0.19f), 0.35f, 0.35f, null);
        var muro   = Mat("Muro_Ocre_Patinado", new Color(0.40f, 0.33f, 0.18f), 0.2f, 0.25f, null);
        var frente = Mat("Relieve_Frente_Foto", Color.white, 0f, 0.25f, Tex("Relieve_Frente.png"));
        var espalda = Mat("Relieve_Espalda_Foto", Color.white, 0f, 0.2f, Tex("Relieve_Espalda.png"));

        // proporcion de la foto del frente (1024x384): el panel con el relieve
        float hPanel = W * 384f / 1024f;          // ~2.4 m
        float hBase = 0.65f, H = hBase + hPanel;   // ~3.05 m en total
        const float D = 0.4f;                      // grosor del muro

        // muro
        Add(muro, cubo, new Vector3(0, H / 2f, D / 2f), Vector3.zero, new Vector3(W, H, D));
        // foto del relieve (cara -Z) y foto de la espalda (cara +Z)
        Add(frente, quad, new Vector3(0, hBase + hPanel / 2f, -0.006f), Vector3.zero, new Vector3(W, hPanel, 1f));
        Add(espalda, quad, new Vector3(0, H / 2f, D + 0.006f), new Vector3(0, 180, 0), new Vector3(W, H, 1f));
        // base de roca de bronce (adelante, sencilla)
        Add(bronce, cubo, new Vector3(0, hBase / 2f, -0.1f), Vector3.zero, new Vector3(W + 0.12f, hBase, 0.6f));
        Add(bronce, cubo, new Vector3(0, hBase - 0.04f, -0.32f), new Vector3(-35, 0, 0), new Vector3(W + 0.12f, 0.12f, 0.2f));   // bisel de arriba

        // ---------- las dos senoras (marcador sencillo) ----------
        var sen = new GameObject("Senoras");
        sen.transform.SetParent(g.transform, false);
        sen.transform.localPosition = new Vector3(-W / 2f + 0.1f, 0f, -1.75f);
        sen.transform.localRotation = Quaternion.Euler(0, 15f, 0);
        var guard = piezas; piezas = new Dictionary<Material, List<CombineInstance>>();
        Add(bronce, cubo, new Vector3(0, 0.25f, 0.1f), Vector3.zero, new Vector3(1.8f, 0.5f, 0.8f));                 // roca / banca
        for (int k = -1; k <= 1; k += 2)
        {
            Vector3 a = new Vector3(0.42f * k, 0.5f, 0f);
            // figura tipo "peon": cuerpo redondeado + cabeza (marcador sencillo)
            Add(bronce, capsula, a + new Vector3(0, 0.36f, 0f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.42f));       // cuerpo
            Add(bronce, esfera, a + new Vector3(0, 0.92f, 0f), Vector3.zero, Vector3.one * 0.26f);                   // cabeza
        }
        var senPiezas = piezas; piezas = guard;
        CombinarEn(sen.transform, senPiezas, "Senoras");
        var colS = sen.AddComponent<BoxCollider>(); colS.center = new Vector3(0, 0.6f, 0.05f); colS.size = new Vector3(1.8f, 1.2f, 0.85f);

        CombinarEn(g.transform, piezas, "Relieve");
        var col = g.AddComponent<BoxCollider>(); col.center = new Vector3(0, H / 2f, 0.15f); col.size = new Vector3(W, H, 0.9f);

        foreach (var tr in g.GetComponentsInChildren<Transform>(true)) tr.gameObject.isStatic = true;
        Registrar(raiz);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(raiz.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Esculturas]</b> Relieve del tren (con fotos) y senoras en " + centro + " (solo 2026).");
    }

    // ------------------------------------------------------------------ utilidades
    static Texture2D Tex(string archivo)
    {
        string path = TEX_DIR + "/" + archivo;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && (imp.wrapMode != TextureWrapMode.Clamp || imp.maxTextureSize != 1024))
        {
            imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.anisoLevel = 4;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Add(Material m, Mesh mesh, Vector3 pos, Vector3 rot, Vector3 esc)
    {
        List<CombineInstance> l;
        if (!piezas.TryGetValue(m, out l)) { l = new List<CombineInstance>(); piezas[m] = l; }
        l.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, Quaternion.Euler(rot), esc) });
    }

    // una sola malla con una submalla por material
    static void CombinarEn(Transform parent, Dictionary<Material, List<CombineInstance>> dic, string name)
    {
        var partes = new List<CombineInstance>(); var mats = new List<Material>();
        foreach (var kv in dic)
        {
            var sub = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            sub.CombineMeshes(kv.Value.ToArray(), true, true);
            partes.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
            mats.Add(kv.Key);
        }
        var mesh = new Mesh { name = "Escultura_" + name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(partes.ToArray(), false, false);
        mesh.RecalculateBounds();
        foreach (var p in partes) Object.DestroyImmediate(p.mesh);
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Escultura_" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name + "_Malla");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
    }

    static Material Mat(string name, Color c, float metal, float smooth, Texture2D tex)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
        m.SetTexture("_BaseMap", tex);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject[] Raices() { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(); }

    // El conjunto (relieve, senoras y reja de David) solo existe en 2026.
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
            bool ya = false;
            for (int j = arr.arraySize - 1; j >= 0; j--)
            {
                var o = arr.GetArrayElementAtIndex(j).objectReferenceValue;
                if (o == null) arr.DeleteArrayElementAtIndex(j); else if (o == obj) ya = true;
            }
            if (ya) continue;
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

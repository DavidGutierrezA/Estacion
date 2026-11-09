#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

// =============================================================================
//  PERSONERIA (version nueva, segun fotos reales)   Menu: Estacion > Personeria > Construir
//  - Plataforma de piedra arenisca (bloques irregulares) con ventilaciones y puertita.
//  - Escaleras en las esquinas de los dos extremos + baranda negra de 3 travesanos.
//  - Puertas con arco rebajado y marco blanco grueso + ventana en abanico
//    (madera hacia la plaza, vidrio hacia la via). Ventana de medio punto en los extremos.
//  - Zocalo y cornisa blancos, alero ancho con borde cafe y mensulas de hierro.
//  - Letrero "PERSONERIA de CAJICA" (solo en 2026).
//  Colores por epoca: muros = tag Estructura, blancos = Fachada, techo = Techo
//  (la Personeria toma los mismos colores que la estacion). La version vieja queda desactivada.
// =============================================================================
public static class PersoneriaNueva
{
    const string ROOT = "Personeria_Nueva";
    const string MAT_DIR = "Assets/Art/Materiales/Personeria";
    const string GEN_DIR = "Assets/Art/Generado/Personeria";

    // Medidas (metros). Centro igual al de la Personeria original.
    static readonly Vector3 C = new Vector3(62.9f, 0f, 57.1f);
    const float PW = 26f, PD = 14f, PH = 1.25f;      // plataforma
    const float BW = 20f, BD = 8.4f, BH = 4.4f;      // edificio
    const float ALERO = 0.9f, TECHO_ALTO = 1.1f;

    [MenuItem("Estacion/Personeria/Construir Personeria nueva")]
    public static void Construir()
    {
        var piedraTex = GenerarPiedra();
        var ladrilloTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generado/Ladrillo_Piso.png");

        var piedra   = Mat("Per2_Piedra_Arenisca", Color.white, 0.1f, piedraTex);
        var pisoRojo = Mat("Per2_Piso_Ladrillo", new Color(0.72f, 0.40f, 0.31f), 0.15f, ladrilloTex);
        var muro     = Mat("Per2_Muro", new Color(0.86f, 0.58f, 0.33f), 0.15f);
        var blanco   = Mat("Per2_Blanco", new Color(0.95f, 0.94f, 0.90f), 0.2f);
        var teja     = Mat("Per2_Techo", new Color(0.66f, 0.33f, 0.27f), 0.25f);
        var fascia   = Mat("Per2_Alero", new Color(0.24f, 0.16f, 0.12f), 0.3f);
        var hierro   = Mat("Per2_Hierro", new Color(0.07f, 0.07f, 0.08f), 0.4f, null, 0.6f);
        var madera   = Mat("Per2_Madera", new Color(0.56f, 0.32f, 0.17f), 0.35f);
        var vidrio   = Mat("Per2_Vidrio", new Color(0.55f, 0.65f, 0.70f), 0.9f, null, 0.1f);
        var oscuro   = Mat("Per2_Oscuro", new Color(0.10f, 0.11f, 0.12f), 0.3f);

        // borrar version anterior de este script y desactivar la Personeria vieja
        foreach (var go in Raices()) if (go.name == ROOT) Undo.DestroyObjectImmediate(go);
        foreach (var go in Raices()) if (go.name == "Personeria_Antigua") { Undo.RecordObject(go, "Ocultar vieja"); go.SetActive(false); }

        var root = new GameObject(ROOT).transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Personeria nueva");
        root.position = C;

        // ---------------- Plataforma de piedra ----------------
        var plat = Grupo("Plataforma", root, Vector3.zero);
        var pm = Obj("Base_Piedra", plat, CajaUV(new Vector3(PW, PH, PD), 2f, 1f), piedra, new Vector3(0, PH / 2f, 0));
        pm.AddComponent<BoxCollider>();
        Obj("Piso_Ladrillo", plat, CajaUV(new Vector3(PW + 0.1f, 0.05f, PD + 0.1f), 2f, 2f), pisoRojo, new Vector3(0, PH + 0.02f, 0));
        // ventilaciones cuadradas y puertita (foto 5)
        var vents = new List<Matrix4x4>();
        for (float x = -PW / 2f + 2f; x <= PW / 2f - 2f; x += 2.4f)
            foreach (int s in new[] { -1, 1 })
                vents.Add(Matrix4x4.TRS(new Vector3(x, PH * 0.55f, s * (PD / 2f + 0.01f)), Quaternion.identity, new Vector3(0.3f, 0.3f, 0.06f)));
        for (float z = -PD / 2f + 2.6f; z <= PD / 2f - 2.6f; z += 2.4f)
            foreach (int s in new[] { -1, 1 })
                if (Mathf.Abs(z) > 1.2f) vents.Add(Matrix4x4.TRS(new Vector3(s * (PW / 2f + 0.01f), PH * 0.55f, z), Quaternion.identity, new Vector3(0.06f, 0.3f, 0.3f)));
        vents.Add(Matrix4x4.TRS(new Vector3(-PW / 2f - 0.01f, 0.45f, 0f), Quaternion.identity, new Vector3(0.06f, 0.9f, 0.9f)));   // puertita
        Combinado("Ventilaciones", plat, vents, oscuro);

        // ---------------- Escaleras en las esquinas de los extremos ----------------
        var esc = Grupo("Escaleras", root, Vector3.zero);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                Escalera(esc, sx, sz, piedra, hierro);

        // ---------------- Baranda (con huecos en las escaleras) ----------------
        var bar = Grupo("Baranda", root, Vector3.zero);
        float yb = PH + 0.02f, ix = PW / 2f - 0.12f, iz = PD / 2f - 0.12f;
        Baranda(bar, new Vector3(-ix, yb, -iz), new Vector3(ix, yb, -iz), hierro);
        Baranda(bar, new Vector3(-ix, yb, iz), new Vector3(ix, yb, iz), hierro);
        foreach (int sx in new[] { -1, 1 })
            Baranda(bar, new Vector3(sx * ix, yb, -iz + 1.9f), new Vector3(sx * ix, yb, iz - 1.9f), hierro);

        // ---------------- Edificio ----------------
        var ed = Grupo("Edificio", root, new Vector3(0, PH, 0));
        var muros = Obj("Muros", ed, CajaUV(new Vector3(BW, BH, BD), 1f, 1f), muro, new Vector3(0, BH / 2f, 0));
        muros.tag = "Estructura";
        muros.AddComponent<BoxCollider>();
        Obj("Zocalo", ed, CajaUV(new Vector3(BW + 0.06f, 0.55f, BD + 0.06f), 1f, 1f), blanco, new Vector3(0, 0.275f, 0)).tag = "Fachada";
        Obj("Cornisa", ed, CajaUV(new Vector3(BW + 0.08f, 0.35f, BD + 0.08f), 1f, 1f), blanco, new Vector3(0, BH - 0.175f, 0)).tag = "Fachada";

        // puertas: plaza (-Z) de madera, via (+Z) de vidrio
        foreach (float x in new[] { -4.6f, 4.6f })
        {
            Puerta(ed, new Vector3(x, 0, -BD / 2f), Vector3.back, madera, blanco, oscuro);
            Puerta(ed, new Vector3(x, 0, BD / 2f), Vector3.forward, vidrio, blanco, oscuro);
        }
        // ventana de medio punto en los extremos
        foreach (int sx in new[] { -1, 1 })
            Lunetilla(ed, new Vector3(sx * BW / 2f, BH - 1.35f, 0), sx > 0 ? Vector3.right : Vector3.left, blanco, oscuro);

        // ---------------- Techo, alero y mensulas ----------------
        var techo = Obj("Cubierta", ed, Cubierta(BW, BD, ALERO, TECHO_ALTO), teja, new Vector3(0, BH, 0));
        techo.tag = "Techo";
        float aw = BW + 2 * ALERO, ad = BD + 2 * ALERO;
        foreach (int s in new[] { -1, 1 })
        {
            Prim(PrimitiveType.Cube, "Alero_L", ed, new Vector3(0, BH - 0.05f, s * ad / 2f), new Vector3(aw + 0.1f, 0.22f, 0.1f), fascia);
            Prim(PrimitiveType.Cube, "Alero_C", ed, new Vector3(s * aw / 2f, BH - 0.05f, 0), new Vector3(0.1f, 0.22f, ad + 0.1f), fascia);
        }
        foreach (float x in new[] { -8.2f, -4.1f, 0f, 4.1f, 8.2f })
            foreach (int s in new[] { -1, 1 })
                Mensula(ed, new Vector3(x, BH - 0.95f, s * BD / 2f), new Vector3(x, BH - 0.12f, s * (BD / 2f + ALERO * 0.85f)), hierro);
        foreach (float z in new[] { -2.4f, 2.4f })
            foreach (int s in new[] { -1, 1 })
                Mensula(ed, new Vector3(s * BW / 2f, BH - 0.95f, z), new Vector3(s * (BW / 2f + ALERO * 0.85f), BH - 0.12f, z), hierro);

        // ---------------- Letrero (solo 2026) ----------------
        var letrero = new GameObject("Letrero_Personeria");
        letrero.transform.SetParent(ed, false);
        letrero.transform.localPosition = new Vector3(0f, 2.9f, -BD / 2f - 0.06f);
        var tmp = letrero.AddComponent<TextMeshPro>();
        tmp.text = "PERSONERÍA\n<size=70%>de CAJICÁ</size>";
        tmp.fontSize = 5.5f; tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.10f, 0.22f, 0.70f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(5f, 1.6f);

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.gameObject != letrero) t.gameObject.isStatic = true;

        Registrar(letrero, root.gameObject);
        MoverPOI();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Personeria]</b> Personeria nueva construida. La vieja quedo desactivada.");
    }

    // ============================================================ piezas
    static void Escalera(Transform p, int sx, int sz, Material piedra, Material hierro)
    {
        const int pasos = 4; const float huella = 0.38f, ancho = 1.5f;
        float contra = PH / pasos;
        var g = Grupo("Escalera", p, new Vector3(sx * PW / 2f, 0, sz * (PD / 2f - 0.95f)));
        for (int i = 0; i < pasos; i++)
        {
            float alto = contra * (i + 1);
            float x = sx * (huella * (pasos - i) - huella / 2f);   // mas alto = mas cerca de la plataforma
            var paso = Obj("Paso_" + i, g, CajaUV(new Vector3(huella, alto, ancho), 2f, 1f), piedra, new Vector3(x, alto / 2f, 0));
            paso.AddComponent<BoxCollider>();
        }
        // pasamanos inclinado del lado de afuera
        float zp = sz * (ancho / 2f + 0.05f) * -1f;
        Vector3 a = new Vector3(sx * huella * pasos, 0.95f, zp), b = new Vector3(0, PH + 0.95f, zp);
        var pas = Prim(PrimitiveType.Cube, "Pasamanos", g, (a + b) / 2f, new Vector3(0.05f, 0.05f, Vector3.Distance(a, b)), hierro);
        pas.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
        Prim(PrimitiveType.Cube, "Poste", g, new Vector3(a.x, 0.47f, zp), new Vector3(0.05f, 0.95f, 0.05f), hierro);
    }

    static void Baranda(Transform p, Vector3 a, Vector3 b, Material hierro)
    {
        float largo = Vector3.Distance(a, b);
        var g = Grupo("Tramo", p, (a + b) / 2f);
        g.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
        foreach (float h in new[] { 0.35f, 0.68f, 1.0f })
            Prim(PrimitiveType.Cube, "Travesano", g, new Vector3(0, h, 0), new Vector3(0.04f, 0.04f, largo), hierro);
        int n = Mathf.Max(1, Mathf.RoundToInt(largo / 1.6f));
        for (int i = 0; i <= n; i++)
            Prim(PrimitiveType.Cube, "Poste", g, new Vector3(0, 0.5f, -largo / 2f + largo * i / n), new Vector3(0.05f, 1.0f, 0.05f), hierro);
        var col = g.gameObject.AddComponent<BoxCollider>();   // pared invisible: no deja caer al jugador
        col.center = new Vector3(0, 0.6f, 0); col.size = new Vector3(0.12f, 1.2f, largo);
    }

    // Puerta con marco blanco grueso y arco rebajado + ventana en abanico. 'normal' apunta hacia afuera.
    static void Puerta(Transform p, Vector3 pos, Vector3 normal, Material hoja, Material blanco, Material oscuro)
    {
        const float w = 1.45f, h = 2.65f, fan = 0.6f, flecha = 0.42f, marco = 0.3f;
        var g = Grupo("Puerta", p, pos);
        g.localRotation = Quaternion.LookRotation(normal, Vector3.up);
        float top = h + fan;
        Prim(PrimitiveType.Cube, "Marco", g, new Vector3(0, top / 2f, 0.04f), new Vector3(w + 2 * marco, top, 0.1f), blanco).tag = "Fachada";
        Arco(g, "Marco_Arco", new Vector3(0, top, 0.04f), w + 2 * marco, flecha + marco * 0.6f, 0.1f, blanco).tag = "Fachada";
        Prim(PrimitiveType.Cube, "Hoja", g, new Vector3(0, h / 2f, 0.1f), new Vector3(w, h, 0.06f), hoja);
        Prim(PrimitiveType.Cube, "Abanico", g, new Vector3(0, h + fan / 2f, 0.1f), new Vector3(w, fan, 0.05f), oscuro);
        Arco(g, "Abanico_Arco", new Vector3(0, top, 0.1f), w, flecha, 0.05f, oscuro);
        for (int i = 1; i <= 4; i++)   // barrotes del abanico
            Prim(PrimitiveType.Cube, "Barrote", g, new Vector3(-w / 2f + w * i / 5f, h + (fan + flecha) / 2f, 0.14f), new Vector3(0.035f, fan + flecha * 0.8f, 0.02f), blanco);
        Prim(PrimitiveType.Cube, "Travesano", g, new Vector3(0, h + 0.02f, 0.14f), new Vector3(w, 0.06f, 0.03f), blanco);
    }

    static void Lunetilla(Transform p, Vector3 pos, Vector3 normal, Material blanco, Material oscuro)
    {
        const float w = 1.7f, alto = 0.45f, flecha = 0.5f, marco = 0.22f;
        var g = Grupo("Ventana_Medio_Punto", p, pos);
        g.localRotation = Quaternion.LookRotation(normal, Vector3.up);
        Prim(PrimitiveType.Cube, "Marco", g, new Vector3(0, 0, 0.04f), new Vector3(w + 2 * marco, alto + 2 * marco, 0.1f), blanco).tag = "Fachada";
        Arco(g, "Marco_Arco", new Vector3(0, alto / 2f + marco, 0.04f), w + 2 * marco, flecha + marco * 0.5f, 0.1f, blanco).tag = "Fachada";
        Prim(PrimitiveType.Cube, "Vidrio", g, new Vector3(0, 0, 0.1f), new Vector3(w, alto, 0.05f), oscuro);
        Arco(g, "Vidrio_Arco", new Vector3(0, alto / 2f, 0.1f), w, flecha, 0.05f, oscuro);
        for (int i = 1; i <= 5; i++)
            Prim(PrimitiveType.Cube, "Barrote", g, new Vector3(-w / 2f + w * i / 6f, 0.15f, 0.14f), new Vector3(0.03f, alto + flecha * 0.9f, 0.02f), blanco);
    }

    // Media elipse vertical (arco) apoyada en 'centro' (base del arco).
    static GameObject Arco(Transform p, string name, Vector3 centro, float ancho, float flecha, float prof, Material m)
    {
        var go = Prim(PrimitiveType.Cylinder, name, p, centro, new Vector3(ancho, prof / 2f, flecha * 2f), m);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return go;
    }

    static void Mensula(Transform p, Vector3 a, Vector3 b, Material hierro)
    {
        var go = Prim(PrimitiveType.Cube, "Mensula", p, (a + b) / 2f, new Vector3(0.06f, 0.06f, Vector3.Distance(a, b)), hierro);
        go.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
    }

    // ============================================================ mallas
    // Caja con UVs en metros (la textura se repite segun el tamano real, sin estirarse).
    static Mesh CajaUV(Vector3 s, float tileU, float tileV)
    {
        string name = string.Format("Caja_{0:F2}x{1:F2}x{2:F2}_{3}_{4}", s.x, s.y, s.z, tileU, tileV);
        string path = GEN_DIR + "/" + name + ".asset";
        var ex = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (ex != null) return ex;
        Vector3 h = s / 2f;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        void Cara(Vector3 o, Vector3 du, Vector3 dv, float lu, float lv)
        {
            int k = v.Count;
            v.Add(o); v.Add(o + du); v.Add(o + du + dv); v.Add(o + dv);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(lu / tileU, 0)); uv.Add(new Vector2(lu / tileU, lv / tileV)); uv.Add(new Vector2(0, lv / tileV));
            t.Add(k); t.Add(k + 2); t.Add(k + 1); t.Add(k); t.Add(k + 3); t.Add(k + 2);
        }
        Cara(new Vector3(-h.x, -h.y, -h.z), new Vector3(s.x, 0, 0), new Vector3(0, s.y, 0), s.x, s.y);   // -Z
        Cara(new Vector3(h.x, -h.y, h.z), new Vector3(-s.x, 0, 0), new Vector3(0, s.y, 0), s.x, s.y);    // +Z
        Cara(new Vector3(h.x, -h.y, -h.z), new Vector3(0, 0, s.z), new Vector3(0, s.y, 0), s.z, s.y);    // +X
        Cara(new Vector3(-h.x, -h.y, h.z), new Vector3(0, 0, -s.z), new Vector3(0, s.y, 0), s.z, s.y);   // -X
        Cara(new Vector3(-h.x, h.y, -h.z), new Vector3(s.x, 0, 0), new Vector3(0, 0, s.z), s.x, s.z);    // +Y
        Cara(new Vector3(-h.x, -h.y, h.z), new Vector3(s.x, 0, 0), new Vector3(0, 0, -s.z), s.x, s.z);   // -Y
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        // asegurar caras hacia afuera
        var tri = m.triangles; var vs = m.vertices;
        for (int i = 0; i < tri.Length; i += 3)
        {
            Vector3 a = vs[tri[i]], b = vs[tri[i + 1]], c = vs[tri[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f) < 0f) { int tmp = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = tmp; }
        }
        m.triangles = tri;
        m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
        EnsureDir(GEN_DIR);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Cubierta a cuatro aguas de pendiente baja, con alero.
    static Mesh Cubierta(float w, float d, float alero, float alto)
    {
        string name = string.Format("Cubierta_{0:F1}x{1:F1}", w, d);
        string path = GEN_DIR + "/" + name + ".asset";
        var ex = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (ex != null) return ex;
        float hl = w / 2f + alero, hd = d / 2f + alero, rl = Mathf.Max(w / 2f - d / 2f, 0.4f);
        Vector3 p0 = new Vector3(-hl, 0, -hd), p1 = new Vector3(hl, 0, -hd), p2 = new Vector3(hl, 0, hd), p3 = new Vector3(-hl, 0, hd);
        Vector3 r0 = new Vector3(-rl, alto, 0), r1 = new Vector3(rl, alto, 0);
        var v = new List<Vector3>(); var t = new List<int>();
        void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0) { var x = b; b = c; c = x; }
            int k = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(k); t.Add(k + 1); t.Add(k + 2);
        }
        Tri(p0, p1, r1, new Vector3(0, 1, -1)); Tri(p0, r1, r0, new Vector3(0, 1, -1));
        Tri(p2, p3, r0, new Vector3(0, 1, 1)); Tri(p2, r0, r1, new Vector3(0, 1, 1));
        Tri(p1, p2, r1, new Vector3(1, 1, 0)); Tri(p3, p0, r0, new Vector3(-1, 1, 0));
        // cara inferior (se ve el alero desde abajo)
        Tri(p0, p2, p1, Vector3.down); Tri(p0, p3, p2, Vector3.down);
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        EnsureDir(GEN_DIR);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Textura de bloques irregulares de arenisca (como la plataforma real).
    static Texture2D GenerarPiedra()
    {
        string path = "Assets/Art/Generado/Piedra_Arenisca.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        const int W = 512, H = 256;   // = 2 m x 1 m
        var r = new System.Random(5);
        var px = new Color[W * H];
        Color junta = new Color(0.78f, 0.76f, 0.70f);
        int y0 = 0;
        while (y0 < H)
        {
            int alto = Mathf.Min(H - y0, 52 + r.Next(30));
            int x0 = -r.Next(60);
            while (x0 < W)
            {
                int ancho = 90 + r.Next(110);
                float t = (float)r.NextDouble();
                Color baseC = Color.Lerp(new Color(0.86f, 0.76f, 0.52f), new Color(0.74f, 0.70f, 0.62f), t);
                if (r.NextDouble() < 0.2) baseC = Color.Lerp(baseC, new Color(0.62f, 0.52f, 0.36f), 0.5f);
                for (int y = y0; y < y0 + alto; y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(W, x0 + ancho); x++)
                    {
                        bool borde = y - y0 < 3 || x - x0 < 3;
                        float n = ((float)r.NextDouble() - 0.5f) * 0.06f;
                        px[y * W + x] = borde ? junta : new Color(baseC.r + n, baseC.g + n, baseC.b + n);
                    }
                x0 += ancho;
            }
            y0 += alto;
        }
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.SetPixels(px); tex.Apply();
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Generado"));
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.maxTextureSize = 512;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ============================================================ epocas / POI
    static void Registrar(GameObject letrero, GameObject personeria)
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
            var act = e.FindPropertyRelative("activar"); var des = e.FindPropertyRelative("desactivar");
            for (int j = act.arraySize - 1; j >= 0; j--) if (act.GetArrayElementAtIndex(j).objectReferenceValue == null) act.DeleteArrayElementAtIndex(j);
            for (int j = des.arraySize - 1; j >= 0; j--) if (des.GetArrayElementAtIndex(j).objectReferenceValue == null) des.DeleteArrayElementAtIndex(j);
            var arr = es2026 ? act : des;
            bool ya = false;
            for (int j = 0; j < arr.arraySize; j++) if (arr.GetArrayElementAtIndex(j).objectReferenceValue == letrero) ya = true;
            if (!ya) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = letrero; }
        }
        so.ApplyModifiedProperties();
        var mi = comp.GetType().GetMethod("AplicarEpoca");
        if (mi != null) mi.Invoke(comp, new object[] { so.FindProperty("epocaActual").intValue });
    }

    // El POI de la Personeria queda sobre la plataforma, frente a la puerta de la plaza.
    static void MoverPOI()
    {
        foreach (var go in Raices())
            if (go.name == EscenaUtil.POIS)
                foreach (var poi in go.GetComponentsInChildren<POI>(true))
                    if (poi.name.ToLower().Contains("personeria"))
                    {
                        Undo.RecordObject(poi.transform, "Mover POI");
                        poi.transform.position = C + new Vector3(0f, PH + 0.03f, -PD / 2f + 1.3f);
                    }
    }

    // ============================================================ utilidades
    static GameObject[] Raices() { return EscenaUtil.Raices(); }

    static Material Mat(string name, Color c, float smooth, Texture2D tex = null, float metal = 0f)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Grupo(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    static GameObject Obj(string name, Transform parent, Mesh mesh, Material m, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    static Mesh cubo;
    static GameObject Combinado(string name, Transform parent, List<Matrix4x4> mats, Material m)
    {
        if (cubo == null) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); cubo = tmp.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(tmp); }
        var ci = new CombineInstance[mats.Count];
        for (int i = 0; i < mats.Count; i++) { ci[i].mesh = cubo; ci[i].transform = mats[i]; }
        var mesh = new Mesh { name = name };
        mesh.CombineMeshes(ci, true, true);
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return Obj(name, parent, mesh, m, Vector3.zero);
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

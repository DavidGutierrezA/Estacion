#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MonumentoPaz
{
    const string ROOT = "Monumento_Paz";
    const string EMPTY = "Ubicacion monumento paz";
    static readonly Vector3 POS_DEFECTO = new Vector3(-38f, 0f, 66.5f);
    const string MAT_DIR = "Assets/Art/Materiales/Esculturas";
    const string GEN_DIR = "Assets/Art/Generado/Esculturas";

    const float ANCHO_BASE = 1.2f, ALTO_BASE = 2.1f, FONDO_BASE = 0.75f;
    const float ANCHO_SUP = 0.72f, ALTO_SUP = 0.3f, FONDO_SUP = 0.5f;
    const float ESCALA_ESCULTURA = 1.15f;
    static readonly Vector3 ROCA = new Vector3(0.38f, 0.26f, 0.28f);

    class Malla
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<int> t = new List<int>();

        public void Tri(int a, int b, int c, Vector3 fuera)
        {
            var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.sqrMagnitude < 1e-14f) return;
            if (Vector3.Dot(n, fuera) < 0f) { t.Add(a); t.Add(c); t.Add(b); }
            else { t.Add(a); t.Add(b); t.Add(c); }
        }

        public void Agregar(Malla otra, Matrix4x4 mat)
        {
            int b = v.Count;
            foreach (var p in otra.v) v.Add(mat.MultiplyPoint3x4(p));
            bool espejo = mat.determinant < 0f;
            for (int i = 0; i < otra.t.Count; i += 3)
            {
                t.Add(b + otra.t[i]);
                t.Add(b + otra.t[espejo ? i + 2 : i + 1]);
                t.Add(b + otra.t[espejo ? i + 1 : i + 2]);
            }
        }

        public void Transformar(Matrix4x4 mat)
        {
            for (int i = 0; i < v.Count; i++) v[i] = mat.MultiplyPoint3x4(v[i]);
            if (mat.determinant < 0f) for (int i = 0; i < t.Count; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; }
        }

        public Mesh Construir(string nombre)
        {
            var m = new Mesh { name = nombre };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    static Malla ESF, CUBO, PLUMA;

    [MenuItem("Estacion/Esculturas/1 - Monumento a la paz")]
    public static void Construir()
    {
        ESF = Esfera(8, 6);
        CUBO = DesdeMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        PLUMA = PlumaPlantilla();

        GameObject empty = EscenaUtil.Raiz(EMPTY);
        if (empty == null)
        {
            empty = new GameObject(EMPTY);
            empty.transform.position = POS_DEFECTO;
            empty.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Undo.RegisterCreatedObjectUndo(empty, "Empty monumento");
        }
        foreach (var go in EscenaUtil.Raices()) if (go.name == ROOT) Undo.DestroyObjectImmediate(go);
        var root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, "Monumento paz");
        root.transform.position = new Vector3(empty.transform.position.x, 0f, empty.transform.position.z);
        root.transform.rotation = Quaternion.Euler(0f, empty.transform.eulerAngles.y, 0f);

        var mBronce = Mat("Bronce", new Color(0.36f, 0.30f, 0.17f), 0.85f, 0.6f);
        var mPatina = Mat("Bronce_Patina", new Color(0.17f, 0.16f, 0.11f), 0.6f, 0.4f);
        var mSombra = Mat("Bronce_Sombra", new Color(0.04f, 0.04f, 0.035f), 0.2f, 0.2f);
        var texLosas = TexturaLosas();
        var mGranito = Mat("Granito_Negro", Color.white, 0.05f, 0.3f);
        mGranito.SetTexture("_BaseMap", texLosas);
        mGranito.SetTextureScale("_BaseMap", Vector2.one);
        var mGranitoSup = Mat("Granito_Negro_Superior", Color.white, 0.05f, 0.3f);
        mGranitoSup.SetTexture("_BaseMap", texLosas);
        mGranitoSup.SetTextureScale("_BaseMap", new Vector2(1f, 2f / 7f));
        var mPlaca = Mat("Placa_Dorada", new Color(0.86f, 0.77f, 0.58f), 0.2f, 0.45f);

        var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        float y0 = ALTO_BASE + ALTO_SUP;
        Pieza(root.transform, "Pedestal", Caja(cubo, new Vector3(0, ALTO_BASE / 2f, 0), new Vector3(ANCHO_BASE, ALTO_BASE, FONDO_BASE)), mGranito, true);
        Pieza(root.transform, "Pedestal_Superior", Caja(cubo, new Vector3(0, ALTO_BASE + ALTO_SUP / 2f, 0), new Vector3(ANCHO_SUP, ALTO_SUP, FONDO_SUP)), mGranitoSup, true);
        Pieza(root.transform, "Placa", Caja(cubo, new Vector3(0, 1.42f, FONDO_BASE / 2f + 0.006f), new Vector3(0.44f, 0.66f, 0.012f)), mPlaca, false);

        var bronce = new Malla();
        var patina = new Malla();
        var sombra = new Malla();

        Roca(patina, ROCA);
        Armas(patina);
        Careta(bronce, sombra, SobreRoca(0.02f, 0.09f, 0f, 0.012f), 0.15f, false);
        Paloma(patina, new Vector3(-0.24f, 0.17f, 0.07f));

        Tubo(bronce, new Vector3(-0.14f, 0.15f, 0.03f), new Vector3(-0.17f, 0.3f, 0.03f), 0.045f, 0.04f, 8);
        bronce.Agregar(ESF, Matrix4x4.TRS(new Vector3(-0.17f, 0.3f, 0.03f), Quaternion.identity, Vector3.one * 0.08f));
        Tubo(bronce, new Vector3(-0.17f, 0.3f, 0.03f), new Vector3(-0.07f, 0.47f, 0f), 0.04f, 0.034f, 8);
        for (int i = -1; i <= 1; i++)
            Tubo(bronce, new Vector3(-0.14f, 0.16f, 0.03f), new Vector3(-0.14f + 0.05f * i, 0.13f, 0.1f), 0.012f, 0.004f, 5);

        Tubo(bronce, new Vector3(0.21f, 0.14f, 0f), new Vector3(0.18f, 0.37f, 0f), 0.05f, 0.04f, 8);
        bronce.Agregar(ESF, Matrix4x4.TRS(new Vector3(0.16f, 0.41f, 0f), Quaternion.Euler(0, 0, -25f), new Vector3(0.11f, 0.09f, 0.065f)));
        for (int i = 0; i < 4; i++)
        {
            float x = 0.115f + 0.026f * i;
            var p0 = new Vector3(x, 0.44f, 0.025f);
            var p1 = new Vector3(x + 0.005f, 0.49f + 0.005f * i, 0.045f);
            var p2 = new Vector3(x + 0.008f, 0.52f + 0.006f * i, 0.025f);
            Tubo(bronce, p0, p1, 0.013f, 0.012f, 6);
            Tubo(bronce, p1, p2, 0.012f, 0.008f, 6);
        }
        Tubo(bronce, new Vector3(0.13f, 0.41f, 0.035f), new Vector3(0.09f, 0.45f, 0.055f), 0.014f, 0.012f, 6);
        Tubo(bronce, new Vector3(0.09f, 0.45f, 0.055f), new Vector3(0.075f, 0.49f, 0.045f), 0.012f, 0.008f, 6);

        Ala(bronce, new Vector3(-0.05f, 0.5f, 0f), -1f, 0.7f, 0.31f, 24f, 10f);
        Ala(bronce, new Vector3(0.07f, 0.48f, 0f), 1f, 0.62f, 0.3f, 36f, -10f);
        Careta(bronce, sombra, Matrix4x4.TRS(new Vector3(0.01f, 0.56f, 0.05f), Quaternion.identity, Vector3.one), 0.16f, true);

        var esc = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.identity, new Vector3(-ESCALA_ESCULTURA, ESCALA_ESCULTURA, ESCALA_ESCULTURA));
        bronce.Transformar(esc); patina.Transformar(esc); sombra.Transformar(esc);
        Pieza(root.transform, "Escultura_Bronce", bronce.Construir("Monumento_Escultura_Bronce"), mBronce, true);
        Pieza(root.transform, "Escultura_Patina", patina.Construir("Monumento_Escultura_Patina"), mPatina, true);
        Pieza(root.transform, "Escultura_Detalles", sombra.Construir("Monumento_Escultura_Detalles"), mSombra, false);

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, ALTO_BASE / 2f, 0);
        col.size = new Vector3(ANCHO_BASE, ALTO_BASE, FONDO_BASE);

        var t = new GameObject("Texto_Placa");
        t.transform.SetParent(root.transform, false);
        t.transform.localPosition = new Vector3(0, 1.42f, FONDO_BASE / 2f + 0.013f);
        t.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        var tmp = t.AddComponent<TextMeshPro>();
        tmp.text = "<b>MONUMENTO A LA PAZ\nY LA RECONCILIACIÓN</b>\n\n<size=75%>Las alas: esperanza y libertad.\nLas armas depuestas: diálogo\ny tolerancia. Las caretas: el arte\nque transforma el dolor en unión.</size>\n\n<size=80%><b>Alcaldía de Cajicá · 2025</b></size>";
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.1f;
        tmp.fontSizeMax = 0.32f;
        tmp.color = new Color(0.16f, 0.13f, 0.08f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(0.4f, 0.6f);

        foreach (var tr in root.GetComponentsInChildren<Transform>(true)) tr.gameObject.isStatic = true;
        Registrar(root);
        EscenaUtil.Ordenar(false);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        int tris = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) tris += mf.sharedMesh.triangles.Length / 3;
        Debug.Log("<b>[Esculturas]</b> Monumento a la paz: " + tris + " triangulos.");
    }

    static Matrix4x4 SobreRoca(float x, float y, float giro, float alto)
    {
        float q = Mathf.Max(0.04f, 1f - (x / ROCA.x) * (x / ROCA.x) - (y / ROCA.y) * (y / ROCA.y));
        var p = new Vector3(x, y, ROCA.z * Mathf.Sqrt(q));
        var n = new Vector3(p.x / (ROCA.x * ROCA.x), p.y / (ROCA.y * ROCA.y), p.z / (ROCA.z * ROCA.z)).normalized;
        return Matrix4x4.TRS(p + n * alto, Quaternion.LookRotation(n, Vector3.up) * Quaternion.Euler(0, 0, giro), Vector3.one);
    }

    static void Armas(Malla m)
    {
        var hacha = SobreRoca(-0.24f, 0.06f, 35f, 0.015f);
        m.Agregar(CUBO, hacha * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.28f, 0.022f, 0.022f)));
        m.Agregar(CUBO, hacha * Matrix4x4.TRS(new Vector3(0.12f, 0.035f, 0f), Quaternion.identity, new Vector3(0.05f, 0.1f, 0.014f)));

        var machete = SobreRoca(-0.07f, 0.12f, -20f, 0.01f);
        m.Agregar(CUBO, machete * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.34f, 0.055f, 0.008f)));
        m.Agregar(CUBO, machete * Matrix4x4.TRS(new Vector3(-0.21f, 0f, 0.006f), Quaternion.identity, new Vector3(0.09f, 0.03f, 0.025f)));

        var cuchillo = SobreRoca(0.16f, 0.12f, 15f, 0.01f);
        m.Agregar(CUBO, cuchillo * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.17f, 0.03f, 0.006f)));
        m.Agregar(CUBO, cuchillo * Matrix4x4.TRS(new Vector3(-0.115f, 0f, 0.006f), Quaternion.identity, new Vector3(0.07f, 0.022f, 0.02f)));

        var manopla = SobreRoca(0.28f, 0.06f, 80f, 0.015f);
        m.Agregar(CUBO, manopla * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.1f, 0.025f, 0.02f)));
        var anillos = new Malla();
        for (int i = 0; i < 4; i++)
        {
            float x = -0.036f + 0.024f * i;
            Tubo(anillos, new Vector3(x, 0.028f, -0.01f), new Vector3(x, 0.028f, 0.01f), 0.014f, 0.014f, 6);
        }
        m.Agregar(anillos, manopla);

        m.Agregar(CUBO, Matrix4x4.TRS(new Vector3(0.08f, ROCA.y - 0.012f, -0.02f), Quaternion.Euler(0, 10f, -6f), new Vector3(0.32f, 0.018f, 0.13f)));
    }

    static void Paloma(Malla m, Vector3 c)
    {
        var M = Matrix4x4.TRS(c, Quaternion.identity, Vector3.one);
        m.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(0, 0.06f, 0), Quaternion.Euler(0, 0, -10f), new Vector3(0.24f, 0.12f, 0.13f)));
        m.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(-0.13f, 0.12f, 0), Quaternion.identity, Vector3.one * 0.075f));
        m.Agregar(CUBO, M * Matrix4x4.TRS(new Vector3(-0.18f, 0.115f, 0), Quaternion.Euler(0, 0, -10f), new Vector3(0.035f, 0.016f, 0.016f)));
        m.Agregar(CUBO, M * Matrix4x4.TRS(new Vector3(0.15f, 0.08f, 0), Quaternion.Euler(0, 0, 15f), new Vector3(0.13f, 0.02f, 0.085f)));
        for (int s = -1; s <= 1; s += 2)
            m.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(0.03f, 0.085f, s * 0.055f), Quaternion.Euler(0, 0, -8f), new Vector3(0.2f, 0.08f, 0.03f)));
    }

    static void Careta(Malla bronce, Malla oscuro, Matrix4x4 donde, float s, bool alegre)
    {
        var M = donde * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * s);
        bronce.Agregar(ESF, M * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.72f, 1f, 0.32f)));
        bronce.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(0f, -0.03f, 0.15f), Quaternion.identity, new Vector3(0.09f, 0.16f, 0.09f)));
        for (int l = -1; l <= 1; l += 2)
        {
            bronce.Agregar(CUBO, M * Matrix4x4.TRS(new Vector3(l * 0.15f, 0.21f, 0.12f), Quaternion.Euler(0, 0, l * (alegre ? 12f : -18f)), new Vector3(0.2f, 0.04f, 0.06f)));
            oscuro.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(l * 0.15f, 0.09f, 0.135f), Quaternion.Euler(0, 0, l * (alegre ? -15f : 20f)), new Vector3(0.17f, alegre ? 0.07f : 0.09f, 0.05f)));
            if (alegre) bronce.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(l * 0.19f, -0.11f, 0.1f), Quaternion.identity, new Vector3(0.2f, 0.15f, 0.12f)));
        }
        for (int k = -2; k <= 2; k++)
        {
            float y = alegre ? -0.29f + 0.03f * k * k : -0.21f - 0.03f * k * k;
            oscuro.Agregar(ESF, M * Matrix4x4.TRS(new Vector3(k * 0.06f, y, 0.13f), Quaternion.identity, new Vector3(0.09f, 0.07f, 0.04f)));
        }
    }

    static void Ala(Malla destino, Vector3 raiz, float lado, float largo, float ancho, float inclin, float giro)
    {
        var ala = new Malla();
        const int n = 12, cols = 3;
        const float e = 0.016f, u0 = -0.03f;
        System.Func<float, float> W = s => ancho * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(s), 0.8f))), 0.65f) + 0.07f * (1f - s);
        System.Func<float, float> Copa = u => 0.03f * (u / ancho) * (u / ancho);

        int frente = ala.v.Count;
        for (int k = 0; k <= n; k++)
            for (int c = 0; c <= cols; c++)
            {
                float s = k / (float)n, u = u0 + W(s) * c / cols;
                ala.v.Add(new Vector3(lado * u, s * largo, e - Copa(u)));
            }
        int atras = ala.v.Count;
        for (int k = 0; k <= n; k++)
            for (int c = 0; c <= cols; c++)
            {
                float s = k / (float)n, u = u0 + W(s) * c / cols;
                ala.v.Add(new Vector3(lado * u, s * largo, -e - Copa(u)));
            }
        int fila = cols + 1;
        for (int k = 0; k < n; k++)
        {
            for (int c = 0; c < cols; c++)
            {
                int a = k * fila + c, b = a + 1, d = a + fila, f = d + 1;
                ala.Tri(frente + a, frente + b, frente + f, Vector3.forward);
                ala.Tri(frente + a, frente + f, frente + d, Vector3.forward);
                ala.Tri(atras + a, atras + b, atras + f, Vector3.back);
                ala.Tri(atras + a, atras + f, atras + d, Vector3.back);
            }
            int i0 = k * fila, i1 = i0 + fila, o0 = i0 + cols, o1 = i1 + cols;
            ala.Tri(frente + i0, frente + i1, atras + i1, Vector3.left * lado);
            ala.Tri(frente + i0, atras + i1, atras + i0, Vector3.left * lado);
            ala.Tri(frente + o0, frente + o1, atras + o1, Vector3.right * lado);
            ala.Tri(frente + o0, atras + o1, atras + o0, Vector3.right * lado);
        }
        for (int c = 0; c < cols; c++)
        {
            ala.Tri(frente + c, frente + c + 1, atras + c + 1, Vector3.down);
            ala.Tri(frente + c, atras + c + 1, atras + c, Vector3.down);
        }

        const int N = 13;
        float paso = 0.86f / (N - 1);
        for (int cara = -1; cara <= 1; cara += 2)
        {
            for (int i = 0; i < N; i++)
            {
                float s = 0.04f + i * paso;
                float a = (32f - 14f * s) * Mathf.Deg2Rad;
                float an = paso * largo * 2.1f;
                var pos = new Vector3(lado * (u0 + 0.01f), s * largo + 0.01f, cara * (e + 0.002f + i * 0.0012f) - 0.008f);
                PonerPluma(ala, pos, lado, a, cara, (W(s) + Mathf.Lerp(0.05f, 0.01f, s)) / Mathf.Cos(a), an, 0.035f);
                if (i < N - 2)
                {
                    float s2 = s + paso * 0.5f;
                    var pos2 = new Vector3(lado * (u0 + 0.01f), s2 * largo + 0.01f, cara * (e + 0.012f + i * 0.0012f) - 0.008f);
                    PonerPluma(ala, pos2, lado, a + 10f * Mathf.Deg2Rad, cara, W(s2) * 0.55f + 0.03f, an * 0.85f, 0.03f);
                }
            }
            for (int j = 0; j < 3; j++)
            {
                float s = 0.6f + j * 0.05f;
                var pos = new Vector3(lado * (u0 + W(s) * (0.25f + 0.2f * j)), s * largo, cara * (e + 0.004f) - 0.008f);
                PonerPluma(ala, pos, lado, (-82f + 7f * j) * Mathf.Deg2Rad, cara, largo * (0.28f - 0.04f * j), 0.07f, 0.025f);
            }
        }
        destino.Agregar(ala, Matrix4x4.TRS(raiz, Quaternion.Euler(0f, giro, -inclin * lado), Vector3.one));
    }

    static void PonerPluma(Malla destino, Vector3 pos, float lado, float a, int cara, float largo, float ancho, float alto)
    {
        float ang = Mathf.Atan2(-Mathf.Sin(a), lado * Mathf.Cos(a)) * Mathf.Rad2Deg;
        var rot = Quaternion.Euler(0, 0, ang) * (cara < 0 ? Quaternion.Euler(180f, 0, 0) : Quaternion.identity);
        destino.Agregar(PLUMA, Matrix4x4.TRS(pos, rot, new Vector3(largo, ancho, alto)));
    }

    static Malla PlumaPlantilla()
    {
        const int n = 4;
        var m = new Malla();
        for (int k = 0; k <= n; k++)
        {
            float x = k / (float)n;
            float hw = k == n ? 0f : 0.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * (0.12f + 0.88f * Mathf.Pow(x, 0.8f)))), 0.7f);
            m.v.Add(new Vector3(x, -hw, 0f));
            m.v.Add(new Vector3(x, 0f, hw));
            m.v.Add(new Vector3(x, hw, 0f));
        }
        for (int k = 0; k < n; k++)
        {
            int a = 3 * k, b = 3 * (k + 1);
            m.Tri(a, b, b + 1, Vector3.forward);
            m.Tri(a, b + 1, a + 1, Vector3.forward);
            m.Tri(a + 1, b + 1, b + 2, Vector3.forward);
            m.Tri(a + 1, b + 2, a + 2, Vector3.forward);
        }
        int bb = m.v.Count;
        for (int k = 0; k <= n; k++) { m.v.Add(m.v[3 * k]); m.v.Add(m.v[3 * k + 2]); }
        for (int k = 0; k < n; k++)
        {
            int a = bb + 2 * k, b = bb + 2 * (k + 1);
            m.Tri(a, b + 1, b, Vector3.back);
            m.Tri(a, a + 1, b + 1, Vector3.back);
        }
        return m;
    }

    static void Roca(Malla m, Vector3 r)
    {
        const int lon = 16, lat = 5;
        var rnd = new System.Random(7);
        int b = m.v.Count;
        for (int j = 0; j <= lat; j++)
        {
            float phi = Mathf.PI * 0.5f * j / lat;
            for (int i = 0; i < lon; i++)
            {
                float th = 2f * Mathf.PI * i / lon;
                float k = (j == 0 || j == lat) ? 1f : 0.9f + 0.2f * (float)rnd.NextDouble();
                m.v.Add(new Vector3(Mathf.Cos(th) * Mathf.Cos(phi) * r.x * k, Mathf.Sin(phi) * r.y * (j == 0 ? 0f : k), Mathf.Sin(th) * Mathf.Cos(phi) * r.z * k));
            }
        }
        for (int j = 0; j < lat; j++)
            for (int i = 0; i < lon; i++)
            {
                int a = b + j * lon + i, c = b + j * lon + (i + 1) % lon, d = a + lon, f = c + lon;
                var cen = (m.v[a] + m.v[f]) / 2f;
                m.Tri(a, c, f, cen);
                m.Tri(a, f, d, cen);
            }
    }

    static void Tubo(Malla m, Vector3 a, Vector3 b, float ra, float rb, int lados)
    {
        var eje = (b - a).normalized;
        var u = Vector3.Cross(eje, Mathf.Abs(eje.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        var w = Vector3.Cross(eje, u);
        int ba = m.v.Count;
        for (int i = 0; i < lados; i++)
        {
            float th = 2f * Mathf.PI * i / lados;
            var d = Mathf.Cos(th) * u + Mathf.Sin(th) * w;
            m.v.Add(a + d * ra);
            m.v.Add(b + d * rb);
        }
        m.v.Add(a);
        m.v.Add(b);
        int ca = m.v.Count - 2, cb = m.v.Count - 1;
        for (int i = 0; i < lados; i++)
        {
            int i0 = ba + 2 * i, i1 = ba + 2 * ((i + 1) % lados);
            var fuera = (m.v[i0] + m.v[i1 + 1]) / 2f - (a + b) / 2f;
            fuera -= Vector3.Dot(fuera, eje) * eje;
            m.Tri(i0, i1, i1 + 1, fuera);
            m.Tri(i0, i1 + 1, i0 + 1, fuera);
            m.Tri(ca, i1, i0, -eje);
            m.Tri(cb, i0 + 1, i1 + 1, eje);
        }
    }

    static Malla Esfera(int lon, int lat)
    {
        var m = new Malla();
        m.v.Add(new Vector3(0, -0.5f, 0));
        for (int j = 1; j < lat; j++)
        {
            float phi = Mathf.PI * j / lat - Mathf.PI / 2f;
            for (int i = 0; i < lon; i++)
            {
                float th = 2f * Mathf.PI * i / lon;
                m.v.Add(0.5f * new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th)));
            }
        }
        m.v.Add(new Vector3(0, 0.5f, 0));
        int top = m.v.Count - 1;
        for (int i = 0; i < lon; i++)
        {
            int i1 = (i + 1) % lon;
            Tri3(m, 0, 1 + i, 1 + i1);
            for (int j = 0; j < lat - 2; j++)
            {
                int a0 = 1 + j * lon + i, b0 = 1 + j * lon + i1;
                Tri3(m, a0, b0, b0 + lon);
                Tri3(m, a0, b0 + lon, a0 + lon);
            }
            Tri3(m, 1 + (lat - 2) * lon + i, 1 + (lat - 2) * lon + i1, top);
        }
        return m;
    }

    static void Tri3(Malla m, int a, int b, int c) { m.Tri(a, b, c, (m.v[a] + m.v[b] + m.v[c]) / 3f); }

    static Malla DesdeMesh(Mesh mesh)
    {
        var m = new Malla();
        m.v.AddRange(mesh.vertices);
        m.t.AddRange(mesh.triangles);
        return m;
    }

    static Mesh Caja(Mesh cubo, Vector3 centro, Vector3 tam)
    {
        var mesh = new Mesh();
        mesh.CombineMeshes(new[] { new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(centro, Quaternion.identity, tam) } }, true, true);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void Pieza(Transform root, string nombre, Mesh mesh, Material mat, bool sombra)
    {
        mesh.name = "Monumento_" + nombre;
        var guardada = Guardar(mesh, GEN_DIR + "/Monumento_" + nombre + ".asset");
        var go = new GameObject(nombre);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = guardada;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
        r.receiveGI = ReceiveGI.LightProbes;
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
        if (mesh.tangents.Length > 0) existente.SetTangents(mesh.tangents);
        existente.SetTriangles(mesh.triangles, 0);
        existente.RecalculateBounds();
        existente.name = mesh.name;
        EditorUtility.SetDirty(existente);
        Object.DestroyImmediate(mesh);
        return existente;
    }

    static Material Mat(string name, Color c, float metal, float smooth)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

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
            bool es2026 = e.FindPropertyRelative("nombre").stringValue.Contains("2026");
            var arr = e.FindPropertyRelative(es2026 ? "activar" : "desactivar");
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

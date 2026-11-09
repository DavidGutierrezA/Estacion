#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class DetallesPlaza
{
    const string MAT_DIR = "Assets/Art/Materiales/Plaza";
    const string GEN_DIR = "Assets/Art/Generado/Plaza";
    const string VITRINA = "Vitrina_Virgen";
    const string BORDES = "EPOCA_2026_Bordes_Fuentes";

    const float ALTO_CAJA_BASE = 1.55f;
    const float ALTO_CAJA_TOPE = 3.6f;
    const float ALTO_ESTATUA = 1.6f;
    const float PISO_ESTATUA = 1.82f;

    static Mesh cubo, esfera;

    [MenuItem("Estacion/Plaza/6 - Vitrina de la Virgen (2026)")]
    public static void Vitrina()
    {
        Cargar();
        var virgen = EscenaUtil.Raiz("Virgen");
        if (virgen == null) { Debug.LogError("[Plaza] No encuentro 'Virgen'"); return; }
        var estatuaRaiz = virgen.transform.Find("Virgen_Estatua");
        if (estatuaRaiz == null) { Debug.LogError("[Plaza] No encuentro 'Virgen/Virgen_Estatua'"); return; }

        var vieja = virgen.transform.Find(VITRINA);
        if (vieja != null) Undo.DestroyObjectImmediate(vieja.gameObject);

        var basePos = estatuaRaiz.position;
        RaycastHit h;
        float suelo = 0f;
        if (Physics.Raycast(new Vector3(basePos.x, 30f, basePos.z), Vector3.down, out h, 60f, ~0, QueryTriggerInteraction.Ignore) && h.collider.name.StartsWith("Suelo")) suelo = h.point.y;

        var root = new GameObject(VITRINA);
        Undo.RegisterCreatedObjectUndo(root, "Vitrina Virgen");
        root.transform.SetParent(virgen.transform, false);
        root.transform.position = new Vector3(basePos.x, suelo, basePos.z);
        root.transform.rotation = Quaternion.Euler(0f, estatuaRaiz.eulerAngles.y, 0f);

        var azul = Mat("Vitrina_Azul", new Color(0.1f, 0.24f, 0.78f), 0.35f, 0f);
        var marco = Mat("Vitrina_Marco", new Color(0.62f, 0.5f, 0.28f), 0.6f, 0.7f);
        var techo = Mat("Vitrina_Techo", new Color(0.07f, 0.09f, 0.16f), 0.3f, 0f);
        var vidrio = Vidrio("Vitrina_Vidrio");
        var reja = Mat("Reja_Cafe", new Color(0.27f, 0.14f, 0.08f), 0.3f, 0.2f);
        var tierra = Mat("Jardinera_Tierra", new Color(0.17f, 0.11f, 0.07f), 0.05f, 0f);
        var hoja1 = Mat("Arbusto_Amarillo", new Color(0.62f, 0.66f, 0.2f), 0.15f, 0f);
        var hoja2 = Mat("Arbusto_Verde", new Color(0.3f, 0.45f, 0.14f), 0.15f, 0f);
        var nube = Mat("Virgen_Nube", new Color(0.62f, 0.78f, 0.95f), 0.3f, 0f);
        var luz = Mat("Vitrina_Luz", Color.white, 0.5f, 0f);
        luz.EnableKeyword("_EMISSION");
        luz.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 2f);
        luz.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        float x0 = -1.2f, x1 = 1.2f, z0 = -0.75f, z1 = 1.1f, alto = 0.2f, grosor = 0.15f;
        var bordillo = new List<CombineInstance>();
        Caja(bordillo, new Vector3(0f, alto / 2f, z1 - grosor / 2f), new Vector3(x1 - x0, alto, grosor));
        Caja(bordillo, new Vector3(0f, alto / 2f, z0 + grosor / 2f), new Vector3(x1 - x0, alto, grosor));
        Caja(bordillo, new Vector3(x0 + grosor / 2f, alto / 2f, (z0 + z1) / 2f), new Vector3(grosor, alto, z1 - z0 - 2f * grosor));
        Caja(bordillo, new Vector3(x1 - grosor / 2f, alto / 2f, (z0 + z1) / 2f), new Vector3(grosor, alto, z1 - z0 - 2f * grosor));
        Pieza(root.transform, "Bordillo", bordillo, azul, true);

        var suelito = new List<CombineInstance>();
        Caja(suelito, new Vector3(0f, 0.07f, (z0 + z1) / 2f), new Vector3(x1 - x0 - 2f * grosor, 0.14f, z1 - z0 - 2f * grosor));
        Pieza(root.transform, "Tierra", suelito, tierra, false);

        var pilar = new List<CombineInstance>();
        Caja(pilar, new Vector3(0f, (ALTO_CAJA_TOPE + 0.05f) / 2f, -0.42f), new Vector3(0.9f, ALTO_CAJA_TOPE + 0.05f, 0.34f));
        Caja(pilar, new Vector3(0f, ALTO_CAJA_BASE / 2f, 0.0f), new Vector3(0.6f, ALTO_CAJA_BASE, 0.5f));
        Caja(pilar, new Vector3(0f, ALTO_CAJA_BASE - 0.04f, 0.05f), new Vector3(0.92f, 0.08f, 0.62f));
        Pieza(root.transform, "Pilar", pilar, azul, true);

        float cx = 0.45f, cz0 = -0.25f, cz1 = 0.36f, b = 0.035f;
        float yb = ALTO_CAJA_BASE, yt = ALTO_CAJA_TOPE, ym = ALTO_CAJA_TOPE - 0.55f;
        var mar = new List<CombineInstance>();
        foreach (float sx in new[] { -1f, 1f })
        {
            Caja(mar, new Vector3(sx * cx, (yb + yt) / 2f, cz1), new Vector3(b, yt - yb, b));
            Caja(mar, new Vector3(sx * cx, (yb + yt) / 2f, cz0), new Vector3(b, yt - yb, b));
            foreach (float y in new[] { yb, ym, yt })
                Caja(mar, new Vector3(sx * cx, y, (cz0 + cz1) / 2f), new Vector3(b, b, cz1 - cz0));
        }
        foreach (float y in new[] { yb, ym, yt })
            Caja(mar, new Vector3(0f, y, cz1), new Vector3(2f * cx, b, b));
        Pieza(root.transform, "Marco", mar, marco, false);

        var vid = new List<CombineInstance>();
        Caja(vid, new Vector3(0f, (yb + yt) / 2f, cz1), new Vector3(2f * cx - b, yt - yb - b, 0.008f));
        Caja(vid, new Vector3(-cx, (yb + yt) / 2f, (cz0 + cz1) / 2f), new Vector3(0.008f, yt - yb - b, cz1 - cz0 - b));
        Caja(vid, new Vector3(cx, (yb + yt) / 2f, (cz0 + cz1) / 2f), new Vector3(0.008f, yt - yb - b, cz1 - cz0 - b));
        Pieza(root.transform, "Vidrios", vid, vidrio, false);

        var tec = new List<CombineInstance>();
        Caja(tec, new Vector3(0f, yt + 0.06f, -0.12f), new Vector3(1.06f, 0.08f, 0.98f));
        Caja(tec, new Vector3(0f, yt + 0.115f, -0.12f), new Vector3(0.96f, 0.03f, 0.88f));
        Pieza(root.transform, "Techo", tec, techo, true);

        var foco = new List<CombineInstance>();
        Esf(foco, new Vector3(0f, yt - 0.12f, 0.08f), Vector3.one * 0.08f);
        Pieza(root.transform, "Foco", foco, luz, false);

        var nub = new List<CombineInstance>();
        Esf(nub, new Vector3(0f, yb + 0.12f, 0.05f), new Vector3(0.78f, 0.26f, 0.5f));
        Esf(nub, new Vector3(-0.2f, yb + 0.2f, 0.12f), new Vector3(0.36f, 0.24f, 0.3f));
        Esf(nub, new Vector3(0.22f, yb + 0.18f, 0.1f), new Vector3(0.34f, 0.22f, 0.3f));
        Pieza(root.transform, "Nube", nub, nube, false);

        var arb1 = new List<CombineInstance>();
        var arb2 = new List<CombineInstance>();
        var rnd = new System.Random(4);
        for (float x = x0 + 0.32f; x <= x1 - 0.3f; x += 0.34f)
            for (float z = z0 + 0.32f; z <= z1 - 0.3f; z += 0.34f)
            {
                if (Mathf.Abs(x) < 0.5f && z < 0.3f) continue;
                float hgt = 0.45f + (float)rnd.NextDouble() * 0.3f;
                var lista = rnd.NextDouble() < 0.6 ? arb1 : arb2;
                Esf(lista, new Vector3(x + (float)(rnd.NextDouble() - 0.5) * 0.1f, 0.14f + hgt * 0.45f, z + (float)(rnd.NextDouble() - 0.5) * 0.1f),
                    new Vector3(0.42f, hgt, 0.42f));
            }
        Pieza(root.transform, "Arbustos_Amarillos", arb1, hoja1, true);
        Pieza(root.transform, "Arbustos_Verdes", arb2, hoja2, true);

        var rj = new List<CombineInstance>();
        float rx = 1.08f, rz0 = z0 + 0.12f, rz1 = z1 - 0.12f;
        Reja(rj, new Vector3(-rx, 0f, rz1), new Vector3(rx, 0f, rz1));
        Reja(rj, new Vector3(rx, 0f, rz0), new Vector3(-rx, 0f, rz0));
        Reja(rj, new Vector3(-rx, 0f, rz0), new Vector3(-rx, 0f, rz1));
        Reja(rj, new Vector3(rx, 0f, rz1), new Vector3(rx, 0f, rz0));
        Pieza(root.transform, "Reja", rj, reja, true);

        var c1 = root.AddComponent<BoxCollider>();
        c1.center = new Vector3(0f, 0.5f, (z0 + z1) / 2f);
        c1.size = new Vector3(x1 - x0, 1f, z1 - z0);
        var c2 = root.AddComponent<BoxCollider>();
        c2.center = new Vector3(0f, 1.9f, -0.15f);
        c2.size = new Vector3(1f, 3.8f, 0.9f);

        var ped = estatuaRaiz.Find("Pedestal");
        if (ped != null) { Undo.RecordObject(ped.gameObject, "Ocultar pedestal"); ped.gameObject.SetActive(false); }
        var est = estatuaRaiz.Find("Estatua");
        if (est != null)
        {
            Undo.RecordObject(est, "Ubicar estatua");
            var rs = est.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var bb = Limites(rs);
                float esc = ALTO_ESTATUA / Mathf.Max(0.01f, bb.size.y);
                est.localScale *= esc;
                bb = Limites(rs);
                var objetivo = root.transform.TransformPoint(new Vector3(0f, PISO_ESTATUA, 0.06f));
                est.position += new Vector3(objetivo.x - bb.center.x, objetivo.y - bb.min.y, objetivo.z - bb.center.z);
            }
        }
        var cap = estatuaRaiz.GetComponent<CapsuleCollider>();
        if (cap != null) { Undo.RecordObject(cap, "Collider"); cap.enabled = false; }

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Plaza]</b> Vitrina de la Virgen construida.");
    }

    [MenuItem("Estacion/Plaza/7 - Bordes de las fuentes (2026)")]
    public static void BordesFuentes()
    {
        Cargar();
        foreach (var go in EscenaUtil.Raices()) if (go.name == BORDES) Undo.DestroyObjectImmediate(go);
        var root = new GameObject(BORDES);
        Undo.RegisterCreatedObjectUndo(root, "Bordes fuentes");

        var piso = EscenaUtil.Raiz("Fuentes_Piso");
        var plano = piso != null ? piso.transform.Find("Plane") : null;
        if (plano != null)
        {
            var r = plano.GetComponent<Renderer>();
            var c = r.bounds.center;
            float lado = Mathf.Max(r.bounds.size.x, r.bounds.size.z) * 0.5f;
            var g = new GameObject("Fuente_Piso_Borde");
            g.transform.SetParent(root.transform, false);
            g.transform.position = new Vector3(c.x, 0f, c.z);

            var rejilla = Mat("Rejilla_Metal", Color.white, 0.45f, 0.6f);
            rejilla.SetTexture("_BaseMap", TexRejilla());
            float anchoR = 0.32f, largoR = 2f * (lado + anchoR);
            rejilla.SetTextureScale("_BaseMap", new Vector2(largoR / 0.5f, 1f));
            var rj = new List<CombineInstance>();
            foreach (int s in new[] { -1, 1 })
            {
                Caja(rj, new Vector3(0f, 0.05f, s * (lado + anchoR / 2f)), new Vector3(largoR, 0.02f, anchoR));
                CajaRot(rj, new Vector3(s * (lado + anchoR / 2f), 0.05f, 0f), 90f, new Vector3(largoR, 0.02f, anchoR));
            }
            Pieza(g.transform, "Rejillas", rj, rejilla, false);

            var losa = Mat("Losa_Oscura", Color.white, 0.35f, 0f);
            losa.SetTexture("_BaseMap", TexLosa());
            float anchoL = 1.1f, inicio = lado + anchoR, largoL = 2f * (inicio + anchoL);
            losa.SetTextureScale("_BaseMap", new Vector2(largoL / 1.0f, anchoL / 1.0f));
            var ls = new List<CombineInstance>();
            foreach (int s in new[] { -1, 1 })
            {
                Caja(ls, new Vector3(0f, 0.03f, s * (inicio + anchoL / 2f)), new Vector3(largoL, 0.02f, anchoL));
                CajaRot(ls, new Vector3(s * (inicio + anchoL / 2f), 0.03f, 0f), 90f, new Vector3(largoL - 2f * anchoL, 0.02f, anchoL));
            }
            Pieza(g.transform, "Losas_Oscuras", ls, losa, false);
        }
        else Debug.LogWarning("[Plaza] No encontre Fuentes_Piso/Plane");

        var redonda = EscenaUtil.Raiz("Fuente 2026");
        var pileta = redonda != null ? redonda.transform.Find("Pileta") : null;
        if (pileta != null)
        {
            var rs = pileta.GetComponentsInChildren<Renderer>();
            var bb = Limites(rs);
            float r0 = Mathf.Max(bb.size.x, bb.size.z) * 0.5f + 0.02f;
            var g = new GameObject("Fuente_Redonda_Anillo");
            g.transform.SetParent(root.transform, false);
            g.transform.position = new Vector3(bb.center.x, 0f, bb.center.z);
            var piedra = Mat("Anillo_Piedra", Color.white, 0.3f, 0f);
            piedra.SetTexture("_BaseMap", TexLosa());
            piedra.SetTextureScale("_BaseMap", Vector2.one);
            var mesh = Anillo(r0, r0 + 1.1f, 0.06f, 48, 2);
            Guardar(mesh, GEN_DIR + "/Anillo_Fuente_Redonda.asset");
            var hijo = new GameObject("Anillo");
            hijo.transform.SetParent(g.transform, false);
            hijo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = hijo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = piedra;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }
        else Debug.LogWarning("[Plaza] No encontre 'Fuente 2026/Pileta'");

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        Registrar(root);
        EscenaUtil.Ordenar(false);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Plaza]</b> Bordes de las fuentes construidos.");
    }

    static void Cargar()
    {
        cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (esfera == null) esfera = Esfera(10, 7);
    }

    static void Caja(List<CombineInstance> l, Vector3 c, Vector3 s)
    {
        l.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(c, Quaternion.identity, s) });
    }

    static void CajaRot(List<CombineInstance> l, Vector3 c, float yaw, Vector3 s)
    {
        l.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(c, Quaternion.Euler(0f, yaw, 0f), s) });
    }

    static void Esf(List<CombineInstance> l, Vector3 c, Vector3 s)
    {
        l.Add(new CombineInstance { mesh = esfera, transform = Matrix4x4.TRS(c, Quaternion.identity, s) });
    }

    static void Barra(List<CombineInstance> l, Vector3 a, Vector3 b, float g)
    {
        var d = b - a;
        if (d.sqrMagnitude < 1e-8f) return;
        l.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS((a + b) / 2f, Quaternion.LookRotation(d.normalized, Mathf.Abs(d.normalized.y) > 0.95f ? Vector3.forward : Vector3.up), new Vector3(g, g, d.magnitude)) });
    }

    static void Reja(List<CombineInstance> l, Vector3 a, Vector3 b)
    {
        const float hBaja = 0.16f, hAlta = 0.62f, hArco = 0.16f, g = 0.025f;
        var dir = b - a;
        float largo = dir.magnitude;
        dir /= largo;
        Barra(l, a + Vector3.up * hBaja, b + Vector3.up * hBaja, g);
        Barra(l, a + Vector3.up * hAlta, b + Vector3.up * hAlta, g);
        int postes = Mathf.Max(1, Mathf.RoundToInt(largo / 1.1f));
        for (int i = 0; i <= postes; i++)
        {
            var p = a + dir * (largo * i / postes);
            Barra(l, p, p + Vector3.up * (hAlta + hArco + 0.08f), 0.055f);
        }
        int barras = Mathf.Max(2, Mathf.RoundToInt(largo / 0.2f));
        for (int i = 1; i < barras; i++)
        {
            var p = a + dir * (largo * i / barras);
            Barra(l, p + Vector3.up * hBaja, p + Vector3.up * hAlta, g * 0.8f);
        }
        float paso = 0.16f, ancho = 0.32f;
        for (float s = 0f; s + ancho <= largo + 0.001f; s += paso)
        {
            Vector3 prev = a + dir * s + Vector3.up * hAlta;
            for (int k = 1; k <= 6; k++)
            {
                float th = Mathf.PI * k / 6f;
                var q = a + dir * (s + ancho * 0.5f * (1f - Mathf.Cos(th))) + Vector3.up * (hAlta + hArco * Mathf.Sin(th));
                Barra(l, prev, q, g * 0.7f);
                prev = q;
            }
        }
    }

    static void Pieza(Transform padre, string nombre, List<CombineInstance> partes, Material mat, bool sombra)
    {
        if (partes.Count == 0) return;
        var mesh = new Mesh { name = padre.name + "_" + nombre, indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(partes.ToArray(), true, true);
        mesh.RecalculateBounds();
        var centro = mesh.bounds.center;
        var v = mesh.vertices;
        for (int i = 0; i < v.Length; i++) v[i] -= centro;
        mesh.vertices = v;
        mesh.RecalculateBounds();
        var guardada = Guardar(mesh, GEN_DIR + "/" + padre.name + "_" + nombre + ".asset");
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
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
        if (mesh.tangents.Length > 0) existente.SetTangents(mesh.tangents);
        existente.SetTriangles(mesh.triangles, 0);
        existente.RecalculateBounds();
        existente.name = mesh.name;
        EditorUtility.SetDirty(existente);
        Object.DestroyImmediate(mesh);
        return existente;
    }

    static Mesh Anillo(float r0, float r1, float alto, int segmentos, int filas)
    {
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        for (int i = 0; i <= segmentos; i++)
        {
            float a = 2f * Mathf.PI * i / segmentos;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int k = 0; k <= filas; k++)
            {
                float r = Mathf.Lerp(r0, r1, k / (float)filas);
                v.Add(d * r + Vector3.up * alto);
                uv.Add(new Vector2(i * 0.5f, k * (r1 - r0) / filas));
            }
        }
        int fila = filas + 1;
        for (int i = 0; i < segmentos; i++)
            for (int k = 0; k < filas; k++)
            {
                int a = i * fila + k, b = a + fila;
                t.Add(a); t.Add(a + 1); t.Add(b + 1);
                t.Add(a); t.Add(b + 1); t.Add(b);
            }
        int baseCanto = v.Count;
        for (int i = 0; i <= segmentos; i++)
        {
            float a = 2f * Mathf.PI * i / segmentos;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v.Add(d * r1 + Vector3.up * alto); uv.Add(new Vector2(i * 0.5f, 0f));
            v.Add(d * r1); uv.Add(new Vector2(i * 0.5f, alto));
        }
        for (int i = 0; i < segmentos; i++)
        {
            int a = baseCanto + i * 2, b = a + 2;
            t.Add(a); t.Add(b + 1); t.Add(a + 1);
            t.Add(a); t.Add(b); t.Add(b + 1);
        }
        var m = new Mesh { name = "Anillo_Fuente_Redonda" };
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        Orientar(m, Vector3.up, baseCanto);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void Orientar(Mesh m, Vector3 arriba, int finSuperior)
    {
        var v = m.vertices;
        var t = m.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            var n = Vector3.Cross(b - a, c - a);
            Vector3 fuera = t[i] < finSuperior ? arriba : new Vector3((a + b + c).x, 0f, (a + b + c).z);
            if (Vector3.Dot(n, fuera) < 0f) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; }
        }
        m.triangles = t;
    }

    static Mesh Esfera(int lon, int lat)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        v.Add(new Vector3(0f, -0.5f, 0f));
        for (int j = 1; j < lat; j++)
        {
            float phi = Mathf.PI * j / lat - Mathf.PI * 0.5f;
            for (int i = 0; i < lon; i++)
            {
                float th = 2f * Mathf.PI * i / lon;
                v.Add(0.5f * new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th)));
            }
        }
        v.Add(new Vector3(0f, 0.5f, 0f));
        int top = v.Count - 1;
        for (int i = 0; i < lon; i++)
        {
            int i1 = (i + 1) % lon;
            t.Add(0); t.Add(1 + i); t.Add(1 + i1);
            for (int j = 0; j < lat - 2; j++)
            {
                int a = 1 + j * lon + i, b = 1 + j * lon + i1;
                t.Add(a); t.Add(b); t.Add(b + lon);
                t.Add(a); t.Add(b + lon); t.Add(a + lon);
            }
            t.Add(1 + (lat - 2) * lon + i); t.Add(1 + (lat - 2) * lon + i1); t.Add(top);
        }
        var tri = t.ToArray();
        for (int i = 0; i < tri.Length; i += 3)
        {
            Vector3 a = v[tri[i]], b = v[tri[i + 1]], c = v[tri[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) { int x = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = x; }
        }
        var m = new Mesh { name = "Esfera_Baja" };
        m.SetVertices(v);
        m.triangles = tri;
        m.RecalculateNormals();
        return m;
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

    static Material Vidrio(string nombre)
    {
        var m = Mat(nombre, new Color(0.82f, 0.9f, 0.95f, 0.16f), 0.95f, 0f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("DepthOnly", false);
        m.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D TexRejilla()
    {
        return Textura("Rejilla_Metal.png", 128, 32, (x, y) =>
        {
            bool borde = y < 3 || y > 28;
            bool ranura = !borde && (x % 16) >= 5 && (x % 16) <= 11;
            float t = ranura ? 0.03f : 0.32f + 0.04f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
            return new Color(t, t, t * 1.04f);
        });
    }

    static Texture2D TexLosa()
    {
        return Textura("Losa_Oscura.png", 128, 128, (x, y) =>
        {
            bool junta = (x % 64) < 2 || (y % 64) < 2;
            float t = junta ? 0.12f : 0.26f + 0.06f * Mathf.PerlinNoise(x * 0.07f + (x / 64) * 5f, y * 0.07f + (y / 64) * 3f);
            return new Color(t, t * 0.98f, t * 0.96f);
        });
    }

    static Texture2D Textura(string nombre, int w, int h, System.Func<int, int, Color> f)
    {
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/" + nombre;
        var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (ex != null) return ex;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = f(x, y);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.anisoLevel = 4;
            imp.maxTextureSize = 256;
            imp.SaveAndReimport();
        }
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

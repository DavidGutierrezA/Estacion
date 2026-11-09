#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class CalleYArboles
{
    const string MAT_DIR = "Assets/Art/Materiales/Plaza";
    const string GEN_DIR = "Assets/Art/Generado/Plaza";

    [Header("Franjas junto a la plaza (de la plaza hacia la calle)")]
    const float ANCHO_BORDE = 0.15f;
    const float ANCHO_CICLO = 2.5f;
    const float ANCHO_LADRILLO = 1.4f;
    const float ANCHO_SARDINEL = 0.4f;
    const float LARGO_X0 = -110f, LARGO_X1 = 150f;

    [Header("Baldosas amarillas")]
    const float RAYA_ANCHO = 0.9f;
    const float RAYA_LARGO = 7.5f;
    const float RAYA_ANGULO = 0f;
    const float RAYA_Z0 = 26.2f;

    static Mesh cubo, esfera;

    [MenuItem("Estacion/Plaza/8 - Cicloruta y franja de ladrillo (2026)")]
    public static void Cicloruta()
    {
        Cargar();
        var calle = EscenaUtil.Raiz("EPOCA_2026_Props");
        var c = calle != null ? calle.transform.Find("Calle") : null;
        var suelo = EscenaUtil.Raiz("Suelo_Oeste");
        if (c == null || suelo == null) { Debug.LogError("[Calle] No encuentro Calle o Suelo_Oeste"); return; }
        var asfalto = c.Find("Asfalto");
        var sardinel = c.Find("Sardinel");
        var linea = c.Find("Linea_Calle");
        if (asfalto == null || sardinel == null) { Debug.LogError("[Calle] Falta Asfalto o Sardinel"); return; }

        float zPlaza = suelo.GetComponent<Renderer>().bounds.min.z;
        float zBorde = zPlaza - ANCHO_BORDE;
        float zCiclo = zBorde - ANCHO_CICLO;
        float zLadrillo = zCiclo - ANCHO_LADRILLO;
        float zSardinel = zLadrillo - ANCHO_SARDINEL;
        float zCalle0 = asfalto.GetComponent<Renderer>().bounds.min.z;
        float centroX = (LARGO_X0 + LARGO_X1) / 2f, largo = LARGO_X1 - LARGO_X0;

        Undo.RecordObject(asfalto, "Calle");
        asfalto.position = new Vector3(asfalto.position.x, asfalto.position.y, (zCalle0 + zSardinel) / 2f);
        asfalto.localScale = new Vector3(asfalto.localScale.x, asfalto.localScale.y, zSardinel - zCalle0);
        Undo.RecordObject(sardinel, "Calle");
        sardinel.position = new Vector3(sardinel.position.x, sardinel.position.y, zSardinel + ANCHO_SARDINEL / 2f);
        sardinel.localScale = new Vector3(sardinel.localScale.x, sardinel.localScale.y, ANCHO_SARDINEL);
        if (linea != null)
        {
            var lr = linea.GetComponent<Renderer>();
            float zLinea = lr.bounds.center.z, zObjetivo = (zCalle0 + zSardinel) / 2f;
            Undo.RecordObject(linea, "Calle");
            linea.position += new Vector3(0f, 0f, zObjetivo - zLinea);
        }

        foreach (var n in new[] { "Borde_Plaza", "Cicloruta", "Cicloruta_Linea", "Franja_Ladrillo" })
        {
            var viejo = c.Find(n);
            if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
        }

        var gris = Mat("Borde_Concreto", new Color(0.66f, 0.65f, 0.62f), 0.2f, 0f);
        Franja(c, "Borde_Plaza", centroX, largo, zBorde, zPlaza, 0.014f, gris, 1f, false);

        var ciclo = Mat("Cicloruta_Asfalto", Color.white, 0.18f, 0f);
        ciclo.SetTexture("_BaseMap", TexAsfalto());
        Franja(c, "Cicloruta", centroX, largo, zCiclo, zBorde, 0.012f, ciclo, 0.5f, true);

        var amarilla = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Plaza/Linea_Amarilla.mat");
        if (amarilla == null && linea != null) amarilla = linea.GetComponent<Renderer>().sharedMaterial;
        if (amarilla == null) amarilla = Mat("Linea_Amarilla_Ciclo", new Color(0.95f, 0.75f, 0.1f), 0.3f, 0f);
        var trazos = new List<CombineInstance>();
        float zMedio = (zCiclo + zBorde) / 2f;
        for (float x = LARGO_X0 + 1f; x < LARGO_X1 - 1f; x += 3f)
            trazos.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(new Vector3(x + 0.75f, 0.016f, zMedio), Quaternion.identity, new Vector3(1.5f, 0.004f, 0.1f)) });
        Pieza(c, "Cicloruta_Linea", trazos, amarilla, false);

        var ladrillo = Mat("Ladrillo_Gris_Pequeno", Color.white, 0.15f, 0f);
        ladrillo.SetTexture("_BaseMap", TexLadrillo());
        Franja(c, "Franja_Ladrillo", centroX, largo, zLadrillo, zCiclo, 0.012f, ladrillo, 1f, true);

        foreach (var t in c.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        Sucia(c.gameObject);
        Debug.Log("<b>[Calle]</b> Plaza z=" + zPlaza.ToString("F2") + " | cicloruta " + zCiclo.ToString("F2") + ".." + zBorde.ToString("F2") + " | ladrillo " + zLadrillo.ToString("F2") + " | calle " + zCalle0.ToString("F2") + ".." + zSardinel.ToString("F2"));
    }

    [MenuItem("Estacion/Plaza/9 - Baldosas amarillas (2026)")]
    public static void BaldosasAmarillas()
    {
        Cargar();
        var props = EscenaUtil.Raiz("EPOCA_2026_Props");
        var arboles = BuscarArboles();
        if (props == null) { Debug.LogError("[Plaza] No encuentro EPOCA_2026_Props"); return; }
        var viejo = props.transform.Find("Baldosas_Amarillas");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        var xs = new List<float>();
        if (arboles != null) foreach (Transform a in arboles.transform) xs.Add(a.position.x);
        xs.Sort();
        if (xs.Count < 2) for (float x = -40f; x <= 84f; x += 9.5f) xs.Add(x);

        var suelos = new List<Transform>();
        foreach (var n in new[] { "Suelo_Oeste", "Suelo_Este" }) { var s = EscenaUtil.Raiz(n); if (s != null) suelos.Add(s.transform); }
        if (suelos.Count == 0) { Debug.LogError("[Plaza] No encuentro los suelos"); return; }
        var baseMat = suelos[0].GetComponent<Renderer>().sharedMaterial;

        var path = MAT_DIR + "/Plaza_Adoquin_Amarillo.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(baseMat); AssetDatabase.CreateAsset(mat, path); }
        else mat.CopyPropertiesFromMaterial(baseMat);
        mat.SetColor("_BaseColor", new Color(1f, 0.86f, 0.5f));
        EditorUtility.SetDirty(mat);

        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        var dir = new Vector3(Mathf.Sin(RAYA_ANGULO * Mathf.Deg2Rad), 0f, Mathf.Cos(RAYA_ANGULO * Mathf.Deg2Rad));
        var lado = new Vector3(dir.z, 0f, -dir.x);
        int n2 = 0;
        for (int i = 0; i < xs.Count - 1; i++)
        {
            float xc = (xs[i] + xs[i + 1]) / 2f;
            var p0 = new Vector3(xc, 0.008f, RAYA_Z0);
            var p1 = p0 + dir * RAYA_LARGO;
            var esquinas = new[] { p0 - lado * RAYA_ANCHO / 2f, p0 + lado * RAYA_ANCHO / 2f, p1 + lado * RAYA_ANCHO / 2f, p1 - lado * RAYA_ANCHO / 2f };
            Transform suelo = null;
            foreach (var s in suelos) { var b = s.GetComponent<Renderer>().bounds; if (xc >= b.min.x && xc <= b.max.x) { suelo = s; break; } }
            if (suelo == null) continue;
            int k = v.Count;
            foreach (var e in esquinas)
            {
                v.Add(e);
                var l = suelo.InverseTransformPoint(e);
                uv.Add(new Vector2(0.5f - l.x / 10f, 0.5f - l.z / 10f));
            }
            t.Add(k); t.Add(k + 2); t.Add(k + 1);
            t.Add(k); t.Add(k + 3); t.Add(k + 2);
            n2++;
        }
        var mesh = new Mesh { name = "Baldosas_Amarillas" };
        var centro = Vector3.zero;
        foreach (var p in v) centro += p;
        centro /= Mathf.Max(1, v.Count);
        for (int i = 0; i < v.Count; i++) v[i] -= centro;
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        ArreglarCaras(mesh, Vector3.up);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        var guardada = Guardar(mesh, GEN_DIR + "/Baldosas_Amarillas.asset");

        var go = new GameObject("Baldosas_Amarillas");
        Undo.RegisterCreatedObjectUndo(go, "Baldosas amarillas");
        go.transform.SetParent(props.transform, false);
        go.transform.position = centro;
        go.AddComponent<MeshFilter>().sharedMesh = guardada;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        go.isStatic = true;
        Sucia(go);
        Debug.Log("<b>[Plaza]</b> " + n2 + " franjas de baldosa amarilla.");
    }

    [MenuItem("Estacion/Plaza/10 - Arboles variados y alcorques (2026)")]
    public static void Arboles()
    {
        Cargar();
        var grupo = BuscarArboles();
        if (grupo == null) { Debug.LogError("[Plaza] No encuentro Arboles_Frente"); return; }

        var concreto = Mat("Alcorque_Concreto", new Color(0.7f, 0.69f, 0.66f), 0.15f, 0f);
        var verde = Mat("Protector_Verde", new Color(0.13f, 0.33f, 0.21f), 0.45f, 0.4f);
        var hojaRoja = Mat("Arbol_Hoja_Acebo", new Color(0.13f, 0.26f, 0.11f), 0.35f, 0f);
        var hojaClara = Mat("Arbol_Hoja_Clara", new Color(0.36f, 0.55f, 0.2f), 0.2f, 0f);
        var hojaClara2 = Mat("Arbol_Hoja_Clara_2", new Color(0.44f, 0.62f, 0.26f), 0.2f, 0f);
        var hojaOscura = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/Arbol_Hoja_Oscura.mat");
        var hojaMedia = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/Arbol_Hoja_Media.mat");

        var borde = MallaBorde();
        var protector = MallaProtector();
        var rnd = new System.Random(11);
        int i = 0;
        foreach (Transform arbol in grupo.transform)
        {
            if (!arbol.name.StartsWith("Arbol")) continue;
            Undo.RegisterFullObjectHierarchyUndo(arbol.gameObject, "Arboles");
            foreach (var n in new[] { "Alcorque_Borde", "Alcorque_Protector", "Frutos", "Tronco_2", "Tronco_3" })
            {
                var x = arbol.Find(n);
                if (x != null) Object.DestroyImmediate(x.gameObject);
            }

            var datos = arbol.Find("Datos_Variacion");
            float previo = datos != null ? datos.localScale.x : 1f;
            if (datos == null)
            {
                datos = new GameObject("Datos_Variacion").transform;
                datos.SetParent(arbol, false);
                datos.gameObject.SetActive(false);
            }
            float f = 0.85f + (float)rnd.NextDouble() * 0.35f;
            float k = f / previo;
            foreach (Transform h in arbol)
            {
                if (h.name.StartsWith("Copa") || h.name == "Tronco")
                {
                    h.localPosition = new Vector3(h.localPosition.x * k, h.localPosition.y * k, h.localPosition.z * k);
                    h.localScale *= k;
                }
            }
            datos.localScale = new Vector3(f, 1f, 1f);

            int tipo = i % 3 == 1 ? 1 : (i % 3 == 2 ? 2 : 0);
            var copas = new List<Transform>();
            foreach (Transform h in arbol) if (h.name.StartsWith("Copa")) copas.Add(h);
            for (int c = 0; c < copas.Count; c++)
            {
                var r = copas[c].GetComponent<Renderer>();
                if (r == null) continue;
                if (tipo == 0) r.sharedMaterial = c % 3 == 1 ? hojaMedia : hojaOscura;
                else if (tipo == 1) r.sharedMaterial = hojaRoja;
                else r.sharedMaterial = c % 2 == 0 ? hojaClara : hojaClara2;
            }

            if (tipo == 1 && copas.Count > 0)
            {
                var tronco = arbol.Find("Tronco");
                if (tronco != null)
                {
                    var tm = tronco.GetComponent<Renderer>().sharedMaterial;
                    foreach (var lado in new[] { -1f, 1f })
                    {
                        var t2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        Object.DestroyImmediate(t2.GetComponent<Collider>());
                        t2.name = lado < 0 ? "Tronco_2" : "Tronco_3";
                        t2.transform.SetParent(arbol, false);
                        t2.transform.localScale = new Vector3(tronco.localScale.x * 0.55f, tronco.localScale.y * 0.9f, tronco.localScale.z * 0.55f);
                        t2.transform.localRotation = Quaternion.Euler(lado * 14f, 0f, lado * 10f);
                        t2.transform.localPosition = new Vector3(lado * 0.18f, tronco.localPosition.y * 0.92f, -lado * 0.1f);
                        t2.GetComponent<MeshRenderer>().sharedMaterial = tm;
                        t2.isStatic = true;
                    }
                }
            }

            var suelo = arbol.Find("Alcorque");
            if (suelo != null)
            {
                suelo.rotation = Quaternion.identity;
                suelo.localScale = new Vector3(1.3f, 0.05f, 1.3f);
                suelo.position = new Vector3(arbol.position.x, 0.025f, arbol.position.z);
            }
            var bo = HijoMalla(arbol, "Alcorque_Borde", borde, concreto, ShadowCastingMode.Off);
            bo.position = new Vector3(arbol.position.x, 0.06f, arbol.position.z);
            bo.rotation = Quaternion.identity;
            var pr = HijoMalla(arbol, "Alcorque_Protector", protector, verde, ShadowCastingMode.Off);
            pr.position = new Vector3(arbol.position.x, 0.12f, arbol.position.z);
            pr.rotation = Quaternion.identity;

            foreach (var tr in arbol.GetComponentsInChildren<Transform>(true)) tr.gameObject.isStatic = true;
            i++;
        }
        Sucia(grupo);
        Debug.Log("<b>[Plaza]</b> " + i + " arboles variados con alcorque nuevo.");
    }

    static GameObject BuscarArboles()
    {
        var e = EscenaUtil.Raiz("COMUN_Entorno");
        var a = e != null ? e.transform.Find("Arboles_Frente") : null;
        return a != null ? a.gameObject : EscenaUtil.Raiz("Arboles_Frente");
    }

    static Mesh MallaBorde()
    {
        const float exterior = 1.6f, grosor = 0.15f, alto = 0.12f;
        var partes = new List<CombineInstance>();
        float m = exterior / 2f - grosor / 2f;
        foreach (var s in new[] { -1f, 1f })
        {
            partes.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(new Vector3(0f, 0f, s * m), Quaternion.identity, new Vector3(exterior, alto, grosor)) });
            partes.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(new Vector3(s * m, 0f, 0f), Quaternion.identity, new Vector3(grosor, alto, exterior - 2f * grosor)) });
        }
        var mesh = new Mesh { name = "Alcorque_Borde" };
        mesh.CombineMeshes(partes.ToArray(), true, true);
        mesh.RecalculateBounds();
        return Guardar(mesh, GEN_DIR + "/Alcorque_Borde.asset");
    }

    static Mesh MallaProtector()
    {
        const float m = 0.66f, anchoArco = 1.05f, alto = 0.45f, g = 0.03f;
        var partes = new List<CombineInstance>();
        for (int lado = 0; lado < 4; lado++)
        {
            var rot = Quaternion.Euler(0f, lado * 90f, 0f);
            foreach (var off in new[] { 0f })
            {
                Vector3 prev = rot * new Vector3(off - anchoArco / 2f, 0f, m);
                for (int s = 1; s <= 8; s++)
                {
                    float th = Mathf.PI * s / 8f;
                    var q = rot * new Vector3(off - Mathf.Cos(th) * anchoArco / 2f, Mathf.Sin(th) * alto, m);
                    var d = q - prev;
                    partes.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS((q + prev) / 2f, Quaternion.LookRotation(d.normalized, Vector3.Cross(d, rot * Vector3.forward).sqrMagnitude > 1e-6f ? rot * Vector3.forward : Vector3.up), new Vector3(g, g * 1.6f, d.magnitude + 0.01f)) });
                    prev = q;
                }
            }
        }
        var mesh = new Mesh { name = "Alcorque_Protector" };
        mesh.CombineMeshes(partes.ToArray(), true, true);
        mesh.RecalculateBounds();
        return Guardar(mesh, GEN_DIR + "/Alcorque_Protector.asset");
    }

    static Transform Hijo(Transform padre, string nombre, List<CombineInstance> partes, Material mat, ShadowCastingMode sombra)
    {
        var mesh = new Mesh { name = padre.name + "_" + nombre };
        mesh.CombineMeshes(partes.ToArray(), true, true);
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = padre.position;
        go.transform.rotation = Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra;
        return go.transform;
    }

    static Transform HijoMalla(Transform padre, string nombre, Mesh mesh, Material mat, ShadowCastingMode sombra)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra;
        return go.transform;
    }

    static void Franja(Transform padre, string nombre, float cx, float largo, float z0, float z1, float y, Material mat, float metrosPorTextura, bool collider)
    {
        float x0 = cx - largo / 2f, x1 = cx + largo / 2f, zc = (z0 + z1) / 2f, ancho = z1 - z0;
        var v = new List<Vector3> { new Vector3(x0 - cx, 0f, z0 - zc), new Vector3(x1 - cx, 0f, z0 - zc), new Vector3(x1 - cx, 0f, z1 - zc), new Vector3(x0 - cx, 0f, z1 - zc) };
        var uv = new List<Vector2>();
        foreach (var p in v) uv.Add(new Vector2((p.x + cx) / metrosPorTextura, (p.z + zc) / metrosPorTextura));
        var t = new List<int> { 0, 2, 1, 0, 3, 2 };
        var mesh = new Mesh { name = nombre };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        ArreglarCaras(mesh, Vector3.up);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        var guardada = Guardar(mesh, GEN_DIR + "/Calle_" + nombre + ".asset");
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre, true);
        go.transform.position = new Vector3(cx, y, zc);
        go.transform.rotation = Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh = guardada;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        if (collider)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, -0.02f, 0f);
            bc.size = new Vector3(largo, 0.04f, ancho);
        }
    }

    static void Pieza(Transform padre, string nombre, List<CombineInstance> partes, Material mat, bool sombra)
    {
        if (partes.Count == 0) return;
        var mesh = new Mesh { name = nombre, indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(partes.ToArray(), true, true);
        mesh.RecalculateBounds();
        var centro = mesh.bounds.center;
        var v = mesh.vertices;
        for (int i = 0; i < v.Length; i++) v[i] -= centro;
        mesh.vertices = v;
        mesh.RecalculateBounds();
        var guardada = Guardar(mesh, GEN_DIR + "/Calle_" + nombre + ".asset");
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre, true);
        go.transform.position = centro;
        go.transform.rotation = Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh = guardada;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }

    static void ArreglarCaras(Mesh m, Vector3 arriba)
    {
        var v = m.vertices;
        var t = m.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
            if (Vector3.Dot(n, arriba) < 0f) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; }
        }
        m.triangles = t;
    }

    static void Cargar()
    {
        cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        esfera = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
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

    static Texture2D TexLadrillo()
    {
        return Textura("Ladrillo_Gris_Pequeno.png", 256, 256, (x, y) =>
        {
            int fila = y / 16, xx = x + (fila % 2) * 16, col = xx / 32;
            bool junta = (y % 16) < 2 || (xx % 32) < 2;
            float semilla = Mathf.Abs(Mathf.Sin(fila * 12.9898f + col * 78.233f) * 43758.5453f) % 1f;
            float t = junta ? 0.3f : 0.5f + 0.12f * semilla + 0.03f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
            return new Color(t, t * 0.99f, t * 0.97f);
        });
    }

    static Texture2D TexAsfalto()
    {
        return Textura("Cicloruta_Asfalto.png", 128, 128, (x, y) =>
        {
            float g = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.04f + Mathf.Abs(Mathf.Sin(x * 91.7f + y * 47.3f) * 1000f) % 1f * 0.035f;
            float t = 0.16f + g;
            return new Color(t, t, t * 1.05f);
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

    static void Sucia(GameObject go)
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        AssetDatabase.SaveAssets();
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

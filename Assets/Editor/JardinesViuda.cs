#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// =============================================================================
//  JARDINES DE LA EPOCA "LA VIUDA DE BLANCO" (estacion amarilla y azul)
//  Menu:  Estacion > Jardines > Construir jardines (epoca Viuda)
//
//  Basado en las fotos historicas: jardineras elevadas con borde de ladrillo y
//  flores, camino central de piedra hacia el portico, matas en macetas de barro,
//  cesped y coniferas altas. Todo queda en el contenedor EPOCA_Viuda_Jardines,
//  que se registra solo en el EpocaManager (activo en la Viuda, oculto en 2026).
//  Las flores y el follaje se unen en pocas mallas (ligero para WebGL).
//  Volver a correrlo borra y reconstruye el contenedor.
// =============================================================================
public static class JardinesViuda
{
    const string MAT_DIR = "Assets/Art/Materiales/Jardines";
    const string GEN_DIR = "Assets/Art/Generado";
    const string ROOT = "EPOCA_Viuda_Jardines";
    // Los canales de agua modernos ("Fuentes rectas") atraviesan el jardin:
    // si es true, se muestran solo en 2026 y se ocultan en las demas epocas.
    const bool OCULTAR_CANALES_FUERA_DE_2026 = true;

    static System.Random rng;

    [MenuItem("Estacion/Jardines/Construir jardines (epoca Viuda)")]
    public static void Construir()
    {
        rng = new System.Random(1926);

        var cesped   = Mat("Jardin_Cesped",      new Color(0.32f, 0.50f, 0.22f), 0.05f);
        var piedra   = Mat("Jardin_Camino",      new Color(0.62f, 0.60f, 0.56f), 0.10f);
        var ladrillo = Mat("Jardin_Ladrillo",    new Color(0.62f, 0.30f, 0.20f), 0.10f);
        var tierra   = Mat("Jardin_Tierra",      new Color(0.25f, 0.18f, 0.12f), 0.05f);
        var barro    = Mat("Jardin_Maceta",      new Color(0.72f, 0.38f, 0.22f), 0.15f);
        var hoja1    = Mat("Jardin_Follaje",     new Color(0.18f, 0.38f, 0.15f), 0.10f);
        var hoja2    = Mat("Jardin_Follaje_Cl",  new Color(0.26f, 0.46f, 0.18f), 0.10f);
        var pinoHoja = Mat("Jardin_Pino",        new Color(0.10f, 0.25f, 0.13f), 0.10f);
        var tronco   = Mat("Jardin_Tronco",      new Color(0.33f, 0.23f, 0.15f), 0.10f);
        Material[] flores = {
            Mat("Flor_Roja",     new Color(0.85f, 0.12f, 0.15f), 0.3f),
            Mat("Flor_Rosada",   new Color(0.95f, 0.50f, 0.65f), 0.3f),
            Mat("Flor_Amarilla", new Color(0.98f, 0.82f, 0.20f), 0.3f),
            Mat("Flor_Blanca",   new Color(0.95f, 0.95f, 0.92f), 0.3f),
            Mat("Flor_Morada",   new Color(0.55f, 0.30f, 0.75f), 0.3f),
            Mat("Flor_Naranja",  new Color(0.98f, 0.55f, 0.15f), 0.3f) };

        var t = PlainTex();
        if (t != null) { piedra.SetTexture("_BaseMap", t); piedra.SetTextureScale("_BaseMap", new Vector2(1.5f, 7f)); }

        var root = Raiz(ROOT);
        var florPos = new List<Matrix4x4>[flores.Length];
        for (int i = 0; i < flores.Length; i++) florPos[i] = new List<Matrix4x4>();
        var follaje1 = new List<Matrix4x4>();
        var follaje2 = new List<Matrix4x4>();

        // ================= DISTRIBUCION (segun las fotos historicas) =================
        //  - Camino recto desde la escalinata hasta la calle (x = 0).
        //  - Plaza de la fuente A UN LADO del camino, con jardineras curvas alrededor.
        //  - Jardineras largas a lo largo de la fachada.
        //  - Piso de ladrillo fuera de las jardineras (color editable en el material).
        //  - Jardineras con pasto y arbustos (sin flores ni macetas). Solo arboles grandes.

        // ---- Piso de ladrillo de toda la plaza ----
        var pisoLadrillo = Mat("Viuda_Piso_Ladrillo", COLOR_PISO, 0.15f);
        var texLad = GenerarLadrillo();
        pisoLadrillo.SetTexture("_BaseMap", texLad); pisoLadrillo.SetTextureScale("_BaseMap", new Vector2(70f, 21f));
        // cubre toda la estacion: plaza del frente, costados y anden (la plataforma y la Personeria quedan por encima)
        Prim(PrimitiveType.Cube, "Piso_Ladrillo", root, new Vector3(20f, 0.025f, 45f), new Vector3(140f, 0.04f, 42f), pisoLadrillo, default, true);

        // ---- Plaza de la fuente (plano de David, 31-oct) ----
        //  Camino central recto (x = -3..3) desde la escalinata hasta la calle.
        //  Izquierda: 2 jardineras con el extremo redondeado hacia el camino.
        //  Derecha: la fuente pegada al camino; las 2 jardineras de la derecha la
        //  abrazan con un arco concavo (la rotonda se cierra solo de ese lado).
        //  Afuera de la plaza las jardineras continuan (costados y fondo).
        Vector3 centroFuente = CENTRO_FUENTE;
        Vector2 F = new Vector2(centroFuente.x, centroFuente.z);
        const float RC = 5.1f;   // radio libre alrededor de la fuente (escalones 3.9 + andencito 1.2)
        var plazaFuente = Grupo("Plaza_Fuente", root, new Vector3(centroFuente.x, 0f, centroFuente.z));
        var plaza = Grupo("Jardineras_Plaza", root, Vector3.zero);
        // izquierda arriba: "D" con la punta redonda hacia el camino
        JardineraPoly(plaza, "Izq_Arriba", Forma.Rect(-20.5f, -3.4f, 41.0f, 46.8f, 0.8f, 2.9f, 2.9f, 0.8f), ladrillo, cesped, hoja1, hoja2);
        // izquierda abajo: esquina de arriba-derecha muy redondeada
        JardineraPoly(plaza, "Izq_Abajo", Forma.Rect(-20.5f, -3.4f, 29.0f, 37.8f, 0.8f, 3.0f, 0.8f, 0.8f), ladrillo, cesped, hoja1, hoja2);
        // derecha arriba / abajo: rectangulos con el mordisco circular de la fuente
        JardineraPoly(plaza, "Der_Arriba", Forma.RectMenosCirculo(3.4f, 19.5f, 41.2f, 47.2f, 0.8f, F, RC), ladrillo, cesped, hoja1, hoja2);
        JardineraPoly(plaza, "Der_Abajo", Forma.RectMenosCirculo(3.4f, 20.0f, 29.0f, 36.8f, 0.8f, F, RC), ladrillo, cesped, hoja1, hoja2);
        // jardinerita redonda a la derecha
        JardineraPoly(plaza, "Redonda", Forma.Circulo(new Vector2(17.6f, 39.0f), 1.3f), ladrillo, cesped, hoja1, hoja2);
        Prim(PrimitiveType.Cylinder, "Escalon_1", plazaFuente, new Vector3(0, 0.09f, 0), new Vector3(7.8f, 0.09f, 7.8f), ladrillo, default, true);
        Prim(PrimitiveType.Cylinder, "Escalon_2", plazaFuente, new Vector3(0, 0.27f, 0), new Vector3(6.6f, 0.09f, 6.6f), piedra, default, true);
        var fuente = Buscar("Fuente viuda");
        if (fuente != null)
        {
            Undo.RecordObject(fuente.transform, "Mover fuente");
            fuente.transform.position = new Vector3(centroFuente.x, -0.08f + 0.36f, centroFuente.z);   // sobre los escalones
        }

        // ---- Jardineras FUERA de la plaza: siguen a los lados y hacia el fondo ----
        var jard = Grupo("Jardineras_Afuera", root, Vector3.zero);
        Jardinera(jard, new Vector2(-31.5f, -23.5f), new Vector2(29.0f, 37.8f), ladrillo, cesped, hoja1, hoja2);
        Jardinera(jard, new Vector2(-31f, -24f), new Vector2(41.0f, 51.5f), ladrillo, cesped, hoja1, hoja2);   // izquierda: termina antes de la Virgen
        Jardinera(jard, new Vector2(23.5f, 31.5f), new Vector2(29.0f, 36.8f), ladrillo, cesped, hoja1, hoja2);
        Jardinera(jard, new Vector2(23.5f, 31.5f), new Vector2(41.2f, 46.0f), ladrillo, cesped, hoja1, hoja2);
        Jardinera(jard, new Vector2(22.8f, 25.4f), new Vector2(48.2f, 63f), ladrillo, cesped, hoja1, hoja2); // derecha: sigue la linea recta hacia el fondo

        // ---- Gruta de la Virgen (foto "Ubicacion antigua de la virgen") ----
        Gruta(root, ladrillo, cesped, hoja1, hoja2);

        // ---- Coniferas altas ----
        var pinos = Grupo("Coniferas", root, Vector3.zero);
        Conifera(pinos, new Vector3(-27.5f, 0f, 46.5f), 1.0f, tronco, pinoHoja);
        Conifera(pinos, new Vector3(27.5f, 0f, 61f), 0.85f, tronco, pinoHoja);

        // ---- Arboles grandes y frondosos (dentro de las jardineras, como en la foto sepia) ----
        var grandes = Grupo("Arboles_Grandes", root, Vector3.zero);
        // como en el plano: uno grande entre las jardineras de cada lado y otros dentro de ellas
        ArbolGrande(grandes, new Vector3(-14.5f, 0f, 39.4f), 1.05f, tronco, follaje1, follaje2);
        ArbolGrande(grandes, new Vector3(13.0f, 0f, 39.0f), 1.0f, tronco, follaje1, follaje2);
        ArbolGrande(grandes, new Vector3(-17.0f, 0f, 44.0f), 1.1f, tronco, follaje1, follaje2);
        ArbolGrande(grandes, new Vector3(-14.0f, 0f, 31.5f), 0.8f, tronco, follaje1, follaje2);
        ArbolGrande(grandes, new Vector3(17.0f, 0f, 31.2f), 0.85f, tronco, follaje1, follaje2);
        ArbolGrande(grandes, new Vector3(-27.5f, 0f, 33.4f), 0.95f, tronco, follaje1, follaje2);
        ArbolGrande(grandes, new Vector3(27.5f, 0f, 33.0f), 0.9f, tronco, follaje1, follaje2);

        // ---- Macizos de flores como prefabs (para que David los coloque a mano) ----
        CrearPrefabsMacizos(hoja1, hoja2, flores);

        // ---- Marquesina del anden (lado de la via) con mensulas ----
        Marquesina(root, Mat("Jardin_Marquesina", new Color(0.52f, 0.55f, 0.56f), 0.35f),
                         Mat("Jardin_Mensula", new Color(0.18f, 0.24f, 0.21f), 0.35f));

        // ---- Copas de los arboles grandes unidas en pocas mallas ----
        var detalle = Grupo("Copas_Arboles", root, Vector3.zero);
        var ico1 = Ico(1, "Ico_Sub1");
        Combinado("Jardin_Copas_A", detalle, follaje1, hoja1, ico1);
        Combinado("Jardin_Copas_B", detalle, follaje2, hoja2, ico1);

        Registrar(root.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Jardines]</b> Jardines de la epoca Viuda construidos (" + ROOT + ").");
    }

    // Color del piso de ladrillo (en las fotos no se sabe si era rojo o gris).
    // Tambien se cambia facil en Unity: material Assets/Art/Materiales/Jardines/Viuda_Piso_Ladrillo -> Base Color.
    static readonly Color COLOR_PISO = new Color(0.74f, 0.45f, 0.36f);      // gris: new Color(0.70f, 0.69f, 0.66f)
    static readonly Vector3 CENTRO_FUENTE = new Vector3(5.6f, 0f, 39.0f);    // fuente pegada al lado derecho del camino (plano de David)
    // Empty que David puso donde iba la Virgen. La gruta mira hacia el +Z (adelante) del empty:
    // si hay que girarla, rota el empty en Y y vuelve a correr el menu.
    const string EMPTY_VIRGEN = "Ubicaion virgen viuda";
    static readonly Vector3 VIRGEN_POR_DEFECTO = new Vector3(-25.86f, 0f, 58.78f);

    // Gruta azul con nicho de arco blanco, Virgen sobre pedestal y jardineras alrededor.
    static void Gruta(Transform root, Material ladrillo, Material cesped, Material h1, Material h2)
    {
        var empty = Buscar(EMPTY_VIRGEN);
        Vector3 pos = empty != null ? empty.transform.position : VIRGEN_POR_DEFECTO; pos.y = 0f;
        float giro = empty != null ? empty.transform.eulerAngles.y : 0f;

        var azul   = Mat("Gruta_Azul",   new Color(0.30f, 0.52f, 0.80f), 0.15f);
        var azulOs = Mat("Gruta_Nicho",  new Color(0.16f, 0.30f, 0.58f), 0.10f);
        var blanco = Mat("Gruta_Blanco", new Color(0.93f, 0.92f, 0.88f), 0.15f);
        var base_  = Mat("Gruta_Base",   new Color(0.66f, 0.64f, 0.60f), 0.10f);

        var g = new GameObject("Gruta_Virgen");
        g.transform.SetParent(root, false);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, giro, 0f);
        g.isStatic = true;
        var t = g.transform;

        // base y muro azul con remate redondo
        Prim(PrimitiveType.Cube, "Base", t, new Vector3(0, 0.15f, 0.25f), new Vector3(4.0f, 0.3f, 1.9f), base_, default, true);
        Prim(PrimitiveType.Cube, "Muro", t, new Vector3(0, 1.6f, -0.3f), new Vector3(3.4f, 2.6f, 0.6f), azul, default, true);
        Prim(PrimitiveType.Cylinder, "Remate", t, new Vector3(0, 2.9f, -0.3f), new Vector3(3.4f, 0.3f, 3.4f), azul, new Vector3(90, 0, 0));
        // marco blanco en forma de arco + fondo azul oscuro del nicho
        Prim(PrimitiveType.Cube, "Marco", t, new Vector3(0, 1.75f, 0.02f), new Vector3(2.1f, 1.5f, 0.06f), blanco);
        Prim(PrimitiveType.Cylinder, "Marco_Arco", t, new Vector3(0, 2.5f, 0.02f), new Vector3(2.1f, 0.03f, 2.1f), blanco, new Vector3(90, 0, 0));
        Prim(PrimitiveType.Cube, "Nicho", t, new Vector3(0, 1.75f, 0.06f), new Vector3(1.7f, 1.5f, 0.04f), azulOs);
        Prim(PrimitiveType.Cylinder, "Nicho_Arco", t, new Vector3(0, 2.5f, 0.06f), new Vector3(1.7f, 0.02f, 1.7f), azulOs, new Vector3(90, 0, 0));
        Prim(PrimitiveType.Cube, "Repisa", t, new Vector3(0, 0.95f, 0.35f), new Vector3(2.3f, 0.1f, 0.7f), blanco);
        Prim(PrimitiveType.Cube, "Cruz_V", t, new Vector3(0, 4.95f, -0.3f), new Vector3(0.1f, 0.7f, 0.1f), blanco);
        Prim(PrimitiveType.Cube, "Cruz_H", t, new Vector3(0, 5.05f, -0.3f), new Vector3(0.42f, 0.1f, 0.1f), blanco);

        // la Virgen (mismo modelo y material que la de 2026)
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Virgen/SantaMaria_1k.fbx");
        var matV = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Juego/Virgen_Piedra.mat");
        if (fbx != null)
        {
            var est = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            est.name = "Virgen";
            est.transform.SetParent(t, false);
            est.transform.localPosition = new Vector3(0, 1.0f, 0.35f);
            est.transform.localRotation = Quaternion.identity;
            var rs = est.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 0.01f) est.transform.localScale = Vector3.one * (1.45f / b.size.y);   // cabe en el nicho
                if (matV != null) foreach (var r in rs) r.sharedMaterial = matV;
            }
            foreach (var tr in est.GetComponentsInChildren<Transform>()) tr.gameObject.isStatic = true;
        }

        // jardineras alrededor (como en la foto): a los lados, detras y una larga al frente
        Jardinera(t, new Vector2(-5.6f, -2.4f), new Vector2(-1.0f, 2.6f), ladrillo, cesped, h1, h2);
        Jardinera(t, new Vector2(2.4f, 3.7f), new Vector2(-1.0f, 2.6f), ladrillo, cesped, h1, h2);   // corta: no pisa el anden
        Jardinera(t, new Vector2(-4.4f, 3.7f), new Vector2(-3.4f, -1.4f), ladrillo, cesped, h1, h2);
        Jardinera(t, new Vector2(-7.5f, -2.0f), new Vector2(4.6f, 6.4f), ladrillo, cesped, h1, h2);
    }
    static int contMallas;

    // Jardinera rectangular de esquinas curvas, con pasto y algunos arbustos (todo dentro de un mismo grupo).
    // x = (borde interior, borde exterior), z = (inicio, fin)
    static void Jardinera(Transform p, Vector2 x, Vector2 z, Material ladrillo, Material cesped, Material h1, Material h2)
    {
        float x0 = Mathf.Min(x.x, x.y), x1 = Mathf.Max(x.x, x.y);
        float cx = (x0 + x1) / 2f, cz = (z.x + z.y) / 2f, w = x1 - x0, d = z.y - z.x;
        const float h = 0.45f, e = 0.25f, radio = 1.6f;
        var g = Grupo("Jardinera", p, new Vector3(cx, 0f, cz));
        var borde = MeshObj("Borde_Ladrillo", g, BordeRedondeado(w, d, radio, e, h), ladrillo);
        borde.AddComponent<MeshCollider>().sharedMesh = borde.GetComponent<MeshFilter>().sharedMesh;
        var tapa = MeshObj("Pasto", g, TapaRedondeada(w - 2 * e, d - 2 * e, radio - e), cesped);
        tapa.transform.localPosition = new Vector3(0, h - 0.04f, 0);

        float ix = w / 2 - e - 0.6f, iz = d / 2 - e - 0.6f, top = h - 0.02f;
        var ico1 = Ico(1, "Ico_Sub1");
        var ms = new List<Mesh>(); var mx = new List<Matrix4x4>(); var mi = new List<int>();
        int n = Mathf.Max(2, Mathf.RoundToInt(w * d * 0.10f));
        for (int i = 0; i < n; i++)
        {
            float s = R(0.8f, 1.4f);
            ms.Add(ico1); mi.Add(i % 2);
            mx.Add(Matrix4x4.TRS(new Vector3(R(-ix, ix), top, R(-iz, iz)), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * 0.65f, s)));
        }
        PlantasObj("Arbustos", g, ms, mx, mi, new[] { h1, h2 });
    }

    // ------------------------------------------------------------------ formas libres
    // Contornos (x, z) para jardineras de cualquier forma.
    static class Forma
    {
        // Rectangulo con radio propio en cada esquina:
        // rArrIzq (x0,z1), rArrDer (x1,z1), rAbaDer (x1,z0), rAbaIzq (x0,z0). "Arriba" = hacia la estacion (+z).
        public static List<Vector2> Rect(float x0, float x1, float z0, float z1, float rArrIzq, float rArrDer, float rAbaDer, float rAbaIzq)
        {
            float lim = Mathf.Min(x1 - x0, z1 - z0) / 2f - 0.01f;
            rArrIzq = Mathf.Min(rArrIzq, lim); rArrDer = Mathf.Min(rArrDer, lim); rAbaDer = Mathf.Min(rAbaDer, lim); rAbaIzq = Mathf.Min(rAbaIzq, lim);
            var p = new List<Vector2>();
            // antihorario: abajo-izq -> abajo-der -> arriba-der -> arriba-izq
            Esquina(p, new Vector2(x0 + rAbaIzq, z0 + rAbaIzq), rAbaIzq, 180f, 270f);
            Linea(p, new Vector2(x0 + rAbaIzq, z0), new Vector2(x1 - rAbaDer, z0));
            Esquina(p, new Vector2(x1 - rAbaDer, z0 + rAbaDer), rAbaDer, 270f, 360f);
            Linea(p, new Vector2(x1, z0 + rAbaDer), new Vector2(x1, z1 - rArrDer));
            Esquina(p, new Vector2(x1 - rArrDer, z1 - rArrDer), rArrDer, 0f, 90f);
            Linea(p, new Vector2(x1 - rArrDer, z1), new Vector2(x0 + rArrIzq, z1));
            Esquina(p, new Vector2(x0 + rArrIzq, z1 - rArrIzq), rArrIzq, 90f, 180f);
            Linea(p, new Vector2(x0, z1 - rArrIzq), new Vector2(x0, z0 + rAbaIzq));
            return Limpiar(p);
        }

        // Rectangulo redondeado con un "mordisco" circular (la jardinera abraza la fuente).
        public static List<Vector2> RectMenosCirculo(float x0, float x1, float z0, float z1, float r, Vector2 c, float rc)
        {
            var p = Rect(x0, x1, z0, z1, r, r, r, r);
            int n = p.Count, inicio = -1;
            for (int i = 0; i < n; i++) if ((p[i] - c).magnitude >= rc) { inicio = i; break; }
            if (inicio < 0) return p;
            var q = new List<Vector2>();
            float angEntrada = 0f;
            for (int k = 0; k < n; k++)
            {
                Vector2 a = p[(inicio + k) % n], b = p[(inicio + k + 1) % n];
                bool aIn = (a - c).magnitude < rc, bIn = (b - c).magnitude < rc;
                if (!aIn) q.Add(a);
                if (!aIn && bIn) angEntrada = Ang(Corte(a, b, c, rc) - c);
                if (aIn && !bIn)
                {
                    // arco en sentido horario (la jardinera rodea la fuente por fuera)
                    float angSalida = Ang(Corte(b, a, c, rc) - c);
                    while (angSalida > angEntrada) angSalida -= 360f;
                    int m = Mathf.Max(2, Mathf.CeilToInt((angEntrada - angSalida) / 6f));
                    for (int s = 0; s <= m; s++)
                    {
                        float ang = Mathf.Lerp(angEntrada, angSalida, (float)s / m) * Mathf.Deg2Rad;
                        q.Add(c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rc);
                    }
                }
            }
            return Limpiar(q);
        }

        static float Ang(Vector2 d) { return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; }

        // punto del segmento afuera->adentro que cae justo sobre el circulo
        static Vector2 Corte(Vector2 afuera, Vector2 adentro, Vector2 c, float rc)
        {
            for (int i = 0; i < 24; i++)
            {
                var mid = (afuera + adentro) * 0.5f;
                if ((mid - c).magnitude < rc) adentro = mid; else afuera = mid;
            }
            return (afuera + adentro) * 0.5f;
        }

        public static List<Vector2> Circulo(Vector2 c, float r)
        {
            var p = new List<Vector2>();
            for (int i = 0; i < 28; i++) { float a = i * Mathf.PI * 2f / 28; p.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return p;
        }

        static void Esquina(List<Vector2> p, Vector2 c, float r, float a0, float a1)
        {
            if (r < 0.02f) { p.Add(c); return; }
            int n = Mathf.Max(3, Mathf.CeilToInt(r * 6f));
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, (float)i / n) * Mathf.Deg2Rad; p.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
        }

        static void Linea(List<Vector2> p, Vector2 a, Vector2 b)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.3f));
            for (int i = 1; i < n; i++) p.Add(Vector2.Lerp(a, b, (float)i / n));
        }

        static List<Vector2> Limpiar(List<Vector2> p)
        {
            var q = new List<Vector2>();
            foreach (var v in p) if (q.Count == 0 || Vector2.Distance(q[q.Count - 1], v) > 0.08f) q.Add(v);
            if (q.Count > 2 && Vector2.Distance(q[0], q[q.Count - 1]) < 0.08f) q.RemoveAt(q.Count - 1);
            return q;
        }
    }

    static float Area(List<Vector2> p)
    {
        float a = 0f;
        for (int i = 0; i < p.Count; i++) { var u = p[i]; var w = p[(i + 1) % p.Count]; a += u.x * w.y - w.x * u.y; }
        return a * 0.5f;
    }

    // Contorno desplazado hacia adentro una distancia g (poligono antihorario).
    static List<Vector2> Desplazar(List<Vector2> p, float g)
    {
        var q = new List<Vector2>();
        int n = p.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = p[(i - 1 + n) % n], b = p[i], c = p[(i + 1) % n];
            Vector2 d1 = (b - a).normalized, d2 = (c - b).normalized;
            Vector2 n1 = new Vector2(-d1.y, d1.x), n2 = new Vector2(-d2.y, d2.x);   // normales hacia adentro
            Vector2 m = (n1 + n2); m = m.sqrMagnitude < 1e-6f ? n1 : m.normalized;
            q.Add(b + m * (g / Mathf.Max(0.35f, Vector2.Dot(m, n1))));
        }
        return q;
    }

    static bool Dentro(Vector2 pt, List<Vector2> p)
    {
        bool c = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            if (((p[i].y > pt.y) != (p[j].y > pt.y)) && (pt.x < (p[j].x - p[i].x) * (pt.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)) c = !c;
        return c;
    }

    static float DistBorde(Vector2 pt, List<Vector2> p)
    {
        float d = float.MaxValue;
        for (int i = 0; i < p.Count; i++)
        {
            Vector2 a = p[i], b = p[(i + 1) % p.Count], ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pt - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            d = Mathf.Min(d, Vector2.Distance(pt, a + ab * t));
        }
        return d;
    }

    // Triangulacion por "orejas" (sirve para formas concavas).
    static List<int> Orejas(List<Vector2> p)
    {
        var idx = new List<int>(); for (int i = 0; i < p.Count; i++) idx.Add(i);
        var tris = new List<int>();
        int guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool corto = false;
            for (int k = 0; k < idx.Count; k++)
            {
                int ia = idx[(k - 1 + idx.Count) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                Vector2 a = p[ia], b = p[ib], c = p[ic];
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) <= 1e-6f) continue;   // vertice concavo
                bool vacia = true;
                foreach (int j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    if (EnTri(p[j], a, b, c)) { vacia = false; break; }
                }
                if (!vacia) continue;
                tris.Add(ia); tris.Add(ib); tris.Add(ic);
                idx.RemoveAt(k); corto = true; break;
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

    // Jardinera de forma libre: borde de ladrillo + pasto + arbustos (orilla y adentro).
    static void JardineraPoly(Transform p, string name, List<Vector2> contorno, Material ladrillo, Material cesped, Material h1, Material h2)
    {
        const float h = 0.45f, e = 0.25f;
        if (Area(contorno) < 0f) contorno.Reverse();
        var interior = Desplazar(contorno, e);
        var g = Grupo("Jardinera_" + name, p, Vector3.zero);

        var v = new List<Vector3>(); var t = new List<int>();
        int n = contorno.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector3 oB = new Vector3(contorno[i].x, 0, contorno[i].y), oB2 = new Vector3(contorno[j].x, 0, contorno[j].y);
            Vector3 iB = new Vector3(interior[i].x, 0, interior[i].y), iB2 = new Vector3(interior[j].x, 0, interior[j].y);
            Vector3 up = Vector3.up * h;
            Vector2 d = (contorno[j] - contorno[i]).normalized;
            Vector3 afuera = new Vector3(d.y, 0, -d.x);
            Quad(v, t, oB + up, oB2 + up, iB2 + up, iB + up, Vector3.up);
            Quad(v, t, oB, oB2, oB2 + up, oB + up, afuera);
            Quad(v, t, iB, iB2, iB2 + up, iB + up, -afuera);
        }
        var borde = MeshObj("Borde_Ladrillo", g, GuardarNuevo("JardPoly_Borde_" + name, v, t), ladrillo);
        borde.AddComponent<MeshCollider>().sharedMesh = borde.GetComponent<MeshFilter>().sharedMesh;

        var cv = new List<Vector3>(); var ct = new List<int>();
        var tri = Orejas(interior);
        for (int k = 0; k < tri.Count; k += 3)
            Tri(cv, ct, new Vector3(interior[tri[k]].x, 0, interior[tri[k]].y), new Vector3(interior[tri[k + 1]].x, 0, interior[tri[k + 1]].y),
                new Vector3(interior[tri[k + 2]].x, 0, interior[tri[k + 2]].y), Vector3.up);
        var tapa = MeshObj("Pasto", g, GuardarNuevo("JardPoly_Pasto_" + name, cv, ct), cesped);
        tapa.transform.localPosition = new Vector3(0, h - 0.04f, 0);

        // arbustos: una fila siguiendo la orilla y algunos sueltos adentro
        var ico1 = Ico(1, "Ico_Sub1");
        var ms = new List<Mesh>(); var mx = new List<Matrix4x4>(); var mi = new List<int>();
        var orilla = Desplazar(interior, 0.5f);
        float acum = 0f;
        for (int i = 0; i < orilla.Count; i++)
        {
            acum += Vector2.Distance(orilla[i], orilla[(i + 1) % orilla.Count]);
            if (acum < 1.25f) continue;
            acum = 0f;
            if (!Dentro(orilla[i], interior) || DistBorde(orilla[i], interior) < 0.35f) continue;
            float s = R(0.65f, 0.95f);
            ms.Add(ico1); mi.Add(ms.Count % 2);
            mx.Add(Matrix4x4.TRS(new Vector3(orilla[i].x, h - 0.02f, orilla[i].y), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * 0.7f, s)));
        }
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var q in interior) { minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x); minZ = Mathf.Min(minZ, q.y); maxZ = Mathf.Max(maxZ, q.y); }
        int sueltos = Mathf.RoundToInt(Mathf.Abs(Area(interior)) * 0.035f);
        for (int i = 0, intentos = 0; i < sueltos && intentos < 300; intentos++)
        {
            var pt = new Vector2(R(minX, maxX), R(minZ, maxZ));
            if (!Dentro(pt, interior) || DistBorde(pt, interior) < 1.6f) continue;
            float s = R(0.9f, 1.4f);
            ms.Add(ico1); mi.Add(i % 2);
            mx.Add(Matrix4x4.TRS(new Vector3(pt.x, h - 0.02f, pt.y), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * 0.65f, s)));
            i++;
        }
        PlantasObj("Arbustos", g, ms, mx, mi, new[] { h1, h2 });
    }

    static Mesh GuardarNuevo(string name, List<Vector3> v, List<int> t)
    {
        cacheMallas.Remove(name);
        return Guardar(name, v, t);
    }

    // Jardinera curva (sector de anillo) alrededor de la fuente. Angulos en grados.
    static void JardineraSector(Transform p, float rIn, float rOut, float a0, float a1, Material ladrillo, Material cesped, Material h1, Material h2)
    {
        const float h = 0.45f, e = 0.25f;
        var g = Grupo("Jardinera_Curva", p, Vector3.zero);
        var borde = MeshObj("Borde_Ladrillo", g, BordeSector(rIn, rOut, a0, a1, e, h), ladrillo);
        borde.AddComponent<MeshCollider>().sharedMesh = borde.GetComponent<MeshFilter>().sharedMesh;
        var tapa = MeshObj("Pasto", g, TapaSector(rIn + e, rOut - e, a0, a1, e), cesped);
        tapa.transform.localPosition = new Vector3(0, h - 0.04f, 0);

        var ico1 = Ico(1, "Ico_Sub1");
        var ms = new List<Mesh>(); var mx = new List<Matrix4x4>(); var mi = new List<int>();
        for (int i = 0; i < 7; i++)
        {
            float r = R(rIn + 1.0f, rOut - 1.0f), a = R(a0 + 8f, a1 - 8f) * Mathf.Deg2Rad, s = R(0.8f, 1.3f);
            ms.Add(ico1); mi.Add(i % 2);
            mx.Add(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * r, h - 0.02f, Mathf.Sin(a) * r), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * 0.65f, s)));
        }
        PlantasObj("Arbustos", g, ms, mx, mi, new[] { h1, h2 });
    }

    // Un solo objeto con varias submallas (una por material): facil de mover y ligero.
    static GameObject PlantasObj(string name, Transform parent, List<Mesh> ms, List<Matrix4x4> mx, List<int> mi, Material[] mats)
    {
        var partes = new List<CombineInstance>(); var usados = new List<Material>();
        for (int k = 0; k < mats.Length; k++)
        {
            var ci = new List<CombineInstance>();
            for (int i = 0; i < ms.Count; i++) if (mi[i] == k) ci.Add(new CombineInstance { mesh = ms[i], transform = mx[i] });
            if (ci.Count == 0) continue;
            var sub = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            sub.CombineMeshes(ci.ToArray(), true, true);
            partes.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
            usados.Add(mats[k]);
        }
        if (partes.Count == 0) return null;
        var mesh = new Mesh { name = name + "_" + (contMallas++), indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(partes.ToArray(), false, false);
        mesh.RecalculateBounds();
        foreach (var pr in partes) Object.DestroyImmediate(pr.mesh);
        EnsureDir(GEN_DIR + "/Jardin");
        string path = GEN_DIR + "/Jardin/" + mesh.name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = usados.ToArray();
        go.isStatic = true;
        return go;
    }

    // Prefabs de macizos de flores: hojas + corolas planas + centro, en UN solo objeto.
    static void CrearPrefabsMacizos(Material h1, Material h2, Material[] f)
    {
        // f: 0 roja, 1 rosada, 2 amarilla, 3 blanca, 4 morada, 5 naranja
        var centro = f[2];
        var variantes = new[] {
            new { nombre = "Macizo_Flores_Rojo_Blanco",     a = f[0], b = f[3] },
            new { nombre = "Macizo_Flores_Amarillo_Naranja", a = f[2], b = f[5] },
            new { nombre = "Macizo_Flores_Rosado_Morado",    a = f[1], b = f[4] } };
        string dir = "Assets/Art/Prefabs/Jardin";
        EnsureDir(dir);
        var ico1 = Ico(1, "Ico_Sub1"); var ico0 = Ico(0, "Ico_Sub0"); var corola = Cono("Corola_Flor", 8);
        foreach (var v in variantes)
        {
            var ms = new List<Mesh>(); var mx = new List<Matrix4x4>(); var mi = new List<int>();
            for (int i = 0; i < 6; i++)   // mata de hojas
            {
                float a = i * 60f * Mathf.Deg2Rad, r = i == 0 ? 0f : 0.35f, s = R(0.55f, 0.75f);
                ms.Add(ico1); mi.Add(i % 2);
                mx.Add(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * r, 0.18f, Mathf.Sin(a) * r), Quaternion.Euler(0, R(0, 360), 0), new Vector3(s, s * 0.7f, s)));
            }
            for (int i = 0; i < 14; i++)  // flores con corola plana y centro amarillo
            {
                float a = R(0, Mathf.PI * 2), r = R(0f, 0.5f);
                var pos = new Vector3(Mathf.Cos(a) * r, R(0.36f, 0.48f), Mathf.Sin(a) * r);
                var rot = Quaternion.Euler(R(-15, 15), R(0, 360), R(-15, 15));
                ms.Add(corola); mi.Add(i % 2 == 0 ? 2 : 3);
                mx.Add(Matrix4x4.TRS(pos, rot, new Vector3(0.24f, 0.05f, 0.24f)));
                ms.Add(ico0); mi.Add(4);
                mx.Add(Matrix4x4.TRS(pos + rot * new Vector3(0, 0.05f, 0), Quaternion.identity, Vector3.one * 0.07f));
            }
            var go = PlantasObj(v.nombre, null, ms, mx, mi, new[] { h1, h2, v.a, v.b, centro });
            go.isStatic = false;
            PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + v.nombre + ".prefab");
            Object.DestroyImmediate(go);
        }
    }

    // Textura de ladrillo en escala de grises (el color lo pone el material).
    static Texture2D GenerarLadrillo()
    {
        EnsureDir(GEN_DIR);
        string path = GEN_DIR + "/Ladrillo_Piso.png";
        const int S = 512, filas = 16, porFila = 8, junta = 3;
        int alto = S / filas, ancho = S / porFila;
        var r = new System.Random(7);
        var tonos = new float[filas, porFila + 1];
        for (int i = 0; i < filas; i++) for (int j = 0; j <= porFila; j++) tonos[i, j] = 0.80f + (float)r.NextDouble() * 0.18f;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        {
            int fila = y / alto;
            int desfase = (fila % 2) * (ancho / 2);
            for (int x = 0; x < S; x++)
            {
                int xx = (x + desfase) % S;
                int col = xx / ancho;
                bool esJunta = (y % alto) < junta || (xx % ancho) < junta;
                float t = esJunta ? 0.55f : tonos[fila, col] + ((float)r.NextDouble() - 0.5f) * 0.04f;
                px[y * S + x] = new Color(t, t, t);
            }
        }
        var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.maxTextureSize = 512;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static List<Vector3> Arco(float r, float a0, float a1, int n, float y)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.Lerp(a0, a1, (float)i / n) * Mathf.Deg2Rad;
            pts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
        }
        return pts;
    }

    // Contorno cerrado de un sector de anillo (arco exterior ida, arco interior vuelta).
    static List<Vector3> ContornoSector(float rIn, float rOut, float a0, float a1, float y)
    {
        var c = Arco(rOut, a0, a1, 12, y);
        var inn = Arco(rIn, a1, a0, 12, y);
        c.AddRange(inn);
        return c;
    }

    static Mesh BordeSector(float rIn, float rOut, float a0, float a1, float g, float h)
    {
        float dOut = g / (rOut - g) * Mathf.Rad2Deg, dIn = g / (rIn + g) * Mathf.Rad2Deg;
        var oB = ContornoSector(rIn, rOut, a0, a1, 0); var oT = ContornoSector(rIn, rOut, a0, a1, h);
        var iB = Arco(rOut - g, a0 + dOut, a1 - dOut, 12, 0); iB.AddRange(Arco(rIn + g, a1 - dIn, a0 + dIn, 12, 0));
        var iT = Arco(rOut - g, a0 + dOut, a1 - dOut, 12, h); iT.AddRange(Arco(rIn + g, a1 - dIn, a0 + dIn, 12, h));
        Vector3 cen = Vector3.zero; foreach (var q in oB) cen += q; cen /= oB.Count;
        var v = new List<Vector3>(); var t = new List<int>();
        int n = oB.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector3 hacia = (oB[i] + oB[j]) * 0.5f - cen; hacia.y = 0;
            Vector3 haciaIn = (iB[i] + iB[j]) * 0.5f - cen; haciaIn.y = 0;
            Quad(v, t, oT[i], oT[j], iT[j], iT[i], Vector3.up);
            Quad(v, t, oB[i], oB[j], oT[j], oT[i], hacia);
            Quad(v, t, iB[i], iB[j], iT[j], iT[i], -haciaIn);
        }
        return Guardar(string.Format("BordeSector_{0:F0}_{1:F0}_{2:F0}_{3:F0}", rIn, rOut, a0, a1), v, t);
    }

    static Mesh TapaSector(float rIn, float rOut, float a0, float a1, float g)
    {
        float dOut = g / rOut * Mathf.Rad2Deg, dIn = g / rIn * Mathf.Rad2Deg;
        var o = Arco(rOut, a0 + dOut, a1 - dOut, 12, 0);
        var i2 = Arco(rIn, a0 + dIn, a1 - dIn, 12, 0);
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < o.Count - 1; i++) Quad(v, t, o[i], o[i + 1], i2[i + 1], i2[i], Vector3.up);
        return Guardar(string.Format("TapaSector_{0:F0}_{1:F0}_{2:F0}_{3:F0}", rIn, rOut, a0, a1), v, t);
    }

    static void Maceta(Transform p, Vector3 pos, float s, Material barro, List<Matrix4x4>[] florPos, List<Matrix4x4> f1)
    {
        var g = Grupo("Maceta", p, pos);
        Prim(PrimitiveType.Cylinder, "Maceta", g, new Vector3(0, 0.28f * s, 0), new Vector3(0.55f, 0.28f, 0.55f) * s, barro, default, true);
        Prim(PrimitiveType.Cylinder, "Borde", g, new Vector3(0, 0.55f * s, 0), new Vector3(0.64f, 0.04f, 0.64f) * s, barro);
        f1.Add(Matrix4x4.TRS(pos + new Vector3(0, 0.75f * s, 0), Quaternion.identity, new Vector3(0.8f, 0.6f, 0.8f) * s));
        for (int i = 0; i < 7; i++)
        {
            float a = R(0, Mathf.PI * 2), r = R(0.05f, 0.3f) * s;
            var fp = pos + new Vector3(Mathf.Cos(a) * r, R(0.85f, 1.0f) * s, Mathf.Sin(a) * r);
            florPos[rng.Next(florPos.Length)].Add(Matrix4x4.TRS(fp, Quaternion.identity, Vector3.one * 0.17f * s));
        }
    }

    static void Conifera(Transform p, Vector3 pos, float s, Material tronco, Material hoja)
    {
        var g = Grupo("Conifera", p, pos);
        Prim(PrimitiveType.Cylinder, "Tronco", g, new Vector3(0, 1.5f * s, 0), new Vector3(0.5f, 1.5f, 0.5f) * s, tronco, default, true);
        var cono = Cono("Cono_Pino", 10);
        float[] ys = { 2.2f, 5.0f, 7.6f, 10.0f };
        float[] rs = { 5.2f, 4.2f, 3.2f, 2.1f };
        float[] hs = { 4.5f, 4.0f, 3.6f, 3.4f };
        for (int i = 0; i < ys.Length; i++)
        {
            var go = new GameObject("Copa_" + i);
            go.transform.SetParent(g, false);
            go.transform.localPosition = new Vector3(0, ys[i] * s, 0);
            go.transform.localScale = new Vector3(rs[i], hs[i], rs[i]) * s;
            go.AddComponent<MeshFilter>().sharedMesh = cono;
            go.AddComponent<MeshRenderer>().sharedMaterial = hoja;
            go.isStatic = true;
        }
    }

    static void ArbolGrande(Transform p, Vector3 pos, float s, Material tronco, List<Matrix4x4> f1, List<Matrix4x4> f2)
    {
        var g = Grupo("Arbol_Grande", p, pos);
        // tronco alto y copa elevada (como en la foto sepia): se ve la plaza por debajo
        Prim(PrimitiveType.Cylinder, "Tronco", g, new Vector3(0, 3.6f * s, 0), new Vector3(0.7f, 3.6f, 0.7f) * s, tronco, default, true);
        Vector3[] off = { new Vector3(0, 8.4f, 0), new Vector3(2.4f, 7.6f, 0.8f), new Vector3(-2.2f, 7.8f, -0.9f),
                          new Vector3(0.6f, 7.4f, -2.3f), new Vector3(-0.8f, 7.5f, 2.2f), new Vector3(0.3f, 9.8f, 0.4f) };
        float[] tam = { 6.0f, 4.4f, 4.6f, 4.2f, 4.3f, 3.8f };
        for (int i = 0; i < off.Length; i++)
            (i % 2 == 0 ? f1 : f2).Add(Matrix4x4.TRS(pos + off[i] * s, Quaternion.Euler(0, R(0, 360), 0), Vector3.one * tam[i] * s));
    }

    // Techo inclinado sobre el anden (cara +Z de la estacion) con mensulas diagonales.
    static void Marquesina(Transform root, Material techo, Material metal)
    {
        var est = Buscar("Estacion");
        float zMuro = 62.87f, xMin = -13.5f, xMax = 13.5f;
        if (est != null)
            foreach (var r in est.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Muros") { zMuro = r.bounds.max.z; xMin = r.bounds.min.x; xMax = r.bounds.max.x; }
        var g = Grupo("Marquesina_Anden", root, Vector3.zero);
        float largo = xMax - xMin + 0.6f, vuelo = 3.0f, yMuro = 4.6f, caida = 0.45f;
        float ang = Mathf.Atan2(caida, vuelo) * Mathf.Rad2Deg;
        Prim(PrimitiveType.Cube, "Techo", g, new Vector3((xMin + xMax) / 2f, yMuro - caida / 2f, zMuro + vuelo / 2f),
             new Vector3(largo, 0.1f, Mathf.Sqrt(vuelo * vuelo + caida * caida)), techo, new Vector3(ang, 0, 0));
        Prim(PrimitiveType.Cube, "Borde", g, new Vector3((xMin + xMax) / 2f, yMuro - caida - 0.08f, zMuro + vuelo),
             new Vector3(largo, 0.22f, 0.06f), metal);
        for (float x = xMin + 0.8f; x <= xMax - 0.5f; x += 2.6f)
        {
            Vector3 a = new Vector3(x, yMuro - 1.4f, zMuro + 0.05f);                  // apoyo en el muro
            Vector3 b = new Vector3(x, yMuro - caida * 0.7f - 0.05f, zMuro + vuelo * 0.7f); // bajo el techo
            var go = Prim(PrimitiveType.Cube, "Mensula", g, (a + b) / 2f, new Vector3(0.09f, 0.09f, Vector3.Distance(a, b)), metal);
            go.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
            Prim(PrimitiveType.Cube, "Soporte", g, new Vector3(x, yMuro - 0.08f, zMuro + vuelo * 0.45f), new Vector3(0.07f, 0.07f, vuelo * 0.9f), metal,
                 new Vector3(ang, 0, 0));
        }
    }

    static GameObject Buscar(string name)
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material m)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        go.isStatic = true;
        return go;
    }

    // Puntos de un rectangulo con esquinas redondeadas (sentido antihorario visto desde arriba).
    static List<Vector3> Contorno(float hw, float hd, float r, float y)
    {
        r = Mathf.Clamp(r, 0.01f, Mathf.Min(hw, hd));
        var c = new[] { new Vector2(hw - r, hd - r), new Vector2(-(hw - r), hd - r), new Vector2(-(hw - r), -(hd - r)), new Vector2(hw - r, -(hd - r)) };
        var pts = new List<Vector3>();
        const int seg = 6;
        for (int k = 0; k < 4; k++)
            for (int s = 0; s <= seg; s++)
            {
                float a = (k * 90f + 90f * s / seg) * Mathf.Deg2Rad;
                pts.Add(new Vector3(c[k].x + Mathf.Cos(a) * r, y, c[k].y + Mathf.Sin(a) * r));
            }
        return pts;
    }

    static readonly Dictionary<string, Mesh> cacheMallas = new Dictionary<string, Mesh>();

    // Anillo de ladrillo (borde de jardinera) con esquinas curvas, de grosor g y alto h.
    static Mesh BordeRedondeado(float w, float d, float r, float g, float h)
    {
        string name = string.Format("Borde_{0:F1}x{1:F1}", w, d);
        var outB = Contorno(w / 2, d / 2, r, 0); var outT = Contorno(w / 2, d / 2, r, h);
        var inB = Contorno(w / 2 - g, d / 2 - g, r - g, 0); var inT = Contorno(w / 2 - g, d / 2 - g, r - g, h);
        var v = new List<Vector3>(); var t = new List<int>();
        int n = outB.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector3 radial = ((outB[i] + outB[j]) * 0.5f); radial.y = 0; radial.Normalize();
            Quad(v, t, outT[i], outT[j], inT[j], inT[i], Vector3.up);        // tapa
            Quad(v, t, outB[i], outB[j], outT[j], outT[i], radial);          // cara exterior
            Quad(v, t, inB[i], inB[j], inT[j], inT[i], -radial);             // cara interior
        }
        return Guardar(name, v, t);
    }

    // Superficie plana con la misma forma (tierra o pasto dentro de la jardinera).
    static Mesh TapaRedondeada(float w, float d, float r)
    {
        string name = string.Format("Tapa_{0:F1}x{1:F1}", w, d);
        var c = Contorno(w / 2, d / 2, r, 0);
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < c.Count; i++) Tri(v, t, Vector3.zero, c[i], c[(i + 1) % c.Count], Vector3.up);
        return Guardar(name, v, t);
    }

    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    { Tri(v, t, a, b, c, normal); Tri(v, t, a, c, d, normal); }

    static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) { var tmp = b; b = c; c = tmp; }
        int k = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(k); t.Add(k + 1); t.Add(k + 2);
    }

    static Mesh Guardar(string name, List<Vector3> v, List<int> t)
    {
        Mesh m;
        if (cacheMallas.TryGetValue(name, out m) && m != null) return m;
        string path = GEN_DIR + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        m = new Mesh { name = name };
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        EnsureDir(GEN_DIR);
        AssetDatabase.CreateAsset(m, path);
        cacheMallas[name] = m;
        return m;
    }

    // ------------------------------------------------------- EpocaManager
    static void Registrar(GameObject jardines)
    {
        GameObject em = null, canales = null, piso2026 = null, arbolitos = null, virgen2026 = null;
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == "EpocaManager") em = go;
            if (go.name == "Virgen") virgen2026 = go;   // la Virgen actual (2026); en la Viuda esta en la gruta
            if (go.name == "Fuentes rectas") canales = go;
            if (go.name == "FuentesPiso") piso2026 = go;
            if (go.name == "COMUN_Entorno") { var t = go.transform.Find("Arboles_Frente"); if (t != null) arbolitos = t.gameObject; }
        }
        if (em == null) { Debug.LogWarning("[Jardines] No encontre EpocaManager"); return; }
        var comp = em.GetComponent("EpocaManager");
        var so = new SerializedObject(comp);
        var list = so.FindProperty("epocas");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            bool es2026 = e.FindPropertyRelative("nombre").stringValue.Contains("2026");
            var act = e.FindPropertyRelative("activar");
            var des = e.FindPropertyRelative("desactivar");
            LimpiarNulos(act); LimpiarNulos(des);
            Agregar(es2026 ? des : act, jardines);
            if (OCULTAR_CANALES_FUERA_DE_2026 && canales != null) Agregar(es2026 ? act : des, canales);
            // solo en 2026: el cuadro de la fuente moderna y los arbolitos actuales del frente
            if (piso2026 != null) Agregar(es2026 ? act : des, piso2026);
            if (arbolitos != null) Agregar(es2026 ? act : des, arbolitos);
            if (virgen2026 != null) Agregar(es2026 ? act : des, virgen2026);
        }
        so.ApplyModifiedProperties();
        int actual = so.FindProperty("epocaActual").intValue;
        var mi = comp.GetType().GetMethod("AplicarEpoca");
        if (mi != null) mi.Invoke(comp, new object[] { actual });
    }

    static void LimpiarNulos(SerializedProperty arr)
    {
        for (int j = arr.arraySize - 1; j >= 0; j--)
            if (arr.GetArrayElementAtIndex(j).objectReferenceValue == null) arr.DeleteArrayElementAtIndex(j);
    }

    static void Agregar(SerializedProperty arr, GameObject go)
    {
        for (int j = 0; j < arr.arraySize; j++) if (arr.GetArrayElementAtIndex(j).objectReferenceValue == go) return;
        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = go;
    }

    // ------------------------------------------------------- utilidades
    static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    static Texture2D PlainTex() { return AssetDatabase.LoadAssetAtPath<Texture2D>(GEN_DIR + "/Adoquin_Plaza.png"); }

    static Material Mat(string name, Color c, float smooth)
    {
        EnsureDir(MAT_DIR);
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", 0f);
        m.enableInstancing = true;
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

    static Transform Grupo(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.isStatic = true;
        return go.transform;
    }

    static Transform Raiz(string name)
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) { Undo.DestroyObjectImmediate(go); break; }
        var r = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(r, "Crear " + name);
        r.isStatic = true;
        return r.transform;
    }

    static GameObject Combinado(string name, Transform parent, List<Matrix4x4> mats, Material m, Mesh baseMesh)
    {
        if (mats.Count == 0) return null;
        var ci = new CombineInstance[mats.Count];
        for (int i = 0; i < mats.Count; i++) { ci[i].mesh = baseMesh; ci[i].transform = mats[i]; }
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
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

    // Icoesfera low-poly (sub 0 = 20 caras, sub 1 = 80 caras), radio 0.5, sombreado plano.
    static Mesh Ico(int sub, string name)
    {
        string path = GEN_DIR + "/" + name + ".asset";
        var ex = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (ex != null) return ex;
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var v = new List<Vector3> {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
        var f = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        for (int s = 0; s < sub; s++)
        {
            var cache = new Dictionary<long, int>();
            var nf = new List<int>();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b, v, cache), bc = Mid(b, c, v, cache), ca = Mid(c, a, v, cache);
                nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            f = nf;
        }
        for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized * 0.5f;
        return GuardarPlano(name, path, v, f, Vector3.zero);
    }

    static int Mid(int a, int b, List<Vector3> v, Dictionary<long, int> cache)
    {
        long k = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
        int i;
        if (cache.TryGetValue(k, out i)) return i;
        v.Add((v[a] + v[b]) * 0.5f);
        cache[k] = v.Count - 1;
        return v.Count - 1;
    }

    // Cono cerrado de altura 1 y radio 0.5 (base en y=0).
    static Mesh Cono(string name, int n)
    {
        string path = GEN_DIR + "/" + name + ".asset";
        var ex = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (ex != null) return ex;
        var v = new List<Vector3> { new Vector3(0, 1, 0), Vector3.zero };
        for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); }
        var f = new List<int>();
        for (int i = 0; i < n; i++)
        {
            int p0 = 2 + i, p1 = 2 + (i + 1) % n;
            f.AddRange(new[] { 0, p1, p0 });   // lado
            f.AddRange(new[] { 1, p0, p1 });   // base
        }
        return GuardarPlano(name, path, v, f, new Vector3(0, 0.3f, 0));
    }

    // Duplica vertices por triangulo (sombreado plano) y corrige caras que miren hacia adentro.
    static Mesh GuardarPlano(string name, string path, List<Vector3> v, List<int> f, Vector3 centro)
    {
        var fv = new List<Vector3>();
        var ft = new List<int>();
        for (int i = 0; i < f.Count; i += 3)
        {
            Vector3 a = v[f[i]], b = v[f[i + 1]], c = v[f[i + 2]];
            Vector3 nrm = Vector3.Cross(b - a, c - a);
            Vector3 hacia = (a + b + c) / 3f - centro;
            if (Vector3.Dot(nrm, hacia) < 0f) { var tmp = b; b = c; c = tmp; }
            int k = fv.Count;
            fv.Add(a); fv.Add(b); fv.Add(c);
            ft.Add(k); ft.Add(k + 1); ft.Add(k + 2);
        }
        var m = new Mesh { name = name };
        m.SetVertices(fv);
        m.SetTriangles(ft, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        EnsureDir(GEN_DIR);
        AssetDatabase.CreateAsset(m, path);
        return m;
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

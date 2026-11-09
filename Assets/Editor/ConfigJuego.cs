#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// =============================================================================
//  Menu: Estacion > Juego > ...
//  1 - Colisiones de edificios  (MeshCollider en las piezas a la altura del jugador)
//  2 - Virgen                   (modelo SantaMaria_1k sobre pedestal, en el lugar del cubo)
//  3 - Interfaz                 (POIs con marcador dorado + panel nuevo + instrucciones)
//  Se pueden volver a correr; reconstruyen lo suyo sin duplicar.
// =============================================================================
public static class ConfigJuego
{
    const string MAT_DIR = "Assets/Art/Materiales/Juego";
    const string ANIO_VIUDA = "1996";   // confirmado por David

    // ---------------------------------------------------------------- 1
    [MenuItem("Estacion/Juego/1 - Colisiones de edificios")]
    public static void Colisiones()
    {
        int n = 0;
        foreach (var nombre in new[] { "Estacion", "Personeria_Antigua" })
        {
            var root = Raiz(nombre);
            if (root == null) continue;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (mr.GetComponent<Collider>() != null) continue;
                var b = mr.bounds;
                if (b.min.y > 2.5f) continue;                                         // fuera del alcance del jugador
                if (Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) < 0.3f) continue; // detalles diminutos
                if (mr.name.StartsWith("Cubierta") || mr.name.StartsWith("Panel_Ovalado")) continue;
                var mc = Undo.AddComponent<MeshCollider>(mr.gameObject);
                mc.sharedMesh = mf.sharedMesh;
                n++;
            }
        }
        foreach (var nombre in new[] { "Fuente 2026", "Fuente viuda" })
        {
            var root = Raiz(nombre);
            if (root == null) continue;
            var viejo = root.transform.Find("Colision");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            var b = Limites(root);
            var go = new GameObject("Colision");
            go.transform.SetParent(root.transform, false);
            go.transform.position = b.center;
            go.transform.rotation = Quaternion.identity;
            var ls = root.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.x, 1f / ls.x);
            var cc = go.AddComponent<CapsuleCollider>();
            cc.direction = 1;
            cc.radius = Mathf.Max(b.size.x, b.size.z) * 0.5f;
            cc.height = Mathf.Max(b.size.y, cc.radius * 2f);
            n++;
        }
        Sucia();
        Debug.Log("<b>[Juego]</b> Colisiones agregadas: " + n);
    }

    // ---------------------------------------------------------------- 2
    [MenuItem("Estacion/Juego/2 - Virgen")]
    public static void Virgen()
    {
        var root = Raiz("Virgen");
        if (root == null) { Debug.LogError("[Juego] No encuentro el objeto 'Virgen'"); return; }
        Vector3 pos = new Vector3(-46.45f, 0f, 60.87f);
        var cubo = root.transform.Find("Cube");
        if (cubo != null) { pos = new Vector3(cubo.position.x, 0f, cubo.position.z); cubo.gameObject.SetActive(false); }
        var viejo = root.transform.Find("Virgen_Estatua");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        var mat = Mat("Virgen_Piedra", Color.white, 0.2f);
        var alb = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Virgen/santa_maria_1k_delighted_albedo.png");
        var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Virgen/santa_maria_1k_normal.png");
        if (alb != null) mat.SetTexture("_BaseMap", alb);
        if (nrm != null) { mat.SetTexture("_BumpMap", nrm); mat.EnableKeyword("_NORMALMAP"); }
        var pedMat = Mat("Virgen_Pedestal", new Color(0.82f, 0.80f, 0.75f), 0.15f);

        var g = new GameObject("Virgen_Estatua");
        g.transform.position = pos;
        // mirar hacia la plaza (frente de la estacion)
        Vector3 hacia = new Vector3(0f, 0f, 45f) - pos; hacia.y = 0f;
        g.transform.rotation = Quaternion.LookRotation(hacia.normalized, Vector3.up);

        var ped = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ped.name = "Pedestal";
        Object.DestroyImmediate(ped.GetComponent<Collider>());
        ped.transform.SetParent(g.transform, false);
        ped.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        ped.transform.localScale = new Vector3(1.3f, 1.0f, 1.3f);
        ped.GetComponent<MeshRenderer>().sharedMaterial = pedMat;

        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Virgen/SantaMaria_1k.fbx"));
        modelo.name = "Estatua";
        modelo.transform.SetParent(g.transform, false);
        modelo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        modelo.transform.localRotation = Quaternion.identity;
        float alto = Limites(modelo).size.y;
        if (alto > 0.01f) modelo.transform.localScale = Vector3.one * (2.0f / alto);   // estatua de 2 m
        foreach (var r in modelo.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;

        var col = g.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.5f, 0f); col.radius = 0.8f; col.height = 3f;
        g.transform.SetParent(root.transform, true);
        foreach (var t in g.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        Undo.RegisterCreatedObjectUndo(g, "Virgen");
        Sucia();
        Debug.Log("<b>[Juego]</b> Virgen colocada en " + pos);
    }

    // ---------------------------------------------------------------- 3
    [MenuItem("Estacion/Juego/3 - Interfaz (POIs + instrucciones)")]
    public static void Interfaz()
    {
        var canvasGO = Raiz("Canvas");
        if (canvasGO == null) { Debug.LogError("[Juego] No encuentro el Canvas"); return; }
        var canvas = canvasGO.transform;
        var pm = canvasGO.GetComponent<POIManager>();

        // apagar la UI vieja de POI (no se borra)
        foreach (var n in new[] { "Indicador", "PanelPoi" }) { var t = canvas.Find(n); if (t != null) t.gameObject.SetActive(false); }
        foreach (var n in new[] { "UI_AvisoPOI", "UI_PanelPOI", "UI_Instrucciones", "UI_BotonAyuda" })
        { var t = canvas.Find(n); if (t != null) Object.DestroyImmediate(t.gameObject); }

        Color navy = new Color(0.08f, 0.11f, 0.18f, 0.96f);
        Color crema = new Color(0.98f, 0.95f, 0.88f);
        Color gris = new Color(0.72f, 0.75f, 0.80f);
        Color terra = new Color(0.86f, 0.47f, 0.29f);

        // ---------- Aviso "Presiona E" ----------
        var aviso = UI("UI_AvisoPOI", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 230), new Vector2(560, 70));
        Img(aviso, new Color(0.08f, 0.11f, 0.18f, 0.88f), true);
        var tecla = UI("Tecla", aviso, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48, 0), new Vector2(52, 46));
        Img(tecla, terra, true);
        Txt(UI("E", tecla, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "E", 30, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Txt(UI("Texto", aviso, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40, 0), new Vector2(-110, 0)),
            "Descubre la historia de este lugar", 24, crema, FontStyles.Normal, TextAlignmentOptions.Center);

        // ---------- Panel POI ----------
        var panel = UI("UI_PanelPOI", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(panel, new Color(0f, 0f, 0f, 0.55f), false);
        var card = UI("Tarjeta", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 560));
        Img(card, navy, true);
        Img(UI("Acento", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -6), new Vector2(-40, 8)), terra, true);

        var marco = UI("MarcoFoto", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, 0), new Vector2(460, 420));
        Img(marco, new Color(0.15f, 0.19f, 0.27f, 1f), true);
        var pend = UI("FotoPendiente", marco, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Txt(pend, "Fotografía histórica\npor agregar", 24, gris, FontStyles.Italic, TextAlignmentOptions.Center);
        var foto = UI("Foto", marco, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20, -20));
        var fotoImg = foto.gameObject.AddComponent<Image>(); fotoImg.preserveAspect = true; fotoImg.raycastTarget = false;

        var badge = UI("Año", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80, 200), new Vector2(150, 46));
        Img(badge, terra, true);
        var yearT = Txt(UI("Texto", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "1926", 26, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        var titleT = Txt(UI("Titulo", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(255, 125), new Vector2(510, 100)),
            "Título", 40, crema, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        var descT = Txt(UI("Descripcion", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(255, -30), new Vector2(510, 210)),
            "Descripción", 23, new Color(0.88f, 0.90f, 0.93f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var srcT = Txt(UI("Fuente", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(255, -175), new Vector2(510, 44)),
            "Fuente", 17, gris, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        Txt(UI("Ayuda", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(255, -240), new Vector2(510, 30)),
            "Esc o X para volver al recorrido", 17, gris, FontStyles.Normal, TextAlignmentOptions.Left);
        var cerrar = UI("Cerrar", card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(52, 52));
        Img(cerrar, new Color(1f, 1f, 1f, 0.12f), true);
        Txt(UI("X", cerrar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "X", 26, crema, FontStyles.Bold, TextAlignmentOptions.Center);
        var btnCerrar = cerrar.gameObject.AddComponent<Button>();
        if (pm != null) UnityEventTools.AddPersistentListener(btnCerrar.onClick, new UnityAction(pm.ClosePanel));

        // ---------- Instrucciones ----------
        var inst = UI("UI_Instrucciones", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(inst, new Color(0.02f, 0.04f, 0.08f, 0.82f), false);
        var ic = UI("Tarjeta", inst, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 720));
        Img(ic, navy, true);
        Img(UI("Acento", ic, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -6), new Vector2(-40, 8)), terra, true);
        Txt(UI("Kicker", ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(900, 30)),
            "RECORRIDO VIRTUAL · CAJICÁ", 20, terra, FontStyles.Bold, TextAlignmentOptions.Center);
        Txt(UI("Titulo", ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 245), new Vector2(950, 70)),
            "La Estación en el Tiempo", 58, crema, FontStyles.Bold, TextAlignmentOptions.Center);
        Txt(UI("Descripcion", ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(900, 90)),
            "Recorre la Estación del Ferrocarril de Cajicá, construida en 1926, y viaja entre sus épocas para descubrir cómo ha cambiado este lugar a lo largo de cien años.",
            24, new Color(0.88f, 0.90f, 0.93f), FontStyles.Normal, TextAlignmentOptions.Center);
        Img(UI("Linea", ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(880, 2)), new Color(1f, 1f, 1f, 0.15f), false);
        Txt(UI("ComoJugar", ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 65), new Vector2(880, 36)),
            "Cómo jugar", 28, crema, FontStyles.Bold, TextAlignmentOptions.Left);
        string[,] filas = {
            { "W A S D", "Caminar (también con las flechas)" },
            { "Clic derecho", "Arrastra para girar la cámara · rueda: zoom" },
            { "E", "Lee la historia en los puntos dorados" },
            { "Botones de abajo", "Viaja entre las épocas de la estación" },
            { "H", "Ver estas instrucciones de nuevo · Esc: cerrar" } };
        for (int i = 0; i < filas.GetLength(0); i++)
        {
            float y = 10 - i * 52;
            var chip = UI("Tecla_" + i, ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320, y), new Vector2(230, 42));
            Img(chip, new Color(1f, 1f, 1f, 0.10f), true);
            Txt(UI("T", chip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), filas[i, 0], 21, crema, FontStyles.Bold, TextAlignmentOptions.Center);
            Txt(UI("Accion_" + i, ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(125, y), new Vector2(600, 42)),
                filas[i, 1], 22, new Color(0.88f, 0.90f, 0.93f), FontStyles.Normal, TextAlignmentOptions.Left);
        }
        var comenzar = UI("Comenzar", ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -290), new Vector2(380, 66));
        Img(comenzar, terra, true);
        Txt(UI("Texto", comenzar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "Comenzar recorrido", 28, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        var btnComenzar = comenzar.gameObject.AddComponent<Button>();

        // boton "?" siempre visible
        var ayuda = UI("UI_BotonAyuda", canvas, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56, -56), new Vector2(64, 64));
        Img(ayuda, new Color(0.08f, 0.11f, 0.18f, 0.85f), true);
        Txt(UI("Texto", ayuda, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "?", 36, crema, FontStyles.Bold, TextAlignmentOptions.Center);
        var btnAyuda = ayuda.gameObject.AddComponent<Button>();

        var pi = canvasGO.GetComponent<PantallaInstrucciones>();
        if (pi == null) pi = canvasGO.AddComponent<PantallaInstrucciones>();
        var so = new SerializedObject(pi); so.FindProperty("panel").objectReferenceValue = inst.gameObject; so.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(btnComenzar.onClick, new UnityAction(pi.Cerrar));
        UnityEventTools.AddPersistentListener(btnAyuda.onClick, new UnityAction(pi.Abrir));

        // el boton de ayuda y las instrucciones quedan encima de todo
        ayuda.SetAsLastSibling(); panel.SetAsLastSibling(); inst.SetAsLastSibling();
        panel.gameObject.SetActive(false);
        aviso.gameObject.SetActive(false);

        // ---------- Conectar POIManager ----------
        if (pm != null)
        {
            pm.indicator = aviso.gameObject;
            pm.panel = panel.gameObject;
            pm.yearText = yearT; pm.titleText = titleT; pm.descriptionText = descT; pm.sourceText = srcT;
            pm.photoImage = fotoImg; pm.fotoPendiente = pend.gameObject;
            EditorUtility.SetDirty(pm);
        }

        Pois();
        Sucia();
        Debug.Log("<b>[Juego]</b> Interfaz lista: POIs, panel e instrucciones.");
    }

    // Activa los POIs, les pone contenido provisional, trigger amplio y marcador dorado.
    static void Pois()
    {
        var pois = Raiz(EscenaUtil.POIS);
        if (pois == null) return;
        pois.SetActive(true);
        var oro = Mat("POI_Dorado", new Color(1f, 0.78f, 0.30f), 0.4f, new Color(1.0f, 0.65f, 0.2f));
        var aro = Mat("POI_Base", new Color(1f, 0.85f, 0.45f), 0.3f, new Color(0.55f, 0.35f, 0.08f));

        foreach (var poi in pois.GetComponentsInChildren<POI>(true))
        {
            string n = poi.name.ToLower();
            if (n.Contains("fuente"))
            {
                poi.title = "El Parque La Estación";
                poi.year = "Hoy";
                poi.description = "La plaza frente a la estación es hoy el Parque La Estación, punto de encuentro de familias, ferias y eventos culturales de Cajicá.";
            }
            else if (n.Contains("personeria"))
            {
                poi.title = "La Personería Municipal";
                poi.year = "Hoy";
                poi.description = "Vecina de la estación, esta edificación de estilo tradicional alberga hoy la Personería Municipal de Cajicá.";
            }
            else
            {
                poi.title = "La Estación del Ferrocarril";
                poi.year = "1926";
                poi.description = "Construida en 1926 con arquitectura republicana, fue la puerta de Cajicá al Ferrocarril de la Sabana. Es Bien de Interés Cultural de la Nación y en 2009 fue restaurada.";
                poi.transform.position = new Vector3(0f, 0f, 49.5f);   // frente a la escalinata
            }
            poi.source = "Texto provisional — por validar con el Instituto de Cultura y Turismo de Cajicá";
            EditorUtility.SetDirty(poi);

            float s = Mathf.Max(0.0001f, poi.transform.lossyScale.x);
            var cap = poi.GetComponent<CapsuleCollider>();
            if (cap != null) { cap.isTrigger = true; cap.radius = 2.4f / s; cap.height = 4f / s; cap.center = new Vector3(0, 1f / s, 0); }

            var viejo = poi.transform.Find("Marcador");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            var m = new GameObject("Marcador");
            m.transform.SetParent(poi.transform, false);
            m.transform.localScale = Vector3.one / s;

            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "Base"; Object.DestroyImmediate(disco.GetComponent<Collider>());
            disco.transform.SetParent(m.transform, false);
            disco.transform.localPosition = new Vector3(0, 0.04f, 0);
            disco.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
            disco.GetComponent<MeshRenderer>().sharedMaterial = aro;

            var gema = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gema.name = "Gema"; Object.DestroyImmediate(gema.GetComponent<Collider>());
            gema.transform.SetParent(m.transform, false);
            gema.transform.localPosition = new Vector3(0, 2.3f, 0);
            gema.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            gema.transform.localScale = Vector3.one * 0.42f;
            gema.GetComponent<MeshRenderer>().sharedMaterial = oro;
            gema.AddComponent<MarcadorPOI>();
        }
    }

    // ---------------------------------------------------------------- 4
    [MenuItem("Estacion/Juego/4 - Selector de epoca (nombres, T, transicion)")]
    public static void Selector()
    {
        var canvasGO = Raiz("Canvas"); var emGO = Raiz("EpocaManager");
        if (canvasGO == null || emGO == null) { Debug.LogError("[Juego] Falta Canvas o EpocaManager"); return; }
        var canvas = canvasGO.transform; var crt = (RectTransform)canvas;
        var sel = canvas.Find("SelectorEpoca");
        var em = emGO.GetComponent<EpocaManager>();
        foreach (var n in new[] { "UI_EtiquetasEpoca", "UI_Destello", "UI_TituloEpoca" })
        { var t = canvas.Find(n); if (t != null) Object.DestroyImmediate(t.gameObject); }

        var ui = sel.GetComponent<SelectorEpocaUI>();
        if (ui == null) ui = sel.gameObject.AddComponent<SelectorEpocaUI>();

        Color crema = new Color(0.98f, 0.95f, 0.88f);
        var cont = UI("UI_EtiquetasEpoca", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        cont.SetSiblingIndex(sel.GetSiblingIndex() + 1);   // debajo de los paneles e instrucciones
        int cant = em.Cantidad;
        var botones = new RectTransform[cant]; var etiquetas = new Image[cant];
        float derecha = -9999f, centroY = 0f;
        for (int i = 0; i < cant; i++)
        {
            string nombre = em.NombreDe(i);
            var b = sel.Find(nombre.Contains("2026") ? "boton 2026" : "boton Viuda") as RectTransform;
            if (b == null) continue;
            botones[i] = b;
            var c = new Vector3[4]; b.GetWorldCorners(c);
            Vector3 mn = crt.InverseTransformPoint(c[0]), mx = crt.InverseTransformPoint(c[2]);
            float cx = (mn.x + mx.x) / 2f; centroY = (mn.y + mx.y) / 2f; derecha = Mathf.Max(derecha, mx.x);
            var et = UI("Etiqueta_" + i, cont, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(cx, mx.y + 34), new Vector2(Mathf.Max(176f, mx.x - mn.x + 30f), 54));
            etiquetas[i] = Img(et, new Color(0.08f, 0.11f, 0.18f, 0.8f), true);
            bool es26 = nombre.Contains("2026");
            string rotulo = "<b>" + (es26 ? "2026" : ANIO_VIUDA) + "</b>\n<size=75%>" + (es26 ? "Actualidad" : "La Viuda de Blanco") + "</size>";
            var tx = Txt(UI("Texto", et, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(-10, -4)), rotulo, 20, crema, FontStyles.Normal, TextAlignmentOptions.Center);
            tx.lineSpacing = -6f;

            var btn = b.GetComponent<Button>();
            for (int k = btn.onClick.GetPersistentEventCount() - 1; k >= 0; k--) UnityEventTools.RemovePersistentListener(btn.onClick, k);
            UnityEventTools.AddIntPersistentListener(btn.onClick, ui.Ir, i);
            EditorUtility.SetDirty(btn);
        }
        var pista = UI("Pista", cont, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(derecha + 130f, centroY), new Vector2(220, 40));
        Txt(pista, "<b>T</b>  ·  cambiar de época", 18, new Color(1f, 1f, 1f, 0.85f), FontStyles.Normal, TextAlignmentOptions.Left);

        var dest = UI("UI_Destello", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var destImg = Img(dest, new Color(1f, 0.96f, 0.88f, 0f), false); destImg.raycastTarget = false;
        var tit = UI("UI_TituloEpoca", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 160));
        var titT = Txt(tit, "Época", 72, new Color(0.30f, 0.18f, 0.10f), FontStyles.Bold, TextAlignmentOptions.Center);
        // debajo del panel de POI y de las instrucciones
        var panelPoi = canvas.Find("UI_PanelPOI");
        int idx = panelPoi != null ? panelPoi.GetSiblingIndex() : canvas.childCount;
        dest.SetSiblingIndex(idx); tit.SetSiblingIndex(idx + 1);

        var so = new SerializedObject(ui);
        so.FindProperty("manager").objectReferenceValue = em;
        var pb = so.FindProperty("botones"); pb.arraySize = cant;
        var pe = so.FindProperty("etiquetas"); pe.arraySize = cant;
        for (int i = 0; i < cant; i++) { pb.GetArrayElementAtIndex(i).objectReferenceValue = botones[i]; pe.GetArrayElementAtIndex(i).objectReferenceValue = etiquetas[i]; }
        // mismo formato para todas las epocas: año + nombre corto
        var pa = so.FindProperty("anios"); pa.arraySize = cant;
        var ps = so.FindProperty("subtitulos"); ps.arraySize = cant;
        for (int i = 0; i < cant; i++)
        {
            bool es2026 = em.NombreDe(i).Contains("2026");
            pa.GetArrayElementAtIndex(i).stringValue = es2026 ? "2026" : ANIO_VIUDA;
            ps.GetArrayElementAtIndex(i).stringValue = es2026 ? "Actualidad" : "La Viuda de Blanco";
        }
        so.FindProperty("destello").objectReferenceValue = destImg;
        so.FindProperty("tituloEpoca").objectReferenceValue = titT;
        so.ApplyModifiedProperties();
        dest.gameObject.SetActive(false); tit.gameObject.SetActive(false);
        Sucia();
        Debug.Log("<b>[Juego]</b> Selector de epoca listo (nombres, T y transicion).");
    }

    // ---------------------------------------------------------------- 5
    [MenuItem("Estacion/Juego/5 - Creditos")]
    public static void Creditos()
    {
        var canvasGO = Raiz("Canvas");
        var inst = canvasGO != null ? canvasGO.transform.Find("UI_Instrucciones") : null;
        if (inst == null) { Debug.LogError("[Juego] Primero corre 3 - Interfaz"); return; }
        var card = inst.Find("Tarjeta");
        foreach (var n in new[] { "BotonCreditos" }) { var t = card.Find(n); if (t != null) Object.DestroyImmediate(t.gameObject); }
        { var t = inst.Find("PanelCreditos"); if (t != null) Object.DestroyImmediate(t.gameObject); }

        Color navy = new Color(0.08f, 0.11f, 0.18f, 0.98f), crema = new Color(0.98f, 0.95f, 0.88f), terra = new Color(0.86f, 0.47f, 0.29f);
        var bc = UI("BotonCreditos", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(410, -290), new Vector2(170, 46));
        Img(bc, new Color(1f, 1f, 1f, 0.10f), true);
        Txt(UI("Texto", bc, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "Créditos", 21, crema, FontStyles.Bold, TextAlignmentOptions.Center);
        var btnC = bc.gameObject.AddComponent<Button>();

        var panel = UI("PanelCreditos", inst, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(panel, new Color(0f, 0f, 0f, 0.6f), false);
        var c2 = UI("Tarjeta", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 620));
        Img(c2, navy, true);
        Img(UI("Acento", c2, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -6), new Vector2(-40, 8)), terra, true);
        Txt(UI("Titulo", c2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 245), new Vector2(860, 60)), "Créditos", 44, crema, FontStyles.Bold, TextAlignmentOptions.Center);
        string cuerpo =
            "<b>Desarrollo</b>\nDavid Mauricio Gutiérrez Acuña — Tecnólogo en Desarrollo de Videojuegos y Entornos Interactivos, SENA\n\n" +
            "<b>Presentado a</b>\nInstituto Municipal de Cultura y Turismo de Cajicá\n\n" +
            "<b>Modelos de terceros</b>\n- Estatua de la Virgen María (low poly): BIGE, Sketchfab. Licencia CC BY (Creative Commons Atribución).\n" +
            "- Personaje: Mini Characters, Kenney (kenney.nl). Licencia CC0.\n\n" +
            "<b>Herramientas</b>\nUnity · Blender\n\n<i>Fotografías y textos históricos: por validar con el Instituto.</i>";
        Txt(UI("Cuerpo", c2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(840, 420)), cuerpo, 21,
            new Color(0.88f, 0.90f, 0.93f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var volver = UI("Volver", c2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -255), new Vector2(240, 56));
        Img(volver, terra, true);
        Txt(UI("Texto", volver, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "Volver", 24, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        var btnV = volver.gameObject.AddComponent<Button>();

        UnityEventTools.AddBoolPersistentListener(btnC.onClick, new UnityAction<bool>(panel.gameObject.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(btnV.onClick, new UnityAction<bool>(panel.gameObject.SetActive), false);
        panel.gameObject.SetActive(false);
        Sucia();
        Debug.Log("<b>[Juego]</b> Creditos agregados a la pantalla de instrucciones.");
    }

    // ---------------------------------------------------------------- 6
    [MenuItem("Estacion/Juego/6 - Quitar scripts faltantes")]
    public static void Limpiar()
    {
        int n = 0;
        foreach (var root in EscenaUtil.Raices())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                int k = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (k > 0) { Undo.RegisterCompleteObjectUndo(t.gameObject, "Quitar faltantes"); GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject); n += k; }
            }
        Sucia();
        Debug.Log("<b>[Juego]</b> Componentes con script faltante quitados: " + n);
    }

    // ---------------------------------------------------------- utilidades
    static RectTransform UI(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image Img(RectTransform rt, Color c, bool redondeado)
    {
        var im = rt.gameObject.AddComponent<Image>();
        im.color = c;
        if (redondeado)
        {
            im.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            im.type = Image.Type.Sliced;
            im.pixelsPerUnitMultiplier = 0.3f;
        }
        return im;
    }

    static TextMeshProUGUI Txt(RectTransform rt, string s, float size, Color c, FontStyles st, TextAlignmentOptions al)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s; t.fontSize = size; t.color = c; t.fontStyle = st; t.alignment = al;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    static Material Mat(string name, Color c, float smooth, Color? emis = null)
    {
        if (!AssetDatabase.IsValidFolder(MAT_DIR)) AssetDatabase.CreateFolder("Assets/Art/Materiales", "Juego");
        string path = MAT_DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        if (emis.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Raiz(string name)
    {
        foreach (var go in EscenaUtil.Raices())
            if (go.name == name) return go;
        return null;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Sucia()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
}
#endif

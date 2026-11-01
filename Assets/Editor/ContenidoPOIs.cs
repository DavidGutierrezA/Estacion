#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// =============================================================================
//  CONTENIDO DE LOS PUNTOS DE INTERES (texto + foto)
//  Menu: Estacion > Juego > 8 - Contenido de los POIs
//
//  Crea/actualiza los POIs dentro de "Pois" (nombres POI_...), con su texto,
//  su foto (Assets/Art/POIs/*.jpg) y su marcador dorado. Los que dependen de la
//  epoca se registran en el EpocaManager. Los POIs de prueba anteriores
//  ("Poi Prueba...") se DESACTIVAN, no se borran.
//  Para cambiar un texto: edita la tabla de abajo y vuelve a correr el menu
//  (o editalo directo en el Inspector del POI).
//  Los textos salen de las placas fotografiadas por David (placa historica de la
//  estacion y placa del monumento, Alcaldia de Cajica) — validar con el Instituto.
// =============================================================================
public static class ContenidoPOIs
{
    const string FOTOS = "Assets/Art/POIs/";
    enum Epoca { Ambas, Solo2026, SoloViuda }

    class Dato
    {
        public string nombre, anio, titulo, texto, fuente, foto;
        public Vector3 pos;
        public Epoca epoca;
    }

    static List<Dato> Tabla()
    {
        const string PLACA = "Fuente: placa histórica «Estación del Tren de Cajicá», Alcaldía de Cajicá.";
        return new List<Dato> {
            new Dato { nombre = "POI_Estacion_2026", epoca = Epoca.Solo2026, pos = new Vector3(0f, 0f, 47.5f),
                anio = "1926", titulo = "La Estación del Tren de Cajicá",
                texto = "En 1925 el Ministerio de Obras Públicas construyó la estación, con sala de espera, expendio de tiquetes, bodega de equipaje, dormitorio para guardias y vivienda para el jefe de estación. Fue inaugurada en 1926, siendo presidente el general Pedro Nel Ospina. En 1994 fue declarada Bien de Interés Cultural de Carácter Nacional y entre 2008 y 2010 fue restaurada.",
                fuente = PLACA, foto = "POI_Estacion_2026.jpg" },
            new Dato { nombre = "POI_Estacion_Viuda", epoca = Epoca.SoloViuda, pos = new Vector3(0f, 0f, 47.5f),
                anio = "1996", titulo = "La estación amarilla y azul",
                texto = "En 1993 la estación se convirtió en el centro de cultura del municipio, con una gran celebración popular a la que asistieron directivas del Tren Turístico de la Sabana, grupos musicales y danzas, con apoyo de Colcultura. Al año siguiente, en 1994, fue declarada Bien de Interés Cultural de Carácter Nacional por su valor histórico y arquitectónico.",
                fuente = PLACA, foto = "POI_Estacion_Viuda.jpg" },
            new Dato { nombre = "POI_Plaza_2026", epoca = Epoca.Solo2026, pos = new Vector3(-41.6f, 0f, 37.6f),
                anio = "Hoy", titulo = "La plaza de la estación",
                texto = "La plaza frente a la estación es hoy un espacio abierto de encuentro, con su fuente, mesas bajo sombrillas y los canales de agua frente a la fachada. Alrededor se reúnen las familias, las ferias y los eventos culturales del municipio.",
                fuente = "Texto del proyecto — por validar con el Instituto de Cultura y Turismo de Cajicá.", foto = "POI_Plaza_2026.jpg" },
            new Dato { nombre = "POI_Plaza_Viuda", epoca = Epoca.SoloViuda, pos = new Vector3(0.6f, 0f, 33.0f),
                anio = "1996", titulo = "La fuente y los jardines",
                texto = "En las fotografías antiguas la plaza tenía una fuente de dos platos sobre una base escalonada, rodeada de jardineras con borde de ladrillo, pasto, arbustos y árboles grandes. Un camino recto llevaba desde la calle hasta la escalinata de la estación.",
                fuente = "Reconstrucción a partir de fotografías antiguas de la plaza.", foto = "POI_Plaza_Viuda.jpg" },
            new Dato { nombre = "POI_Personeria", epoca = Epoca.Ambas, pos = new Vector3(62.9f, 1.28f, 51.4f),
                anio = "Hoy", titulo = "La Personería Municipal",
                texto = "Este edificio vecino, levantado sobre una plataforma de piedra con escaleras y baranda, alberga hoy la Personería Municipal de Cajicá. La placa histórica cuenta que el conjunto de la estación está compuesto por dos edificios de rasgos republicanos y que en 1950 se construyó el segundo, destinado a almacenar los productos de la región.",
                fuente = PLACA, foto = "POI_Personeria.jpg" },
            new Dato { nombre = "POI_Virgen_Viuda", epoca = Epoca.SoloViuda, pos = new Vector3(-25.9f, 0f, 62.0f),
                anio = "Años 70", titulo = "La Virgen del Carmen",
                texto = "La imagen de la Virgen del Carmen fue colocada por iniciativa de los ferroviarios en los años setenta. En esta época estaba en una gruta azul con nicho blanco, al costado de la estación, rodeada de jardineras.",
                fuente = PLACA, foto = "POI_Virgen_Viuda.jpg" },
            new Dato { nombre = "POI_Virgen_2026", epoca = Epoca.Solo2026, pos = new Vector3(-43.4f, 0f, 57.1f),
                anio = "Años 70", titulo = "La Virgen del Carmen",
                texto = "La imagen de la Virgen del Carmen fue colocada por iniciativa de los ferroviarios en los años setenta y sigue siendo parte de la estación. En la época de La Viuda de Blanco estaba en una gruta azul junto al andén, como muestra esta fotografía.",
                fuente = PLACA, foto = "POI_Virgen_Viuda.jpg" },
            new Dato { nombre = "POI_Monumento_Paz", epoca = Epoca.Solo2026, pos = new Vector3(-38f, 0f, 64.0f),
                anio = "2025", titulo = "Monumento a la paz y la reconciliación",
                texto = "Las alas representan la esperanza y la libertad; las armas depuestas, la decisión de optar por el diálogo y la tolerancia; y las caretas, el papel del arte para transformar el dolor y el conflicto en expresión y unión.",
                fuente = "Fuente: placa del monumento, Alcaldía de Cajicá · Secretaría de Seguridad y Convivencia, 2025.", foto = "POI_Monumento_Paz.jpg" },
            new Dato { nombre = "POI_Relieve_Tren", epoca = Epoca.Solo2026, pos = new Vector3(37.0f, 0f, 62.2f),
                anio = "Hoy", titulo = "El relieve del tren",
                texto = "Este relieve en bronce cuenta la historia del lugar: la locomotora y su vagón, una pareja de viajeros con su maleta y la fachada de la estación, bajo las montañas de la Sabana. A su lado, dos señoras sentadas esperan el tren. Por detrás está la placa con la historia de la estación.",
                fuente = "Texto del proyecto — por validar con el Instituto de Cultura y Turismo de Cajicá.", foto = "POI_Relieve_Tren.jpg" },
        };
    }

    [MenuItem("Estacion/Juego/8 - Contenido de los POIs (texto y fotos)")]
    public static void Construir()
    {
        GameObject pois = null, em = null;
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == "Pois") pois = go;
            if (go.name == "EpocaManager") em = go;
        }
        if (pois == null) { Debug.LogError("[POIs] No encuentro 'Pois'"); return; }
        pois.SetActive(true);

        // desactivar los POIs de prueba
        foreach (var p in pois.GetComponentsInChildren<POI>(true))
            if (!p.name.StartsWith("POI_")) { Undo.RecordObject(p.gameObject, "POI viejo"); p.gameObject.SetActive(false); }

        var oro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Juego/POI_Dorado.mat");
        var aro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materiales/Juego/POI_Base.mat");
        var solo2026 = new List<GameObject>(); var soloViuda = new List<GameObject>();
        int sinFoto = 0;

        foreach (var d in Tabla())
        {
            var t = pois.transform.Find(d.nombre);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(d.nombre);
                Undo.RegisterCreatedObjectUndo(go, "POI");
                go.transform.SetParent(pois.transform, false);
            }
            else go = t.gameObject;
            go.SetActive(true);
            go.transform.position = d.pos;
            go.transform.rotation = Quaternion.identity;
            var ls = pois.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);

            var cap = go.GetComponent<CapsuleCollider>(); if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true; cap.radius = 2.4f; cap.height = 4f; cap.center = new Vector3(0, 1f, 0);
            var poi = go.GetComponent<POI>(); if (poi == null) poi = go.AddComponent<POI>();
            poi.year = d.anio; poi.title = d.titulo; poi.description = d.texto; poi.source = d.fuente;
            poi.photo = Foto(d.foto);
            if (poi.photo == null) sinFoto++;
            EditorUtility.SetDirty(poi);

            Marcador(go.transform, oro, aro);
            if (d.epoca == Epoca.Solo2026) solo2026.Add(go);
            if (d.epoca == Epoca.SoloViuda) soloViuda.Add(go);
        }

        if (em != null) Registrar(em, solo2026, soloViuda);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pois.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[POIs]</b> " + Tabla().Count + " POIs con contenido (" + sinFoto + " sin foto).");
    }

    static Sprite Foto(string archivo)
    {
        if (string.IsNullOrEmpty(archivo)) return null;
        string path = FOTOS + archivo;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        if (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 1024;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Marcador(Transform poi, Material oro, Material aro)
    {
        var viejo = poi.Find("Marcador");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        var m = new GameObject("Marcador");
        m.transform.SetParent(poi, false);

        var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disco.name = "Base"; Object.DestroyImmediate(disco.GetComponent<Collider>());
        disco.transform.SetParent(m.transform, false);
        disco.transform.localPosition = new Vector3(0, 0.04f, 0);
        disco.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
        if (aro != null) disco.GetComponent<MeshRenderer>().sharedMaterial = aro;

        var gema = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gema.name = "Gema"; Object.DestroyImmediate(gema.GetComponent<Collider>());
        gema.transform.SetParent(m.transform, false);
        gema.transform.localPosition = new Vector3(0, 2.3f, 0);
        gema.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        gema.transform.localScale = Vector3.one * 0.42f;
        if (oro != null) gema.GetComponent<MeshRenderer>().sharedMaterial = oro;
        gema.AddComponent<MarcadorPOI>();
    }

    static void Registrar(GameObject emGO, List<GameObject> solo2026, List<GameObject> soloViuda)
    {
        var comp = emGO.GetComponent("EpocaManager");
        var so = new SerializedObject(comp);
        var list = so.FindProperty("epocas");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            bool es2026 = e.FindPropertyRelative("nombre").stringValue.Contains("2026");
            var act = e.FindPropertyRelative("activar");
            var des = e.FindPropertyRelative("desactivar");
            foreach (var g in solo2026) { Agregar(es2026 ? act : des, g); Quitar(es2026 ? des : act, g); }
            foreach (var g in soloViuda) { Agregar(es2026 ? des : act, g); Quitar(es2026 ? act : des, g); }
        }
        so.ApplyModifiedProperties();
        var mi = comp.GetType().GetMethod("AplicarEpoca");
        if (mi != null) mi.Invoke(comp, new object[] { so.FindProperty("epocaActual").intValue });
    }

    static void Agregar(SerializedProperty arr, GameObject go)
    {
        for (int j = arr.arraySize - 1; j >= 0; j--)
        {
            var o = arr.GetArrayElementAtIndex(j).objectReferenceValue;
            if (o == null) arr.DeleteArrayElementAtIndex(j); else if (o == go) return;
        }
        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = go;
    }

    static void Quitar(SerializedProperty arr, GameObject go)
    {
        for (int j = arr.arraySize - 1; j >= 0; j--)
            if (arr.GetArrayElementAtIndex(j).objectReferenceValue == go) arr.DeleteArrayElementAtIndex(j);
    }
}
#endif

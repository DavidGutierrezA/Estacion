#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EscenaUtil
{
    public const string SISTEMA = "_SISTEMA";
    public const string JUGADOR = "_JUGADOR";
    public const string COMUN = "_COMUN";
    public const string EPOCA_2026 = "_EPOCA_2026";
    public const string EPOCA_VIUDA = "_EPOCA_VIUDA";
    public const string POIS = "_POIS";
    const string PREF_AUTO = "EstacionAutoOrdenar";
    const string MENU_AUTO = "Estacion/Escena/Auto-ordenar al construir";

    static readonly string[] GRUPOS = { SISTEMA, JUGADOR, COMUN, EPOCA_2026, EPOCA_VIUDA };
    static readonly string[] ORDEN = { SISTEMA, JUGADOR, COMUN, EPOCA_2026, EPOCA_VIUDA, POIS };

    static readonly Dictionary<string, string> DESTINO = new Dictionary<string, string>
    {
        { "Camera", SISTEMA }, { "CinemachineCamera", SISTEMA }, { "Iluminacion", SISTEMA }, { "EpocaManager", SISTEMA }, { "Canvas", SISTEMA },
        { "Jugador", JUGADOR },
        { "Estacion", COMUN }, { "Suelo_Oeste", COMUN }, { "Suelo_Este", COMUN }, { "Personeria_Nueva", COMUN }, { "Personeria_Antigua", COMUN },
        { "Fuente 2026", EPOCA_2026 }, { "Fuentes_Rectas", EPOCA_2026 }, { "Fuentes_Piso", EPOCA_2026 }, { "Virgen", EPOCA_2026 },
        { "Estatua_Tren", EPOCA_2026 }, { "Monumento_Paz", EPOCA_2026 }, { "Ubicacion monumento paz", EPOCA_2026 },
        { "Fuente viuda", EPOCA_VIUDA }, { "Ubicacion virgen viuda", EPOCA_VIUDA },
    };

    static bool ocupado;

    static EscenaUtil() { EditorApplication.hierarchyChanged += AlCambiar; }

    public static bool EsGrupo(GameObject go)
    {
        return go != null && go.transform.parent == null && System.Array.IndexOf(GRUPOS, go.name) >= 0;
    }

    public static GameObject[] Raices()
    {
        var res = new List<GameObject>();
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            res.Add(go);
            if (EsGrupo(go)) foreach (Transform t in go.transform) res.Add(t.gameObject);
        }
        return res.ToArray();
    }

    public static GameObject Raiz(string nombre)
    {
        foreach (var go in Raices()) if (go.name == nombre) return go;
        return null;
    }

    public static string GrupoDe(string nombre)
    {
        string g;
        if (DESTINO.TryGetValue(nombre, out g)) return g;
        if (nombre.StartsWith("COMUN_")) return COMUN;
        if (nombre.StartsWith("EPOCA_2026")) return EPOCA_2026;
        if (nombre.StartsWith("EPOCA_Viuda")) return EPOCA_VIUDA;
        return null;
    }

    static GameObject Grupo(Scene escena, string nombre, bool crear, bool conUndo)
    {
        foreach (var go in escena.GetRootGameObjects()) if (go.name == nombre) return go;
        if (!crear) return null;
        var g = new GameObject(nombre);
        if (conUndo) Undo.RegisterCreatedObjectUndo(g, "Crear " + nombre);
        return g;
    }

    public static int Ordenar(bool conUndo)
    {
        var escena = SceneManager.GetActiveScene();
        int n = 0;
        foreach (var go in escena.GetRootGameObjects())
        {
            if (EsGrupo(go) || go.name == POIS) continue;
            var destino = GrupoDe(go.name);
            if (destino == null) continue;
            var g = Grupo(escena, destino, true, conUndo);
            if (conUndo) Undo.SetTransformParent(go.transform, g.transform, "Ordenar jerarquia");
            else go.transform.SetParent(g.transform, true);
            n++;
        }
        int i = 0;
        foreach (var nombre in ORDEN)
        {
            var g = Grupo(escena, nombre, false, false);
            if (g != null && g.transform.GetSiblingIndex() != i) g.transform.SetSiblingIndex(i);
            if (g != null) i++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(escena);
        return n;
    }

    static void AlCambiar()
    {
        if (ocupado || EditorApplication.isPlayingOrWillChangePlaymode || !EditorPrefs.GetBool(PREF_AUTO, true)) return;
        var escena = SceneManager.GetActiveScene();
        if (!escena.IsValid() || !escena.isLoaded) return;
        bool hayGrupos = false;
        foreach (var go in escena.GetRootGameObjects()) if (EsGrupo(go)) { hayGrupos = true; break; }
        if (!hayGrupos) return;
        ocupado = true;
        try { Ordenar(false); } finally { ocupado = false; }
    }

    [MenuItem("Estacion/Escena/Ordenar jerarquia en carpetas")]
    static void MenuOrdenar()
    {
        int n = Ordenar(true);
        Debug.Log("<b>[Escena]</b> " + n + " objetos movidos a sus carpetas.");
    }

    [MenuItem("Estacion/Escena/Sacar todo de las carpetas")]
    static void MenuDesagrupar()
    {
        EditorPrefs.SetBool(PREF_AUTO, false);
        var escena = SceneManager.GetActiveScene();
        foreach (var go in escena.GetRootGameObjects())
        {
            if (!EsGrupo(go)) continue;
            var hijos = new List<Transform>();
            foreach (Transform t in go.transform) hijos.Add(t);
            foreach (var t in hijos) Undo.SetTransformParent(t, null, "Sacar de carpetas");
            Undo.DestroyObjectImmediate(go);
        }
        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("<b>[Escena]</b> Jerarquia plana otra vez. Auto-ordenar desactivado.");
    }

    [MenuItem(MENU_AUTO)]
    static void MenuAuto() { EditorPrefs.SetBool(PREF_AUTO, !EditorPrefs.GetBool(PREF_AUTO, true)); }

    [MenuItem(MENU_AUTO, true)]
    static bool MenuAutoValidar()
    {
        Menu.SetChecked(MENU_AUTO, EditorPrefs.GetBool(PREF_AUTO, true));
        return true;
    }
}
#endif

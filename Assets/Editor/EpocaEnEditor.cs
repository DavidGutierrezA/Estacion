#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// =============================================================================
//  CAMBIAR DE EPOCA SIN DARLE PLAY
//  Menu: Estacion > Epoca > Ver 2026  /  Ver La Viuda de Blanco
//  Atajos: Ctrl+Alt+1 (2026) y Ctrl+Alt+2 (Viuda).
//  Hace lo mismo que el selector del juego (activa/oculta objetos y cambia
//  materiales). Antes de guardar conviene volver a 2026, que es la epoca
//  con la que arranca el recorrido.
// =============================================================================
public static class EpocaEnEditor
{
    [MenuItem("Estacion/Epoca/Ver 2026 %&1")]
    static void Ver2026() { Ver(true); }

    [MenuItem("Estacion/Epoca/Ver La Viuda de Blanco %&2")]
    static void VerViuda() { Ver(false); }

    static void Ver(bool es2026)
    {
        var em = Object.FindFirstObjectByType<EpocaManager>(FindObjectsInactive.Include);
        if (em == null) { Debug.LogWarning("[Epoca] No hay EpocaManager en la escena."); return; }
        for (int i = 0; i < em.Cantidad; i++)
        {
            if (em.NombreDe(i).Contains("2026") != es2026) continue;
            em.AplicarEpoca(i);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(em.gameObject.scene);
            Debug.Log("<b>[Epoca]</b> Mostrando: " + em.NombreDe(i));
            return;
        }
    }

    // solo disponible fuera de Play (en Play se usa la tecla T o los botones)
    [MenuItem("Estacion/Epoca/Ver 2026 %&1", true)]
    [MenuItem("Estacion/Epoca/Ver La Viuda de Blanco %&2", true)]
    static bool Valido() { return !EditorApplication.isPlaying; }
}
#endif

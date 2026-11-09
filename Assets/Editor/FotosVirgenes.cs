#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FotosVirgenes
{
    static readonly string[,] PARES =
    {
        { "POI_Virgen_2026", "Assets/Art/POIs/POI_Virgen_2026.jpg" },
        { "POI_Virgen_Viuda", "Assets/Art/POIs/POI_Virgen_Viuda_Santuario.jpg" },
    };

    [MenuItem("Estacion/Juego/10 - Fotos de las Virgenes")]
    public static void Asignar()
    {
        var pois = EscenaUtil.Raiz(EscenaUtil.POIS);
        if (pois == null) { Debug.LogError("[POIs] No encuentro " + EscenaUtil.POIS); return; }
        int ok = 0;
        for (int i = 0; i < PARES.GetLength(0); i++)
        {
            string nombre = PARES[i, 0], path = PARES[i, 1];
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[POIs] Falta la foto " + path); continue; }
            if (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled || imp.maxTextureSize != 1024)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var t = pois.transform.Find(nombre);
            var poi = t != null ? t.GetComponent<POI>() : null;
            if (poi == null || sprite == null) { Debug.LogWarning("[POIs] No pude asignar " + nombre); continue; }
            Undo.RecordObject(poi, "Foto " + nombre);
            poi.photo = sprite;
            EditorUtility.SetDirty(poi);
            ok++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pois.scene);
        Debug.Log("<b>[POIs]</b> Fotos de las Virgenes asignadas: " + ok + " de " + PARES.GetLength(0) + ".");
    }
}
#endif

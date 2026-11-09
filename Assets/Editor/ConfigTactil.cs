#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// =============================================================================
//  CONTROLES PARA CELULAR     Menu: Estacion > Juego > 9 - Controles para celular
//  Crea en el Canvas:
//   - UI_ControlesTactiles: joystick abajo a la izquierda (oculto en PC).
//   - UI_BotonTactil: boton "Táctil: SÍ/NO" junto al "?" para activarlos a mano.
//   - El aviso dorado de los POIs se vuelve tocable (abre la historia).
//   - Una linea en las instrucciones explicando los controles del celular.
//  Se activan solos si el juego se abre desde un celular o tablet.
//  Se puede volver a correr (reemplaza lo anterior).
// =============================================================================
public static class ConfigTactil
{
    [MenuItem("Estacion/Juego/9 - Controles para celular")]
    public static void Construir()
    {
        GameObject canvasGO = null;
        foreach (var go in EscenaUtil.Raices())
            if (go.name == "Canvas") canvasGO = go;
        if (canvasGO == null) { Debug.LogError("[Tactil] No encuentro el Canvas"); return; }
        var canvas = canvasGO.transform;
        foreach (var n in new[] { "UI_ControlesTactiles", "UI_BotonTactil" })
        { var t = canvas.Find(n); if (t != null) Undo.DestroyObjectImmediate(t.gameObject); }

        var circulo = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var redondeado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Color crema = new Color(0.98f, 0.95f, 0.88f);
        Color navy = new Color(0.08f, 0.11f, 0.18f, 0.85f);

        // ---------- contenedor + joystick ----------
        var raiz = UI("UI_ControlesTactiles", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Controles tactiles");
        var ctrl = raiz.gameObject.AddComponent<ControlesTactiles>();

        var joy = UI("Joystick", raiz, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(230, 250), new Vector2(380, 380));
        var zona = joy.gameObject.AddComponent<Image>(); zona.color = new Color(1, 1, 1, 0.001f);   // area tactil grande e invisible
        var fondo = UI("Fondo", joy, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 240));
        var imgF = fondo.gameObject.AddComponent<Image>(); imgF.sprite = circulo; imgF.color = new Color(0.08f, 0.11f, 0.18f, 0.45f); imgF.raycastTarget = false;
        var aro = UI("Aro", fondo, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14, -14));
        var imgA = aro.gameObject.AddComponent<Image>(); imgA.sprite = circulo; imgA.color = new Color(1f, 1f, 1f, 0.08f); imgA.raycastTarget = false;
        var perilla = UI("Perilla", fondo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
        var imgP = perilla.gameObject.AddComponent<Image>(); imgP.sprite = circulo; imgP.color = new Color(0.86f, 0.47f, 0.29f, 0.9f); imgP.raycastTarget = false;
        var js = joy.gameObject.AddComponent<JoystickVirtual>();
        var soJ = new SerializedObject(js);
        soJ.FindProperty("fondo").objectReferenceValue = fondo;
        soJ.FindProperty("perilla").objectReferenceValue = perilla;
        soJ.ApplyModifiedProperties();
        var ayudaJoy = Txt(UI("Ayuda", joy, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(380, 30)),
                           "Caminar  ·  desliza la pantalla para mirar", 18, new Color(1f, 1f, 1f, 0.75f));
        ayudaJoy.raycastTarget = false;

        // ---------- boton Táctil SÍ/NO (junto al "?") ----------
        var boton = UI("UI_BotonTactil", canvas, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-196, -56), new Vector2(190, 64));
        Undo.RegisterCreatedObjectUndo(boton.gameObject, "Boton tactil");
        var imgB = boton.gameObject.AddComponent<Image>(); imgB.sprite = redondeado; imgB.type = Image.Type.Sliced; imgB.color = navy;
        var txtB = Txt(UI("Texto", boton, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "Táctil: NO", 22, crema);
        var btn = boton.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(ctrl.Alternar));

        // ---------- aviso del POI tocable ----------
        TMP_Text textoAviso = null; GameObject tecla = null;
        var aviso = canvas.Find("UI_AvisoPOI");
        var pm = canvasGO.GetComponent<POIManager>();
        if (aviso != null)
        {
            var img = aviso.GetComponent<Image>(); if (img != null) img.raycastTarget = true;
            var b = aviso.GetComponent<Button>(); if (b == null) b = Undo.AddComponent<Button>(aviso.gameObject);
            for (int k = b.onClick.GetPersistentEventCount() - 1; k >= 0; k--) UnityEventTools.RemovePersistentListener(b.onClick, k);
            if (pm != null) UnityEventTools.AddPersistentListener(b.onClick, new UnityAction(pm.Interactuar));
            var tt = aviso.Find("Texto"); if (tt != null) textoAviso = tt.GetComponent<TMP_Text>();
            var te = aviso.Find("Tecla"); if (te != null) tecla = te.gameObject;
        }

        // ---------- pista "T · cambiar de época" (se oculta en celular) ----------
        GameObject pista = null;
        var etiquetas = canvas.Find("UI_EtiquetasEpoca");
        if (etiquetas != null) { var p = etiquetas.Find("Pista"); if (p != null) pista = p.gameObject; }

        // ---------- linea en las instrucciones ----------
        var tarjeta = canvas.Find("UI_Instrucciones/Tarjeta");
        if (tarjeta != null)
        {
            var vieja = tarjeta.Find("NotaCelular"); if (vieja != null) Object.DestroyImmediate(vieja.gameObject);
            var nota = Txt(UI("NotaCelular", tarjeta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -238), new Vector2(900, 30)),
                "En celular o tablet: joystick para caminar · desliza para mirar · pellizca para zoom · toca el aviso dorado",
                18, new Color(0.98f, 0.80f, 0.55f));
            nota.raycastTarget = false;
        }

        var so = new SerializedObject(ctrl);
        so.FindProperty("joystick").objectReferenceValue = joy.gameObject;
        so.FindProperty("textoBoton").objectReferenceValue = txtB;
        so.FindProperty("textoAviso").objectReferenceValue = textoAviso;
        so.FindProperty("teclaAviso").objectReferenceValue = tecla;
        so.FindProperty("pistaTeclado").objectReferenceValue = pista;
        so.ApplyModifiedProperties();

        // orden: debajo de paneles e instrucciones (para que no los tapen)
        var panelPoi = canvas.Find("UI_PanelPOI");
        int idx = panelPoi != null ? panelPoi.GetSiblingIndex() : canvas.childCount - 1;
        raiz.SetSiblingIndex(idx);
        var ayuda = canvas.Find("UI_BotonAyuda");
        boton.SetSiblingIndex(ayuda != null ? ayuda.GetSiblingIndex() : idx + 1);
        joy.gameObject.SetActive(false);   // en el editor queda apagado; en el juego decide ControlesTactiles

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("<b>[Tactil]</b> Controles para celular listos (joystick, boton Táctil, aviso tocable).");
    }

    static RectTransform UI(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static TextMeshProUGUI Txt(RectTransform rt, string texto, float tam, Color c)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = tam; t.color = c; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        return t;
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

// =============================================================================
//  Configuracion del jugador y la camara — Menu: Estacion > Jugador > ...
//  1 - Personaje: pone character-female-b (Kenney, CC0) como modelo del jugador,
//      con Animator (idle / walk / sprint segun la velocidad).
//      Si existe Assets/Art/Personaje/colormap.png, se usa como textura.
//  2 - Camara orbital: cambia CinemachineFollow por OrbitalFollow + RotationComposer
//      + CamaraOrbital (arrastrar para girar, rueda para zoom).
//  Se pueden volver a correr cuantas veces quieras.
// =============================================================================
public static class ConfigJugador
{
    const string MODELO = "Assets/Art/Personaje/character-female-b.fbx";
    const string TEXTURA = "Assets/Art/Personaje/colormap.png";
    const string MATERIAL = "Assets/Art/Personaje/Personaje_Kenney.mat";
    const string CONTROLLER = "Assets/Art/Personaje/Jugador.controller";
    const float ALTURA = 1.45f;   // altura del personaje en metros

    [MenuItem("Estacion/Jugador/1 - Personaje (modelo + animaciones)")]
    public static void Personaje()
    {
        // ---- 1. Importacion: escala correcta y animaciones en bucle ----
        var imp = (ModelImporter)AssetImporter.GetAtPath(MODELO);
        if (imp == null) { Debug.LogError("[Jugador] No encuentro " + MODELO); return; }
        var temp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELO));
        float alturaActual = Altura(temp);
        Object.DestroyImmediate(temp);
        if (alturaActual > 0.01f) imp.globalScale = imp.globalScale * ALTURA / alturaActual;
        var clips = imp.clipAnimations;
        if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
        foreach (var c in clips)
            if (c.name == "idle" || c.name == "walk" || c.name == "sprint") c.loopTime = true;
        imp.clipAnimations = clips;
        imp.SaveAndReimport();

        // ---- 2. Animator Controller: blend idle -> walk -> sprint por "Velocidad" ----
        AssetDatabase.DeleteAsset(CONTROLLER);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        BlendTree tree;
        ctrl.CreateBlendTreeInController("Locomocion", out tree, 0);
        tree.blendParameter = "Velocidad";
        tree.blendType = BlendTreeType.Simple1D;
        tree.useAutomaticThresholds = false;
        tree.AddChild(Clip("idle"), 0f);
        tree.AddChild(Clip("walk"), 2.5f);
        tree.AddChild(Clip("sprint"), 6.5f);
        EditorUtility.SetDirty(ctrl);

        // ---- 3. Material (con colormap.png si existe) ----
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, MATERIAL); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURA);
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", tex != null ? Color.white : new Color(0.95f, 0.90f, 0.86f));
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);

        // ---- 4. Jugador en escena ----
        var player = GameObject.Find("player");
        if (player == null) { Debug.LogError("[Jugador] No encuentro 'player'"); return; }
        Undo.RegisterFullObjectHierarchyUndo(player, "Configurar jugador");

        var mr = player.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;   // capsula invisible
        foreach (Transform ch in player.transform)
            if (ch.name.StartsWith("Sphere")) ch.gameObject.SetActive(false);                 // cabeza/ojos viejos

        var viejo = player.transform.Find("Modelo");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELO), player.transform);
        modelo.name = "Modelo";
        var cap = player.GetComponent<CapsuleCollider>();
        float pie = cap != null ? cap.center.y - cap.height / 2f : -1f;
        modelo.transform.localPosition = new Vector3(0f, pie, 0f);
        modelo.transform.localRotation = Quaternion.identity;
        var s = player.transform.lossyScale;
        modelo.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
        foreach (var r in modelo.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;

        var anim = modelo.GetComponent<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var aj = player.GetComponent<AnimacionJugador>();
        if (aj == null) aj = player.AddComponent<AnimacionJugador>();
        var so = new SerializedObject(aj); so.FindProperty("animator").objectReferenceValue = anim; so.ApplyModifiedProperties();

        var pc = player.GetComponent<PlayerController>();
        if (pc != null) { var so2 = new SerializedObject(pc); so2.FindProperty("speed").floatValue = 4.5f; so2.ApplyModifiedProperties(); }
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.interpolation = RigidbodyInterpolation.Interpolate;

        Sucia();
        Debug.Log("<b>[Jugador]</b> Personaje listo. Altura " + ALTURA + " m. Textura: " + (tex != null ? "colormap.png" : "FALTA colormap.png"));
    }

    [MenuItem("Estacion/Jugador/2 - Camara orbital")]
    public static void Camara()
    {
        var cmGO = GameObject.Find("CinemachineCamera");
        var player = GameObject.Find("player");
        if (cmGO == null || player == null) { Debug.LogError("[Jugador] Falta CinemachineCamera o player"); return; }
        Undo.RegisterFullObjectHierarchyUndo(cmGO, "Camara orbital");

        var follow = cmGO.GetComponent<CinemachineFollow>();
        if (follow != null) Object.DestroyImmediate(follow);

        var orb = cmGO.GetComponent<CinemachineOrbitalFollow>();
        if (orb == null) orb = cmGO.AddComponent<CinemachineOrbitalFollow>();
        orb.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orb.Radius = 12f;
        orb.TargetOffset = new Vector3(0f, 0.6f, 0f);
        orb.TrackerSettings.BindingMode = BindingMode.WorldSpace;   // el giro del personaje no mueve la camara
        orb.TrackerSettings.PositionDamping = new Vector3(0.2f, 0.2f, 0.2f);

        var h = orb.HorizontalAxis; h.Range = new Vector2(-180f, 180f); h.Wrap = true; h.Center = 0f; h.Value = 0f; orb.HorizontalAxis = h;
        var v = orb.VerticalAxis; v.Range = new Vector2(8f, 60f); v.Wrap = false; v.Center = 28f; v.Value = 28f; orb.VerticalAxis = v;
        var r = orb.RadialAxis; r.Range = new Vector2(0.6f, 1.5f); r.Wrap = false; r.Center = 1f; r.Value = 1f; orb.RadialAxis = r;

        var rc = cmGO.GetComponent<CinemachineRotationComposer>();
        if (rc == null) rc = cmGO.AddComponent<CinemachineRotationComposer>();
        rc.TargetOffset = new Vector3(0f, 0.6f, 0f);
        rc.Damping = new Vector2(0.2f, 0.2f);

        var cam = cmGO.GetComponent<CinemachineCamera>();
        cam.Follow = player.transform;
        cam.LookAt = player.transform;

        if (cmGO.GetComponent<CamaraOrbital>() == null) cmGO.AddComponent<CamaraOrbital>();
        Sucia();
        Debug.Log("<b>[Jugador]</b> Camara orbital lista (arrastrar para girar, rueda para zoom).");
    }

    static AnimationClip Clip(string nombre)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(MODELO))
            if (a is AnimationClip && a.name == nombre) return (AnimationClip)a;
        Debug.LogWarning("[Jugador] No encontre la animacion " + nombre);
        return null;
    }

    static float Altura(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 0f;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.size.y;
    }

    static void Sucia()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
}
#endif

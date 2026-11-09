using System.Collections.Generic;
using UnityEngine;

public class BanderasOndeantes : MonoBehaviour
{
    [Header("Viento")]
    [SerializeField] float amplitud = 0.13f;
    [SerializeField] float largoOnda = 1.5f;
    [SerializeField] float velocidad = 3.4f;
    [SerializeField, Range(4, 32)] int segmentos = 16;

    class Franja
    {
        public Renderer original;
        public GameObject obj;
        public Mesh malla;
        public Vector3[] baseV;
        public Vector3[] v;
        public float[] dist;
        public float largo;
        public float fase;
    }

    readonly List<Franja> franjas = new List<Franja>();
    bool creado;

    void Start()
    {
        var lista = new List<KeyValuePair<Renderer, Transform>>();
        foreach (Transform asta in transform)
        {
            Transform mastil = null;
            foreach (var t in asta.GetComponentsInChildren<Transform>(true)) if (t.name == "Mastil") { mastil = t; break; }
            if (mastil == null) continue;
            foreach (var r in asta.GetComponentsInChildren<MeshRenderer>(true))
                if (r.name.StartsWith("Bandera")) lista.Add(new KeyValuePair<Renderer, Transform>(r, mastil));
        }
        var fases = new Dictionary<Transform, float>();
        foreach (var par in lista)
        {
            float fase;
            if (!fases.TryGetValue(par.Value, out fase)) { fase = Random.Range(0f, 20f); fases[par.Value] = fase; }
            Crear(par.Key, par.Value, fase);
        }
        creado = true;
        Aplicar(true);
    }

    void OnEnable() { if (creado) Aplicar(true); }

    void OnDisable() { if (creado) Aplicar(false); }

    void Aplicar(bool animadas)
    {
        foreach (var f in franjas)
        {
            if (f.original != null) f.original.enabled = !animadas;
            if (f.obj != null) f.obj.SetActive(animadas);
        }
    }

    void Crear(Renderer r, Transform mastil, float fase)
    {
        var tr = r.transform;
        var centro = r.bounds.center;
        var rot = tr.rotation;
        var tam = Quaternion.Inverse(rot) * r.bounds.size;
        float largo = Mathf.Abs(tam.x), alto = Mathf.Abs(tam.y);
        if (largo < 0.01f || alto < 0.01f) return;
        var local = Quaternion.Inverse(rot) * (mastil.position - centro);
        float lado = local.x < 0f ? -1f : 1f;
        float bordeMastil = lado * largo * 0.5f;

        int n = segmentos + 1;
        var baseV = new Vector3[n * 4];
        var dist = new float[n * 4];
        for (int i = 0; i < n; i++)
        {
            float x = -largo * 0.5f + largo * i / segmentos;
            float d = Mathf.Abs(x - bordeMastil);
            for (int cara = 0; cara < 2; cara++)
            {
                int k = cara * n * 2 + i * 2;
                baseV[k] = new Vector3(x, -alto * 0.5f, 0f);
                baseV[k + 1] = new Vector3(x, alto * 0.5f, 0f);
                dist[k] = d;
                dist[k + 1] = d;
            }
        }
        var tris = new List<int>();
        for (int i = 0; i < segmentos; i++)
        {
            int a = i * 2;
            tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
            tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
            int b = n * 2 + i * 2;
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
            tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
        }
        var malla = new Mesh { name = "Bandera_Ondeante" };
        malla.MarkDynamic();
        malla.vertices = baseV;
        malla.SetTriangles(tris, 0);
        malla.RecalculateNormals();
        malla.bounds = new Bounds(Vector3.zero, new Vector3(largo + 0.2f, alto + 0.2f, amplitud * 4f + 0.2f));

        var go = new GameObject(r.name + "_Ondeante");
        go.transform.SetParent(transform, true);
        go.transform.SetPositionAndRotation(centro, rot);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = r.sharedMaterials;
        mr.shadowCastingMode = r.shadowCastingMode;

        franjas.Add(new Franja { original = r, obj = go, malla = malla, baseV = baseV, v = (Vector3[])baseV.Clone(), dist = dist, largo = largo, fase = fase });
    }

    void Update()
    {
        float k = 2f * Mathf.PI / Mathf.Max(0.1f, largoOnda);
        float t = Time.time * velocidad;
        foreach (var f in franjas)
        {
            if (f.obj == null || !f.obj.activeInHierarchy) continue;
            for (int i = 0; i < f.v.Length; i++)
            {
                float d = f.dist[i];
                float u = d / f.largo;
                float z = amplitud * u * Mathf.Sin(k * d - t + f.fase) + amplitud * 0.35f * u * Mathf.Sin(2.3f * k * d - 1.7f * t + f.fase * 1.3f);
                var p = f.baseV[i];
                f.v[i] = new Vector3(p.x, p.y - 0.04f * u * u, z);
            }
            f.malla.vertices = f.v;
            f.malla.RecalculateNormals();
        }
    }
}

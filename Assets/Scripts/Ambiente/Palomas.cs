using System.Collections.Generic;
using UnityEngine;

public class Palomas : MonoBehaviour
{
    [Header("Bandada")]
    [SerializeField] int cantidad = 14;
    [SerializeField] Vector3 zonaCentro = new Vector3(5f, 0f, 38f);
    [SerializeField] Vector3 zonaTamano = new Vector3(100f, 0f, 24f);
    [SerializeField] float alturaMaximaSuelo = 0.8f;
    [SerializeField] string[] evitarSuperficies = { "agua", "Agua" };

    [Header("Posaderos")]
    [SerializeField] string[] nombresPosadero = { "Luminaria", "Remate", "Bola" };
    [SerializeField, Range(0f, 1f)] float probabilidadPosadero = 0.35f;

    [Header("Jugador")]
    [SerializeField] Transform jugador;
    [SerializeField] float distanciaSusto = 3.2f;

    [Header("Aspecto")]
    [SerializeField] float tamano = 1.15f;
    [SerializeField] Material materialCuerpo;
    [SerializeField] Material materialCabeza;
    [SerializeField] Material materialAlas;
    [SerializeField] bool proyectarSombra = true;

    enum Estado { Caminando, Picoteando, Volando, Posada }

    class Paloma
    {
        public Transform raiz, cabeza, alaIzq, alaDer;
        public Estado estado;
        public Vector3 destino;
        public float espera;
        public Vector3 p0, p1, p2, p3;
        public float t, duracion, fase;
        public Renderer posadero;
        public float susto;
    }

    readonly List<Paloma> bandada = new List<Paloma>();
    readonly List<Renderer> posaderos = new List<Renderer>();
    static readonly Vector3 CABEZA_BASE = new Vector3(0f, 0.165f, 0.1f);
    Mesh mallaCuerpo, mallaCabeza, mallaAla;
    Transform contenedor;

    void Start()
    {
        if (jugador == null)
        {
            var j = GameObject.Find("Jugador");
            if (j != null) jugador = j.transform;
            else if (Camera.main != null) jugador = Camera.main.transform;
        }
        BuscarPosaderos();
        CrearMallas();
        CrearMateriales();
        contenedor = new GameObject("Bandada").transform;
        contenedor.SetParent(transform, false);
        for (int i = 0; i < cantidad; i++)
        {
            Vector3 p;
            if (!PuntoZona(out p)) continue;
            var pal = Crear(i);
            pal.raiz.position = p;
            pal.raiz.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Plegar(pal);
            pal.estado = Estado.Picoteando;
            pal.espera = Random.Range(0.5f, 4f);
            bandada.Add(pal);
        }
    }

    void OnEnable() { if (contenedor != null) contenedor.gameObject.SetActive(true); }

    void OnDisable() { if (contenedor != null) contenedor.gameObject.SetActive(false); }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 pj = jugador != null ? jugador.position : new Vector3(99999f, 0f, 99999f);
        for (int i = 0; i < bandada.Count; i++)
        {
            var p = bandada[i];
            if (p.estado != Estado.Volando && p.susto <= 0f && Plano(p.raiz.position - pj).magnitude < distanciaSusto && Mathf.Abs(p.raiz.position.y - pj.y) < 2.5f)
            {
                Asustar(p, pj);
                for (int k = 0; k < bandada.Count; k++)
                {
                    var o = bandada[k];
                    if (o != p && o.estado != Estado.Volando && o.susto <= 0f && (o.raiz.position - p.raiz.position).sqrMagnitude < 9f)
                        o.susto = Random.Range(0.1f, 0.5f);
                }
            }
            if (p.susto > 0f)
            {
                p.susto -= dt;
                if (p.susto <= 0f && p.estado != Estado.Volando) Asustar(p, pj);
            }
            switch (p.estado)
            {
                case Estado.Caminando: Caminar(p, dt); break;
                case Estado.Picoteando: Picotear(p, dt, pj); break;
                case Estado.Volando: Volar(p, dt); break;
                case Estado.Posada: Posar(p, dt, pj); break;
            }
        }
    }

    void Caminar(Paloma p, float dt)
    {
        var d = Plano(p.destino - p.raiz.position);
        float paso = 0.35f * tamano * dt;
        p.fase += dt * 14f;
        p.cabeza.localPosition = CABEZA_BASE * tamano + Vector3.forward * (0.025f * tamano * Mathf.Sin(p.fase));
        if (d.magnitude <= paso)
        {
            p.raiz.position = p.destino;
            p.cabeza.localPosition = CABEZA_BASE * tamano;
            p.estado = Estado.Picoteando;
            p.espera = Random.Range(1f, 4f);
            return;
        }
        var dir = d.normalized;
        p.raiz.rotation = Quaternion.RotateTowards(p.raiz.rotation, Quaternion.LookRotation(dir), 360f * dt);
        p.raiz.position += dir * paso;
    }

    void Picotear(Paloma p, float dt, Vector3 pj)
    {
        p.espera -= dt;
        p.fase += dt * 9f;
        float s = Mathf.Sin(p.fase);
        p.cabeza.localRotation = Quaternion.Euler(s > 0.6f ? (s - 0.6f) * 110f : 0f, 0f, 0f);
        if (p.espera > 0f) return;
        p.cabeza.localRotation = Quaternion.identity;

        Vector3 aqui;
        if (!Suelo(p.raiz.position, out aqui) || aqui.y > p.raiz.position.y + 0.3f)
        {
            Asustar(p, pj);
            return;
        }
        if (Random.value < 0.08f)
        {
            Reubicar(p, pj);
            return;
        }
        Vector3 q;
        if (PuntoCerca(p.raiz.position, 0.4f, 1.8f, pj, distanciaSusto * 1.5f, out q))
        {
            p.destino = q;
            p.estado = Estado.Caminando;
        }
        else p.espera = Random.Range(1f, 3f);
    }

    void Posar(Paloma p, float dt, Vector3 pj)
    {
        p.espera -= dt;
        bool sigue = p.posadero != null && p.posadero.enabled && p.posadero.gameObject.activeInHierarchy;
        if (sigue && p.espera > 0f) return;
        Vector3 q;
        if (PuntoCerca(p.raiz.position, 4f, 14f, pj, distanciaSusto * 2f, out q) || PuntoZona(out q))
            Despegar(p, q, null);
    }

    void Volar(Paloma p, float dt)
    {
        p.t = Mathf.Min(1f, p.t + dt / p.duracion);
        var pos = Bezier(p.p0, p.p1, p.p2, p.p3, p.t);
        var dir = pos - p.raiz.position;
        p.raiz.position = pos;
        if (dir.sqrMagnitude > 1e-6f)
        {
            var plano = Plano(dir);
            if (plano.sqrMagnitude > 1e-6f)
            {
                float pitch = Mathf.Clamp(-Mathf.Atan2(dir.y, plano.magnitude) * Mathf.Rad2Deg, -35f, 35f);
                var obj = Quaternion.LookRotation(plano.normalized) * Quaternion.Euler(pitch, 0f, 0f);
                p.raiz.rotation = Quaternion.Slerp(p.raiz.rotation, obj, 8f * dt);
            }
        }
        p.fase += dt * 22f;
        float ang = p.t < 0.15f || p.t > 0.8f ? Mathf.Sin(p.fase) * 65f : 15f + Mathf.Sin(p.fase * 0.5f) * 25f;
        Abrir(p, ang);
        if (p.t < 1f) return;
        p.raiz.rotation = Quaternion.Euler(0f, p.raiz.eulerAngles.y, 0f);
        Plegar(p);
        if (p.posadero != null)
        {
            p.estado = Estado.Posada;
            p.espera = Random.Range(8f, 25f);
        }
        else
        {
            p.estado = Estado.Picoteando;
            p.espera = Random.Range(0.5f, 2f);
        }
    }

    void Asustar(Paloma p, Vector3 pj)
    {
        if (posaderos.Count > 0 && Random.value < probabilidadPosadero)
        {
            var r = posaderos[Random.Range(0, posaderos.Count)];
            if (r != null && r.enabled && r.gameObject.activeInHierarchy && Plano(r.bounds.center - pj).magnitude > distanciaSusto * 2f)
            {
                var b = r.bounds;
                Despegar(p, new Vector3(b.center.x, b.max.y, b.center.z), r);
                return;
            }
        }
        Vector3 q;
        if (PuntoCerca(p.raiz.position, 7f, 18f, pj, 9f, out q) || PuntoZona(out q))
            Despegar(p, q, null);
    }

    void Reubicar(Paloma p, Vector3 pj)
    {
        Vector3 q;
        if (PuntoCerca(p.raiz.position, 5f, 15f, pj, distanciaSusto * 2f, out q)) Despegar(p, q, null);
        else p.espera = Random.Range(1f, 3f);
    }

    void Despegar(Paloma p, Vector3 destino, Renderer posadero)
    {
        p.p0 = p.raiz.position;
        p.p3 = destino;
        float d = Plano(p.p3 - p.p0).magnitude;
        float h = Mathf.Max(p.p0.y, p.p3.y) + 1.2f + d * 0.18f;
        p.p1 = Vector3.Lerp(p.p0, p.p3, 0.2f); p.p1.y = h;
        p.p2 = Vector3.Lerp(p.p0, p.p3, 0.8f); p.p2.y = h;
        p.duracion = Mathf.Max(0.8f, (d + 2f * h) / (6f * tamano));
        p.t = 0f;
        p.posadero = posadero;
        p.estado = Estado.Volando;
        p.cabeza.localPosition = CABEZA_BASE * tamano;
        p.cabeza.localRotation = Quaternion.identity;
    }

    void Plegar(Paloma p)
    {
        p.alaDer.localRotation = Quaternion.Euler(8f, 80f, 0f);
        p.alaIzq.localRotation = Quaternion.Euler(8f, -80f, 0f);
    }

    void Abrir(Paloma p, float ang)
    {
        p.alaDer.localRotation = Quaternion.Euler(0f, 0f, ang);
        p.alaIzq.localRotation = Quaternion.Euler(0f, 0f, -ang);
    }

    bool PuntoZona(out Vector3 punto)
    {
        for (int i = 0; i < 40; i++)
        {
            var c = zonaCentro + new Vector3((Random.value - 0.5f) * zonaTamano.x, 0f, (Random.value - 0.5f) * zonaTamano.z);
            if (jugador != null && Plano(c - jugador.position).magnitude < distanciaSusto * 2f) continue;
            if (Suelo(c, out punto)) return true;
        }
        punto = Vector3.zero;
        return false;
    }

    bool PuntoCerca(Vector3 centro, float rMin, float rMax, Vector3 evitar, float distanciaMinima, out Vector3 punto)
    {
        for (int i = 0; i < 20; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(rMin, rMax);
            var c = centro + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (Mathf.Abs(c.x - zonaCentro.x) > zonaTamano.x * 0.5f || Mathf.Abs(c.z - zonaCentro.z) > zonaTamano.z * 0.5f) continue;
            if (Plano(c - evitar).magnitude < distanciaMinima) continue;
            if (Suelo(c, out punto)) return true;
        }
        punto = centro;
        return false;
    }

    bool Suelo(Vector3 c, out Vector3 punto)
    {
        punto = c;
        RaycastHit h;
        if (!Physics.Raycast(new Vector3(c.x, 40f, c.z), Vector3.down, out h, 60f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (h.point.y > alturaMaximaSuelo || h.normal.y < 0.9f) return false;
        foreach (var n in evitarSuperficies) if (!string.IsNullOrEmpty(n) && h.collider.name.Contains(n)) return false;
        punto = h.point;
        return true;
    }

    void BuscarPosaderos()
    {
        posaderos.Clear();
        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var n in nombresPosadero)
                if (r.name == n) { posaderos.Add(r); break; }
    }

    Paloma Crear(int i)
    {
        var p = new Paloma();
        p.raiz = new GameObject("Paloma_" + i).transform;
        p.raiz.SetParent(contenedor, false);
        Pieza("Cuerpo", p.raiz, mallaCuerpo, materialCuerpo, Vector3.zero, Vector3.one * tamano, proyectarSombra);
        p.cabeza = new GameObject("Cabeza").transform;
        p.cabeza.SetParent(p.raiz, false);
        p.cabeza.localPosition = CABEZA_BASE * tamano;
        Pieza("Cabeza_Malla", p.cabeza, mallaCabeza, materialCabeza, Vector3.zero, Vector3.one * tamano, false);
        p.alaDer = new GameObject("Ala_Der").transform;
        p.alaDer.SetParent(p.raiz, false);
        p.alaDer.localPosition = new Vector3(0.05f, 0.15f, 0.03f) * tamano;
        Pieza("Ala", p.alaDer, mallaAla, materialAlas, Vector3.zero, Vector3.one * tamano, false);
        p.alaIzq = new GameObject("Ala_Izq").transform;
        p.alaIzq.SetParent(p.raiz, false);
        p.alaIzq.localPosition = new Vector3(-0.05f, 0.15f, 0.03f) * tamano;
        Pieza("Ala", p.alaIzq, mallaAla, materialAlas, Vector3.zero, new Vector3(-1f, 1f, 1f) * tamano, false);
        p.fase = Random.Range(0f, 10f);
        return p;
    }

    static void Pieza(string nombre, Transform padre, Mesh malla, Material mat, Vector3 pos, Vector3 escala, bool sombra)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
    }

    void CrearMateriales()
    {
        if (materialCuerpo == null) materialCuerpo = Mat(new Color(0.55f, 0.57f, 0.62f));
        if (materialCabeza == null) materialCabeza = Mat(new Color(0.3f, 0.36f, 0.4f));
        if (materialAlas == null) materialAlas = Mat(new Color(0.45f, 0.47f, 0.52f));
    }

    static Material Mat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        m.SetColor("_BaseColor", c);
        m.color = c;
        return m;
    }

    void CrearMallas()
    {
        var esf = Esfera(10, 7);
        var caja = Caja();
        mallaCuerpo = Combinar("Paloma_Cuerpo", new[] {
            new CombineInstance { mesh = esf, transform = Matrix4x4.TRS(new Vector3(0f, 0.11f, 0f), Quaternion.identity, new Vector3(0.13f, 0.12f, 0.25f)) },
            new CombineInstance { mesh = esf, transform = Matrix4x4.TRS(new Vector3(0f, 0.15f, 0.08f), Quaternion.Euler(-25f, 0f, 0f), new Vector3(0.075f, 0.1f, 0.075f)) },
            new CombineInstance { mesh = caja, transform = Matrix4x4.TRS(new Vector3(0f, 0.125f, -0.15f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.08f, 0.015f, 0.13f)) },
            new CombineInstance { mesh = caja, transform = Matrix4x4.TRS(new Vector3(0.025f, 0.025f, 0.01f), Quaternion.identity, new Vector3(0.012f, 0.05f, 0.012f)) },
            new CombineInstance { mesh = caja, transform = Matrix4x4.TRS(new Vector3(-0.025f, 0.025f, 0.01f), Quaternion.identity, new Vector3(0.012f, 0.05f, 0.012f)) },
        });
        mallaCabeza = Combinar("Paloma_Cabeza", new[] {
            new CombineInstance { mesh = esf, transform = Matrix4x4.TRS(new Vector3(0f, 0.02f, 0.02f), Quaternion.identity, Vector3.one * 0.075f) },
            new CombineInstance { mesh = caja, transform = Matrix4x4.TRS(new Vector3(0f, 0.012f, 0.07f), Quaternion.Euler(10f, 0f, 0f), new Vector3(0.016f, 0.014f, 0.04f)) },
        });
        mallaAla = Combinar("Paloma_Ala", new[] {
            new CombineInstance { mesh = esf, transform = Matrix4x4.TRS(new Vector3(0.13f, 0f, -0.02f), Quaternion.identity, new Vector3(0.28f, 0.018f, 0.11f)) },
        });
    }

    static Mesh Combinar(string nombre, CombineInstance[] partes)
    {
        var m = new Mesh { name = nombre };
        m.CombineMeshes(partes, true, true);
        m.RecalculateBounds();
        return m;
    }

    static Mesh Esfera(int lon, int lat)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        v.Add(new Vector3(0f, -0.5f, 0f));
        for (int j = 1; j < lat; j++)
        {
            float phi = Mathf.PI * j / lat - Mathf.PI * 0.5f;
            for (int i = 0; i < lon; i++)
            {
                float th = 2f * Mathf.PI * i / lon;
                v.Add(0.5f * new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th)));
            }
        }
        v.Add(new Vector3(0f, 0.5f, 0f));
        int top = v.Count - 1;
        for (int i = 0; i < lon; i++)
        {
            int i1 = (i + 1) % lon;
            t.Add(0); t.Add(1 + i1); t.Add(1 + i);
            for (int j = 0; j < lat - 2; j++)
            {
                int a = 1 + j * lon + i, b = 1 + j * lon + i1;
                t.Add(a); t.Add(b); t.Add(b + lon);
                t.Add(a); t.Add(b + lon); t.Add(a + lon);
            }
            int ua = 1 + (lat - 2) * lon + i, ub = 1 + (lat - 2) * lon + i1;
            t.Add(ua); t.Add(ub); t.Add(top);
        }
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        AjustarCaras(m);
        m.RecalculateNormals();
        return m;
    }

    static Mesh Caja()
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var d in n)
        {
            var a = Vector3.Cross(d, Mathf.Abs(d.y) > 0.5f ? Vector3.forward : Vector3.up) * 0.5f;
            var b = Vector3.Cross(d, a) ;
            var c = d * 0.5f;
            int k = v.Count;
            v.Add(c - a - b); v.Add(c + a - b); v.Add(c + a + b); v.Add(c - a + b);
            t.Add(k); t.Add(k + 1); t.Add(k + 2);
            t.Add(k); t.Add(k + 2); t.Add(k + 3);
        }
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        AjustarCaras(m);
        m.RecalculateNormals();
        return m;
    }

    static void AjustarCaras(Mesh m)
    {
        var v = m.vertices;
        var t = m.triangles;
        var centro = Vector3.zero;
        foreach (var p in v) centro += p;
        centro /= v.Length;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            var normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, (a + b + c) / 3f - centro) < 0f) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; }
        }
        m.triangles = t;
    }

    static Vector3 Plano(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fuente de piso: los chorros salen "de vez en cuando" con distintos juegos
// (todos juntos, ola desde el centro, al azar) y pausas entre uno y otro.
// Los chorros son ParticleSystems hijos cuyo nombre empieza por "Agua_ChorroPiso".
public class ChorrosPiso : MonoBehaviour
{
    [SerializeField] float pausaMin = 2.5f;
    [SerializeField] float pausaMax = 5f;
    [SerializeField] float duracionJuego = 5f;

    readonly List<ParticleSystem> chorros = new List<ParticleSystem>();
    Vector3 centro;

    void OnEnable()
    {
        chorros.Clear();
        centro = Vector3.zero;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.name.StartsWith("Agua_ChorroPiso")) { chorros.Add(ps); centro += ps.transform.position; }
        if (chorros.Count == 0) return;
        centro /= chorros.Count;
        Todos(false);
        StartCoroutine(Ciclo());
    }

    IEnumerator Ciclo()
    {
        int juego = 0;
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(pausaMin, pausaMax));
            switch (juego % 3)
            {
                case 0: yield return TodosJuntos(); break;
                case 1: yield return Ola(); break;
                default: yield return AlAzar(); break;
            }
            Todos(false);
            juego++;
        }
    }

    IEnumerator TodosJuntos()
    {
        Todos(true);
        yield return new WaitForSeconds(duracionJuego * 0.6f);
    }

    // Anillos que se encienden desde el centro hacia afuera y se apagan en el mismo orden.
    IEnumerator Ola()
    {
        var orden = new List<ParticleSystem>(chorros);
        orden.Sort((a, b) => Dist(a).CompareTo(Dist(b)));
        float paso = 0.35f;
        foreach (var ps in orden) { ps.Play(); yield return new WaitForSeconds(paso * 0.5f); }
        yield return new WaitForSeconds(1.2f);
        foreach (var ps in orden) { ps.Stop(); yield return new WaitForSeconds(paso * 0.5f); }
    }

    // Unos pocos chorros al azar que van cambiando.
    IEnumerator AlAzar()
    {
        float fin = Time.time + duracionJuego;
        while (Time.time < fin)
        {
            Todos(false);
            int n = Mathf.Max(1, chorros.Count / 4);
            for (int i = 0; i < n; i++) chorros[Random.Range(0, chorros.Count)].Play();
            yield return new WaitForSeconds(0.9f);
        }
    }

    float Dist(ParticleSystem ps) { var d = ps.transform.position - centro; d.y = 0f; return d.magnitude; }

    void Todos(bool encender)
    {
        foreach (var ps in chorros)
            if (encender) ps.Play(); else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}

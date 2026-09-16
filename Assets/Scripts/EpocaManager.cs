using System.Collections.Generic;
using UnityEngine;

public class EpocaManager : MonoBehaviour
{
    [System.Serializable]
    public class CambioMaterial
    {
        public string tag = "Untagged";
        public Material material;
    }

    [System.Serializable]
    public class Epoca
    {
        [Header("Identidad")]
        public string nombre = "Época";

        [Header("Materiales por tag")]
        public CambioMaterial[] materiales;

        [Header("Objetos (fuente, props, etc.)")]
        public GameObject[] activar;
        public GameObject[] desactivar;
    }

    [Header("Épocas")]
    [SerializeField] private List<Epoca> epocas = new List<Epoca>();

    [Header("Estado inicial")]
    [SerializeField] private int epocaActual = 0;

    private void Start()
    {
        if (epocas.Count > 0) AplicarEpoca(epocaActual);
    }

    public void SiguienteEpoca()
    {
        if (epocas.Count == 0) return;
        epocaActual = (epocaActual + 1) % epocas.Count;
        AplicarEpoca(epocaActual);
    }

    public void AplicarEpoca(int indice)
    {
        if (indice < 0 || indice >= epocas.Count) return;
        epocaActual = indice;
        Epoca e = epocas[indice];

        if (e.materiales != null)
        {
            foreach (CambioMaterial cm in e.materiales)
            {
                if (cm.material == null || string.IsNullOrEmpty(cm.tag)) continue;

                GameObject[] objetos = GameObject.FindGameObjectsWithTag(cm.tag);
                foreach (GameObject go in objetos)
                {
                    Renderer r = go.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = cm.material;
                }
            }
        }

        if (e.activar != null)
            foreach (GameObject go in e.activar)
                if (go != null) go.SetActive(true);

        if (e.desactivar != null)
            foreach (GameObject go in e.desactivar)
                if (go != null) go.SetActive(false);
    }

    private void OnMouseDown()
    {
        SiguienteEpoca();
    }
}

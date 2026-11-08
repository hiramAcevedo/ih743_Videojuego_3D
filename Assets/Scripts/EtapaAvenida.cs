using System;
using System.Collections.Generic;
using UnityEngine;

// Una etapa de la avenida, como el Stage del tutorial: un asset que se crea con clic derecho > Create > Camión de ruta >
// Etapa de la avenida y se ajusta en el Inspector. Dice desde qué metro rige, qué porciones (prefabs de tramo) la componen y
// con qué peso, y cómo se reparten los obstáculos en sus filas. GeneradorAvenida guarda la lista de etapas en orden.
[CreateAssetMenu(fileName = "Nueva etapa", menuName = "Camión de ruta/Etapa de la avenida", order = 0)]
public class EtapaAvenida : ScriptableObject
{
    // Una porción de la avenida: el prefab de un tramo y qué tan seguido sale dentro de la etapa.
    [Serializable]
    public class Porcion
    {
        [Tooltip("Sólo para leer la lista en el Inspector.")]
        public string nombre;
        [Tooltip("Prefab del tramo: asfalto, banquetas y edificios, con el origen al principio del tramo.")]
        public GameObject prefab;
        [Tooltip("Qué tan seguido sale frente a las demás porciones de la etapa. Con 0 no sale.")]
        [Min(0f)] public float peso = 1f;
    }

    [Tooltip("El nombre que aparece en pantalla al entrar a la etapa.")]
    public string nombre = "Tráfico ligero";
    [Tooltip("Metro de la avenida a partir del cual rige esta etapa.")]
    [Min(0f)] public float desdeMetro;

    [Header("Porciones que la componen")]
    public List<Porcion> porciones = new List<Porcion>();

    [Header("Filas de obstáculos")]
    [Tooltip("Metros entre una fila y la siguiente: cada vez se elige al azar entre el mínimo y el máximo.")]
    [Min(8f)] public float separacionMinima = 30f;
    [Min(8f)] public float separacionMaxima = 40f;
    [Tooltip("Probabilidad de que una fila traiga dos autos parados en lugar de uno. Siempre queda un carril libre.")]
    [Range(0f, 1f)] public float probabilidadDosAutos;
    [Tooltip("Probabilidad de que una fila sea un tope de lado a lado en lugar de autos.")]
    [Range(0f, 1f)] public float probabilidadTope = 0.2f;

    void OnValidate()
    {
        separacionMaxima = Mathf.Max(separacionMaxima, separacionMinima);
    }
}

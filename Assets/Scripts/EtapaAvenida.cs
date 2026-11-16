using System;
using System.Collections.Generic;
using UnityEngine;

// Una etapa de la avenida, como el Stage del tutorial: un asset que se crea con clic derecho > Create > Camión de ruta >
// Etapa de la avenida y se ajusta en el Inspector. Dice desde qué metro rige, qué porciones (prefabs de tramo) la componen y
// con qué peso, cómo se reparten los obstáculos en sus filas, cuántos autos por kilómetro vienen a rebasar al camión y su
// ambiente: el color del cielo, el sol y la luz a esa hora del día (LuzDelDia pasa de uno a otro). En la 4.6 también qué tan
// seguido trae onda verde un cruce y cuántas barras del tráfico enciende en el tablero. GeneradorAvenida guarda la lista de
// etapas en orden.
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

    [Header("Onda verde (4.6)")]
    [Tooltip("Probabilidad de que un cruce de esta etapa traiga la franja de la onda verde en uno de sus carriles. Con 0 no sale.")]
    [Range(0f, 1f)] public float probabilidadOndaVerde;

    [Header("Tablero (4.6)")]
    [Tooltip("Cuántas de las cuatro barras del tráfico se encienden en el tablero: 1 ligero, 2 medio, 3 hora pico y 4 quincena.")]
    [Range(1, 4)] public int barrasTrafico = 1;

    [Header("Autos que rebasan")]
    [Tooltip("Cuántos autos por kilómetro vienen por detrás, más rápido que el camión, por el carril de al lado, lo rebasan y " +
             "se le meten adelante. Cada uno sale en cuanto hay un carril libre para rebasar. Con 0 no sale ninguno en esta etapa.")]
    [Range(0f, 15f)] public float rebasesPorKilometro;

    [Header("Ambiente: la hora del día")]
    [Tooltip("Color del cielo: el fondo de la cámara y la niebla.")]
    public Color colorCielo = new Color32(150, 196, 236, 255);
    [Tooltip("Color de la luz del sol.")]
    public Color colorSol = new Color32(255, 244, 226, 255);
    [Tooltip("Qué tan fuerte alumbra el sol.")]
    [Range(0f, 2f)] public float intensidadSol = 1.25f;
    [Tooltip("Qué tan alto va el sol, en grados sobre el horizonte: bajo, sombras largas.")]
    [Range(2f, 90f)] public float alturaSol = 48f;
    [Tooltip("De dónde viene el sol, en grados alrededor del eje vertical.")]
    [Range(-180f, 180f)] public float giroSol = -38f;
    [Tooltip("Color de la luz que baja del cielo y rellena las sombras.")]
    public Color colorAmbiente = new Color32(171, 208, 240, 255);

    void OnValidate()
    {
        separacionMaxima = Mathf.Max(separacionMaxima, separacionMinima);
    }
}

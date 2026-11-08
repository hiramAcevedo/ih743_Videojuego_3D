using System.Collections.Generic;
using UnityEngine;

// Arma la avenida mientras se juega, por tramos. Siempre deja "tramosPorDelante" tramos delante del camión y destruye los que
// quedan atrás de la cámara, así que la avenida no se acaba. La etapa que rige en cada metro (lista "etapas", assets de
// EtapaAvenida) decide qué porción sale en cada tramo y cómo se reparten sus filas de obstáculos: uno o dos autos parados,
// siempre con un carril libre, o un tope de lado a lado. ControlPartida llama a Reiniciar cuando se vuelve a empezar.
public class GeneradorAvenida : MonoBehaviour
{
    [Header("Tramos")]
    [Tooltip("Largo de cada tramo en metros; las porciones están hechas para este largo.")]
    [SerializeField, Min(10f)] float largoTramo = 40f;
    [Tooltip("Cuántos tramos se mantienen armados delante del camión.")]
    [SerializeField, Range(2, 12)] int tramosPorDelante = 6;
    [Tooltip("Metros que un tramo sigue en pie después de que el camión lo deja atrás (la cámara va 15 m detrás).")]
    [SerializeField, Min(0f)] float margenDetras = 30f;

    [Header("Obstáculos")]
    [SerializeField] GameObject[] autos;
    [SerializeField] GameObject tope;
    [SerializeField, Min(1f)] float anchoCarril = 3.2f;
    [SerializeField, Range(2, 5)] int carriles = 3;
    [Tooltip("Metros sin obstáculos al arrancar.")]
    [SerializeField, Min(0f)] float salidaLibre = 30f;

    [Header("Curva de dificultad: etapas en orden")]
    [SerializeField] List<EtapaAvenida> etapas = new List<EtapaAvenida>();

    [Header("Azar")]
    [Tooltip("Con 0, cada partida trae una avenida distinta; con cualquier otro número se repite siempre la misma.")]
    [SerializeField] int semilla;

    [Header("Referencias")]
    [SerializeField] CamionRuta camion;

    // El piso que pisa el camión es una sola caja larga que se recorre con él: los tramos sólo se ven, así no hay costuras
    // entre un tramo y otro que le den un brinco al camión.
    const float LargoSuelo = 1200f;
    const float AdelanteSuelo = 900f;   // al colocarlo: 900 m delante del camión y 300 m detrás

    readonly Queue<Transform> tramos = new Queue<Transform>();
    System.Random azar;
    Transform suelo;
    float origenZ;           // el metro 0 de la avenida: donde arranca el camión
    float zTramo;            // donde empieza el siguiente tramo
    float zFila;             // donde va la siguiente fila de obstáculos
    GameObject ultimaPorcion;

    public int TramosCreados { get; private set; }
    public int TramosDestruidos { get; private set; }
    public int TramosEnPie => tramos.Count;
    public int SemillaUsada { get; private set; }

    void Awake()
    {
        suelo = new GameObject("Suelo").transform;
        suelo.SetParent(transform, false);
        var caja = suelo.gameObject.AddComponent<BoxCollider>();
        caja.size = new Vector3(anchoCarril * carriles, 0.2f, LargoSuelo);
    }

    void Start()
    {
        if (camion == null || etapas.Count == 0 || etapas[0] == null)
        {
            Debug.LogError("GeneradorAvenida necesita el camión y al menos una etapa.", this);
            enabled = false;
            return;
        }
        Reiniciar();
    }

    void Update()
    {
        float z = camion.transform.position.z;
        while (zTramo < z + tramosPorDelante * largoTramo)
        {
            CrearTramo();
        }
        while (tramos.Count > 0 && tramos.Peek().position.z + largoTramo < z - margenDetras)
        {
            Destroy(tramos.Dequeue().gameObject);
            TramosDestruidos++;
        }
        if (z > suelo.position.z)   // a la mitad del camino se vuelve a poner adelante
        {
            ColocarSuelo(z);
        }
    }

    // Borra la avenida y arma una nueva desde la salida.
    public void Reiniciar()
    {
        while (tramos.Count > 0)
        {
            Destroy(tramos.Dequeue().gameObject);
        }
        SemillaUsada = semilla != 0 ? semilla : Random.Range(1, int.MaxValue);
        azar = new System.Random(SemillaUsada);
        origenZ = camion.PosicionSalida.z;
        zTramo = origenZ - largoTramo;   // un tramo detrás de la salida, para que la cámara no vea el vacío
        zFila = origenZ + salidaLibre;
        ultimaPorcion = null;
        TramosCreados = 0;
        TramosDestruidos = 0;
        ColocarSuelo(origenZ);
        Update();
    }

    // La etapa que rige en un metro de la avenida: la última de la lista cuyo inicio ya pasó.
    public EtapaAvenida EtapaEn(float metro)
    {
        EtapaAvenida actual = null;
        foreach (var etapa in etapas)
        {
            if (etapa != null && (actual == null || metro >= etapa.desdeMetro))
            {
                actual = etapa;
            }
        }
        return actual;
    }

    void CrearTramo()
    {
        var etapa = EtapaEn(zTramo - origenZ);
        var prefab = ElegirPorcion(etapa);
        var tramo = Instantiate(prefab, new Vector3(0f, 0f, zTramo), Quaternion.identity, transform).transform;
        TramosCreados++;
        tramo.name = "Tramo " + TramosCreados + " · " + prefab.name;
        float fin = zTramo + largoTramo;
        while (zFila < fin)
        {
            var etapaFila = EtapaEn(zFila - origenZ);
            CrearFila(tramo, zFila, etapaFila);
            zFila += Mathf.Lerp(etapaFila.separacionMinima, etapaFila.separacionMaxima, (float)azar.NextDouble());
        }
        tramos.Enqueue(tramo);
        zTramo = fin;
    }

    // Sorteo por peso entre las porciones de la etapa, sin repetir la anterior si hay otra con qué cambiarla.
    GameObject ElegirPorcion(EtapaAvenida etapa)
    {
        var elegida = Sortear(etapa, ultimaPorcion) ?? Sortear(etapa, null);
        if (elegida == null)
        {
            throw new MissingReferenceException("La etapa " + etapa.name + " no tiene porciones con prefab y peso.");
        }
        ultimaPorcion = elegida;
        return elegida;
    }

    GameObject Sortear(EtapaAvenida etapa, GameObject excluida)
    {
        float total = 0f;
        foreach (var p in etapa.porciones)
        {
            if (p.prefab != null && p.prefab != excluida)
            {
                total += p.peso;
            }
        }
        if (total <= 0f)
        {
            return null;
        }
        double tiro = azar.NextDouble() * total;
        GameObject ultima = null;
        foreach (var p in etapa.porciones)
        {
            if (p.prefab == null || p.prefab == excluida || p.peso <= 0f)
            {
                continue;
            }
            ultima = p.prefab;
            tiro -= p.peso;
            if (tiro < 0)
            {
                break;
            }
        }
        return ultima;
    }

    // Una fila: un tope de lado a lado, o uno o dos autos parados que siempre dejan un carril libre.
    void CrearFila(Transform tramo, float z, EtapaAvenida etapa)
    {
        if (tope != null && azar.NextDouble() < etapa.probabilidadTope)
        {
            Instantiate(tope, new Vector3(0f, 0f, z), Quaternion.identity, tramo);
            return;
        }
        if (autos == null || autos.Length == 0)
        {
            return;
        }
        int libre = azar.Next(carriles);
        bool dos = azar.NextDouble() < etapa.probabilidadDosAutos;
        int conAuto = azar.Next(carriles - 1);   // con un solo auto, cuál de los otros carriles lo lleva
        for (int c = 0, otro = 0; c < carriles; c++)
        {
            if (c == libre)
            {
                continue;
            }
            if (dos || otro == conAuto)
            {
                float x = (c - (carriles - 1) / 2f) * anchoCarril;
                Instantiate(autos[azar.Next(autos.Length)], new Vector3(x, 0f, z), Quaternion.identity, tramo);
            }
            otro++;
        }
    }

    void ColocarSuelo(float z)
    {
        suelo.position = new Vector3(0f, -0.1f, z - LargoSuelo / 2f + AdelanteSuelo);
    }
}

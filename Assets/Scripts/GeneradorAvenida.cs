using System.Collections.Generic;
using UnityEngine;

// Arma la avenida mientras se juega, por tramos. Siempre deja "tramosPorDelante" tramos delante del camión y destruye los que
// quedan atrás de la cámara, así que la avenida no se acaba. La etapa que rige en cada metro (lista "etapas", assets de
// EtapaAvenida) decide qué porción sale en cada tramo y cómo se reparten sus filas de obstáculos: uno o dos autos parados,
// siempre con un carril libre, o un tope de lado a lado. También decide cada cuánto sale un auto por detrás a rebasar al camión
// (AutoQueRebasa). ControlPartida llama a Reiniciar cuando se vuelve a empezar.
// En un nivel (4.5) las etapas son las del nivel, la avenida sigue armándose después de la terminal para que no se vea el vacío,
// pero sin obstáculos desde unos metros antes de ella, y ya no salen autos a rebasar cuando falta poco para llegar.
public class GeneradorAvenida : MonoBehaviour
{
    [Header("Nivel")]
    [Tooltip("El nivel que se juega en la escena: sus etapas, la terminal y desde dónde salen los autos locos mandan sobre las de " +
             "abajo. Vacío en la ruta libre, la avenida sin fin.")]
    [SerializeField] NivelRuta nivel;
    [Tooltip("La terminal: el arco con el letrero de la ciudad y la raya de llegada. Se pone una vez, a los metros del nivel.")]
    [SerializeField] GameObject terminal;
    [Tooltip("Metros antes de la terminal desde los que ya no salen filas de obstáculos.")]
    [SerializeField, Min(0f)] float libreAntesDeTerminal = 45f;
    [Tooltip("Metros antes de la terminal desde los que ya no sale ningún auto a rebasar.")]
    [SerializeField, Min(0f)] float sinRebasesAntesDeTerminal = 220f;

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
    [Tooltip("Segundos que quedan para cambiar de carril entre una fila y la siguiente (la separación menos el largo del camión y " +
             "de un auto, a la velocidad del camión). Si son menos, el carril libre de una fila de dos autos queda a un solo cambio " +
             "de algún carril libre de la fila anterior: nunca hay que cruzar dos carriles de golpe.")]
    [SerializeField, Min(0f)] float segundosParaDosCarriles = 0.6f;
    [Tooltip("Segundos que siempre quedan para pasarse al carril de junto entre una fila y la siguiente: a más velocidad, las " +
             "filas se separan más metros que los de la etapa si hace falta.")]
    [SerializeField, Min(0f)] float segundosMinimosEntreFilas = 0.3f;

    [Header("Autos que rebasan")]
    [Tooltip("Prefabs de los autos que vienen por detrás a rebasar al camión; cada uno trae su AutoQueRebasa. Cuántos salen lo " +
             "dice cada etapa.")]
    [SerializeField] GameObject[] autosQueRebasan;
    [Tooltip("Metros detrás del camión donde aparece el auto que va a rebasar; con 11 sale de fuera de la cámara.")]
    [SerializeField, Min(0f)] float apareceDetras = 11f;
    [Tooltip("Metros de más, sin autos parados, en el carril por donde va a rebasar, después de donde termina de rebasar: lo que " +
             "necesita para no frenar antes de meterse.")]
    [SerializeField, Min(0f)] float margenLibreRebase = 20f;
    [Tooltip("Segundos de camino delante del camión en que ninguna fila puede tener como único carril libre el carril por donde va " +
             "a rebasar el auto: mientras lo rebasan, el camión no tiene que meterse ahí.")]
    [SerializeField, Min(0f)] float segundosLibresCamion = 1.6f;
    [Tooltip("Metro desde el que, además de los tranquilos, salen autos locos: pasan mucho más rápido y se pierden de vista.")]
    [SerializeField, Min(0f)] float rebasesLocosDesde = 2500f;
    [Tooltip("Desde ese metro, la probabilidad de que el auto que rebasa sea loco.")]
    [SerializeField, Range(0f, 1f)] float probabilidadLoco = 0.5f;
    [Tooltip("Metros delante del camión en que un auto loco ya se perdió de vista y se quita.")]
    [SerializeField, Min(30f)] float locoSePierdeA = 130f;

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
    const float MedioLargoCamion = 4.3f;
    const float LargoCamionYAuto = 12.9f;    // 8.6 m del camión y 4.3 m de un auto: lo que no sirve para cambiar de carril
    const float RebaseFueraDeCamara = 20f;   // metros detrás del camión en que ya no se ve un auto que rebasó
    const float UnoALaVez = 60f;             // no sale otro auto a rebasar mientras el último esté a menos de esto del camión

    readonly Queue<Transform> tramos = new Queue<Transform>();
    readonly Queue<(float z, int libres)> filas = new Queue<(float z, int libres)>();   // las filas en pie y sus carriles libres
    System.Random azar;
    Transform suelo;
    Transform contenedorRebases;
    float origenZ;           // el metro 0 de la avenida: donde arranca el camión
    float zTramo;            // donde empieza el siguiente tramo
    float zFila;             // donde va la siguiente fila de obstáculos
    float zFilaAnterior;
    int libresAnteriores;    // los carriles libres de la fila anterior, uno por bit
    GameObject ultimaPorcion;
    AutoQueRebasa rebaseActivo;              // el último que salió
    readonly List<AutoQueRebasa> rebases = new List<AutoQueRebasa>();
    EtapaAvenida etapaRebases;   // la etapa con la que se programó el siguiente rebase
    float proximoRebase;         // metro de la avenida en que sale el siguiente auto que rebasa; -1 si la etapa no trae

    public int TramosCreados { get; private set; }
    public int TramosDestruidos { get; private set; }
    public int TramosEnPie => tramos.Count;
    public int SemillaUsada { get; private set; }
    public int RebasesSalidos { get; private set; }
    public AutoQueRebasa RebaseActivo => rebaseActivo;
    public NivelRuta Nivel => nivel;
    public Transform Terminal { get; private set; }

    void Awake()
    {
        if (nivel != null)
        {
            etapas = new List<EtapaAvenida>(nivel.etapas);
            rebasesLocosDesde = nivel.rebasesLocosDesde;
        }
        suelo = new GameObject("Suelo").transform;
        suelo.SetParent(transform, false);
        var caja = suelo.gameObject.AddComponent<BoxCollider>();
        caja.size = new Vector3(anchoCarril * carriles, 0.2f, LargoSuelo);
        contenedorRebases = new GameObject("Autos que rebasan").transform;
        contenedorRebases.SetParent(transform, false);
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
        while (filas.Count > 0 && filas.Peek().z < z - margenDetras)
        {
            filas.Dequeue();
        }
        if (z > suelo.position.z)   // a la mitad del camino se vuelve a poner adelante
        {
            ColocarSuelo(z);
        }
        for (int i = rebases.Count - 1; i >= 0; i--)
        {
            float dz = rebases[i].transform.position.z - z;
            if (dz < -RebaseFueraDeCamara || dz > (rebases[i].Loco ? locoSePierdeA : tramosPorDelante * largoTramo))
            {
                Destroy(rebases[i].gameObject);
                rebases.RemoveAt(i);
            }
        }
        ProgramarRebases();
    }

    // Borra la avenida y arma una nueva desde la salida.
    public void Reiniciar()
    {
        while (tramos.Count > 0)
        {
            Destroy(tramos.Dequeue().gameObject);
        }
        foreach (Transform auto in contenedorRebases)
        {
            Destroy(auto.gameObject);
        }
        filas.Clear();
        rebases.Clear();
        rebaseActivo = null;
        etapaRebases = null;
        proximoRebase = -1f;
        RebasesSalidos = 0;
        SemillaUsada = semilla != 0 ? semilla : Random.Range(1, int.MaxValue);
        azar = new System.Random(SemillaUsada);
        origenZ = camion.PosicionSalida.z;
        zTramo = origenZ - largoTramo;   // un tramo detrás de la salida, para que la cámara no vea el vacío
        zFila = origenZ + salidaLibre;
        zFilaAnterior = origenZ;
        libresAnteriores = (1 << carriles) - 1;
        ultimaPorcion = null;
        TramosCreados = 0;
        TramosDestruidos = 0;
        ColocarSuelo(origenZ);
        PonerTerminal();
        Update();
    }

    // La terminal del nivel, a sus metros de la salida, con el nombre de la ciudad y su color en el letrero, que brilla un poco.
    void PonerTerminal()
    {
        if (Terminal != null)
        {
            Destroy(Terminal.gameObject);
            Terminal = null;
        }
        if (nivel == null || terminal == null)
        {
            return;
        }
        Terminal = Instantiate(terminal, new Vector3(0f, 0f, origenZ + nivel.metrosTerminal), Quaternion.identity, transform).transform;
        Terminal.name = "Terminal " + nivel.ciudad;
        foreach (var letrero in Terminal.GetComponentsInChildren<TextMesh>(true))
        {
            if (letrero.name == "Ciudad")
            {
                letrero.text = nivel.ciudad.ToUpperInvariant();
            }
        }
        foreach (var r in Terminal.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.name == "Letrero")
            {
                var bloque = new MaterialPropertyBlock();
                r.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", nivel.colorCiudad);
                // Brilla un poco con el mismo color, para que se lea también de noche.
                bloque.SetColor("_EmissionColor", nivel.colorCiudad * 0.8f);
                r.SetPropertyBlock(bloque);
            }
        }
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
        // En un nivel, la última fila queda unos metros antes de la terminal: de ahí en adelante la avenida va libre.
        float limite = nivel != null ? origenZ + nivel.metrosTerminal - libreAntesDeTerminal : float.MaxValue;
        while (zFila < fin && zFila < limite)
        {
            var etapaFila = EtapaEn(zFila - origenZ);
            CrearFila(tramo, zFila, etapaFila);
            float separacion = Mathf.Lerp(etapaFila.separacionMinima, etapaFila.separacionMaxima, (float)azar.NextDouble());
            float minima = LargoCamionYAuto + camion.VelocidadEn(zFila - origenZ) / 3.6f * segundosMinimosEntreFilas;
            zFila += Mathf.Max(separacion, minima);
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

    // Una fila: un tope de lado a lado, o uno o dos autos parados que siempre dejan un carril libre. Con dos autos queda un solo
    // carril libre; si entre la fila anterior y ésta quedan menos de "segundosParaDosCarriles" para cambiar de carril, ese
    // carril queda a un cambio o menos de algún carril que la anterior dejó libre: viendo las dos filas, siempre se alcanza.
    void CrearFila(Transform tramo, float z, EtapaAvenida etapa)
    {
        float segundos = (z - zFilaAnterior - LargoCamionYAuto) / Mathf.Max(1f, camion.VelocidadEn(z - origenZ) / 3.6f);
        zFilaAnterior = z;
        int todos = (1 << carriles) - 1;
        if (tope != null && azar.NextDouble() < etapa.probabilidadTope)
        {
            Instantiate(tope, new Vector3(0f, 0f, z), Quaternion.identity, tramo);
            libresAnteriores = todos;
            filas.Enqueue((z, todos));
            return;
        }
        if (autos == null || autos.Length == 0)
        {
            return;
        }
        bool dos = azar.NextDouble() < etapa.probabilidadDosAutos;
        int libre = azar.Next(carriles);
        if (dos && segundos < segundosParaDosCarriles)
        {
            var alcanzables = new List<int>();
            for (int c = 0; c < carriles; c++)
            {
                for (int p = 0; p < carriles; p++)
                {
                    if ((libresAnteriores & (1 << p)) != 0 && Mathf.Abs(c - p) <= 1)
                    {
                        alcanzables.Add(c);
                        break;
                    }
                }
            }
            libre = alcanzables[azar.Next(alcanzables.Count)];
        }
        int conAuto = azar.Next(carriles - 1);   // con un solo auto, cuál de los otros carriles lo lleva
        int libres = 1 << libre;
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
            else
            {
                libres |= 1 << c;
            }
            otro++;
        }
        libresAnteriores = libres;
        filas.Enqueue((z, libres));
    }

    // Si entre "desde" y "hasta" hay una fila cuyo único carril libre es ese (con la numeración del camión: -1, 0 y 1). Un auto
    // que rebasa no se mete ni frena ahí: el camión se quedaría sin salida.
    public bool EsElUnicoLibre(int carril, float desde, float hasta)
    {
        int bit = 1 << (carril + carriles / 2);
        foreach (var fila in filas)
        {
            if (fila.z >= desde && fila.z <= hasta && fila.libres == bit)
            {
                return true;
            }
        }
        return false;
    }

    // Los autos que rebasan: cada etapa dice cuántos salen por kilómetro, y la distancia entre uno y otro varía al azar entre
    // 60 % y 140 % del promedio. Al entrar a una etapa que trae, el primero sale a la mitad de esa distancia. Sale uno a la vez
    // (el anterior ya a más de 60 m), por un carril junto al del camión que esté sin autos parados hasta donde termina de rebasar
    // (más "margenLibreRebase") y que no sea el único libre de ninguna fila en los "segundosLibresCamion" que pasa junto al
    // camión; si no hay, lo intenta 10 m después.
    void ProgramarRebases()
    {
        if (autosQueRebasan == null || autosQueRebasan.Length == 0 || camion.Detenido || camion.Llegando)
        {
            return;
        }
        float metro = camion.Metros;
        if (nivel != null && metro > nivel.metrosTerminal - sinRebasesAntesDeTerminal)
        {
            return;
        }
        var etapa = EtapaEn(metro);
        if (etapa != etapaRebases)
        {
            etapaRebases = etapa;
            if (etapa.rebasesPorKilometro <= 0f)
            {
                proximoRebase = -1f;
            }
            else
            {
                float primero = metro + Intervalo(etapa) * 0.5f;
                proximoRebase = proximoRebase < 0f ? primero : Mathf.Min(proximoRebase, primero);
            }
        }
        bool otroCerca = rebaseActivo != null && Mathf.Abs(rebaseActivo.transform.position.z - camion.transform.position.z) < UnoALaVez;
        if (proximoRebase < 0f || metro < proximoRebase || otroCerca)
        {
            return;
        }
        proximoRebase = metro + (SacarRebase() ? Intervalo(etapa) : 10f);
    }

    float Intervalo(EtapaAvenida etapa)
    {
        return 1000f / etapa.rebasesPorKilometro * Mathf.Lerp(0.6f, 1.4f, (float)azar.NextDouble());
    }

    bool SacarRebase()
    {
        float z = camion.transform.position.z;
        float frente = z + MedioLargoCamion;
        float velocidad = camion.VelocidadActual / 3.6f;
        float mientrasPasa = frente + velocidad * segundosLibresCamion;
        var prefab = autosQueRebasan[azar.Next(autosQueRebasan.Length)];
        bool loco = camion.Metros >= rebasesLocosDesde && azar.NextDouble() < probabilidadLoco;
        float hasta = z + prefab.GetComponent<AutoQueRebasa>().MetrosParaRebasar(velocidad, apareceDetras, loco) + margenLibreRebase;
        int limite = carriles / 2;
        var opciones = new List<int>();
        foreach (int c in new[] { camion.Carril - 1, camion.Carril + 1 })
        {
            if (c >= -limite && c <= limite && CarrilLibre(c, z - apareceDetras - 3f, hasta) && !EsElUnicoLibre(c, z, mientrasPasa))
            {
                opciones.Add(c);
            }
        }
        if (opciones.Count == 0)
        {
            return false;
        }
        int carril = opciones[azar.Next(opciones.Count)];
        var auto = Instantiate(prefab, new Vector3(carril * anchoCarril, 0f, z - apareceDetras), Quaternion.identity, contenedorRebases);
        RebasesSalidos++;
        auto.name = "Rebase " + RebasesSalidos + (loco ? " loco · " : " · ") + prefab.name;
        rebaseActivo = auto.GetComponent<AutoQueRebasa>();
        rebases.Add(rebaseActivo);
        rebaseActivo.Arrancar(camion, this, carril, anchoCarril, carriles, loco);
        return true;
    }

    // Sin autos parados (ni el que ya va rebasando) en ese carril entre "desde" y "hasta".
    bool CarrilLibre(int carril, float desde, float hasta)
    {
        var centro = new Vector3(carril * anchoCarril, 0.8f, (desde + hasta) / 2f);
        var mitad = new Vector3(anchoCarril * 0.4f, 0.5f, (hasta - desde) / 2f);
        foreach (var c in Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            if (c.CompareTag("Obstaculo"))
            {
                return false;
            }
        }
        return true;
    }

    void ColocarSuelo(float z)
    {
        suelo.position = new Vector3(0f, -0.1f, z - LargoSuelo / 2f + AdelanteSuelo);
    }
}

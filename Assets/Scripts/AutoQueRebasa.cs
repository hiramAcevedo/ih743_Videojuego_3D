using UnityEngine;

// Un auto que viene por detrás, más rápido que el camión, por el carril de al lado: lo rebasa, pone la direccional, se le mete
// adelante y frena mientras el camión siga detrás de él, así que hay que esquivarlo; en cuanto el camión se cambia de carril,
// acelera y se va. Si su carril nuevo es el único libre de una fila cercana, se mete sin frenar, para no dejar al camión sin
// salida. Mientras rebasa, si se topa con un auto parado en su carril se cambia a otro libre (nunca al del camión si
// todavía no lo pasa) y, si no hay, frena detrás: el que choca siempre es el camión. Lo saca GeneradorAvenida según los autos
// por kilómetro de cada etapa y lo quita cuando queda lejos. Se mueve con un Rigidbody cinemático para que el camión sí lo choque.
[RequireComponent(typeof(Rigidbody))]
public class AutoQueRebasa : MonoBehaviour
{
    public enum Estado { Rebasando, Direccional, Cambiando, Adelante, Atras }

    [Header("Rebase")]
    [Tooltip("Cuánto más rápido que el camión va mientras lo rebasa, en km/h.")]
    [SerializeField, Min(5f)] float ventajaKmh = 45f;
    [Tooltip("Metros entre la trompa del camión y la cajuela del auto para empezar a meterse.")]
    [SerializeField, Min(0f)] float adelanteParaMeterse = 4f;
    [Tooltip("Segundos de anticipación con que ve un auto parado en su carril para cambiarse a otro mientras rebasa.")]
    [SerializeField, Min(0.2f)] float segundosParaEsquivar = 1.2f;
    [Tooltip("Segundos con la direccional prendida antes de cambiarse de carril.")]
    [SerializeField, Min(0f)] float segundosDireccional = 0.5f;
    [Tooltip("Segundos que tarda en pasarse al otro carril.")]
    [SerializeField, Min(0.1f)] float segundosCambio = 0.6f;
    [Tooltip("Ya adelante del camión y en su carril, frena hasta esta parte de la velocidad del camión: el camión lo alcanza y hay " +
             "que esquivarlo. Si el camión se cambia de carril, vuelve a acelerar y se va.")]
    [SerializeField, Range(0.3f, 1f)] float frenaHasta = 0.68f;
    [Tooltip("Qué tan fuerte acelera o frena al rebasar y al meterse, en m/s².")]
    [SerializeField, Min(1f)] float aceleracion = 8f;
    [Tooltip("Qué tan fuerte frena cuando tiene un auto parado o el camión enfrente, en m/s²: frena tarde, como quien piensa " +
             "cambiarse de carril antes.")]
    [SerializeField, Min(1f)] float frenadoFuerte = 14f;

    [Header("Direccionales")]
    [Tooltip("Las del lado izquierdo, adelante y atrás. Sólo parpadean las del lado al que se cambia.")]
    [SerializeField] Renderer[] direccionalesIzquierda;
    [SerializeField] Renderer[] direccionalesDerecha;
    [SerializeField, Min(0.5f)] float parpadeosPorSegundo = 2.5f;

    const float MedioLargo = 2.15f;        // el auto mide 4.3 m de largo
    const float MedioLargoCamion = 4.3f;   // y el camión, 8.6 m
    const float Separacion = 3f;           // los metros que deja al frenar detrás de algo

    Rigidbody cuerpo;
    CamionRuta camion;
    GeneradorAvenida generador;
    bool frenaDelante;                     // si al quedar delante del camión frena; no, si estorbaría el único carril libre
    bool frenaAlMeterse;                   // lo que valdrá frenaDelante cuando termine de meterse
    Rigidbody cuerpoCamion;
    float anchoCarril;
    int limite;                            // el carril de la orilla: con tres carriles, -1 y 1
    int carril;
    int carrilDestino;
    float velocidad;                       // en m/s
    float desdeX;
    float tiempoEnEstado;
    float inicioDireccional;
    float cerca;                           // metros hasta lo que tiene enfrente en su carril (o infinito)
    float velocidadDeEnfrente;             // la velocidad de eso, en m/s: 0 si es un auto parado

    public Estado EstadoActual { get; private set; }
    public int Carril => carril;
    public int CarrilDestino => carrilDestino;
    public float VelocidadKmh => velocidad * 3.6f;
    public float AvanceCambio => EstadoActual == Estado.Cambiando ? Mathf.Clamp01(tiempoEnEstado / segundosCambio) : 0f;
    public bool DireccionalPrendida { get; private set; }
    public bool SeMetioAlCarrilDelCamion { get; private set; }
    public bool CambioHaciaElCamion { get; private set; }   // el cambio de carril en curso va al carril del camión

    float VelocidadCamion => camion.VelocidadActual / 3.6f;

    // Hasta dónde llega su trompa, en metros delante del centro del camión, cuando termina de rebasarlo y pone la direccional,
    // si sale "detras" metros atrás y el camión va a "velocidadCamion" m/s. El generador revisa que ese tramo de su carril esté
    // sin autos parados antes de sacarlo, para que no se atore a medio rebase.
    public float MetrosParaRebasar(float velocidadCamion, float detras)
    {
        float segundos = (detras + MedioLargoCamion + adelanteParaMeterse + MedioLargo) / (ventajaKmh / 3.6f);
        return MedioLargoCamion + adelanteParaMeterse + 2f * MedioLargo + velocidadCamion * segundos;
    }

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        PrenderDireccionales(0);
    }

    // Lo llama el generador al sacarlo detrás del camión, en un carril de junto.
    public void Arrancar(CamionRuta camionQueRebasa, GeneradorAvenida generadorDeLaAvenida, int carrilInicial, float ancho, int carriles)
    {
        generador = generadorDeLaAvenida;
        limite = carriles / 2;
        camion = camionQueRebasa;
        cuerpoCamion = camion.GetComponent<Rigidbody>();
        carril = carrilDestino = carrilInicial;
        anchoCarril = ancho;
        velocidad = VelocidadCamion + ventajaKmh / 3.6f;
        Cambiar(Estado.Rebasando);
    }

    void FixedUpdate()
    {
        if (camion == null || camion.Detenido)
        {
            return;
        }
        float dt = Time.fixedDeltaTime;
        tiempoEnEstado += dt;
        var pos = cuerpo.position;
        float zCamion = cuerpoCamion.position.z;
        float vc = VelocidadCamion;
        float rebase = vc + ventajaKmh / 3.6f;
        float objetivo = rebase;
        MirarAdelante(pos);
        switch (EstadoActual)
        {
            case Estado.Rebasando:
                float adelante = pos.z - MedioLargo - (zCamion + MedioLargoCamion);
                if (adelante > adelanteParaMeterse + 24f)
                {
                    // No encontró hueco para meterse: sigue de largo.
                    SeMetioAlCarrilDelCamion = carril == camion.Carril;
                    frenaDelante = false;
                    Cambiar(Estado.Adelante);
                    break;
                }
                if (adelante >= adelanteParaMeterse && BuscarHueco(pos))
                {
                    break;   // ya pone la direccional para meterse
                }
                if (velocidadDeEnfrente < 1f && cerca < 6f + velocidad * segundosParaEsquivar)
                {
                    Esquivar(pos, adelante >= adelanteParaMeterse);
                }
                break;
            case Estado.Direccional:
                Parpadear();
                if (tiempoEnEstado >= segundosDireccional)
                {
                    desdeX = pos.x;
                    Cambiar(Estado.Cambiando);
                }
                break;
            case Estado.Cambiando:
                Parpadear();
                objetivo = vc + ventajaKmh / 3.6f * 0.3f;
                float avance = AvanceCambio;
                pos.x = Mathf.Lerp(desdeX, carrilDestino * anchoCarril, Mathf.SmoothStep(0f, 1f, avance));
                if (avance >= 1f)
                {
                    carril = carrilDestino;
                    PrenderDireccionales(0);
                    if (CambioHaciaElCamion)
                    {
                        SeMetioAlCarrilDelCamion = carril == camion.Carril;
                        frenaDelante = frenaAlMeterse;
                        Cambiar(Estado.Adelante);
                    }
                    else
                    {
                        Cambiar(Estado.Rebasando);   // sólo esquivó un auto parado: sigue rebasando
                    }
                }
                break;
            case Estado.Adelante:
                // Frena sólo mientras tenga al camión detrás en su carril; si el camión ya lo esquivó, acelera y se va, así no se
                // queda despacio en el único carril libre de una fila.
                objetivo = frenaDelante && camion.Carril == carril ? vc * frenaHasta : rebase;
                if (pos.z + MedioLargo < zCamion - MedioLargoCamion)
                {
                    Cambiar(Estado.Atras);
                }
                else if (velocidadDeEnfrente < 1f && cerca < 6f + velocidad * segundosParaEsquivar)
                {
                    // Un auto parado enfrente: lo esquiva en lugar de formarse detrás, que alargaría la fila y le quitaría
                    // espacio al camión para cambiarse de carril.
                    Esquivar(pos, pos.z - MedioLargo >= zCamion + MedioLargoCamion + adelanteParaMeterse);
                }
                break;
            case Estado.Atras:
                objetivo = vc * frenaHasta;
                break;
        }
        float porEnfrente = FrenarDetras(objetivo);
        float tasa = porEnfrente < objetivo ? frenadoFuerte : aceleracion;
        velocidad = Mathf.MoveTowards(velocidad, porEnfrente, tasa * dt);
        pos.z += velocidad * dt;
        cuerpo.MovePosition(pos);
    }

    // Ya pasó al camión: se cambia un carril hacia el del camión si ahí cabe sin pegarse a un auto parado.
    bool BuscarHueco(Vector3 pos)
    {
        int carrilCamion = camion.Carril;
        float tramoSinSalida = pos.z + VelocidadCamion * 3f + 10f;   // lo que tarda el camión en dejarlo atrás o esquivarlo
        if (carrilCamion == carril)
        {
            // El camión se pasó a su carril por detrás: ya lo tiene adelante; frena, salvo que estorbe el único carril libre.
            SeMetioAlCarrilDelCamion = true;
            frenaDelante = !EsElUnicoLibre(carril, pos.z - 5f, tramoSinSalida);
            Cambiar(Estado.Adelante);
            return true;
        }
        int destino = carril + (carrilCamion > carril ? 1 : -1);
        if (!CarrilLibre(destino, pos.z - MedioLargo - 2f, pos.z + MedioLargo + 8f))
        {
            return false;
        }
        CambiarDeCarril(destino, destino == carrilCamion, pos);
        return true;
    }

    // Un auto parado enfrente mientras rebasa: se pasa a un carril de junto que esté libre. Al del camión, sólo si ya lo pasó.
    void Esquivar(Vector3 pos, bool yaLoPaso)
    {
        foreach (int destino in new[] { carril - 1, carril + 1 })
        {
            if (destino < -limite || destino > limite || (destino == camion.Carril && !yaLoPaso))
            {
                continue;
            }
            if (CarrilLibre(destino, pos.z - MedioLargo - 2f, pos.z + MedioLargo + 14f))
            {
                CambiarDeCarril(destino, destino == camion.Carril, pos);
                return;
            }
        }
    }

    // Si va al carril del camión, decide desde ahora si al terminar frena: no, si ese carril es el único libre de una fila en lo
    // que tarda el camión en esquivarlo o dejarlo atrás.
    void CambiarDeCarril(int destino, bool haciaElCamion, Vector3 pos)
    {
        carrilDestino = destino;
        CambioHaciaElCamion = haciaElCamion;
        frenaAlMeterse = haciaElCamion && !EsElUnicoLibre(destino, pos.z - 5f, pos.z + VelocidadCamion * 3f + 10f);
        Cambiar(Estado.Direccional);
    }

    bool EsElUnicoLibre(int c, float desde, float hasta) => generador != null && generador.EsElUnicoLibre(c, desde, hasta);

    bool CarrilLibre(int c, float desde, float hasta)
    {
        var centro = new Vector3(c * anchoCarril, 0.8f, (desde + hasta) / 2f);
        var mitad = new Vector3(anchoCarril * 0.4f, 0.5f, (hasta - desde) / 2f);
        foreach (var otro in Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            if (otro.CompareTag("Obstaculo") && otro.attachedRigidbody != cuerpo)
            {
                return false;
            }
        }
        return true;
    }

    // Lo que tiene enfrente en su carril: un auto parado (velocidad 0), el camión o, si no hay nada en el alcance, infinito.
    void MirarAdelante(Vector3 pos)
    {
        float alcance = Mathf.Max(Separacion + velocidad * velocidad / (2f * frenadoFuerte) + 2f, 6f + velocidad * segundosParaEsquivar);
        var origen = new Vector3(pos.x, 0.8f, pos.z + MedioLargo + 0.1f);
        cerca = float.MaxValue;
        velocidadDeEnfrente = 0f;
        foreach (var golpe in Physics.BoxCastAll(origen, new Vector3(0.85f, 0.5f, 0.05f), Vector3.forward, Quaternion.identity, alcance,
                     ~0, QueryTriggerInteraction.Ignore))
        {
            var rb = golpe.rigidbody;
            if (rb == cuerpo)
            {
                continue;
            }
            float v;
            if (rb != null && rb == cuerpoCamion)
            {
                v = VelocidadCamion;
            }
            else if (golpe.collider.CompareTag("Obstaculo"))
            {
                var otro = golpe.collider.GetComponentInParent<AutoQueRebasa>();
                v = otro != null ? otro.velocidad : 0f;
            }
            else
            {
                continue;
            }
            if (golpe.distance < cerca)
            {
                cerca = golpe.distance;
                velocidadDeEnfrente = v;
            }
        }
    }

    // Baja la velocidad para quedarse a "Separacion" metros detrás de lo que tiene enfrente; si ya está encima, iguala su
    // velocidad de golpe.
    float FrenarDetras(float objetivo)
    {
        if (cerca == float.MaxValue)
        {
            return objetivo;
        }
        if (cerca < Separacion * 0.5f)
        {
            velocidad = Mathf.Min(velocidad, velocidadDeEnfrente);
        }
        return Mathf.Min(objetivo, velocidadDeEnfrente + Mathf.Sqrt(2f * frenadoFuerte * Mathf.Max(0f, cerca - Separacion)));
    }

    void Parpadear()
    {
        bool prendida = Mathf.Repeat((Time.time - inicioDireccional) * parpadeosPorSegundo, 1f) < 0.5f;
        PrenderDireccionales(prendida ? (carrilDestino < carril ? -1 : 1) : 0);
    }

    void PrenderDireccionales(int lado)
    {
        DireccionalPrendida = lado != 0;
        if (direccionalesIzquierda != null)
        {
            foreach (var r in direccionalesIzquierda)
            {
                if (r != null) r.enabled = lado < 0;
            }
        }
        if (direccionalesDerecha != null)
        {
            foreach (var r in direccionalesDerecha)
            {
                if (r != null) r.enabled = lado > 0;
            }
        }
    }

    void Cambiar(Estado estado)
    {
        EstadoActual = estado;
        tiempoEnEstado = 0f;
        if (estado == Estado.Direccional)
        {
            inicioDireccional = Time.time;
        }
    }
}

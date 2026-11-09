using UnityEngine;

// Un auto que viene por detrás, más rápido que el camión, por el carril de al lado, y lo rebasa sin meterse con él. Hay dos:
//   el tranquilo rebasa, busca su lugar y ahí se queda: si se topa con un auto parado, se pasa a un carril libre que no sea el
//   del camión o se forma detrás;
//   el loco (desde los 2,500 m) pasa mucho más rápido, se abre paso entre los autos parados y se pierde de vista.
// Ninguno se cambia al carril del camión cerca de él ni frena delante de él; si el camión se le mete enfrente, frena detrás.
// Lo saca GeneradorAvenida según los autos por kilómetro de cada etapa y lo quita cuando queda lejos. Se mueve con un Rigidbody
// cinemático para que el camión sí lo choque si se le echa encima de lado.
// Guardada para cuando exista el freno (4.4): la maniobra de meterse delante del camión y frenar está en el commit ea17497 de
// este archivo (CabeDelante, CorredorLibre y frenaDelante). Sin freno, un auto que se mete lento deja al camión sin salida.
[RequireComponent(typeof(Rigidbody))]
public class AutoQueRebasa : MonoBehaviour
{
    public enum Estado { Rebasando, Direccional, Cambiando, Formado }

    [Header("Rebase")]
    [Tooltip("Cuánto más rápido que el camión va el tranquilo, en km/h.")]
    [SerializeField, Min(5f)] float ventajaKmh = 45f;
    [Tooltip("Cuánto más rápido que el camión va el loco, en km/h.")]
    [SerializeField, Min(5f)] float ventajaLocoKmh = 90f;
    [Tooltip("Segundos de anticipación con que ve un auto parado en su carril para cambiarse a otro.")]
    [SerializeField, Min(0.2f)] float segundosParaEsquivar = 1.2f;
    [Tooltip("Segundos con la direccional prendida antes de cambiarse de carril.")]
    [SerializeField, Min(0f)] float segundosDireccional = 0.4f;
    [Tooltip("Segundos que tarda en pasarse al otro carril.")]
    [SerializeField, Min(0.1f)] float segundosCambio = 0.5f;
    [Tooltip("Metros alrededor del camión (adelante y atrás) en los que nunca se cambia a su carril.")]
    [SerializeField, Min(0f)] float distanciaRespeto = 60f;
    [Tooltip("Qué tan fuerte acelera, en m/s².")]
    [SerializeField, Min(1f)] float aceleracion = 8f;
    [Tooltip("Qué tan fuerte frena cuando tiene un auto parado o el camión enfrente, en m/s².")]
    [SerializeField, Min(1f)] float frenadoFuerte = 14f;

    [Header("Direccionales")]
    [Tooltip("Las del lado izquierdo, adelante y atrás. Sólo parpadean las del lado al que se cambia.")]
    [SerializeField] Renderer[] direccionalesIzquierda;
    [SerializeField] Renderer[] direccionalesDerecha;
    [SerializeField, Min(0.5f)] float parpadeosPorSegundo = 2.5f;

    const float MedioLargo = 2.15f;        // el auto mide 4.3 m de largo
    const float MedioLargoCamion = 4.3f;   // y el camión, 8.6 m
    const float Separacion = 1f;           // los metros que deja al formarse detrás de un auto parado

    Rigidbody cuerpo;
    CamionRuta camion;
    Rigidbody cuerpoCamion;
    GeneradorAvenida generador;
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
    public bool Loco { get; private set; }
    public int Carril => carril;
    public int CarrilDestino => carrilDestino;
    public float VelocidadKmh => velocidad * 3.6f;
    public float AvanceCambio => EstadoActual == Estado.Cambiando ? Mathf.Clamp01(tiempoEnEstado / segundosCambio) : 0f;
    public bool DireccionalPrendida { get; private set; }

    float VelocidadCamion => camion.VelocidadActual / 3.6f;
    float Ventaja => (Loco ? ventajaLocoKmh : ventajaKmh) / 3.6f;

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        PrenderDireccionales(0);
    }

    // Hasta dónde llega su trompa, en metros delante del centro del camión, cuando termina de pasarlo (su cajuela 4 m delante de
    // la trompa del camión), si sale "detras" metros atrás y el camión va a "velocidadCamion" m/s. El generador revisa que ese
    // tramo de su carril esté sin autos parados antes de sacarlo, para que no se atore a medio rebase.
    public float MetrosParaRebasar(float velocidadCamion, float detras, bool loco)
    {
        float ventaja = (loco ? ventajaLocoKmh : ventajaKmh) / 3.6f;
        float segundos = (detras + MedioLargoCamion + 4f + MedioLargo) / ventaja;
        return MedioLargoCamion + 4f + 2f * MedioLargo + velocidadCamion * segundos;
    }

    // Lo llama el generador al sacarlo detrás del camión, en un carril de junto.
    public void Arrancar(CamionRuta camionQueRebasa, GeneradorAvenida generadorDeLaAvenida, int carrilInicial, float ancho, int carriles, bool loco)
    {
        generador = generadorDeLaAvenida;
        limite = carriles / 2;
        camion = camionQueRebasa;
        cuerpoCamion = camion.GetComponent<Rigidbody>();
        carril = carrilDestino = carrilInicial;
        anchoCarril = ancho;
        Loco = loco;
        velocidad = VelocidadCamion + Ventaja;
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
        float objetivo = VelocidadCamion + Ventaja;
        MirarAdelante(pos);
        switch (EstadoActual)
        {
            case Estado.Rebasando:
                if (velocidadDeEnfrente < 1f && cerca < 6f + velocidad * segundosParaEsquivar)
                {
                    Esquivar(pos);
                }
                if (EstadoActual == Estado.Rebasando && velocidad < 0.3f && velocidadDeEnfrente < 1f && cerca < Separacion + 1f)
                {
                    Cambiar(Estado.Formado);   // encontró su lugar detrás de un auto parado y ahí se queda
                }
                break;
            case Estado.Direccional:
                Parpadear();
                if (tiempoEnEstado >= segundosDireccional)
                {
                    if (!PuedeIrA(carrilDestino, pos))
                    {
                        // Mientras ponía la direccional cambió algo (el camión se acercó o se ocupó el carril): desiste.
                        PrenderDireccionales(0);
                        carrilDestino = carril;
                        Cambiar(Estado.Rebasando);
                        break;
                    }
                    desdeX = pos.x;
                    Cambiar(Estado.Cambiando);
                }
                break;
            case Estado.Cambiando:
                Parpadear();
                if (carrilDestino != carril && carrilDestino == camion.Carril && Mathf.Abs(pos.z - cuerpoCamion.position.z) < distanciaRespeto * 0.5f)
                {
                    // El camión se pasó al carril a donde iba: regresa al suyo.
                    carrilDestino = carril;
                    desdeX = pos.x;
                    tiempoEnEstado = 0f;
                }
                float avance = AvanceCambio;
                pos.x = Mathf.Lerp(desdeX, carrilDestino * anchoCarril, Mathf.SmoothStep(0f, 1f, avance));
                if (avance >= 1f)
                {
                    carril = carrilDestino;
                    PrenderDireccionales(0);
                    Cambiar(Estado.Rebasando);
                }
                break;
            case Estado.Formado:
                objetivo = 0f;
                if (velocidadDeEnfrente >= 1f || cerca > Separacion + 3f)
                {
                    Cambiar(Estado.Rebasando);   // lo de enfrente se movió
                }
                break;
        }
        float porEnfrente = FrenarDetras(objetivo);
        float tasa = porEnfrente < velocidad ? frenadoFuerte : aceleracion;
        velocidad = Mathf.MoveTowards(velocidad, porEnfrente, tasa * dt);
        pos.z += velocidad * dt;
        cuerpo.MovePosition(pos);
    }

    // Un auto parado enfrente: se pasa a un carril de junto que esté libre y que no sea el del camión cerca de él.
    void Esquivar(Vector3 pos)
    {
        foreach (int destino in new[] { carril - 1, carril + 1 })
        {
            if (PuedeIrA(destino, pos))
            {
                carrilDestino = destino;
                Cambiar(Estado.Direccional);
                return;
            }
        }
    }

    // A dónde puede cambiarse: un carril que exista, sin autos parados cerca, que no sea el del camión mientras estén cerca, y
    // que, cerca del camión, no sea el único libre de una fila próxima (no le estorba la salida).
    bool PuedeIrA(int destino, Vector3 pos)
    {
        if (destino < -limite || destino > limite || destino == carril)
        {
            return false;
        }
        float dz = pos.z - cuerpoCamion.position.z;
        bool cercaDelCamion = Mathf.Abs(dz) < distanciaRespeto;
        if (cercaDelCamion && destino == camion.Carril)
        {
            return false;
        }
        float hasta = pos.z + MedioLargo + Mathf.Max(14f, velocidad * 1f);
        if (!CarrilLibre(destino, pos.z - MedioLargo - 2f, hasta))
        {
            return false;
        }
        return !(cercaDelCamion && generador != null && generador.EsElUnicoLibre(destino, pos.z - 5f, pos.z + VelocidadCamion * 3f + 10f));
    }

    bool CarrilLibre(int c, float desde, float hasta)
    {
        var centro = new Vector3(c * anchoCarril, 0.8f, (desde + hasta) / 2f);
        var mitad = new Vector3(anchoCarril * 0.4f, 0.5f, (hasta - desde) / 2f);
        foreach (var otro in Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
        {
            if ((otro.CompareTag("Obstaculo") && otro.attachedRigidbody != cuerpo) || otro.attachedRigidbody == cuerpoCamion)
            {
                return false;
            }
        }
        return true;
    }

    // Lo que tiene enfrente en su carril: un auto parado (velocidad 0), el camión u otro auto que rebasa; o infinito.
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
    // velocidad de golpe. Nunca choca con el camión ni con un auto parado.
    float FrenarDetras(float objetivo)
    {
        if (cerca == float.MaxValue)
        {
            return objetivo;
        }
        if (cerca < Separacion)
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

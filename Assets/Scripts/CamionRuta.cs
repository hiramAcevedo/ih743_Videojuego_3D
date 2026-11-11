using UnityEngine;
using UnityEngine.InputSystem;

// El camión de ruta, personaje principal. Se mueve en los tres ejes con su Rigidbody:
//   Z  avanza solo por la avenida, cada vez más rápido según los metros recorridos (la curva de dificultad), de 36 km/h a un
//      tope de 200 km/h;
//   X  cambia entre tres carriles con las flechas, A y D o los botones de la pantalla;
//   Y  brinca con la barra, W, flecha arriba o el botón BRINCAR, y sube y baja al pasar los topes; desde 120 km/h hay que
//      brincarlos: pasar uno sin brincar lo avisa con el evento TopeSinBrincar (ControlPartida le quita una vida y lo frena).
// Si choca con un obstáculo se detiene y lo avisa con el evento Choco; ControlPartida lo regresa a la salida con Reiniciar.
// En un nivel (4.5), la velocidad la dice el nivel (NivelRuta) y no la del Inspector; al pasar la terminal el camión deja de
// obedecer los controles, frena hasta pararse y lo avisa con el evento Llego.
[RequireComponent(typeof(Rigidbody))]
public class CamionRuta : MonoBehaviour
{
    [Header("Nivel")]
    [Tooltip("El nivel que se juega en la escena: su velocidad inicial, su velocidad máxima y su curva mandan sobre las de abajo. " +
             "Vacío en la ruta libre.")]
    [SerializeField] NivelRuta nivel;
    [Tooltip("Al pasar la terminal, cuántos km/h por segundo frena hasta pararse.")]
    [SerializeField, Min(1f)] float frenadoEnTerminal = 45f;

    [Header("Avance (eje Z) y curva de dificultad")]
    [Tooltip("Velocidad al salir, en km/h.")]
    [SerializeField] float velocidadInicial = 36f;
    [Tooltip("El tope: velocidad en km/h a la que llega en los metros de metrosHastaMaxima. De ahí ya no sube.")]
    [SerializeField] float velocidadMaxima = 200f;
    [Tooltip("Metros recorridos en los que llega al tope de velocidad.")]
    [SerializeField] float metrosHastaMaxima = 5000f;
    [Tooltip("Cómo crece la velocidad con los metros recorridos: 0 es la salida y 1, metrosHastaMaxima. Sube rápido al principio " +
             "(72 km/h a los 380 m), luego más despacio (95 km/h a los 1,000 m, 110 a los 1,500 m) y sigue parejo, unos 2.5 km/h " +
             "cada 100 m (150 km/h a los 3,000 m), hasta el tope.")]
    [SerializeField] AnimationCurve curvaDificultad = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Carriles (eje X)")]
    [SerializeField] float anchoCarril = 3.2f;
    [SerializeField] int carriles = 3;
    [SerializeField] float rapidezCambio = 9f;

    [Header("Brinco (eje Y)")]
    [SerializeField] float impulsoBrinco = 6.5f;
    [SerializeField] float distanciaSuelo = 0.25f;
    [Tooltip("Lo más que puede subir el camión sin brincar: al pasar un tope, la rampa lo levanta y no lo lanza.")]
    [SerializeField] float subidaMaxima = 2.2f;
    [Tooltip("Desde esta velocidad, en km/h, pasar un tope sin brincar cuesta una vida.")]
    [SerializeField] float brincoObligatorioDesde = 120f;
    [Tooltip("Después de frenar por un tope, cuántos km/h por segundo recupera hasta volver a la curva de dificultad.")]
    [SerializeField, Min(1f)] float recuperacionKmhPorSegundo = 25f;

    [Header("Avisos")]
    [SerializeField] Navegacion navegacion;

    const float KmhPorMetroPorSegundo = 3.6f;   // Unity mide la velocidad en metros por segundo

    Rigidbody cuerpo;
    BoxCollider caja;
    int carril;              // -1 izquierda, 0 centro, 1 derecha
    bool brincoPedido;
    bool brincando;
    bool detenido;
    float salidaZ;
    float ordenZ;            // la velocidad hacia adelante que se pidió en el paso anterior, en m/s

    public event System.Action Choco;
    public event System.Action TopeSinBrincar;
    public event System.Action Llego;

    public int Carril => carril;
    public bool Detenido => detenido;
    public bool Llegando { get; private set; }     // ya pasó la terminal y va frenando
    public bool EnTerminal { get; private set; }   // ya se paró en la terminal
    public NivelRuta Nivel => nivel;
    public bool EnElSuelo { get; private set; }
    public float Metros => Mathf.Max(0f, cuerpo.position.z - salidaZ);
    public float VelocidadActual { get; private set; }   // en km/h
    public Vector3 PosicionSalida { get; private set; }
    public bool CambiandoDeCarril => Mathf.Abs(cuerpo.position.x - carril * anchoCarril) > 0.25f;   // va entre dos carriles
    public Collider UltimoChoque { get; private set; }   // con qué chocó: un auto parado, uno que venía rebasando o un tope
    public Collider UltimoTope { get; private set; }     // el último tope que pasó sin brincar a más de 120 km/h

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        caja = GetComponent<BoxCollider>();
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cuerpo.constraints = RigidbodyConstraints.FreezeRotation;
        PosicionSalida = cuerpo.position;
        salidaZ = PosicionSalida.z;
        if (nivel != null)
        {
            velocidadInicial = nivel.velocidadInicial;
            velocidadMaxima = nivel.velocidadMaxima;
            metrosHastaMaxima = nivel.metrosHastaMaxima;
            curvaDificultad = nivel.curvaDificultad;
        }
        VelocidadActual = velocidadInicial;
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null || detenido || Llegando || (navegacion != null && navegacion.EnPausa))
        {
            return;
        }
        if (teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame)
        {
            CarrilIzquierdo();
        }
        if (teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame)
        {
            CarrilDerecho();
        }
        if (teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame || teclado.upArrowKey.wasPressedThisFrame)
        {
            Brincar();
        }
    }

    void FixedUpdate()
    {
        EnElSuelo = TocaSuelo();
        var v = cuerpo.linearVelocity;
        if (detenido)
        {
            cuerpo.linearVelocity = new Vector3(0f, v.y, 0f);
            return;
        }
        float destinoX = carril * anchoCarril;
        float vx = Mathf.Clamp((destinoX - cuerpo.position.x) * rapidezCambio, -anchoCarril * rapidezCambio, anchoCarril * rapidezCambio);
        float vy = v.y;
        if (brincoPedido)
        {
            brincoPedido = false;
            if (EnElSuelo)
            {
                vy = impulsoBrinco;
                brincando = true;
            }
        }
        else if (brincando && EnElSuelo && vy <= 0f)
        {
            brincando = false;
        }
        if (!brincando)
        {
            vy = Mathf.Min(vy, subidaMaxima);
            // Destrabe: si en el paso anterior algo que no es obstáculo lo frenó de frente (a mucha velocidad puede pegar con la
            // orilla de un tope en vez de subir por la rampa), sube con la rapidez de la rampa hasta librarla.
            if (ordenZ > 1f && v.z < ordenZ * 0.5f)
            {
                vy = subidaMaxima;
            }
        }
        if (Llegando)
        {
            // Pasó la terminal: frena parejo hasta pararse y avisa una sola vez.
            VelocidadActual = Mathf.MoveTowards(VelocidadActual, 0f, frenadoEnTerminal * Time.fixedDeltaTime);
            if (VelocidadActual <= 0f && !EnTerminal)
            {
                EnTerminal = true;
                Llego?.Invoke();
            }
        }
        else
        {
            // La curva manda; si un tope lo frenó, recupera poco a poco hasta alcanzarla otra vez.
            VelocidadActual = Mathf.Min(VelocidadEn(Metros), VelocidadActual + recuperacionKmhPorSegundo * Time.fixedDeltaTime);
        }
        ordenZ = VelocidadActual / KmhPorMetroPorSegundo;
        cuerpo.linearVelocity = new Vector3(vx, vy, ordenZ);
    }

    // ControlPartida lo llama al pasar la terminal del nivel.
    public void LlegarATerminal()
    {
        if (!detenido)
        {
            Llegando = true;
        }
    }

    // La velocidad en km/h que lleva a esos metros de la salida, según la curva de dificultad. El generador la usa para saber
    // cuánto tiempo hay entre una fila de autos y la siguiente.
    public float VelocidadEn(float metros)
    {
        return Mathf.Lerp(velocidadInicial, velocidadMaxima, curvaDificultad.Evaluate(Mathf.Clamp01(metros / metrosHastaMaxima)));
    }

    // Baja la velocidad de golpe, como al pegar en un tope sin brincar; luego la recupera.
    public void Frenar(float kmh)
    {
        VelocidadActual = Mathf.Min(VelocidadActual, kmh);
    }

    // Termina la partida como un choque: con un obstáculo o al quedarse sin vidas en un tope.
    public void Detener(Collider causa)
    {
        if (detenido || Llegando)
        {
            return;
        }
        detenido = true;
        UltimoChoque = causa;
        Choco?.Invoke();
    }

    // Los botones de la pantalla llaman a estos tres métodos.
    public void CarrilIzquierdo() => CambiarCarril(-1);
    public void CarrilDerecho() => CambiarCarril(1);

    public void Brincar()
    {
        if (!detenido && !Llegando)
        {
            brincoPedido = true;
        }
    }

    void CambiarCarril(int direccion)
    {
        if (detenido || Llegando)
        {
            return;
        }
        int limite = carriles / 2;
        carril = Mathf.Clamp(carril + direccion, -limite, limite);
    }

    bool TocaSuelo()
    {
        var b = caja.bounds;
        var origen = new Vector3(b.center.x, b.min.y + 0.1f, b.center.z);
        return Physics.BoxCast(origen, new Vector3(b.extents.x * 0.9f, 0.05f, b.extents.z * 0.9f), Vector3.down,
            Quaternion.identity, distanciaSuelo, ~0, QueryTriggerInteraction.Ignore);
    }

    // Vuelve a la salida en el carril central, con la velocidad inicial, para empezar otra partida.
    public void Reiniciar()
    {
        detenido = false;
        Llegando = false;
        EnTerminal = false;
        UltimoChoque = null;
        UltimoTope = null;
        brincoPedido = false;
        brincando = false;
        carril = 0;
        VelocidadActual = velocidadInicial;
        ordenZ = 0f;
        cuerpo.linearVelocity = Vector3.zero;
        cuerpo.position = PosicionSalida;
        transform.position = PosicionSalida;
        Physics.SyncTransforms();
    }

    void OnCollisionEnter(Collision choque)
    {
        if (detenido || Llegando)
        {
            return;
        }
        if (choque.collider.CompareTag("Obstaculo"))
        {
            Detener(choque.collider);
        }
        else if (choque.collider.CompareTag("Tope") && choque.collider != UltimoTope && !brincando && VelocidadActual >= brincoObligatorioDesde)
        {
            // Cada tope cuesta una vida como mucho, aunque el camión lo vuelva a tocar al pasarlo.
            UltimoTope = choque.collider;
            TopeSinBrincar?.Invoke();
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

// El camión de ruta, personaje principal. Se mueve en los tres ejes con su Rigidbody:
//   Z  avanza solo por la avenida, cada vez más rápido según los metros recorridos (la curva de dificultad), de 36 km/h a un
//      tope de 110 km/h;
//   X  cambia entre tres carriles con las flechas, A y D o los botones de la pantalla;
//   Y  brinca con la barra, W, flecha arriba o el botón BRINCAR, y sube y baja al pasar los topes.
// Si choca con un obstáculo se detiene y lo avisa con el evento Choco; ControlPartida lo regresa a la salida con Reiniciar.
[RequireComponent(typeof(Rigidbody))]
public class CamionRuta : MonoBehaviour
{
    [Header("Avance (eje Z) y curva de dificultad")]
    [Tooltip("Velocidad al salir, en km/h.")]
    [SerializeField] float velocidadInicial = 36f;
    [Tooltip("El tope: velocidad en km/h a la que llega en los metros de metrosHastaMaxima. De ahí ya no sube.")]
    [SerializeField] float velocidadMaxima = 110f;
    [Tooltip("Metros recorridos en los que llega al tope de velocidad.")]
    [SerializeField] float metrosHastaMaxima = 1500f;
    [Tooltip("Cómo crece la velocidad con los metros recorridos: 0 es la salida y 1, metrosHastaMaxima. Sube rápido al principio " +
             "(72 km/h a los 380 m) y luego más despacio (95 km/h a los 1,000 m) hasta el tope.")]
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

    public int Carril => carril;
    public bool Detenido => detenido;
    public bool EnElSuelo { get; private set; }
    public float Metros => Mathf.Max(0f, cuerpo.position.z - salidaZ);
    public float VelocidadActual { get; private set; }   // en km/h
    public Vector3 PosicionSalida { get; private set; }
    public Collider UltimoChoque { get; private set; }   // con qué chocó: un auto parado o uno que venía rebasando

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        caja = GetComponent<BoxCollider>();
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cuerpo.constraints = RigidbodyConstraints.FreezeRotation;
        PosicionSalida = cuerpo.position;
        salidaZ = PosicionSalida.z;
        VelocidadActual = velocidadInicial;
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null || detenido || (navegacion != null && navegacion.EnPausa))
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
        VelocidadActual = VelocidadEn(Metros);
        ordenZ = VelocidadActual / KmhPorMetroPorSegundo;
        cuerpo.linearVelocity = new Vector3(vx, vy, ordenZ);
    }

    // La velocidad en km/h que lleva a esos metros de la salida, según la curva de dificultad. El generador la usa para saber
    // cuánto tiempo hay entre una fila de autos y la siguiente.
    public float VelocidadEn(float metros)
    {
        return Mathf.Lerp(velocidadInicial, velocidadMaxima, curvaDificultad.Evaluate(Mathf.Clamp01(metros / metrosHastaMaxima)));
    }

    // Los botones de la pantalla llaman a estos tres métodos.
    public void CarrilIzquierdo() => CambiarCarril(-1);
    public void CarrilDerecho() => CambiarCarril(1);

    public void Brincar()
    {
        if (!detenido)
        {
            brincoPedido = true;
        }
    }

    void CambiarCarril(int direccion)
    {
        if (detenido)
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
        UltimoChoque = null;
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
        if (!detenido && choque.collider.CompareTag("Obstaculo"))
        {
            detenido = true;
            UltimoChoque = choque.collider;
            Choco?.Invoke();
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

// El camión de ruta, personaje principal. Se mueve en los tres ejes con su Rigidbody:
//   Z  avanza solo por la avenida, cada vez más rápido según los metros recorridos (la curva de dificultad);
//   X  cambia entre tres carriles con las flechas, A y D o los botones de la pantalla;
//   Y  brinca con la barra, W, flecha arriba o el botón BRINCAR, y sube y baja al pasar los topes.
// Si choca con un obstáculo se detiene y avisa a la navegación; si llega a la base, termina el tramo de prueba.
[RequireComponent(typeof(Rigidbody))]
public class CamionRuta : MonoBehaviour
{
    [Header("Avance (eje Z) y curva de dificultad")]
    [Tooltip("Velocidad al salir, en metros por segundo.")]
    [SerializeField] float velocidadInicial = 10f;
    [Tooltip("Velocidad a la que llega en los metros de metrosHastaMaxima.")]
    [SerializeField] float velocidadMaxima = 20f;
    [SerializeField] float metrosHastaMaxima = 380f;
    [Tooltip("Cómo crece la velocidad con los metros recorridos: 0 es la salida y 1, metrosHastaMaxima.")]
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

    Rigidbody cuerpo;
    BoxCollider caja;
    int carril;              // -1 izquierda, 0 centro, 1 derecha
    bool brincoPedido;
    bool brincando;
    bool detenido;
    float salidaZ;

    public int Carril => carril;
    public bool Detenido => detenido;
    public bool EnElSuelo { get; private set; }
    public float Metros => Mathf.Max(0f, cuerpo.position.z - salidaZ);
    public float VelocidadActual { get; private set; }

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        caja = GetComponent<BoxCollider>();
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cuerpo.constraints = RigidbodyConstraints.FreezeRotation;
        salidaZ = cuerpo.position.z;
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
        }
        float avance = curvaDificultad.Evaluate(Mathf.Clamp01(Metros / metrosHastaMaxima));
        VelocidadActual = Mathf.Lerp(velocidadInicial, velocidadMaxima, avance);
        cuerpo.linearVelocity = new Vector3(vx, vy, VelocidadActual);
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

    void OnCollisionEnter(Collision choque)
    {
        if (!detenido && choque.collider.CompareTag("Obstaculo"))
        {
            detenido = true;
            if (navegacion != null)
            {
                navegacion.MostrarChoque();
            }
        }
    }

    void OnTriggerEnter(Collider otro)
    {
        if (!detenido && otro.CompareTag("Finish"))
        {
            detenido = true;
            if (navegacion != null)
            {
                navegacion.MostrarFin();
            }
        }
    }
}

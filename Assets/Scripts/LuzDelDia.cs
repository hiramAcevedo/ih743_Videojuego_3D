using UnityEngine;

// La hora del día de la avenida, como el color de fondo de cada Stage del tutorial. Cada etapa (EtapaAvenida) trae el color del
// cielo, el del sol, su fuerza y su altura, y el de la luz ambiental. LuzDelDia lee la etapa del metro donde va el camión y pasa
// de un ambiente al otro en unos segundos, sin saltos: mañana en el tráfico ligero, mediodía en el medio, atardecer en la hora
// pico y anochecer en el viernes de quincena. El color del cielo es el fondo de la cámara y el de la niebla. Al jugar de nuevo
// regresa a la mañana de golpe.
public class LuzDelDia : MonoBehaviour
{
    [SerializeField] CamionRuta camion;
    [SerializeField] GeneradorAvenida generador;
    [SerializeField] Light sol;
    [SerializeField] Camera camara;
    [Tooltip("Segundos que tarda en pasar de la luz de una etapa a la de la siguiente.")]
    [SerializeField, Min(0f)] float segundosTransicion = 3f;

    // Los tonos de la luz del horizonte y del suelo con la luz ambiental de la mañana; con otra luz se oscurecen o aclaran igual.
    static readonly Color Horizonte = new Color32(150, 146, 140, 255);
    static readonly Color Suelo = new Color32(90, 80, 72, 255);
    const float GrisDeLaManana = 0.79f;

    struct Ambiente
    {
        public Color cielo, sol, ambiente;
        public float intensidad, altura, giro;
    }

    Ambiente desde, hacia, actual;
    EtapaAvenida etapa;
    float inicio = float.NegativeInfinity;

    public EtapaAvenida Etapa => etapa;
    public Color Cielo => actual.cielo;
    public bool EnTransicion => Time.time - inicio < segundosTransicion;

    void Start() => Reiniciar();

    // La luz de la primera etapa, de golpe: al empezar y al jugar de nuevo.
    public void Reiniciar()
    {
        etapa = generador.EtapaEn(0f);
        if (etapa == null)
        {
            enabled = false;
            return;
        }
        actual = desde = hacia = De(etapa);
        inicio = float.NegativeInfinity;
        Aplicar(actual);
    }

    void Update()
    {
        var ahora = generador.EtapaEn(camion.Metros);
        if (ahora != null && ahora != etapa)
        {
            etapa = ahora;
            desde = actual;
            hacia = De(ahora);
            inicio = Time.time;
        }
        float avance = segundosTransicion <= 0f ? 1f : Mathf.Clamp01((Time.time - inicio) / segundosTransicion);
        actual = Mezcla(desde, hacia, Mathf.SmoothStep(0f, 1f, avance));
        Aplicar(actual);
    }

    static Ambiente De(EtapaAvenida e) => new Ambiente
    {
        cielo = e.colorCielo, sol = e.colorSol, ambiente = e.colorAmbiente,
        intensidad = e.intensidadSol, altura = e.alturaSol, giro = e.giroSol,
    };

    static Ambiente Mezcla(Ambiente a, Ambiente b, float t) => new Ambiente
    {
        cielo = Color.Lerp(a.cielo, b.cielo, t),
        sol = Color.Lerp(a.sol, b.sol, t),
        ambiente = Color.Lerp(a.ambiente, b.ambiente, t),
        intensidad = Mathf.Lerp(a.intensidad, b.intensidad, t),
        altura = Mathf.Lerp(a.altura, b.altura, t),
        giro = Mathf.LerpAngle(a.giro, b.giro, t),
    };

    void Aplicar(Ambiente a)
    {
        if (camara != null)
        {
            camara.backgroundColor = a.cielo;
        }
        RenderSettings.fogColor = a.cielo;
        float brillo = a.ambiente.grayscale / GrisDeLaManana;
        RenderSettings.ambientSkyColor = a.ambiente;
        RenderSettings.ambientEquatorColor = Horizonte * brillo;
        RenderSettings.ambientGroundColor = Suelo * brillo;
        if (sol != null)
        {
            sol.color = a.sol;
            sol.intensity = a.intensidad;
            sol.transform.rotation = Quaternion.Euler(a.altura, a.giro, 0f);
        }
    }
}

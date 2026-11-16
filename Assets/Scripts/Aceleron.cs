using System;
using UnityEngine;

// El acelerón de la 4.6, como la súper velocidad del cap. 11 de HelixJump: un estado de velocidad extra que prende un evento
// del escenario (la onda verde de algunos cruces o el poder sin topes) y que se apaga solo. Mientras dura, el camión va
// "kmhDeMas" arriba de su curva (CamionRuta lo lee), las dos estelas de las esquinas de atrás (Trail Renderer) emiten y la
// cámara abre un poco su campo de visión. Al terminar, las estelas dejan de emitir y se borran solas en "segundosEstela", el
// tiempo de vida de cada punto: del primer punto de estela a que se borra el último no pasan 3 segundos. Al acabar de borrarse
// se limpian (Clear), porque sin emitir el Trail Renderer sigue guardando puntos que no se ven. Pisar el freno, chocar o llegar
// a la terminal lo cortan.
[RequireComponent(typeof(CamionRuta))]
public class Aceleron : MonoBehaviour
{
    // Lo más que puede verse la estela, por el criterio de la actividad.
    public const float SegundosMaximos = 3f;

    [Header("El acelerón")]
    [Tooltip("Km/h de más sobre la curva de dificultad mientras dura.")]
    [SerializeField, Range(10f, 60f)] float kmhDeMas = 35f;
    [Tooltip("Segundos que dura. Junto con el borrado de la estela no pasa de 3 segundos.")]
    [SerializeField, Range(0.5f, 2.5f)] float segundos = 2.5f;
    [Tooltip("Qué tan rápido llega a la velocidad del acelerón, en km/h por segundo.")]
    [SerializeField, Min(10f)] float subidaKmhPorSegundo = 140f;

    [Header("Estela (Trail Renderer)")]
    [Tooltip("Las estelas de las esquinas de atrás del camión. Sólo emiten mientras dura el acelerón.")]
    [SerializeField] TrailRenderer[] estelas;
    [Tooltip("Segundos que tarda en borrarse la estela: el tiempo de vida de cada punto.")]
    [SerializeField, Range(0.1f, 0.5f)] float segundosEstela = 0.4f;

    [Header("Cámara")]
    [Tooltip("La cámara que sigue al camión: abre su campo de visión mientras dura el acelerón.")]
    [SerializeField] Camera camara;
    [Tooltip("Grados que se abre el campo de visión.")]
    [SerializeField, Range(0f, 15f)] float campoDeMas = 7f;
    [Tooltip("Grados por segundo con que se abre y se cierra.")]
    [SerializeField, Min(1f)] float rapidezCampo = 30f;

    CamionRuta camion;
    float hasta = -1f;     // Time.time en que termina; -1 sin acelerón
    float borradaEn = -1f; // Time.time en que la estela termina de borrarse; -1 si no se está borrando
    float campoBase;

    public event Action<string> Empezo;
    public event Action Termino;

    public bool Activo => hasta > 0f;
    public float KmhDeMas => kmhDeMas;
    public float Segundos => segundos;
    public float SegundosEstela => segundosEstela;
    public float SubidaKmhPorSegundo => subidaKmhPorSegundo;
    public string Motivo { get; private set; } = "";
    public int Veces { get; private set; }
    // Lo que le queda, de 1 al empezar a 0 al terminar: la barra del tablero (un Slider, como la del cap. 12).
    public float Restante => Activo ? Mathf.Clamp01((hasta - Time.time) / segundos) : 0f;
    public TrailRenderer[] Estelas => estelas;

    // Si la estela se ve: mientras emite y mientras se borran sus últimos puntos.
    public bool EstelaVisible => Activo || borradaEn > 0f;

    void Awake()
    {
        camion = GetComponent<CamionRuta>();
        if (camara == null)
        {
            camara = Camera.main;
        }
        campoBase = camara != null ? camara.fieldOfView : 60f;
        foreach (var e in estelas)
        {
            if (e != null)
            {
                e.time = segundosEstela;
                e.emitting = false;
                e.Clear();
            }
        }
    }

    void OnValidate()
    {
        segundos = Mathf.Min(segundos, SegundosMaximos - segundosEstela);
    }

    // Lo llaman la franja de la onda verde y el poder sin topes; "motivo" es el aviso que sale en el tablero.
    public void Acelerar(string motivo)
    {
        if (camion.Detenido || camion.Llegando)
        {
            return;
        }
        Motivo = motivo;
        hasta = Time.time + segundos;
        borradaEn = -1f;
        Veces++;
        foreach (var e in estelas)
        {
            if (e != null)
            {
                e.time = segundosEstela;
                e.Clear();
                e.emitting = true;
            }
        }
        Empezo?.Invoke(motivo);
    }

    // Se acaba: las estelas dejan de emitir y se borran solas en "segundosEstela".
    public void Cortar()
    {
        if (!Activo)
        {
            return;
        }
        hasta = -1f;
        borradaEn = Time.time + segundosEstela;
        foreach (var e in estelas)
        {
            if (e != null)
            {
                e.emitting = false;
            }
        }
        Termino?.Invoke();
    }

    // Partida nueva: sin acelerón, sin estela y la cámara como estaba.
    public void Reiniciar()
    {
        hasta = -1f;
        borradaEn = -1f;
        foreach (var e in estelas)
        {
            if (e != null)
            {
                e.emitting = false;
                e.Clear();
            }
        }
        if (camara != null)
        {
            camara.fieldOfView = campoBase;
        }
    }

    void Update()
    {
        if (Activo && (Time.time >= hasta || camion.Detenido || camion.Llegando))
        {
            Cortar();
        }
        if (borradaEn > 0f && Time.time >= borradaEn)
        {
            borradaEn = -1f;
            foreach (var e in estelas)
            {
                if (e != null)
                {
                    e.Clear();
                }
            }
        }
        if (camara != null)
        {
            float objetivo = campoBase + (Activo ? campoDeMas : 0f);
            camara.fieldOfView = Mathf.MoveTowards(camara.fieldOfView, objetivo, rapidezCampo * Time.deltaTime);
        }
    }
}

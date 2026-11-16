using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// El poder sin topes (4.6), para pasar Monterrey. Cada tope que el camión pasa sin tocarlo (brincándolo) suma uno; con
// "brincosParaCargar" el poder queda guardado hasta usarlo y el indicador del tablero, arriba a la izquierda, lo dice. Se usa
// tocando el indicador o con la E (o solo, con "activarSolo"): durante "segundos", los topes que quedan a menos de "alcance"
// metros delante del camión se hunden en el asfalto y dejan de estorbar, y el camión da el acelerón con su estela, como con la
// onda verde. Lo brincado y el poder guardado se guardan con PlayerPrefs: pasan de Guadalajara a Monterrey y de una partida a
// la siguiente hasta usarlo, así que el poder ganado en un intento sirve en el siguiente.
public class PoderSinTopes : MonoBehaviour
{
    public const string ClaveBrincos = "PoderSinTopesBrincos";
    public const string ClaveGuardado = "PoderSinTopesGuardado";

    [Header("El poder")]
    [Tooltip("Topes brincados para cargar el poder.")]
    [SerializeField, Min(1)] int brincosParaCargar = 10;
    [Tooltip("Segundos sin topes.")]
    [SerializeField, Min(1f)] float segundos = 10f;
    [Tooltip("Metros delante del camión en que se hunden los topes mientras dura.")]
    [SerializeField, Min(20f)] float alcance = 160f;
    [Tooltip("Se usa solo en cuanto se carga, sin tocar el indicador.")]
    [SerializeField] bool activarSolo;
    [Tooltip("Segundos que tarda un tope en hundirse.")]
    [SerializeField, Min(0.05f)] float segundosHundirse = 0.35f;

    [Header("Referencias")]
    [SerializeField] CamionRuta camion;
    [SerializeField] GeneradorAvenida generador;
    [SerializeField] Navegacion navegacion;
    [SerializeField] Aviso aviso;

    [Header("El indicador del tablero")]
    [Tooltip("El indicador: se toca para usar el poder.")]
    [SerializeField] Button indicador;
    [SerializeField] Text texto;
    [Tooltip("La barra del indicador: se llena con los brincos y se vacía mientras dura el poder (Image de tipo Filled).")]
    [SerializeField] Image carga;
    [SerializeField] Color colorCargando = new Color32(255, 255, 255, 255);
    [SerializeField] Color colorListo = new Color32(255, 204, 51, 255);

    const float MedioLargoCamion = 4.3f;

    readonly HashSet<Collider> revisados = new HashSet<Collider>();
    readonly List<(Transform tope, float desde, Vector3 escala, Vector3 posicion)> hundiendose = new List<(Transform, float, Vector3, Vector3)>();
    float hasta = -1f;
    string escrito;

    public int Brincos { get; private set; }
    public bool Cargado { get; private set; }
    public bool Activo => hasta > 0f;
    public int Usos { get; private set; }
    public int TopesHundidos { get; private set; }
    public int BrincosParaCargar => brincosParaCargar;
    public float Segundos => segundos;
    public float Alcance => alcance;
    public float Restante => Activo ? Mathf.Clamp01((hasta - Time.time) / segundos) : 0f;

    void Awake()
    {
        Brincos = Mathf.Clamp(PlayerPrefs.GetInt(ClaveBrincos, 0), 0, brincosParaCargar);
        Cargado = PlayerPrefs.GetInt(ClaveGuardado, 0) == 1 || Brincos >= brincosParaCargar;
    }

    void Start() => Pintar();

    void Guardar()
    {
        PlayerPrefs.SetInt(ClaveBrincos, Brincos);
        PlayerPrefs.SetInt(ClaveGuardado, Cargado ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (camion == null || generador == null)
        {
            return;
        }
        bool enPausa = navegacion != null && navegacion.EnPausa;
        if (!enPausa)
        {
            ContarBrincos();
            if (Cargado && (activarSolo || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)))
            {
                Usar();
            }
            if (Activo)
            {
                if (Time.time >= hasta || camion.Detenido)
                {
                    hasta = -1f;
                }
                else
                {
                    HundirTopesDelante();
                }
            }
        }
        AnimarHundidos();
        Pintar();
    }

    // Lo que hace el indicador al tocarlo.
    public void Usar()
    {
        if (!Cargado || Activo || camion.Detenido || camion.Llegando || (navegacion != null && navegacion.EnPausa))
        {
            return;
        }
        Cargado = false;
        Brincos = 0;
        Guardar();
        hasta = Time.time + segundos;
        Usos++;
        HundirTopesDelante();
        if (camion.Aceleron != null)
        {
            camion.Aceleron.Acelerar("¡SIN TOPES!");
        }
    }

    // Partida nueva: sin poder activo; lo brincado y el poder guardado se quedan.
    public void Reiniciar()
    {
        hasta = -1f;
        revisados.Clear();
        hundiendose.Clear();
        Pintar();
    }

    // Un tope cuenta como brincado cuando el camión ya lo pasó entero sin tocarlo. Mientras el poder está guardado o activo no
    // se cuentan: ya está lleno.
    void ContarBrincos()
    {
        float trasera = camion.transform.position.z - MedioLargoCamion;
        foreach (var tope in generador.Topes)
        {
            if (tope == null || revisados.Contains(tope) || !tope.enabled)
            {
                continue;
            }
            if (tope.bounds.max.z >= trasera)
            {
                continue;
            }
            revisados.Add(tope);
            if (camion.Detenido || camion.Toco(tope) || Cargado || Activo)
            {
                continue;
            }
            Brincos++;
            if (Brincos >= brincosParaCargar)
            {
                Brincos = brincosParaCargar;
                Cargado = true;
                if (aviso != null && !activarSolo)
                {
                    aviso.Mostrar("¡Poder sin topes listo!", colorListo, 1.8f);
                }
            }
            Guardar();
        }
        if (revisados.Count > 64)
        {
            revisados.RemoveWhere(t => t == null);
        }
    }

    // Los topes que quedan delante, a menos de "alcance" metros, dejan de chocar y se hunden en el asfalto.
    void HundirTopesDelante()
    {
        float z = camion.transform.position.z;
        foreach (var tope in generador.Topes)
        {
            if (tope == null || !tope.enabled)
            {
                continue;
            }
            float dz = tope.transform.position.z - z;
            if (dz < -MedioLargoCamion || dz > alcance)
            {
                continue;
            }
            tope.enabled = false;
            revisados.Add(tope);
            var t = tope.transform;
            hundiendose.Add((t, Time.time, t.localScale, t.localPosition));
            TopesHundidos++;
        }
    }

    void AnimarHundidos()
    {
        for (int i = hundiendose.Count - 1; i >= 0; i--)
        {
            var (tope, desde, escala, posicion) = hundiendose[i];
            if (tope == null)
            {
                hundiendose.RemoveAt(i);
                continue;
            }
            float k = Mathf.Clamp01((Time.time - desde) / segundosHundirse);
            float alto = Mathf.Lerp(1f, 0.02f, k * k);
            tope.localScale = new Vector3(escala.x, escala.y * alto, escala.z);
            tope.localPosition = posicion + Vector3.down * 0.02f * k;
            if (k >= 1f)
            {
                foreach (var r in tope.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
                hundiendose.RemoveAt(i);
            }
        }
    }

    // El indicador: «Topes 4/10» mientras se carga; «¡SIN TOPES!» latiendo, con la barra llena, cuando está listo; los segundos que
    // le quedan mientras dura. La barra se llena con los brincos y se vacía con el poder.
    void Pintar()
    {
        string nuevo;
        float lleno;
        Color color;
        if (Activo)
        {
            nuevo = "Sin topes " + Mathf.CeilToInt(hasta - Time.time) + " s";
            lleno = Restante;
            color = colorListo;
        }
        else if (Cargado)
        {
            nuevo = "¡SIN TOPES!";
            lleno = 1f;
            color = colorListo;
        }
        else
        {
            nuevo = "Topes " + Brincos + "/" + brincosParaCargar;
            lleno = Brincos / (float)brincosParaCargar;
            color = colorCargando;
        }
        if (texto != null && nuevo != escrito)
        {
            texto.text = nuevo;
            texto.color = color;
            escrito = nuevo;
        }
        if (carga != null)
        {
            carga.fillAmount = lleno;
            carga.color = Activo || Cargado ? colorListo : colorCargando;
        }
        if (indicador != null)
        {
            float latido = Cargado ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 7f) : 1f;
            indicador.transform.localScale = new Vector3(latido, latido, 1f);
        }
    }
}

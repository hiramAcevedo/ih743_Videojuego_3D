using System;
using UnityEngine;

// El sonido del producto integrador: la música de la escena y los efectos del juego. Hay uno en cada escena.
// Música: cada escena trae su lista (Guadalajara, Monterrey y Ciudad de México, la de su ciudad; el menú y la ruta libre, todas).
// Al empezar cada partida suena una canción al azar y, cuando se acaba, sigue otra de la misma lista, así nunca se queda en el
// fundido del final. En la pausa y en AJUSTES baja a "volumenEnPausa", y al cambiar de escena se apaga con el fundido de
// Transicion.
// Efectos: cada uno con su clip y su volumen en el Inspector, y su propia fuente. El juego los pide con Sonido.Tocar desde lo
// que ya pasaba: el choque, el brinco, el tope y el cambio de carril (CamionRuta), el zumbido del auto que rebasa al pasar
// junto al camión (AutoQueRebasa), el turbo de la onda verde (OndaVerde), el arranque del poder sin topes (PoderSinTopes) y el
// claxon que avisa que viene un auto a rebasar (GeneradorAvenida).
// Los interruptores de AJUSTES (BotonSonido) prenden y apagan la música y los efectos, y queda guardado con PlayerPrefs.
// Sin los archivos de audio (no van en el repositorio por la licencia de Pixabay) los clips quedan vacíos: no suena nada y no
// hay errores; la música sólo elige entre las canciones que sí tienen clip.
public class Sonido : MonoBehaviour
{
    public enum Efecto { Choque, Brinco, Tope, Rebase, Turbo, Poder, Claxon, Carril }

    [Serializable]
    public class Pista
    {
        public Efecto efecto;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen = 1f;
    }

    public const string ClaveMusica = "SonidoMusica";
    public const string ClaveEfectos = "SonidoEfectos";

    // Cambió un interruptor: las etiquetas de AJUSTES se vuelven a pintar y la música se prende o se apaga.
    public static event Action Cambiaron;
    // El juego pidió un efecto (suene o no, según el interruptor): lo usa la prueba.
    public static event Action<Efecto> Pedido;

    public static bool MusicaEncendida
    {
        get => PlayerPrefs.GetInt(ClaveMusica, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(ClaveMusica, value ? 1 : 0);
            PlayerPrefs.Save();
            Cambiaron?.Invoke();
        }
    }

    public static bool EfectosEncendidos
    {
        get => PlayerPrefs.GetInt(ClaveEfectos, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(ClaveEfectos, value ? 1 : 0);
            PlayerPrefs.Save();
            Cambiaron?.Invoke();
        }
    }

    [Header("Música")]
    [Tooltip("Las canciones de la escena: en cada partida suena una al azar y, al acabarse, sigue otra de la lista.")]
    [SerializeField] AudioClip[] canciones = new AudioClip[0];
    [SerializeField] AudioSource musica;
    [SerializeField, Range(0f, 1f)] float volumenMusica = 0.6f;
    [Tooltip("En la pausa y en AJUSTES, la música baja a esta parte de su volumen.")]
    [SerializeField, Range(0f, 1f)] float volumenEnPausa = 0.4f;
    [Tooltip("Segundos en que sube la música al empezar una canción.")]
    [SerializeField, Min(0.05f)] float segundosEntrada = 1f;
    [Tooltip("Segundos en que se apaga al salir de la escena (el fundido de Transicion dura 0.35 s).")]
    [SerializeField, Min(0.05f)] float segundosSalida = 0.3f;
    [Tooltip("Para saber si el juego está en pausa; vacío en el menú.")]
    [SerializeField] Navegacion navegacion;

    [Header("Efectos")]
    [Tooltip("Cada efecto con su clip y su volumen.")]
    [SerializeField] Pista[] efectos = new Pista[0];

    static Sonido actual;
    readonly System.Random azar = new System.Random();
    readonly System.Collections.Generic.List<int> conClip = new System.Collections.Generic.List<int>();   // canciones con archivo
    AudioSource[] fuentes;
    int cancion = -1;
    float fundido;          // 0 a 1: la entrada de cada canción y la salida de la escena
    float inicioCancion;

    public static Sonido Actual => actual;
    public AudioSource Musica => musica;
    public int Canciones => canciones.Length;
    public string CancionActual => musica != null && musica.clip != null ? musica.clip.name : "";
    public float VolumenMusica => musica != null ? musica.volume : 0f;
    public float VolumenEnPausa => volumenEnPausa;
    // El volumen al que va la música sin contar el fundido: el suyo o, en la pausa y en AJUSTES, su parte "volumenEnPausa".
    public float VolumenMeta => volumenMusica * (navegacion != null && navegacion.EnPausa ? volumenEnPausa : 1f);

    void Awake()
    {
        actual = this;
        for (int i = 0; i < canciones.Length; i++)
        {
            if (canciones[i] != null)
            {
                conClip.Add(i);
            }
        }
        fuentes = new AudioSource[Enum.GetValues(typeof(Efecto)).Length];
        foreach (var p in efectos)
        {
            if (p == null || p.clip == null)
            {
                continue;
            }
            var f = gameObject.AddComponent<AudioSource>();
            f.clip = p.clip;
            f.volume = p.volumen;
            f.playOnAwake = false;
            f.loop = false;
            f.spatialBlend = 0f;
            f.priority = 64;
            fuentes[(int)p.efecto] = f;
        }
        if (musica != null)
        {
            musica.playOnAwake = false;
            musica.loop = false;
            musica.spatialBlend = 0f;
            musica.priority = 0;
            musica.volume = 0f;
        }
    }

    void OnEnable() => Cambiaron += AlCambiar;
    void OnDisable() => Cambiaron -= AlCambiar;

    void OnDestroy()
    {
        if (actual == this)
        {
            actual = null;
        }
    }

    void Start() => NuevaCancion();

    void Update()
    {
        if (musica == null)
        {
            return;
        }
        bool saliendo = Transicion.Cargando;
        fundido = saliendo ? Mathf.MoveTowards(fundido, 0f, Time.unscaledDeltaTime / segundosSalida)
            : Mathf.MoveTowards(fundido, 1f, Time.unscaledDeltaTime / segundosEntrada);
        musica.volume = VolumenMeta * fundido;
        // Se acabó la canción: sigue otra. El segundo de gracia es por si el audio todavía no arranca.
        if (MusicaEncendida && !saliendo && conClip.Count > 0 && !musica.isPlaying && Time.unscaledTime - inicioCancion > 1f)
        {
            NuevaCancion();
        }
    }

    // Otra canción al azar de la lista, distinta de la que sonaba si hay más de una con archivo.
    void NuevaCancion()
    {
        if (musica == null || conClip.Count == 0)
        {
            return;
        }
        if (!MusicaEncendida)
        {
            musica.Stop();
            return;
        }
        int n = conClip.Count;
        int k = conClip.IndexOf(cancion);
        k = n == 1 || k < 0 ? azar.Next(n) : (k + 1 + azar.Next(n - 1)) % n;
        cancion = conClip[k];
        musica.clip = canciones[cancion];
        fundido = 0f;
        musica.volume = 0f;
        musica.Play();
        inicioCancion = Time.unscaledTime;
    }

    // Partida nueva en la misma escena (JUGAR DE NUEVO y REINICIAR): otra canción de la lista.
    public static void NuevaPartida()
    {
        if (actual != null)
        {
            actual.NuevaCancion();
        }
    }

    void AlCambiar()
    {
        if (musica == null)
        {
            return;
        }
        if (!MusicaEncendida)
        {
            musica.Stop();
        }
        else if (!musica.isPlaying)
        {
            NuevaCancion();
        }
    }

    // El juego pide un efecto: suena si los efectos están prendidos y la escena trae su clip.
    public static void Tocar(Efecto efecto)
    {
        Pedido?.Invoke(efecto);
        if (actual == null || !EfectosEncendidos)
        {
            return;
        }
        var f = actual.fuentes[(int)efecto];
        if (f != null)
        {
            f.PlayOneShot(f.clip);
        }
    }
}

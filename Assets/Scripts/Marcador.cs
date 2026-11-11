using UnityEngine;
using UnityEngine.UI;

// El contador de la avenida, el velocímetro, el récord y la etapa. Los metros suben parejo con el avance del camión en Z y el
// velocímetro, en la esquina inferior derecha, dice a cuántos km/h va. El récord es la mejor distancia: se guarda con
// PlayerPrefs al terminar la partida (choque o regreso al menú desde la pausa), así que sigue ahí la próxima vez que se abre
// el juego. Debajo del récord va el nombre de la etapa que rige; al cambiar se resalta unos segundos. En el menú, sin camión,
// sólo muestra el récord. En un nivel (4.5) el récord no se guarda (es de la ruta libre): en su lugar va la ciudad con los
// metros que faltan para la terminal, y debajo una barra que se llena conforme el camión se acerca.
public class Marcador : MonoBehaviour
{
    public const string ClaveRecord = "RecordMetros";

    [Header("Nivel")]
    [Tooltip("El nivel de la escena; vacío en el menú y en la ruta libre.")]
    [SerializeField] NivelRuta nivel;
    [Tooltip("La barra de avance hacia la terminal: una imagen de tipo Filled.")]
    [SerializeField] Image barraAvance;

    [Header("Marcador")]
    [SerializeField] CamionRuta camion;
    [SerializeField] Text textoMetros;
    [Tooltip("El velocímetro: la velocidad del camión en km/h.")]
    [SerializeField] Text textoVelocidad;
    [SerializeField] Text textoRecord;

    [Header("Etapa en pantalla")]
    [SerializeField] GeneradorAvenida generador;
    [SerializeField] Text textoEtapa;
    [SerializeField, Min(0f)] float segundosResaltada = 2.5f;
    [SerializeField] Color colorEtapa = new Color32(240, 240, 236, 255);
    [SerializeField] Color colorEtapaNueva = new Color32(255, 196, 40, 255);

    bool enAvenida;
    bool guardado;
    int metros;
    int kmh = -1;
    int record;
    EtapaAvenida etapaMostrada;
    float resaltadaHasta;

    public static int Record => PlayerPrefs.GetInt(ClaveRecord, 0);

    void Start()
    {
        enAvenida = camion != null;
        record = Record;
        Pintar();
    }

    void Update()
    {
        if (!enAvenida)
        {
            return;
        }
        int recorrido = Mathf.FloorToInt(camion.Metros);
        if (recorrido != metros)
        {
            metros = recorrido;
            Pintar();
        }
        int velocidad = Mathf.RoundToInt(camion.VelocidadActual);
        if (velocidad != kmh)
        {
            kmh = velocidad;
            PintarVelocidad();
        }
        if (camion.Detenido)
        {
            Guardar();
        }
        PintarEtapa();
    }

    // También cuenta la partida que se deja a medias para volver al menú.
    void OnDestroy() => Guardar();

    // Guarda la distancia de la partida si supera el récord; dice si fue récord nuevo.
    public bool Guardar()
    {
        if (!enAvenida || guardado || nivel != null)
        {
            return false;
        }
        guardado = true;
        if (camion != null)
        {
            // Los metros de este momento, los mismos que dice el aviso de choque: el contador puede ir un cuadro atrás.
            metros = Mathf.FloorToInt(camion.Metros);
        }
        if (metros <= record)
        {
            return false;
        }
        record = metros;
        PlayerPrefs.SetInt(ClaveRecord, record);
        PlayerPrefs.Save();
        Pintar();
        return true;
    }

    // Partida nueva: el contador a cero; el récord se queda.
    public void Reiniciar()
    {
        guardado = false;
        metros = 0;
        kmh = -1;
        etapaMostrada = null;
        Pintar();
    }

    // El número grande y la unidad más chica, como en un tablero.
    void PintarVelocidad()
    {
        if (textoVelocidad != null)
        {
            textoVelocidad.text = kmh + " <size=" + Mathf.RoundToInt(textoVelocidad.fontSize * 0.55f) + ">km/h</size>";
        }
    }

    void PintarEtapa()
    {
        if (generador == null || textoEtapa == null)
        {
            return;
        }
        var etapa = generador.EtapaEn(camion.Metros);
        if (etapa != etapaMostrada)
        {
            etapaMostrada = etapa;
            textoEtapa.text = etapa != null ? etapa.nombre : "";
            resaltadaHasta = Time.time + segundosResaltada;
        }
        textoEtapa.color = Time.time < resaltadaHasta ? colorEtapaNueva : colorEtapa;
    }

    void Pintar()
    {
        if (textoMetros != null)
        {
            textoMetros.text = Formato.Metros(metros);
        }
        if (nivel != null)
        {
            // La ciudad y lo que falta para la terminal, en el renglón del récord; la barra, debajo.
            float faltan = Mathf.Max(0f, nivel.metrosTerminal - metros);
            if (textoRecord != null)
            {
                textoRecord.text = faltan > 0f ? nivel.ciudad + " · faltan " + Formato.Metros(faltan) : nivel.ciudad + " · ¡terminal!";
            }
            if (barraAvance != null)
            {
                barraAvance.fillAmount = Mathf.Clamp01(metros / nivel.metrosTerminal);
            }
            return;
        }
        if (textoRecord != null)
        {
            textoRecord.text = "Récord: " + Formato.Metros(record);
        }
    }
}

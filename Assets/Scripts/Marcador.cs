using UnityEngine;
using UnityEngine.UI;

// El contador de la avenida y el récord. Los metros suben parejo con el avance del camión en Z. El récord es la mejor
// distancia: se guarda con PlayerPrefs al terminar la partida (choque, llegada a la base o regreso al menú desde la
// pausa), así que sigue ahí la próxima vez que se abre el juego. En el menú, sin camión, sólo muestra el récord.
public class Marcador : MonoBehaviour
{
    public const string ClaveRecord = "RecordMetros";

    [SerializeField] CamionRuta camion;
    [SerializeField] Text textoMetros;
    [SerializeField] Text textoRecord;

    bool enAvenida;
    bool guardado;
    int metros;
    int record;

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
        if (camion.Detenido)
        {
            Guardar();
        }
    }

    // También cuenta la partida que se deja a medias para volver al menú.
    void OnDestroy() => Guardar();

    void Guardar()
    {
        if (!enAvenida || guardado)
        {
            return;
        }
        guardado = true;
        if (metros > record)
        {
            record = metros;
            PlayerPrefs.SetInt(ClaveRecord, record);
            PlayerPrefs.Save();
            Pintar();
        }
    }

    void Pintar()
    {
        if (textoMetros != null)
        {
            textoMetros.text = metros + " m";
        }
        if (textoRecord != null)
        {
            textoRecord.text = "Récord: " + record + " m";
        }
    }
}

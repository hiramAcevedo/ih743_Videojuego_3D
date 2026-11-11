using UnityEngine;
using UnityEngine.UI;

// Una tarjeta del panel NIVELES del menú: el nombre del nivel, sus metros y su estado. Un nivel terminado dice TERMINADO; el
// que sigue, POR TERMINAR; uno cerrado se ve apagado, no se puede pulsar y dice qué ciudad hay que terminar antes. Sin nivel,
// la tarjeta es la ruta libre y muestra el récord. Se repinta cada vez que se abre el panel.
public class BotonNivel : MonoBehaviour
{
    [Tooltip("El nivel de la tarjeta; vacío, la ruta libre.")]
    [SerializeField] NivelRuta nivel;
    [SerializeField] RutaNiveles ruta;
    [SerializeField] Button boton;
    [Tooltip("El texto de la derecha: TERMINADO, POR TERMINAR, cerrado o el récord.")]
    [SerializeField] Text estado;
    [SerializeField] Color colorTerminado = new Color32(46, 150, 64, 255);
    [SerializeField] Color colorPorTerminar = new Color32(176, 120, 0, 255);
    [SerializeField] Color colorCerrado = new Color32(120, 120, 120, 255);
    [SerializeField] Color colorRecord = new Color32(60, 66, 80, 255);

    public NivelRuta Nivel => nivel;

    void OnEnable() => Pintar();

    public void Pintar()
    {
        if (boton == null || estado == null)
        {
            return;
        }
        if (nivel == null)
        {
            boton.interactable = true;
            estado.text = "Récord: " + Formato.Metros(Marcador.Record);
            estado.color = colorRecord;
            return;
        }
        bool abierto = ruta == null || ruta.Abierto(nivel);
        boton.interactable = abierto;
        if (nivel.Terminado)
        {
            estado.text = "TERMINADO";
            estado.color = colorTerminado;
        }
        else if (abierto)
        {
            estado.text = "POR TERMINAR";
            estado.color = colorPorTerminar;
        }
        else
        {
            var anterior = ruta.Anterior(nivel);
            estado.text = "CERRADO\n<size=" + Mathf.RoundToInt(estado.fontSize * 0.72f) + ">Termina " +
                (anterior != null ? anterior.ciudad : "el anterior") + "</size>";
            estado.color = colorCerrado;
        }
    }
}

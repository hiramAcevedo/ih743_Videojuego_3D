using System.Collections.Generic;
using UnityEngine;

// Un nivel de Camión de ruta: una ciudad con su avenida, que termina en la terminal. Como los niveles del tutorial, es un asset
// que se crea con clic derecho > Create > Camión de ruta > Nivel y se ajusta en el Inspector: la ciudad, la escena que lo
// juega, a cuántos metros está la terminal, la velocidad del camión y las etapas de su avenida. RutaNiveles los guarda en
// orden; terminar uno abre el siguiente. Que está terminado se guarda con PlayerPrefs, así que sigue terminado la próxima vez
// que se abre el juego.
[CreateAssetMenu(fileName = "Nuevo nivel", menuName = "Camión de ruta/Nivel", order = 1)]
public class NivelRuta : ScriptableObject
{
    const string Prefijo = "NivelTerminado_";

    [Tooltip("El número del nivel, como sale en pantalla.")]
    [Min(1)] public int numero = 1;
    [Tooltip("La ciudad: el nombre del nivel y el letrero de su terminal.")]
    public string ciudad = "Guadalajara";
    [Tooltip("La escena que juega este nivel; tiene que estar en Build Settings.")]
    public string escena = "Guadalajara";
    [Tooltip("Metros de la salida a la terminal: al llegar ahí, el camión frena y el nivel queda terminado.")]
    [Min(100f)] public float metrosTerminal = 1200f;

    [Header("Velocidad del camión, en km/h")]
    [Tooltip("Velocidad al salir.")]
    [Min(1f)] public float velocidadInicial = 36f;
    [Tooltip("La velocidad a la que llega en los metros de metrosHastaMaxima. De ahí ya no sube.")]
    [Min(1f)] public float velocidadMaxima = 200f;
    [Tooltip("Metros recorridos en los que llega a la velocidad máxima.")]
    [Min(100f)] public float metrosHastaMaxima = 5000f;
    [Tooltip("Cómo crece la velocidad con los metros: 0 es la salida y 1, metrosHastaMaxima.")]
    public AnimationCurve curvaDificultad = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("La avenida")]
    [Tooltip("Las etapas del tráfico en orden; cada una rige desde su metro.")]
    public List<EtapaAvenida> etapas = new List<EtapaAvenida>();
    [Tooltip("Metro desde el que, además de los tranquilos, salen autos locos que rebasan rapidísimo.")]
    [Min(0f)] public float rebasesLocosDesde = 2500f;
    [Tooltip("Color del letrero de la terminal y de la barra de avance.")]
    public Color colorCiudad = new Color32(50, 166, 62, 255);

    public bool Terminado => PlayerPrefs.GetInt(Clave(escena), 0) == 1;

    public void MarcarTerminado()
    {
        PlayerPrefs.SetInt(Clave(escena), 1);
        PlayerPrefs.Save();
    }

    // La clave de PlayerPrefs donde se guarda si el nivel de esa escena ya se terminó.
    public static string Clave(string escenaDelNivel) => Prefijo + escenaDelNivel;
}

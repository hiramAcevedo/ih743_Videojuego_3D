using UnityEngine;
using UnityEngine.UI;

// El bucle de juego de la avenida, como el GameManager del tutorial. La partida corre hasta que el camión choca con un auto
// parado o con uno que se le cerró al rebasarlo, o hasta que se queda sin vidas: desde 120 km/h, cada tope sin brincar quita
// una de las tres y frena el camión a 32 km/h. Al terminar se guarda el récord y aparece el aviso con la distancia. El botón
// JUGAR DE NUEVO llama a ReiniciarNivel, que vuelve a empezar en la misma escena: el contador regresa a 0 m, el camión a la
// salida con sus tres vidas y el generador arma una avenida nueva. El récord se conserva.
// En un nivel (4.5) la partida también termina bien: al pasar la terminal el nivel queda terminado (PlayerPrefs), el camión
// frena y, ya parado, aparece el aviso de nivel completado con SIGUIENTE NIVEL; después del último, el de fin de la ruta. En los
// niveles no se guarda récord: el récord es de la ruta libre.
// 4.6: las vidas son tres corazones dibujados (imágenes) en el tablero nuevo, los avisos salen con Aviso arriba de los metros y
// los km/h, y al jugar de nuevo se apagan el acelerón y el poder activo; lo brincado y el poder guardado se quedan para el
// intento nuevo (PoderSinTopes).
public class ControlPartida : MonoBehaviour
{
    [Header("Nivel")]
    [Tooltip("El nivel de la escena; vacío en la ruta libre.")]
    [SerializeField] NivelRuta nivel;
    [SerializeField] RutaNiveles ruta;
    [Tooltip("El texto del aviso de nivel completado: la ciudad, los metros y las vidas con que se llegó.")]
    [SerializeField] Text textoCompletado;
    [Tooltip("La etiqueta del botón SIGUIENTE NIVEL, que dice a qué ciudad lleva.")]
    [SerializeField] Text etiquetaSiguiente;
    [Tooltip("El texto del aviso de fin de la ruta.")]
    [SerializeField] Text textoFinDeRuta;

    [Header("Partida")]
    [SerializeField] CamionRuta camion;
    [SerializeField] GeneradorAvenida generador;
    [SerializeField] Marcador marcador;
    [SerializeField] Navegacion navegacion;
    [SerializeField] CamaraSeguimiento camara;
    [Tooltip("La hora del día: al jugar de nuevo regresa a la mañana.")]
    [SerializeField] LuzDelDia luz;
    [Tooltip("El texto del aviso de choque que dice a cuántos metros se llegó.")]
    [SerializeField] Text textoDistancia;
    [Tooltip("El texto del aviso de choque que dice con qué se chocó: un auto parado o uno que venía rebasando.")]
    [SerializeField] Text textoDetalle;

    [Header("Vidas")]
    [Tooltip("Vidas al empezar. Desde 120 km/h, cada tope sin brincar quita una y frena el camión.")]
    [SerializeField, Min(1)] int vidasIniciales = 3;
    [Tooltip("La velocidad, en km/h, a la que baja el camión al perder una vida en un tope.")]
    [SerializeField, Min(0f)] float velocidadTrasTope = 32f;
    [Tooltip("Los corazones, arriba a la derecha, uno por vida.")]
    [SerializeField] Image[] corazones;
    [SerializeField] Color colorVida = new Color32(232, 50, 60, 255);
    [SerializeField] Color colorVidaPerdida = new Color32(255, 255, 255, 70);
    [Tooltip("El aviso que sale unos segundos al perder una vida o al pasar la terminal.")]
    [SerializeField] Aviso aviso;
    [SerializeField] Color colorAviso = new Color32(255, 204, 51, 255);
    [SerializeField, Min(0f)] float segundosAviso = 1.6f;

    [Header("4.6")]
    [SerializeField] PoderSinTopes poder;

    public int Partida { get; private set; } = 1;
    public int Vidas { get; private set; }
    public NivelRuta Nivel => nivel;
    public NivelRuta SiguienteNivel => nivel != null && ruta != null ? ruta.Siguiente(nivel) : null;

    void Awake()
    {
        Vidas = vidasIniciales;
    }

    void Start()
    {
        PintarVidas();
        Avisar("");
    }

    void OnEnable()
    {
        if (camion != null)
        {
            camion.Choco += Perdio;
            camion.TopeSinBrincar += PerdioVida;
            camion.Llego += Llego;
        }
    }

    void OnDisable()
    {
        if (camion != null)
        {
            camion.Choco -= Perdio;
            camion.TopeSinBrincar -= PerdioVida;
            camion.Llego -= Llego;
        }
    }

    void Update()
    {
        // La terminal: el nivel se guarda como terminado en cuanto se pasa, aunque se salga antes de que el camión se pare.
        if (nivel != null && !camion.Llegando && !camion.Detenido && camion.Metros >= nivel.metrosTerminal)
        {
            nivel.MarcarTerminado();
            camion.LlegarATerminal();
            Avisar("¡Terminal " + nivel.ciudad + "!");
        }
    }

    // Ya parado en la terminal: el aviso de nivel completado o, después del último nivel, el de fin de la ruta.
    void Llego()
    {
        Avisar("");
        var siguiente = SiguienteNivel;
        string vidas = Vidas == 1 ? "1 vida" : Vidas + " vidas";
        if (siguiente != null)
        {
            if (textoCompletado != null)
            {
                textoCompletado.text = "Nivel " + nivel.numero + " · " + nivel.ciudad + "\n" + Formato.Metros(nivel.metrosTerminal) +
                    " con " + vidas + ".";
            }
            if (etiquetaSiguiente != null)
            {
                etiquetaSiguiente.text = "SIGUIENTE: " + siguiente.ciudad.ToUpperInvariant();
            }
            navegacion.MostrarNivelCompletado();
        }
        else
        {
            if (textoFinDeRuta != null)
            {
                textoFinDeRuta.text = "Terminal de " + nivel.ciudad + " con " + vidas + ".\n" + Recorrido() + ".";
            }
            navegacion.MostrarFinDeRuta();
        }
    }

    // Las ciudades de la ruta, en orden: «Guadalajara y Monterrey».
    string Recorrido()
    {
        if (ruta == null || ruta.niveles.Count == 0)
        {
            return nivel.ciudad;
        }
        var ciudades = new System.Collections.Generic.List<string>();
        foreach (var n in ruta.niveles)
        {
            if (n != null)
            {
                ciudades.Add(n.ciudad);
            }
        }
        // Con tres ciudades o más, la última va en su renglón: «Guadalajara, Monterrey / y Ciudad de México», sin que «de México»
        // quede sola abajo.
        string lista = ciudades.Count > 1
            ? string.Join(", ", ciudades.GetRange(0, ciudades.Count - 1)) + (ciudades.Count > 2 ? "\ny " : " y ") + ciudades[ciudades.Count - 1]
            : ciudades[0];
        return "Ya manejaste " + lista;
    }

    // Un tope sin brincar a más de 120 km/h: una vida menos y el camión frenado; sin vidas, se acaba la partida.
    void PerdioVida()
    {
        Vidas = Mathf.Max(0, Vidas - 1);
        PintarVidas();
        if (Vidas == 0)
        {
            camion.Detener(camion.UltimoTope);
            return;
        }
        camion.Frenar(velocidadTrasTope);
        Avisar(Vidas == 1 ? "¡Tope! Te queda 1 vida" : "¡Tope! Te quedan " + Vidas + " vidas");
    }

    void PintarVidas()
    {
        if (corazones == null)
        {
            return;
        }
        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] != null)
            {
                corazones[i].color = i < Vidas ? colorVida : colorVidaPerdida;
            }
        }
    }

    void Avisar(string texto)
    {
        if (aviso == null)
        {
            return;
        }
        if (texto.Length == 0)
        {
            aviso.Borrar();
        }
        else
        {
            aviso.Mostrar(texto, colorAviso, segundosAviso);
        }
    }

    void Perdio()
    {
        int metros = Mathf.FloorToInt(camion.Metros);
        bool recordNuevo = marcador.Guardar();
        if (textoDistancia != null)
        {
            textoDistancia.text = nivel != null
                ? "Llegaste a " + Formato.Metros(metros) + " de " + Formato.Metros(nivel.metrosTerminal) + "."
                : "Llegaste a " + Formato.Metros(metros) + "." + (recordNuevo ? "\n¡Récord nuevo!" : "");
        }
        if (textoDetalle != null)
        {
            textoDetalle.text = QuienSeMovio(camion.UltimoChoque);
        }
        Avisar("");
        navegacion.MostrarChoque();
    }

    // Con qué chocó y quién se movió: así se sabe si fue culpa del camión o del auto que rebasaba.
    string QuienSeMovio(Collider con)
    {
        if (con != null && con.CompareTag("Tope"))
        {
            return "Te quedaste sin vidas en los topes.";
        }
        var auto = con != null ? con.GetComponentInParent<AutoQueRebasa>() : null;
        if (auto == null)
        {
            return "Un auto parado te cerró el paso.";
        }
        if (auto.EstadoActual == AutoQueRebasa.Estado.Cambiando)
        {
            return "Un auto que te venía rebasando se te cerró.";
        }
        return camion.CambiandoDeCarril ? "Te le cerraste a un auto que te rebasaba."
            : "Alcanzaste a un auto que te había rebasado.";
    }

    // Lo que hace el botón JUGAR DE NUEVO.
    public void ReiniciarNivel()
    {
        camion.Reiniciar();
        if (camion.Aceleron != null)
        {
            camion.Aceleron.Reiniciar();
        }
        generador.Reiniciar();
        marcador.Reiniciar();
        if (poder != null)
        {
            poder.Reiniciar();
        }
        Vidas = vidasIniciales;
        PintarVidas();
        Avisar("");
        if (camara != null)
        {
            camara.Colocar();
        }
        if (luz != null)
        {
            luz.Reiniciar();
        }
        navegacion.MostrarJuego();
        Partida++;
    }
}

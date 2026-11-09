using UnityEngine;
using UnityEngine.UI;

// El bucle de juego de la avenida, como el GameManager del tutorial. La partida corre hasta que el camión choca con un auto
// parado o con uno que se le cerró al rebasarlo, o hasta que se queda sin vidas: desde 120 km/h, cada tope sin brincar quita
// una de las tres y frena el camión a 32 km/h. Al terminar se guarda el récord y aparece el aviso con la distancia. El botón
// JUGAR DE NUEVO llama a ReiniciarNivel, que vuelve a empezar en la misma escena: el contador regresa a 0 m, el camión a la
// salida con sus tres vidas y el generador arma una avenida nueva. El récord se conserva.
public class ControlPartida : MonoBehaviour
{
    [SerializeField] CamionRuta camion;
    [SerializeField] GeneradorAvenida generador;
    [SerializeField] Marcador marcador;
    [SerializeField] Navegacion navegacion;
    [SerializeField] CamaraSeguimiento camara;
    [Tooltip("El texto del aviso de choque que dice a cuántos metros se llegó.")]
    [SerializeField] Text textoDistancia;
    [Tooltip("El texto del aviso de choque que dice con qué se chocó: un auto parado o uno que venía rebasando.")]
    [SerializeField] Text textoDetalle;

    [Header("Vidas")]
    [Tooltip("Vidas al empezar. Desde 120 km/h, cada tope sin brincar quita una y frena el camión.")]
    [SerializeField, Min(1)] int vidasIniciales = 3;
    [Tooltip("La velocidad, en km/h, a la que baja el camión al perder una vida en un tope.")]
    [SerializeField, Min(0f)] float velocidadTrasTope = 32f;
    [Tooltip("Los corazones, arriba a la derecha.")]
    [SerializeField] Text textoVidas;
    [Tooltip("El aviso que sale unos segundos al perder una vida.")]
    [SerializeField] Text textoAviso;
    [SerializeField, Min(0f)] float segundosAviso = 1.6f;

    public int Partida { get; private set; } = 1;
    public int Vidas { get; private set; }

    float avisoHasta;

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
        }
    }

    void OnDisable()
    {
        if (camion != null)
        {
            camion.Choco -= Perdio;
            camion.TopeSinBrincar -= PerdioVida;
        }
    }

    void Update()
    {
        if (avisoHasta > 0f && Time.time >= avisoHasta)
        {
            Avisar("");
        }
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
        if (textoVidas == null)
        {
            return;
        }
        var corazones = new System.Text.StringBuilder();
        for (int i = 0; i < vidasIniciales; i++)
        {
            corazones.Append(i > 0 ? " " : "").Append(i < Vidas ? "<color=#E8323C>♥</color>" : "<color=#FFFFFF55>♥</color>");
        }
        textoVidas.text = corazones.ToString();
    }

    void Avisar(string texto)
    {
        if (textoAviso != null)
        {
            textoAviso.text = texto;
        }
        avisoHasta = texto.Length > 0 ? Time.time + segundosAviso : 0f;
    }

    void Perdio()
    {
        int metros = Mathf.FloorToInt(camion.Metros);
        bool recordNuevo = marcador.Guardar();
        if (textoDistancia != null)
        {
            textoDistancia.text = "Llegaste a " + metros + " m." + (recordNuevo ? "\n¡Récord nuevo!" : "");
        }
        if (textoDetalle != null)
        {
            var con = camion.UltimoChoque;
            textoDetalle.text = con != null && con.CompareTag("Tope") ? "Te quedaste sin vidas en los topes."
                : con != null && con.GetComponentInParent<AutoQueRebasa>() != null ? "Un auto que te venía rebasando se te cerró."
                : "Un auto parado te cerró el paso.";
        }
        Avisar("");
        navegacion.MostrarChoque();
    }

    // Lo que hace el botón JUGAR DE NUEVO.
    public void ReiniciarNivel()
    {
        camion.Reiniciar();
        generador.Reiniciar();
        marcador.Reiniciar();
        Vidas = vidasIniciales;
        PintarVidas();
        Avisar("");
        if (camara != null)
        {
            camara.Colocar();
        }
        navegacion.MostrarJuego();
        Partida++;
    }
}

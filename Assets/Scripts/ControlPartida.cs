using UnityEngine;
using UnityEngine.UI;

// El bucle de juego de la avenida, como el GameManager del tutorial. La partida corre hasta que el camión choca con un auto
// parado o con uno que se le cerró al rebasarlo: entonces se guarda el récord y aparece el aviso con la distancia. El botón JUGAR DE NUEVO llama a ReiniciarNivel,
// que vuelve a empezar en la misma escena: el contador regresa a 0 m, el camión a la salida y el generador arma una avenida
// nueva. El récord se conserva.
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

    public int Partida { get; private set; } = 1;

    void OnEnable()
    {
        if (camion != null)
        {
            camion.Choco += Perdio;
        }
    }

    void OnDisable()
    {
        if (camion != null)
        {
            camion.Choco -= Perdio;
        }
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
            bool rebasaba = camion.UltimoChoque != null && camion.UltimoChoque.GetComponentInParent<AutoQueRebasa>() != null;
            textoDetalle.text = rebasaba ? "Un auto que te venía rebasando se te cerró." : "Un auto parado te cerró el paso.";
        }
        navegacion.MostrarChoque();
    }

    // Lo que hace el botón JUGAR DE NUEVO.
    public void ReiniciarNivel()
    {
        camion.Reiniciar();
        generador.Reiniciar();
        marcador.Reiniciar();
        if (camara != null)
        {
            camara.Colocar();
        }
        navegacion.MostrarJuego();
        Partida++;
    }
}

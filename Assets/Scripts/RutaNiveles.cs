using System.Collections.Generic;
using UnityEngine;

// La ruta completa: los niveles en el orden en que se juegan. El menú la usa para saber a qué nivel lleva JUGAR (el primero sin
// terminar) y cuáles están abiertos en el panel NIVELES; la escena de un nivel, para saber cuál sigue al llegar a la terminal.
// La ruta libre es la avenida sin fin de las entregas anteriores, con su récord: siempre está abierta.
[CreateAssetMenu(fileName = "Ruta de niveles", menuName = "Camión de ruta/Ruta de niveles", order = 2)]
public class RutaNiveles : ScriptableObject
{
    [Tooltip("Los niveles en orden. Un nivel se abre al terminar el anterior.")]
    public List<NivelRuta> niveles = new List<NivelRuta>();
    [Tooltip("La escena de la ruta libre: la avenida sin fin.")]
    public string escenaLibre = "Ruta";

    public NivelRuta Siguiente(NivelRuta nivel)
    {
        int i = niveles.IndexOf(nivel);
        return i >= 0 && i + 1 < niveles.Count ? niveles[i + 1] : null;
    }

    public NivelRuta Anterior(NivelRuta nivel)
    {
        int i = niveles.IndexOf(nivel);
        return i > 0 ? niveles[i - 1] : null;
    }

    // Abierto: es el primero o ya se terminó el anterior.
    public bool Abierto(NivelRuta nivel)
    {
        var anterior = Anterior(nivel);
        return anterior == null || anterior.Terminado;
    }

    // El nivel al que lleva JUGAR: el primero sin terminar; con todos terminados, la ruta vuelve a empezar en el primero.
    public NivelRuta AJugar()
    {
        foreach (var nivel in niveles)
        {
            if (nivel != null && !nivel.Terminado)
            {
                return nivel;
            }
        }
        return niveles.Count > 0 ? niveles[0] : null;
    }

    public bool TodosTerminados()
    {
        foreach (var nivel in niveles)
        {
            if (nivel != null && !nivel.Terminado)
            {
                return false;
            }
        }
        return niveles.Count > 0;
    }
}

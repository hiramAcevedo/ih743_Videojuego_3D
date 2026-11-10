using UnityEngine;
using UnityEngine.UI;

// El botón PINTURA del menú: cada vez que se pulsa, el camión que gira cambia a la siguiente pintura, la guarda para la avenida y
// el botón escribe su nombre.
public class BotonPintura : MonoBehaviour
{
    [SerializeField] PintarCamion camion;
    [SerializeField] Text etiqueta;

    void Start()
    {
        // El orden de los Start no está garantizado: si el camión todavía no se pinta, se pinta aquí con la pintura guardada.
        if (camion != null && camion.Actual == null)
        {
            camion.Aplicar(PintarCamion.Elegida);
        }
        Escribir();
    }

    public void Siguiente()
    {
        if (camion != null)
        {
            camion.Siguiente();
        }
        Escribir();
    }

    void Escribir()
    {
        if (camion != null && etiqueta != null)
        {
            var pintura = camion.Actual;
            etiqueta.text = "PINTURA: " + (pintura != null ? pintura.nombre.ToUpperInvariant() : "");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

// El interruptor de las guías táctiles, en la pausa y en AJUSTES: cada toque las enciende o las apaga y la etiqueta dice cómo
// quedaron.
public class BotonGuias : MonoBehaviour
{
    [SerializeField] Text etiqueta;

    void OnEnable()
    {
        Guias.Cambiaron += Pintar;
        Pintar();
    }

    void OnDisable() => Guias.Cambiaron -= Pintar;

    public void Cambiar() => Guias.Encendidas = !Guias.Encendidas;

    void Pintar()
    {
        if (etiqueta != null)
        {
            etiqueta.text = Guias.Encendidas ? "GUÍAS: ENCENDIDAS" : "GUÍAS: APAGADAS";
        }
    }
}

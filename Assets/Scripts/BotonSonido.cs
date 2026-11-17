using UnityEngine;
using UnityEngine.UI;

// Los interruptores de AJUSTES para la música y los efectos: cada toque los prende o los apaga y la etiqueta dice cómo
// quedaron. Lo guarda Sonido en PlayerPrefs.
public class BotonSonido : MonoBehaviour
{
    public enum Que { Musica, Efectos }

    [SerializeField] Que que;
    [SerializeField] Text etiqueta;

    void OnEnable()
    {
        Sonido.Cambiaron += Pintar;
        Pintar();
    }

    void OnDisable() => Sonido.Cambiaron -= Pintar;

    public void Cambiar()
    {
        if (que == Que.Musica)
        {
            Sonido.MusicaEncendida = !Sonido.MusicaEncendida;
        }
        else
        {
            Sonido.EfectosEncendidos = !Sonido.EfectosEncendidos;
        }
    }

    void Pintar()
    {
        if (etiqueta == null)
        {
            return;
        }
        etiqueta.text = que == Que.Musica
            ? (Sonido.MusicaEncendida ? "MÚSICA: ENCENDIDA" : "MÚSICA: APAGADA")
            : (Sonido.EfectosEncendidos ? "EFECTOS: ENCENDIDOS" : "EFECTOS: APAGADOS");
    }
}

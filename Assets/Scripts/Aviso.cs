using UnityEngine;
using UnityEngine.UI;

// Los avisos cortos del tablero (4.6): «¡Tope! Te quedan 2 vidas», «¡ONDA VERDE!», «¡SIN TOPES!», «¡Terminal Monterrey!». Salen
// arriba de los metros y los km/h, no a media pantalla, para no tapar la avenida; cada uno con su color y sus segundos.
[RequireComponent(typeof(Text))]
public class Aviso : MonoBehaviour
{
    Text texto;
    float hasta;

    public string Mensaje => texto != null ? texto.text : "";

    void Awake()
    {
        texto = GetComponent<Text>();
        texto.text = "";
    }

    public void Mostrar(string mensaje, Color color, float segundos)
    {
        if (texto == null)
        {
            texto = GetComponent<Text>();
        }
        texto.text = mensaje;
        texto.color = color;
        hasta = Time.time + segundos;
    }

    public void Borrar()
    {
        if (texto != null)
        {
            texto.text = "";
        }
        hasta = 0f;
    }

    void Update()
    {
        if (hasta > 0f && Time.time >= hasta)
        {
            Borrar();
        }
    }
}

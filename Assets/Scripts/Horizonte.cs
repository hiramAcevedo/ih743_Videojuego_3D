using UnityEngine;

// El cerro del fondo de Monterrey, el Cerro de la Silla: se queda siempre a la misma distancia delante de la cámara, como algo
// que está lejísimos y no se alcanza nunca. Su material (Ruta/Horizonte) se dibuja antes que la avenida y sin profundidad, así
// que los edificios, los autos y el camión siempre quedan enfrente, y se tiñe con el color de la niebla, que es el del cielo de
// la etapa: al atardecer se pone naranja y al anochecer casi se pierde en el cielo.
public class Horizonte : MonoBehaviour
{
    [SerializeField] Transform camara;
    [Tooltip("Metros delante de la cámara; debe quedar antes del plano lejano de la cámara.")]
    [SerializeField, Min(10f)] float distancia = 200f;

    public Transform Camara { get => camara; set => camara = value; }

    void LateUpdate()
    {
        if (camara == null)
        {
            return;
        }
        var p = transform.position;
        transform.position = new Vector3(p.x, p.y, camara.position.z + distancia);
    }
}

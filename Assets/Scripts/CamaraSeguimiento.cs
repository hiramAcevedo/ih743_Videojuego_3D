using UnityEngine;

// La cámara sigue al camión sólo en X y Z. La altura (Y) no cambia aunque el camión brinque o pase un tope, y la
// distancia en Z respecto al camión es fija y constante: cada cuadro queda exactamente a "distanciaZ" detrás de él.
// En X acompaña al cambio de carril con un suavizado corto para que la imagen no tiemble.
public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] Transform objetivo;
    [SerializeField] float altura = 9f;
    [SerializeField] float distanciaZ = -15f;
    [SerializeField] float desplazamientoX = 0f;
    [SerializeField] float suavizadoX = 0.08f;

    float velocidadX;

    public Transform Objetivo { get => objetivo; set => objetivo = value; }
    public float Altura => altura;
    public float DistanciaZ => distanciaZ;

    void Start()
    {
        if (objetivo != null)
        {
            transform.position = new Vector3(objetivo.position.x + desplazamientoX, altura, objetivo.position.z + distanciaZ);
        }
    }

    void LateUpdate()
    {
        if (objetivo == null)
        {
            return;
        }
        float x = Mathf.SmoothDamp(transform.position.x, objetivo.position.x + desplazamientoX, ref velocidadX, suavizadoX);
        transform.position = new Vector3(x, altura, objetivo.position.z + distanciaZ);
    }
}

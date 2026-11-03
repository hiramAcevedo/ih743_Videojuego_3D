using UnityEngine;

// El camión de exhibición del menú gira, y en un teléfono vertical no cabía de largo a la distancia con que se armó la
// escena: se cortaban el frente y la parte de atrás. Este componente aleja la cámara sobre su propio eje hasta que el
// camión quepa en el ancho de la pantalla en cualquier momento del giro. En una pantalla horizontal o en el editor la
// cámara se queda donde está.
[RequireComponent(typeof(Camera))]
public class EncuadreExhibicion : MonoBehaviour
{
    [SerializeField] Transform exhibicion;
    // Parte del ancho de la pantalla que puede ocupar el camión, como máximo.
    [SerializeField, Range(0.5f, 1f)] float anchoMaximo = 0.8f;

    Camera cam;
    Vector3 pivote;       // el punto del eje de la cámara a la altura del camión
    float distanciaBase;  // la distancia con que se armó la escena
    float radio;          // visto desde arriba, el camión cabe en un círculo de este radio, gire como gire

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (exhibicion == null)
        {
            enabled = false;
            return;
        }
        var filtros = exhibicion.GetComponentsInChildren<MeshFilter>();
        var limites = new Bounds(exhibicion.position, Vector3.zero);
        foreach (var f in filtros)
        {
            var b = f.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var esquina = f.transform.TransformPoint(new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z));
                limites.Encapsulate(esquina);
                var plano = esquina - exhibicion.position;
                radio = Mathf.Max(radio, new Vector2(plano.x, plano.z).magnitude);
            }
        }
        distanciaBase = Vector3.Dot(limites.center - transform.position, transform.forward);
        pivote = transform.position + transform.forward * distanciaBase;
    }

    void LateUpdate()
    {
        var destino = cam.targetTexture;
        float aspecto = destino != null ? (float)destino.width / destino.height : (float)Screen.width / Screen.height;
        // Lo que el camión puede abrirse a cada lado por cada metro de distancia.
        float t = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspecto * anchoMaximo;
        // El punto del giro que más se abre en pantalla está más cerca de la cámara que el centro: r / √(d² − r²) ≤ t.
        float necesaria = radio * Mathf.Sqrt(1f + 1f / (t * t));
        transform.position = pivote - transform.forward * Mathf.Max(distanciaBase, necesaria);
    }
}

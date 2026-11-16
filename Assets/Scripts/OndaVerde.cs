using UnityEngine;

// La franja de la onda verde: agarrar la onda verde es pasar los semáforos en verde uno tras otro. GeneradorAvenida la pone al
// pie del semáforo de algunos cruces, en uno de los tres carriles, y deja la avenida libre detrás. Si el camión pasa por encima
// (por el asfalto, no brincándola), agarra la onda verde: Aceleron.Acelerar. La franja se apaga al tomarla.
[RequireComponent(typeof(BoxCollider))]
public class OndaVerde : MonoBehaviour
{
    [Tooltip("La franja y sus flechas: se apagan al tomarla.")]
    [SerializeField] Renderer[] partes;
    [Tooltip("El material de la franja ya tomada.")]
    [SerializeField] Material apagada;

    public bool Tomada { get; private set; }

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (Tomada)
        {
            return;
        }
        var camion = otro.GetComponentInParent<CamionRuta>();
        if (camion == null || camion.Detenido || camion.Llegando || camion.Aceleron == null)
        {
            return;
        }
        Tomada = true;
        camion.Aceleron.Acelerar("¡ONDA VERDE!");
        if (apagada != null)
        {
            foreach (var r in partes)
            {
                if (r != null)
                {
                    r.sharedMaterial = apagada;
                }
            }
        }
    }
}

using UnityEngine;

// Una pintura del camión, como el color de la bola de cada Stage del tutorial: un asset que se crea con clic derecho > Create >
// Camión de ruta > Pintura del camión y se ajusta en el Inspector. Trae el nombre que sale en el botón PINTURA del menú, el color
// de cada parte de la carrocería y su acabado. PintarCamion guarda la lista de pinturas y aplica la elegida.
[CreateAssetMenu(fileName = "Nueva pintura", menuName = "Camión de ruta/Pintura del camión", order = 1)]
public class PinturaCamion : ScriptableObject
{
    [Tooltip("El nombre que aparece en el botón PINTURA del menú.")]
    public string nombre = "Verde Mi Transporte";

    [Header("Colores")]
    public Color carroceria = new Color32(50, 166, 62, 255);
    public Color techo = new Color32(58, 176, 70, 255);
    [Tooltip("Los cuadros de los costados que en la pintura verde son verde claro.")]
    public Color cuadrosA = new Color32(170, 214, 96, 255);
    [Tooltip("Los cuadros de los costados que en la pintura verde son amarillos.")]
    public Color cuadrosB = new Color32(232, 196, 70, 255);
    [Tooltip("Los cuadros de los costados que en la pintura verde son blancos.")]
    public Color cuadrosC = new Color32(242, 240, 232, 255);
    [Tooltip("Las defensas de adelante y de atrás.")]
    public Color defensas = new Color32(22, 22, 24, 255);

    [Header("Acabado")]
    [Tooltip("Qué tanto brilla la pintura: 0, mate; 1, como espejo.")]
    [Range(0f, 1f)] public float suavidad = 0.4f;
    [Tooltip("Qué tan metálica se ve la pintura.")]
    [Range(0f, 1f)] public float metalico;
}

using UnityEngine;

// Ajusta el rectángulo de la interfaz al área segura del teléfono, para que la muesca o la barra de gestos no
// tapen los botones. Se revisa en cada cuadro porque el área cambia al girar el teléfono.
[RequireComponent(typeof(RectTransform))]
public class AreaSegura : MonoBehaviour
{
    RectTransform rect;
    Rect aplicada;
    Vector2Int pantalla;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        Aplicar();
    }

    void Update()
    {
        if (Screen.safeArea != aplicada || pantalla.x != Screen.width || pantalla.y != Screen.height)
        {
            Aplicar();
        }
    }

    void Aplicar()
    {
        aplicada = Screen.safeArea;
        pantalla = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }
        rect.anchorMin = new Vector2(aplicada.xMin / Screen.width, aplicada.yMin / Screen.height);
        rect.anchorMax = new Vector2(aplicada.xMax / Screen.width, aplicada.yMax / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

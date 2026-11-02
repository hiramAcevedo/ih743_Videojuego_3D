using UnityEngine;

// El contenido de un panel (títulos, textos y botones) se diseñó para una pantalla vertical de 1080 × 1920. Este
// componente lo mantiene en ese tamaño y lo escala completo para que quepa en el panel, centrado: en un teléfono
// vertical ocupa todo el ancho, y en una pantalla horizontal o en el editor queda como una columna al centro. Así los
// textos nunca se enciman entre sí, sea cual sea la proporción de la pantalla.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ContenidoVertical : MonoBehaviour
{
    static readonly Vector2 Diseno = new Vector2(1080f, 1920f);

    RectTransform rect;
    RectTransform padre;

    void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        padre = transform.parent as RectTransform;
        Ajustar();
    }

    void LateUpdate() => Ajustar();

    void Ajustar()
    {
        if (rect == null || padre == null)
        {
            return;
        }
        var disponible = padre.rect.size;
        if (disponible.x <= 0f || disponible.y <= 0f)
        {
            return;
        }
        float escala = Mathf.Min(disponible.x / Diseno.x, disponible.y / Diseno.y);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Diseno;
        rect.localScale = new Vector3(escala, escala, 1f);
    }
}

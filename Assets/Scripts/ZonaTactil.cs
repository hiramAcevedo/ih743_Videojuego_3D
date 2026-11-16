using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Una zona táctil sin botón (4.6): no dibuja nada, sólo recibe los toques y se los pasa a ControlesTactiles. Las esquinas sólo
// responden dentro de su arco (un cuarto de círculo de "radio" desde la esquina de abajo); el salto ocupa toda la pantalla y
// queda debajo de todo, así que recibe lo que no tocó otra zona ni un elemento del tablero (la pausa, el engrane y el poder).
[RequireComponent(typeof(CanvasRenderer))]
public class ZonaTactil : MaskableGraphic, IPointerDownHandler, IPointerUpHandler, ICanvasRaycastFilter
{
    public enum Accion { CarrilIzquierdo, CarrilDerecho, Salto, Freno, Acelerador }

    [SerializeField] Accion accion;
    [SerializeField] ControlesTactiles controles;
    [Tooltip("En las esquinas, el radio del arco en unidades del lienzo (1080 de ancho); en las demás zonas no se usa.")]
    [SerializeField, Min(10f)] float radio = 215f;

    protected ZonaTactil()
    {
        useLegacyMeshGeneration = false;
    }

    public Accion Que => accion;
    public float Radio => radio;

    // No dibuja: la zona es invisible y lo que se ilumina al tocar va aparte.
    protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

    public bool IsRaycastLocationValid(Vector2 punto, Camera camara)
    {
        if (accion != Accion.CarrilIzquierdo && accion != Accion.CarrilDerecho)
        {
            return true;
        }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, punto, camara, out var local))
        {
            return false;
        }
        var r = rectTransform.rect;
        var esquina = new Vector2(accion == Accion.CarrilIzquierdo ? r.xMin : r.xMax, r.yMin);
        return (local - esquina).sqrMagnitude <= radio * radio;
    }

    public void OnPointerDown(PointerEventData datos)
    {
        if (controles != null)
        {
            controles.Bajo(accion, datos);
        }
    }

    public void OnPointerUp(PointerEventData datos)
    {
        if (controles != null)
        {
            controles.Subio(accion, datos);
        }
    }
}

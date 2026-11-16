using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Los controles táctiles sin botones (4.6), como los dibujó Hiram: en cada esquina de abajo, dentro de un arco chico, el cambio
// de carril; fuera de los arcos, hacia arriba, el salto; abajo, entre los dos arcos y divididos al centro sin raya, el freno a
// la izquierda y el acelerador a la derecha, que actúan mientras se mantiene el dedo (en horizontal, junto a sus arcos:
// TableroHorizontal). No hay botones visibles: al tocar sólo se ilumina la zona, en rojo el freno y en verde el acelerador, y los
// carriles y el salto con un destello. Las guías (arcos, flechas y letreros) se encienden y se apagan con el interruptor de la
// pausa y de AJUSTES (Guias). Cada dedo es un puntero aparte: se puede frenar con uno y cambiar de carril con otro.
public class ControlesTactiles : MonoBehaviour
{
    [SerializeField] CamionRuta camion;

    [Header("Lo que se ilumina al tocar")]
    [SerializeField] Graphic brilloFreno;
    [SerializeField] Graphic brilloAcelerador;
    [SerializeField] Graphic brilloIzquierda;
    [SerializeField] Graphic brilloDerecha;
    [Tooltip("El destello del salto: un anillo que sale donde se tocó.")]
    [SerializeField] Graphic destelloSalto;
    [SerializeField, Range(0f, 1f)] float opacidadPisado = 0.42f;
    [SerializeField, Range(0f, 1f)] float opacidadDestello = 0.35f;
    [SerializeField, Min(0.05f)] float segundosDestello = 0.28f;

    [Header("Guías")]
    [Tooltip("Los arcos, las flechas, «Salto», «Freno» y «Acelerador»: se ven sólo con las guías encendidas.")]
    [SerializeField] GameObject guias;

    readonly HashSet<int> dedosFreno = new HashSet<int>();
    readonly HashSet<int> dedosAcelerador = new HashSet<int>();
    float tocoIzquierda = -10f, tocoDerecha = -10f, tocoSalto = -10f;
    float opacidadFreno, opacidadAcelerador;

    public int Toques { get; private set; }
    public ZonaTactil.Accion UltimaAccion { get; private set; }
    public bool GuiasVisibles => guias != null && guias.activeSelf;

    void OnEnable()
    {
        Guias.Cambiaron += PintarGuias;
        PintarGuias();
        Pintar();
    }

    void OnDisable()
    {
        Guias.Cambiaron -= PintarGuias;
        // Al pausar o al chocar se apagan los controles: el dedo que se quedó en el freno o el acelerador ya no cuenta.
        dedosFreno.Clear();
        dedosAcelerador.Clear();
        if (camion != null)
        {
            camion.PisarFreno(false);
            camion.PisarAcelerador(false);
        }
        opacidadFreno = opacidadAcelerador = 0f;
        tocoIzquierda = tocoDerecha = tocoSalto = -10f;
        Pintar();
    }

    public void Bajo(ZonaTactil.Accion accion, PointerEventData datos)
    {
        Toques++;
        UltimaAccion = accion;
        switch (accion)
        {
            case ZonaTactil.Accion.CarrilIzquierdo:
                camion.CarrilIzquierdo();
                tocoIzquierda = Time.unscaledTime;
                break;
            case ZonaTactil.Accion.CarrilDerecho:
                camion.CarrilDerecho();
                tocoDerecha = Time.unscaledTime;
                break;
            case ZonaTactil.Accion.Salto:
                camion.Brincar();
                tocoSalto = Time.unscaledTime;
                ColocarDestello(datos);
                break;
            case ZonaTactil.Accion.Freno:
                dedosFreno.Add(datos.pointerId);
                camion.PisarFreno(true);
                break;
            case ZonaTactil.Accion.Acelerador:
                dedosAcelerador.Add(datos.pointerId);
                camion.PisarAcelerador(true);
                break;
        }
    }

    public void Subio(ZonaTactil.Accion accion, PointerEventData datos)
    {
        if (accion == ZonaTactil.Accion.Freno && dedosFreno.Remove(datos.pointerId) && dedosFreno.Count == 0)
        {
            camion.PisarFreno(false);
        }
        else if (accion == ZonaTactil.Accion.Acelerador && dedosAcelerador.Remove(datos.pointerId) && dedosAcelerador.Count == 0)
        {
            camion.PisarAcelerador(false);
        }
    }

    void ColocarDestello(PointerEventData datos)
    {
        if (destelloSalto == null)
        {
            return;
        }
        var padre = destelloSalto.rectTransform.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, datos.position, datos.pressEventCamera, out var local))
        {
            destelloSalto.rectTransform.anchoredPosition = local - padre.rect.min;
        }
    }

    void PintarGuias()
    {
        if (guias != null)
        {
            guias.SetActive(Guias.Encendidas);
        }
    }

    void Update() => Pintar();

    void Pintar()
    {
        float ahora = Time.unscaledTime;
        float paso = Time.unscaledDeltaTime * 6f;
        opacidadFreno = Mathf.MoveTowards(opacidadFreno, dedosFreno.Count > 0 ? opacidadPisado : 0f, paso);
        opacidadAcelerador = Mathf.MoveTowards(opacidadAcelerador, dedosAcelerador.Count > 0 ? opacidadPisado : 0f, paso);
        Opacidad(brilloFreno, opacidadFreno);
        Opacidad(brilloAcelerador, opacidadAcelerador);
        Opacidad(brilloIzquierda, Destello(ahora - tocoIzquierda));
        Opacidad(brilloDerecha, Destello(ahora - tocoDerecha));
        float salto = Mathf.Clamp01((ahora - tocoSalto) / segundosDestello);
        Opacidad(destelloSalto, salto < 1f ? opacidadDestello * 1.6f * (1f - salto) : 0f);
        if (destelloSalto != null)
        {
            float escala = Mathf.Lerp(0.55f, 1.25f, salto);
            destelloSalto.rectTransform.localScale = new Vector3(escala, escala, 1f);
        }
    }

    float Destello(float desde) => desde < segundosDestello ? opacidadDestello * (1f - desde / segundosDestello) : 0f;

    static void Opacidad(Graphic g, float a)
    {
        if (g == null)
        {
            return;
        }
        var c = g.color;
        if (!Mathf.Approximately(c.a, a))
        {
            c.a = a;
            g.color = c;
        }
    }
}

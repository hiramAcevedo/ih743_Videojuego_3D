using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// El tablero de abajo con la pantalla acostada (4.6). En vertical no cambia nada. En horizontal, la lectura de metros y km/h
// ya no va arriba del freno y del acelerador, donde tapaba el camión: baja hasta abajo, al centro, con la barra del acelerón
// debajo, como en vertical; «Freno» queda junto al arco izquierdo y «Acelerador» junto al derecho, y cada zona va de su arco al
// centro, donde se dividen sin dibujarse. Los avisos van entre la lectura y el camión, más bajitos para no taparlo. Se acomoda
// solo cuando cambia el tamaño de la pantalla (al girar el teléfono o al cambiar la vista de juego), y al volver a vertical todo
// regresa a como estaba.
[RequireComponent(typeof(RectTransform))]
public class TableroHorizontal : MonoBehaviour
{
    [Header("Freno y acelerador")]
    [SerializeField] RectTransform zonaFreno;
    [SerializeField] RectTransform zonaAcelerador;
    [SerializeField] RectTransform brilloFreno;
    [SerializeField] RectTransform brilloAcelerador;
    [SerializeField] Text letreroFreno;
    [SerializeField] Text letreroAcelerador;
    [SerializeField] RectTransform subrayadoFreno;
    [SerializeField] RectTransform subrayadoAcelerador;

    [Header("La lectura y lo que va con ella")]
    [Tooltip("El trazo de la lectura primero; después los metros, los km/h, sus subrayados y las rayitas. Bajan juntos.")]
    [SerializeField] RectTransform[] lectura;
    [SerializeField] RectTransform barraAceleron;
    [SerializeField] RectTransform aviso;

    [Header("En horizontal, en unidades del lienzo")]
    [Tooltip("Ancho de los arcos de las esquinas: ahí empiezan las zonas del freno y del acelerador.")]
    [SerializeField, Min(0f)] float anchoArco = 240f;
    [Tooltip("Espacio entre el arco y su letrero.")]
    [SerializeField, Min(0f)] float separacion = 18f;
    [Tooltip("Alto de las zonas del freno y del acelerador, desde abajo.")]
    [SerializeField, Min(50f)] float altoZonas = 210f;
    [Tooltip("Altura del centro de la lectura, desde abajo.")]
    [SerializeField] float alturaLectura = 112f;
    [Tooltip("Altura de la barra del acelerón, debajo de la lectura.")]
    [SerializeField] float alturaBarra = 20f;
    [Tooltip("Altura del centro de los avisos, entre la lectura y el camión.")]
    [SerializeField] float alturaAviso = 220f;
    [Tooltip("Alto de la caja de los avisos: la letra se achica para caber y no llegar al camión.")]
    [SerializeField, Min(30f)] float altoAviso = 68f;

    struct Estado
    {
        public Vector2 anclaMin, anclaMax, pivote, posicion, tam;
    }

    readonly Dictionary<RectTransform, Estado> vertical = new Dictionary<RectTransform, Estado>();
    TextAnchor alineacionFreno, alineacionAcelerador;
    bool listo;
    bool? aplicado;

    // Si está acomodado en horizontal.
    public bool Horizontal => aplicado == true;

    void Awake()
    {
        // La escena guarda el tablero vertical: eso es lo que se regresa al volver a vertical.
        foreach (var r in Todos())
        {
            if (r != null && !vertical.ContainsKey(r))
            {
                vertical[r] = new Estado { anclaMin = r.anchorMin, anclaMax = r.anchorMax, pivote = r.pivot, posicion = r.anchoredPosition, tam = r.sizeDelta };
            }
        }
        alineacionFreno = letreroFreno != null ? letreroFreno.alignment : TextAnchor.MiddleCenter;
        alineacionAcelerador = letreroAcelerador != null ? letreroAcelerador.alignment : TextAnchor.MiddleCenter;
        listo = true;
    }

    void Start() => Acomodar();

    // Si la pantalla giró con el tablero oculto (en la pausa), se acomoda al volver.
    void OnEnable()
    {
        if (listo)
        {
            Acomodar();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        if (listo && isActiveAndEnabled)
        {
            Acomodar();
        }
    }

    IEnumerable<RectTransform> Todos()
    {
        yield return zonaFreno;
        yield return zonaAcelerador;
        yield return brilloFreno;
        yield return brilloAcelerador;
        yield return letreroFreno != null ? letreroFreno.rectTransform : null;
        yield return letreroAcelerador != null ? letreroAcelerador.rectTransform : null;
        yield return subrayadoFreno;
        yield return subrayadoAcelerador;
        yield return barraAceleron;
        yield return aviso;
        if (lectura != null)
        {
            foreach (var r in lectura)
            {
                yield return r;
            }
        }
    }

    void Acomodar()
    {
        var area = ((RectTransform)transform).rect;
        bool horizontal = area.width > area.height;
        if (aplicado == horizontal)
        {
            return;
        }
        aplicado = horizontal;
        if (horizontal)
        {
            AHorizontal();
        }
        else
        {
            AVertical();
        }
    }

    void AVertical()
    {
        foreach (var par in vertical)
        {
            var r = par.Key;
            var e = par.Value;
            r.anchorMin = e.anclaMin;
            r.anchorMax = e.anclaMax;
            r.pivot = e.pivote;
            r.anchoredPosition = e.posicion;
            r.sizeDelta = e.tam;
        }
        if (letreroFreno != null)
        {
            letreroFreno.alignment = alineacionFreno;
        }
        if (letreroAcelerador != null)
        {
            letreroAcelerador.alignment = alineacionAcelerador;
        }
    }

    void AHorizontal()
    {
        // Cada zona, de su arco al centro.
        Tramo(zonaFreno, new Vector2(0f, 0f), new Vector2(0.5f, 0f), anchoArco, 0f);
        Tramo(brilloFreno, new Vector2(0f, 0f), new Vector2(0.5f, 0f), anchoArco, 0f);
        Tramo(zonaAcelerador, new Vector2(0.5f, 0f), new Vector2(1f, 0f), 0f, -anchoArco);
        Tramo(brilloAcelerador, new Vector2(0.5f, 0f), new Vector2(1f, 0f), 0f, -anchoArco);

        // Los letreros, pegados a su arco; el subrayado, debajo de la palabra.
        float desdeArco = anchoArco + separacion;
        Letrero(letreroFreno, subrayadoFreno, new Vector2(0f, 0f), desdeArco, TextAnchor.MiddleLeft);
        Letrero(letreroAcelerador, subrayadoAcelerador, new Vector2(1f, 0f), -desdeArco, TextAnchor.MiddleRight);

        // La lectura baja junta, hasta abajo, con la barra del acelerón debajo; los avisos, entre ella y el camión.
        if (lectura != null && lectura.Length > 0 && lectura[0] != null && vertical.TryGetValue(lectura[0], out var trazo))
        {
            float bajar = trazo.posicion.y - alturaLectura;
            foreach (var r in lectura)
            {
                if (r != null && vertical.TryGetValue(r, out var e))
                {
                    r.anchoredPosition = new Vector2(e.posicion.x, e.posicion.y - bajar);
                }
            }
        }
        Altura(barraAceleron, alturaBarra);
        Altura(aviso, alturaAviso);
        if (aviso != null)
        {
            aviso.sizeDelta = new Vector2(aviso.sizeDelta.x, altoAviso);
        }
    }

    // Un rectángulo que va de un ancla a otra en la orilla de abajo, con su alto.
    void Tramo(RectTransform r, Vector2 anclaMin, Vector2 anclaMax, float izquierda, float derecha)
    {
        if (r == null)
        {
            return;
        }
        r.anchorMin = anclaMin;
        r.anchorMax = anclaMax;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = new Vector2(izquierda, 0f);
        r.offsetMax = new Vector2(derecha, altoZonas);
    }

    void Letrero(Text letrero, RectTransform subrayado, Vector2 ancla, float x, TextAnchor alineacion)
    {
        if (letrero == null || !vertical.TryGetValue(letrero.rectTransform, out var e))
        {
            return;
        }
        var r = letrero.rectTransform;
        float lado = ancla.x < 0.5f ? 0f : 1f;
        r.anchorMin = ancla;
        r.anchorMax = ancla;
        r.pivot = new Vector2(lado, 0.5f);
        r.anchoredPosition = new Vector2(x, e.posicion.y);
        r.sizeDelta = e.tam;
        letrero.alignment = alineacion;
        if (subrayado == null || !vertical.TryGetValue(subrayado, out var s))
        {
            return;
        }
        // El ancho de la palabra: lo que mide el texto con su letra, o tres quintos de su caja si aún no se puede medir.
        float palabra = Mathf.Min(letrero.preferredWidth, e.tam.x);
        if (palabra <= 1f)
        {
            palabra = e.tam.x * 0.6f;
        }
        subrayado.anchorMin = ancla;
        subrayado.anchorMax = ancla;
        subrayado.pivot = new Vector2(0.5f, 0.5f);
        subrayado.anchoredPosition = new Vector2(x + (lado == 0f ? 1f : -1f) * palabra * 0.5f, s.posicion.y);
        subrayado.sizeDelta = s.tam;
    }

    void Altura(RectTransform r, float y)
    {
        if (r != null && vertical.TryGetValue(r, out var e))
        {
            r.anchoredPosition = new Vector2(e.posicion.x, y);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

// La navegación entre escenas y paneles, como el menú del tutorial (SceneManager con las escenas de Build Settings) pero con
// fundido (Transicion). En el menú: JUGAR, que lleva al primer nivel sin terminar; NIVELES, el panel para elegir ciudad o la
// ruta libre; cómo se juega y salir. En una avenida: pausa (detiene el tiempo) con continuar, reiniciar y volver al menú; el
// aviso de choque, que ControlPartida llena y desde el que se juega de nuevo; y en los niveles, el aviso de nivel completado
// con SIGUIENTE NIVEL y el de fin de la ruta. Build Settings: Menu (0), Guadalajara (1), Monterrey (2) y Ruta, la ruta libre (3).
public class Navegacion : MonoBehaviour
{
    public const string EscenaMenu = "Menu";
    public const string EscenaRuta = "Ruta";

    [Tooltip("Los niveles en orden; en el menú y en las escenas de nivel.")]
    [SerializeField] RutaNiveles ruta;
    [Tooltip("El nivel de la escena; vacío en el menú y en la ruta libre.")]
    [SerializeField] NivelRuta nivel;

    [Header("Paneles")]
    [Tooltip("En el menú, el panel de los botones; en una avenida, el HUD con los controles.")]
    [SerializeField] GameObject panelPrincipal;
    [SerializeField] GameObject panelInstrucciones;
    [SerializeField] GameObject panelNiveles;
    [SerializeField] GameObject panelPausa;
    [SerializeField] GameObject panelChoque;
    [SerializeField] GameObject panelCompletado;
    [SerializeField] GameObject panelFinDeRuta;
    [SerializeField] GameObject controles;
    [Tooltip("En el menú, la etiqueta de JUGAR: dice a qué nivel lleva.")]
    [SerializeField] Text etiquetaJugar;

    public bool EnPausa { get; private set; }
    public GameObject PanelVisible { get; private set; }

    void Start()
    {
        Time.timeScale = 1f;
        Mostrar(panelPrincipal);
        EscribirJugar();
    }

    // JUGAR: el primer nivel sin terminar; con todos terminados, la ruta vuelve a empezar en Guadalajara.
    public void Jugar()
    {
        var aJugar = ruta != null ? ruta.AJugar() : null;
        Cargar(aJugar != null ? aJugar.escena : EscenaRuta);
    }

    public void JugarNivel(NivelRuta elegido)
    {
        if (elegido != null && (ruta == null || ruta.Abierto(elegido)))
        {
            Cargar(elegido.escena);
        }
    }

    public void JugarRutaLibre() => Cargar(ruta != null ? ruta.escenaLibre : EscenaRuta);

    public void SiguienteNivel()
    {
        var siguiente = ruta != null && nivel != null ? ruta.Siguiente(nivel) : null;
        Cargar(siguiente != null ? siguiente.escena : EscenaMenu);
    }

    public void VolverAlMenu() => Cargar(EscenaMenu);

    public void VerInstrucciones() => Mostrar(panelInstrucciones);
    public void VerNiveles() => Mostrar(panelNiveles);

    public void VolverAlPrincipal()
    {
        Mostrar(panelPrincipal);
        EscribirJugar();
    }

    // Lo que hacía antes CerrarInstrucciones: el botón VOLVER del panel de Cómo se juega.
    public void CerrarInstrucciones() => VolverAlPrincipal();

    public void Pausar()
    {
        EnPausa = true;
        Time.timeScale = 0f;
        Mostrar(panelPausa);
    }

    public void Continuar() => MostrarJuego();

    // El HUD y los controles, con el tiempo corriendo: al continuar desde la pausa y al empezar otra partida.
    public void MostrarJuego()
    {
        EnPausa = false;
        Time.timeScale = 1f;
        Mostrar(panelPrincipal);
    }

    public void MostrarChoque() => Mostrar(panelChoque);
    public void MostrarNivelCompletado() => Mostrar(panelCompletado);
    public void MostrarFinDeRuta() => Mostrar(panelFinDeRuta);

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Cargar(string escena)
    {
        EnPausa = false;
        Transicion.Cargar(escena);
    }

    void EscribirJugar()
    {
        if (etiquetaJugar == null || ruta == null)
        {
            return;
        }
        var aJugar = ruta.AJugar();
        if (aJugar == null)
        {
            return;
        }
        int chica = Mathf.RoundToInt(etiquetaJugar.fontSize * 0.5f);
        etiquetaJugar.text = "JUGAR\n<size=" + chica + ">Nivel " + aJugar.numero + " · " + aJugar.ciudad + "</size>";
    }

    // Deja visible sólo el panel pedido; los controles del camión se ven únicamente mientras se maneja.
    void Mostrar(GameObject panel)
    {
        foreach (var p in new[] { panelPrincipal, panelInstrucciones, panelNiveles, panelPausa, panelChoque, panelCompletado, panelFinDeRuta })
        {
            if (p != null)
            {
                p.SetActive(p == panel);
            }
        }
        if (controles != null)
        {
            controles.SetActive(panel == panelPrincipal);
        }
        PanelVisible = panel;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

// La navegación entre escenas y paneles. En el menú: jugar, cómo se juega y salir. En la avenida: pausa (detiene el
// tiempo), continuar, volver al menú, y los avisos de choque y de llegada a la base.
public class Navegacion : MonoBehaviour
{
    public const string EscenaMenu = "Menu";
    public const string EscenaRuta = "Ruta";

    [SerializeField] GameObject panelPrincipal;
    [SerializeField] GameObject panelInstrucciones;
    [SerializeField] GameObject panelPausa;
    [SerializeField] GameObject panelChoque;
    [SerializeField] GameObject panelFin;
    [SerializeField] GameObject controles;

    public bool EnPausa { get; private set; }

    void Start()
    {
        Time.timeScale = 1f;
        Mostrar(panelPrincipal);
    }

    public void Jugar() => SceneManager.LoadScene(EscenaRuta);

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(EscenaMenu);
    }

    public void VerInstrucciones() => Mostrar(panelInstrucciones);
    public void CerrarInstrucciones() => Mostrar(panelPrincipal);

    public void Pausar()
    {
        EnPausa = true;
        Time.timeScale = 0f;
        Mostrar(panelPausa);
    }

    public void Continuar()
    {
        EnPausa = false;
        Time.timeScale = 1f;
        Mostrar(panelPrincipal);
    }

    public void MostrarChoque() => Mostrar(panelChoque);
    public void MostrarFin() => Mostrar(panelFin);

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Deja visible sólo el panel pedido; los controles del camión se ven únicamente mientras se maneja.
    void Mostrar(GameObject panel)
    {
        foreach (var p in new[] { panelPrincipal, panelInstrucciones, panelPausa, panelChoque, panelFin })
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
    }
}

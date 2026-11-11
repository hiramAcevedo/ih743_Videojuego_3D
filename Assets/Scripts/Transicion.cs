using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El cambio de escena con fundido, como el cambio de nivel del tutorial pero sin cortes: la pantalla se oscurece, SceneManager
// carga la escena nueva en segundo plano (LoadSceneAsync) y ésta aparece aclarándose. Va en el lienzo de cada escena, encima de
// todo; mientras funde, el velo no deja tocar los botones. Usa el tiempo sin escala, así funciona también desde la pausa.
public class Transicion : MonoBehaviour
{
    [Tooltip("La imagen negra que cubre la pantalla.")]
    [SerializeField] Image velo;
    [Tooltip("Segundos que tarda en oscurecer o en aclarar.")]
    [SerializeField, Min(0f)] float segundos = 0.35f;

    static Transicion actual;

    bool cargando;

    // Qué tan cubierta está la pantalla (0, nada; 1, negra) y si ya se pidió otra escena: la prueba espera a que aclare.
    public static float Opacidad => actual != null && actual.velo != null ? actual.velo.color.a : 0f;
    public static bool Cargando => actual != null && actual.cargando;

    void Awake()
    {
        actual = this;
        Cubrir(1f);
    }

    IEnumerator Start()
    {
        yield return Fundir(1f, 0f);
    }

    void OnDestroy()
    {
        if (actual == this)
        {
            actual = null;
        }
    }

    // Carga una escena con fundido; sin Transicion en la escena, la carga de golpe.
    public static void Cargar(string escena)
    {
        if (actual != null)
        {
            actual.StartCoroutine(actual.CargarEscena(escena));
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(escena);
        }
    }

    IEnumerator CargarEscena(string escena)
    {
        if (cargando)
        {
            yield break;
        }
        cargando = true;
        var carga = SceneManager.LoadSceneAsync(escena);
        carga.allowSceneActivation = false;
        yield return Fundir(Opacidad, 1f);
        // La escena nueva arranca con el tiempo corriendo, aunque se haya salido desde la pausa.
        Time.timeScale = 1f;
        carga.allowSceneActivation = true;
        while (!carga.isDone)
        {
            yield return null;
        }
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        for (float t = 0f; t < segundos; t += Time.unscaledDeltaTime)
        {
            Cubrir(Mathf.Lerp(desde, hasta, t / segundos));
            yield return null;
        }
        Cubrir(hasta);
    }

    void Cubrir(float alfa)
    {
        if (velo == null)
        {
            return;
        }
        var c = velo.color;
        c.a = alfa;
        velo.color = c;
        velo.enabled = alfa > 0.001f || cargando;
        velo.raycastTarget = velo.enabled;
    }
}

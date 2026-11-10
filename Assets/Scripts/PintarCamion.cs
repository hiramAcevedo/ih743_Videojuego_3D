using System.Collections.Generic;
using UnityEngine;

// Pinta el camión con una de sus pinturas (assets de PinturaCamion): la que se eligió con el botón PINTURA del menú y quedó
// guardada con PlayerPrefs. Como en el tutorial, cambia el color del material de cada Renderer; pero hace materiales propios del
// camión a partir de los suyos, así no toca los que comparten los autos y los edificios. Va en el modelo del camión: el que gira
// en el menú y el que maneja el jugador.
public class PintarCamion : MonoBehaviour
{
    public const string ClavePintura = "PinturaCamion";

    [Tooltip("Las pinturas que se pueden elegir, en el orden en que las recorre el botón PINTURA.")]
    [SerializeField] PinturaCamion[] pinturas;

    enum Parte { Carroceria, Techo, CuadrosA, CuadrosB, CuadrosC, Defensas }

    readonly Dictionary<Renderer, Parte> partes = new Dictionary<Renderer, Parte>();
    readonly Dictionary<Parte, Material> propios = new Dictionary<Parte, Material>();

    // El número de la pintura guardada; con PlayerPrefs sigue ahí la próxima vez que se abre el juego.
    public static int Elegida
    {
        get => PlayerPrefs.GetInt(ClavePintura, 0);
        set
        {
            PlayerPrefs.SetInt(ClavePintura, value);
            PlayerPrefs.Save();
        }
    }

    public int Cantidad => pinturas != null ? pinturas.Length : 0;
    public PinturaCamion Actual { get; private set; }

    void Awake()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            var parte = ParteDe(r);
            if (parte.HasValue)
            {
                partes[r] = parte.Value;
            }
        }
    }

    void Start() => Aplicar(Elegida);

    // Lo que hace el botón PINTURA: pasa a la siguiente pintura, la aplica y la guarda.
    public void Siguiente()
    {
        if (Cantidad == 0)
        {
            return;
        }
        Elegida = (Elegida + 1) % Cantidad;
        Aplicar(Elegida);
    }

    public void Aplicar(int indice)
    {
        if (Cantidad == 0)
        {
            return;
        }
        Actual = pinturas[((indice % Cantidad) + Cantidad) % Cantidad];
        foreach (var par in partes)
        {
            var material = Propio(par.Value, par.Key.sharedMaterial);
            material.SetColor("_BaseColor", ColorDe(par.Value));
            material.SetFloat("_Smoothness", Actual.suavidad);
            material.SetFloat("_Metallic", Actual.metalico);
            par.Key.sharedMaterial = material;
        }
    }

    // Qué parte pinta cada Renderer: las defensas por su nombre (usan el material de las llantas) y lo demás por su material.
    static Parte? ParteDe(Renderer r)
    {
        if (r.name.StartsWith("Defensa"))
        {
            return Parte.Defensas;
        }
        var m = r.sharedMaterial;
        if (m == null)
        {
            return null;
        }
        switch (m.name)
        {
            case "camion_carroceria": return Parte.Carroceria;
            case "camion_techo": return Parte.Techo;
            case "cuadro_verde": return Parte.CuadrosA;
            case "cuadro_amarillo": return Parte.CuadrosB;
            case "cuadro_blanco": return Parte.CuadrosC;
            default: return null;
        }
    }

    // Un material propio por parte, copiado del original la primera vez.
    Material Propio(Parte parte, Material original)
    {
        if (!propios.TryGetValue(parte, out var m))
        {
            m = new Material(original) { name = original.name + " (pintura)" };
            propios[parte] = m;
        }
        return m;
    }

    Color ColorDe(Parte parte)
    {
        switch (parte)
        {
            case Parte.Carroceria: return Actual.carroceria;
            case Parte.Techo: return Actual.techo;
            case Parte.CuadrosA: return Actual.cuadrosA;
            case Parte.CuadrosB: return Actual.cuadrosB;
            case Parte.CuadrosC: return Actual.cuadrosC;
            default: return Actual.defensas;
        }
    }

    void OnDestroy()
    {
        foreach (var m in propios.Values)
        {
            Destroy(m);
        }
    }
}

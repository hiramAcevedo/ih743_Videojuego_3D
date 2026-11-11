using System.Globalization;

// Los números como se leen en México: los miles con coma (1,200 m). Con la cultura invariante, para que salga igual en la
// computadora y en el teléfono, sin depender del idioma del sistema.
public static class Formato
{
    public static string Entero(float valor) => string.Format(CultureInfo.InvariantCulture, "{0:#,0}", UnityEngine.Mathf.FloorToInt(valor));

    public static string Metros(float metros) => Entero(metros) + " m";
}

using System;
using UnityEngine;

// Las guías de los controles táctiles (4.6): los arcos de las esquinas con sus flechas, «Salto», «Freno» y «Acelerador». Se
// encienden y se apagan con el interruptor de la pausa y de AJUSTES; queda guardado con PlayerPrefs y al principio están
// encendidas. Las zonas funcionan igual con las guías apagadas.
public static class Guias
{
    public const string Clave = "GuiasTactiles";

    public static event Action Cambiaron;

    public static bool Encendidas
    {
        get => PlayerPrefs.GetInt(Clave, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(Clave, value ? 1 : 0);
            PlayerPrefs.Save();
            Cambiaron?.Invoke();
        }
    }
}

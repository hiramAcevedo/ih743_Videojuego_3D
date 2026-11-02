using UnityEngine;

// Gira el camión de exhibición del menú sobre su eje vertical.
public class Girar : MonoBehaviour
{
    [SerializeField] float gradosPorSegundo = 18f;

    void Update()
    {
        transform.Rotate(0f, gradosPorSegundo * Time.unscaledDeltaTime, 0f, Space.World);
    }
}

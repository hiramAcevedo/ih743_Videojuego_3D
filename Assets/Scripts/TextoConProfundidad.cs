using UnityEngine;

// Pone a un TextMesh el material de "Ruta/Texto con profundidad" con la textura de su fuente. La fuente es dinámica:
// cuando Unity rehace su textura, el material toma la nueva.
[RequireComponent(typeof(TextMesh))]
public class TextoConProfundidad : MonoBehaviour
{
    [SerializeField] Shader shader;

    TextMesh texto;
    Material material;

    void Awake()
    {
        texto = GetComponent<TextMesh>();
        if (shader == null || texto.font == null)
        {
            return;
        }
        material = new Material(shader) { mainTexture = texto.font.material.mainTexture };
        GetComponent<MeshRenderer>().sharedMaterial = material;
        Font.textureRebuilt += AlRehacerFuente;
    }

    void OnDestroy()
    {
        Font.textureRebuilt -= AlRehacerFuente;
        if (material != null)
        {
            Destroy(material);
        }
    }

    void AlRehacerFuente(Font fuente)
    {
        if (material != null && fuente == texto.font)
        {
            material.mainTexture = fuente.material.mainTexture;
        }
    }
}

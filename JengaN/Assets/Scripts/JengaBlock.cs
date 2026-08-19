using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class JengaBlock : MonoBehaviour
{
    public int Level { get; private set; }
    public int Index { get; private set; }
    public bool IsPlacedOnTop { get; set; }

    private Rigidbody rb;
    private BoxCollider col;
    private MeshRenderer meshRenderer;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private Color originalColor;
    private bool isSelected = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Initialize(int level, int index)
    {
        Level = level;
        Index = index;
        IsPlacedOnTop = false;

        // Guardar posiciones locales iniciales relativas al padre (la torre)
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        if (meshRenderer != null && meshRenderer.material != null)
        {
            originalColor = meshRenderer.material.color;
        }
        else
        {
            originalColor = Color.white;
        }
    }

    public void SetSelected(bool selected, Color highlightColor)
    {
        isSelected = selected;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            if (isSelected)
            {
                meshRenderer.material.color = highlightColor;
                // Si el material tiene emisión (como en URP), podemos habilitarlo
                meshRenderer.material.EnableKeyword("_EMISSION");
                meshRenderer.material.SetColor("_EmissionColor", highlightColor * 0.4f);
            }
            else
            {
                meshRenderer.material.color = originalColor;
                meshRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }

    public void SetKinematic(bool kinematic)
    {
        if (rb != null)
        {
            rb.isKinematic = kinematic;
            if (!kinematic)
            {
                rb.WakeUp();
            }
        }
    }

    public void ResetToStart()
    {
        // Regresar a la posición inicial en local
        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;
        SetKinematic(false);
        SetSelected(false, Color.clear);
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void UpdateTopPlacement(int newLevel, int newIndex)
    {
        Level = newLevel;
        Index = newIndex;
        IsPlacedOnTop = true;
        
        // Guardar las nuevas coordenadas locales como las iniciales por si acaso
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
    }

    public void SetOriginalColor(Color color)
    {
        originalColor = color;
        if (meshRenderer != null && meshRenderer.material != null && !isSelected)
        {
            meshRenderer.material.color = originalColor;
        }
    }

    public Vector3 GetStartLocalPosition()
    {
        return startLocalPosition;
    }
}

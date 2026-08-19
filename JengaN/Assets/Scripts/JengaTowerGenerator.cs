using UnityEngine;
using System.Collections.Generic;

public class JengaTowerGenerator : MonoBehaviour
{
    [Header("Jenga Block Dimensions (Meters)")]
    [SerializeField] private float blockWidth = 0.05f;   // 5 cm
    [SerializeField] private float blockHeight = 0.03f;  // 3 cm
    [SerializeField] private float blockLength = 0.15f;  // 15 cm (3x width)
    [SerializeField] private float blockGap = 0.0005f;   // 0.5 mm to prevent initial physics collision glitches
    [SerializeField] private int numLevels = 18;         // 18 levels * 3 blocks = 54 blocks
    
    [Header("References")]
    [Tooltip("If null, a standard cube primitive will be generated automatically.")]
    [SerializeField] private GameObject blockPrefab;

    [Header("Visual Colors")]
    [SerializeField] private Color[] blockColors = new Color[]
    {
        new Color(0.18f, 0.64f, 0.6f),   // Teal
        new Color(0.92f, 0.40f, 0.33f),  // Coral
        new Color(0.95f, 0.65f, 0.22f)   // Amber/Gold
    };

    private List<JengaBlock> allBlocks = new List<JengaBlock>();
    
    // Tracking current tower state
    private int topLevel = 17;
    private int blocksInTopLevel = 3;

    public float BlockWidth => blockWidth;
    public float BlockHeight => blockHeight;
    public float BlockLength => blockLength;
    public List<JengaBlock> AllBlocks => allBlocks;
    public int TopLevel => topLevel;
    public int BlocksInTopLevel => blocksInTopLevel;

    private PhysicsMaterial blockPhysicsMaterial;

    void Awake()
    {
        // Generar material de física con alta fricción y cero rebote en ejecución
        blockPhysicsMaterial = new PhysicsMaterial("JengaFrictionMaterial")
        {
            staticFriction = 0.7f,
            dynamicFriction = 0.6f,
            bounciness = 0.0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }

    public void GenerateTower()
    {
        // Forzar la escala local a (1,1,1) para evitar bloques invisibles por escala del padre a cero
        transform.localScale = Vector3.one;

        // Seguridad: Si hay un Rigidbody en el root, forzarlo a ser cinemático para que no se caiga todo el Jenga
        Rigidbody rootRb = GetComponent<Rigidbody>();
        if (rootRb != null)
        {
            rootRb.isKinematic = true;
            Debug.Log("[JengaAR] Rigidbody detectado en el root JengaGame. Forzando isKinematic = true para evitar caída.");
        }

        // Limpiar bloques existentes si hay alguno
        ClearTower();

        topLevel = numLevels - 1;
        blocksInTopLevel = 3;

        // Crear base sólida (BasePlate) para sostener físicamente la torre (espesor de 10cm para evitar tunneling de PhysX)
        GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePlate.name = "JengaBasePlate";
        basePlate.transform.SetParent(transform, false);
        basePlate.transform.localPosition = new Vector3(0, -0.05f, 0); // Su centro a -5cm, la parte superior queda en Y=0
        basePlate.transform.localScale = new Vector3(blockLength * 1.3f, 0.1f, blockLength * 1.3f);
        
        // Asignar color oscuro estético
        MeshRenderer baseRenderer = basePlate.GetComponent<MeshRenderer>();
        if (baseRenderer != null && baseRenderer.material != null)
        {
            baseRenderer.material.color = new Color(0.12f, 0.12f, 0.12f);
        }

        // Configurar físicas de la base
        BoxCollider baseCol = basePlate.GetComponent<BoxCollider>();
        if (baseCol == null) baseCol = basePlate.AddComponent<BoxCollider>();
        baseCol.sharedMaterial = blockPhysicsMaterial;
        baseCol.contactOffset = 0.0005f;

        Rigidbody baseRb = basePlate.GetComponent<Rigidbody>();
        if (baseRb == null) baseRb = basePlate.AddComponent<Rigidbody>();
        baseRb.isKinematic = true; // Cinemático para que sea una base fija e inamovible

        // Alinear la física para mejorar la estabilidad inicial
        Physics.defaultSolverIterations = 15;
        Physics.defaultSolverVelocityIterations = 15;

        float verticalGap = 0.0002f; // 0.2 mm de espacio de seguridad vertical para evitar jitter en spawn

        for (int l = 0; l < numLevels; l++)
        {
            // Determinar orientación:
            // Niveles pares (0, 2, ...): a lo largo del eje X (rotación 0°)
            // Niveles impares (1, 3, ...): a lo largo del eje Z (rotación 90° en Y)
            bool isEvenLevel = (l % 2 == 0);
            
            for (int i = 0; i < 3; i++)
            {
                // Calcular posición local del bloque
                float offset = (i - 1) * (blockWidth + blockGap);
                Vector3 localPos = Vector3.zero;
                Quaternion localRot = Quaternion.identity;

                if (isEvenLevel)
                {
                    // Bloques alineados a lo largo de X, espaciados en Z
                    localPos = new Vector3(0, l * (blockHeight + verticalGap) + (blockHeight / 2f), offset);
                    localRot = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    // Bloques alineados a lo largo de Z, espaciados en X (rotados 90 grados)
                    localPos = new Vector3(offset, l * (blockHeight + verticalGap) + (blockHeight / 2f), 0);
                    localRot = Quaternion.Euler(0, 90, 0);
                }

                // Crear el GameObject del bloque
                GameObject blockObj;
                if (blockPrefab != null)
                {
                    blockObj = Instantiate(blockPrefab, transform);
                    blockObj.transform.localPosition = localPos;
                    blockObj.transform.localRotation = localRot;
                    blockObj.transform.localScale = new Vector3(blockLength, blockHeight, blockWidth);
                }
                else
                {
                    // Crear cubo primitivo si no hay prefab asignado
                    blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blockObj.transform.SetParent(transform, false);
                    blockObj.transform.localPosition = localPos;
                    blockObj.transform.localRotation = localRot;
                    blockObj.transform.localScale = new Vector3(blockLength, blockHeight, blockWidth);
                }

                blockObj.name = $"Block_L{l}_I{i}";

                // Asegurar componentes físicos
                Rigidbody rb = blockObj.GetComponent<Rigidbody>();
                if (rb == null) rb = blockObj.AddComponent<Rigidbody>();
                
                rb.mass = 0.1f;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;
                rb.isKinematic = true; // Cinemático al inicio para evitar explosiones por asentamiento de física
                rb.solverIterations = 25; // Más iteraciones para hacer el bloque físicamente estable
                rb.solverVelocityIterations = 25;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                BoxCollider col = blockObj.GetComponent<BoxCollider>();
                if (col == null) col = blockObj.AddComponent<BoxCollider>();
                col.sharedMaterial = blockPhysicsMaterial;
                col.contactOffset = 0.0005f; // Ajustar distancia de contacto para máxima precisión y estabilidad

                // Añadir script de comportamiento de bloque
                JengaBlock jengaBlock = blockObj.GetComponent<JengaBlock>();
                if (jengaBlock == null) jengaBlock = blockObj.AddComponent<JengaBlock>();

                // Asignar color premium
                Color blockColor = blockColors[(l + i) % blockColors.Length];
                jengaBlock.SetOriginalColor(blockColor);

                // Inicializar
                jengaBlock.Initialize(l, i);
                allBlocks.Add(jengaBlock);
            }
        }

        Debug.Log($"[JengaAR] Torre generada exitosamente. Total bloques: {allBlocks.Count}");
    }

    public void ClearTower()
    {
        foreach (var block in allBlocks)
        {
            if (block != null)
            {
                Destroy(block.gameObject);
            }
        }
        allBlocks.Clear();
        
        // Destruir cualquier hijo sobrante por seguridad
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Calcula la posición y rotación local de la siguiente ranura disponible en la cima.
    /// </summary>
    public void GetNextTopPositionAndRotation(out Vector3 localPos, out Quaternion localRot, out int targetLevel, out int targetIndex)
    {
        int checkLevel = topLevel;
        int checkIndex = blocksInTopLevel;

        if (checkIndex >= 3)
        {
            // El nivel de arriba está lleno, abrimos un nivel nuevo
            checkLevel++;
            checkIndex = 0;
        }

        targetLevel = checkLevel;
        targetIndex = checkIndex;

        bool isEvenLevel = (checkLevel % 2 == 0);
        float offset = (checkIndex - 1) * (blockWidth + blockGap);

        if (isEvenLevel)
        {
            localPos = new Vector3(0, checkLevel * blockHeight + (blockHeight / 2f), offset);
            localRot = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            localPos = new Vector3(offset, checkLevel * blockHeight + (blockHeight / 2f), 0);
            localRot = Quaternion.Euler(0, 90, 0);
        }
    }

    /// <summary>
    /// Mueve físicamente el bloque a la cima de la torre y actualiza su estado.
    /// </summary>
    public void AddBlockToTop(JengaBlock block)
    {
        GetNextTopPositionAndRotation(out Vector3 localPos, out Quaternion localRot, out int targetLevel, out int targetIndex);

        // Convertir posición local a mundial relativa al padre
        block.transform.SetParent(transform);
        block.transform.localPosition = localPos;
        block.transform.localRotation = localRot;

        // Actualizar la estructura interna del bloque
        block.UpdateTopPlacement(targetLevel, targetIndex);

        // Actualizar variables de la cima
        topLevel = targetLevel;
        blocksInTopLevel = targetIndex + 1;

        // Reactivar físicas del bloque colocado
        block.SetKinematic(false);
        block.SetSelected(false, Color.clear);

        Debug.Log($"[JengaAR] Bloque colocado en la cima: L{topLevel} I{targetIndex}");
    }
}

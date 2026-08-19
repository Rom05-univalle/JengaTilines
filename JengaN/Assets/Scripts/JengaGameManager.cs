using UnityEngine;
using System.Collections;
using UnityEngine.XR.ARFoundation;

public enum JengaGameState
{
    Setup,          // Esperando inicialización
    PlayerTurn,     // Turno del jugador activo, esperando que seleccione y extraiga un bloque
    BlockPulled,    // Bloque extraído con éxito, esperando colocación en la cima
    Settling,       // Bloque colocado en la cima, esperando que la física se asiente
    GameOver        // Torre derribada
}

public class JengaGameManager : MonoBehaviour
{
    public static JengaGameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int numPlayers = 3;
    [SerializeField] private float settlingTime = 3.0f; // Tiempo de espera para verificar estabilidad física
    [SerializeField] private float collapseThreshold = 0.05f; // Cuántos metros por debajo del plano base define caída
    [SerializeField] private Color[] playerColors = new Color[]
    {
        new Color(0f, 0.8f, 0.8f),   // Cyan / Turquesa (Jugador 1)
        new Color(1f, 0.4f, 0.4f),   // Coral / Rosa (Jugador 2)
        new Color(1f, 0.8f, 0.2f)    // Oro / Amarillo (Jugador 3)
    };

    [Header("Highlight Settings")]
    [SerializeField] private Color selectionHighlightColor = new Color(0.9f, 0.8f, 0.1f); // Dorado brillante

    // Referencias locales
    private JengaTowerGenerator towerGenerator;
    private JengaUIManager uiManager;
    private JengaInteractionManager interactionManager;

    // Estados de juego
    private JengaGameState currentState = JengaGameState.Setup;
    private int activePlayerIndex = 0; // 0 = Jugador 1, 1 = Jugador 2, 2 = Jugador 3
    private JengaBlock selectedBlock = null;
    private float baseHeight = 0f;
    private bool isTrackingStable = true;
    private bool canCheckCollapse = false;

    public JengaGameState CurrentState => currentState;
    public int ActivePlayerIndex => activePlayerIndex;
    public Color ActivePlayerColor => playerColors[activePlayerIndex % playerColors.Length];
    public JengaBlock SelectedBlock => selectedBlock;
    public JengaTowerGenerator TowerGenerator => towerGenerator;
    public Color SelectionHighlightColor => selectionHighlightColor;
    public bool IsTrackingStable => isTrackingStable;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        towerGenerator = GetComponent<JengaTowerGenerator>();
        uiManager = GetComponent<JengaUIManager>();
        if (uiManager == null) uiManager = gameObject.AddComponent<JengaUIManager>();
        
        interactionManager = GetComponent<JengaInteractionManager>();
        if (interactionManager == null) interactionManager = gameObject.AddComponent<JengaInteractionManager>();
    }

    void Start()
    {
        // Guardar la altura base (Y) del spawn
        baseHeight = transform.position.y;
        
        // Generar la torre e iniciar la partida
        if (towerGenerator != null)
        {
            towerGenerator.GenerateTower();
            StartGame();
            StartCoroutine(EnableCollapseCheckingAfterDelay());
        }
        else
        {
            Debug.LogError("[JengaAR] JengaTowerGenerator es null en GameManager.");
        }
    }

    void Update()
    {
        // 1. Verificar estabilidad del seguimiento AR
        UpdateARTrackingStatus();

        // Si la partida está activa (no en Setup o GameOver) y hay un colapso físico, terminar juego
        if (currentState == JengaGameState.PlayerTurn || currentState == JengaGameState.BlockPulled || currentState == JengaGameState.Settling)
        {
            if (CheckIfTowerCollapsed())
            {
                TriggerGameOver();
            }
        }
    }

    private void UpdateARTrackingStatus()
    {
        // En AR Foundation, si el estado de la sesión no está en Tracking, consideramos seguimiento inestable
        bool currentTracking = (ARSession.state == ARSessionState.SessionTracking);
        
        #if UNITY_EDITOR
        // En el editor, forzar siempre que el tracking sea estable para poder depurar con el mouse
        currentTracking = true;
        #endif
        
        if (currentTracking != isTrackingStable)
        {
            isTrackingStable = currentTracking;
            if (uiManager != null)
            {
                uiManager.SetTrackingWarningActive(!isTrackingStable);
            }
            Debug.Log($"[JengaAR] Estado de seguimiento AR cambiado: Estable = {isTrackingStable}");
        }
    }

    public void StartGame()
    {
        activePlayerIndex = 0;
        selectedBlock = null;
        currentState = JengaGameState.PlayerTurn;
        
        if (uiManager != null)
        {
            uiManager.UpdateTurnUI(activePlayerIndex, playerColors[activePlayerIndex]);
            uiManager.SetActionButtonActive(false);
        }
        Debug.Log("[JengaAR] Juego iniciado. Turno del Jugador 1.");
    }

    public bool IsValidBlockSelection(JengaBlock block)
    {
        if (currentState != JengaGameState.PlayerTurn)
            return false;

        // No se pueden retirar bloques del nivel superior de la torre
        if (block.Level >= towerGenerator.TopLevel)
        {
            Debug.LogWarning($"[JengaAR] Bloque inválido. L{block.Level} es el nivel superior (Cima: L{towerGenerator.TopLevel}).");
            return false;
        }

        return true;
    }

    public void SelectBlock(JengaBlock block)
    {
        if (!IsValidBlockSelection(block))
            return;

        if (selectedBlock != null)
        {
            selectedBlock.SetSelected(false, Color.clear);
        }

        selectedBlock = block;
        selectedBlock.SetSelected(true, selectionHighlightColor);
        selectedBlock.SetKinematic(true); // Kinematic para control manual de arrastre
        Debug.Log($"[JengaAR] Bloque seleccionado: {block.name}");
    }

    public void DeselectAndResetBlock()
    {
        if (selectedBlock != null)
        {
            selectedBlock.ResetToStart();
            selectedBlock = null;
            Debug.Log("[JengaAR] Bloque deseleccionado y restaurado a su posición original.");
        }
    }

    public void SetBlockPulled()
    {
        if (currentState != JengaGameState.PlayerTurn || selectedBlock == null)
            return;

        currentState = JengaGameState.BlockPulled;
        if (uiManager != null)
        {
            uiManager.SetActionButtonActive(true);
        }
        Debug.Log("[JengaAR] Bloque extraído por completo. Listo para colocarse en la cima.");
    }

    public void PlaceBlockOnTop()
    {
        if (currentState != JengaGameState.BlockPulled || selectedBlock == null)
            return;

        if (uiManager != null)
        {
            uiManager.SetActionButtonActive(false);
        }

        // Colocar el bloque en la cima a través del generador
        towerGenerator.AddBlockToTop(selectedBlock);
        
        // Entrar en fase de asentamiento para dejar que las físicas actúen
        StartCoroutine(SettlingRoutine());
    }

    private IEnumerator SettlingRoutine()
    {
        currentState = JengaGameState.Settling;
        selectedBlock = null; // Liberar referencia ya colocado
        Debug.Log("[JengaAR] Bloque colocado. Asentando físicas...");

        yield return new WaitForSeconds(settlingTime);

        // Si la rutina termina y la torre sigue de pie, pasar de turno
        if (currentState == JengaGameState.Settling)
        {
            NextTurn();
        }
    }

    private void NextTurn()
    {
        activePlayerIndex = (activePlayerIndex + 1) % numPlayers;
        currentState = JengaGameState.PlayerTurn;
        
        if (uiManager != null)
        {
            uiManager.UpdateTurnUI(activePlayerIndex, playerColors[activePlayerIndex % playerColors.Length]);
        }
        Debug.Log($"[JengaAR] Turno cambiado. Siguiente: Jugador {activePlayerIndex + 1}");
    }

    private IEnumerator EnableCollapseCheckingAfterDelay()
    {
        canCheckCollapse = false;
        yield return new WaitForSeconds(0.5f); // Esperar un instante inicial
        
        // Habilitar física para todos los bloques
        if (towerGenerator != null)
        {
            foreach (var block in towerGenerator.AllBlocks)
            {
                if (block != null)
                {
                    block.SetKinematic(false);
                }
            }
            Debug.Log("[JengaAR] Físicas activadas en todos los bloques (isKinematic = false).");
        }
        
        yield return new WaitForSeconds(1.5f); // Tiempo de gracia para asentamiento físico bajo gravedad
        canCheckCollapse = true;
        Debug.Log("[JengaAR] Monitoreo de colapso de la torre activado.");
    }

    private bool CheckIfTowerCollapsed()
    {
        if (!canCheckCollapse)
            return false;

        float collapseY = baseHeight - collapseThreshold;

        // Comprobar si algún bloque (que no sea el que se está manipulando) se ha caído del plano base
        foreach (var block in towerGenerator.AllBlocks)
        {
            if (block == null) continue;
            
            // Si el bloque está siendo arrastrado, ignorar su altura
            if (block == selectedBlock) continue;

            if (block.transform.position.y < collapseY)
            {
                Debug.LogWarning($"[JengaAR] Bloque {block.name} cayó por debajo de {collapseY}m (Su Y: {block.transform.position.y}m, Base Y: {baseHeight}m). ¡Torre derribada!");
                
                // Imprimir diagnóstico completo de todas las alturas para encontrar el problema exacto
                Debug.LogWarning($"[JengaAR] --- INICIO DIAGNÓSTICO FÍSICO ---");
                Debug.LogWarning($"[JengaAR] Parent Game Object: {gameObject.name} en pos {transform.position}");
                var bp = transform.Find("JengaBasePlate");
                if (bp != null)
                {
                    Debug.LogWarning($"[JengaAR] Base Plate: {bp.name} en pos {bp.position}, localScale: {bp.localScale}");
                }
                else
                {
                    Debug.LogWarning($"[JengaAR] Base Plate NO ENCONTRADA como hijo.");
                }

                foreach (var b in towerGenerator.AllBlocks)
                {
                    if (b != null)
                    {
                        Rigidbody rb = b.GetComponent<Rigidbody>();
                        Debug.Log($"[JengaAR] > {b.name}: LocalY={b.transform.localPosition.y:F4}, WorldY={b.transform.position.y:F4}, isKinematic={rb.isKinematic}, velocity={rb.linearVelocity}");
                    }
                }
                Debug.LogWarning($"[JengaAR] --- FIN DIAGNÓSTICO FÍSICO ---");
                
                return true;
            }
        }
        return false;
    }

    private void TriggerGameOver()
    {
        StopAllCoroutines();
        currentState = JengaGameState.GameOver;
        
        if (uiManager != null)
        {
            uiManager.ShowGameOverUI(activePlayerIndex, playerColors[activePlayerIndex % playerColors.Length]);
        }
        Debug.LogWarning($"[JengaAR] Fin del juego. El Jugador {activePlayerIndex + 1} derribó la torre.");
    }

    public void RestartGame()
    {
        Debug.Log("[JengaAR] Reiniciando juego...");
        
        // Encontrar PlaceObjectOnPlane para reiniciar el proceso de escaneo y posicionamiento
        PlaceObjectOnPlane placeScript = FindAnyObjectByType<PlaceObjectOnPlane>();
        if (placeScript != null)
        {
            placeScript.ResetSpawning();
        }
        else
        {
            // Si no se encuentra (caso de pruebas editor), simplemente limpiar y regenerar localmente
            towerGenerator.GenerateTower();
            StartGame();
        }
    }
}

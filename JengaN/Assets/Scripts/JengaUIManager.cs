using UnityEngine;
using UnityEngine.UI;

public class JengaUIManager : MonoBehaviour
{
    private GameObject uiCanvas;
    
    // UI Elements References
    private Text turnText;
    private Image turnBackground;
    private GameObject trackingWarningPanel;
    private GameObject actionPanel;
    private Button placeButton;
    private GameObject gameOverPanel;
    private Text gameOverText;

    private Font uiFont;

    void Awake()
    {
        // Intentar cargar fuente estándar de Unity
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        CreateUIElements();
    }

    private void CreateUIElements()
    {
        // 1. Crear Canvas Principal
        uiCanvas = new GameObject("JengaCanvas");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = uiCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        uiCanvas.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(uiCanvas);

        // 2. Crear Panel Superior (Turnos y Reiniciar)
        GameObject headerPanel = CreatePanel(uiCanvas.transform, "HeaderPanel", 
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), // Top anchor
            new Vector2(0, -150f), new Vector2(1080f, 250f),
            new Color(0.1f, 0.1f, 0.1f, 0.85f));

        // Fondo de acento del color del jugador actual
        GameObject accentObj = CreatePanel(headerPanel.transform, "AccentBar",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), // Bottom anchor of header
            new Vector2(0, 10f), new Vector2(1080f, 15f),
            new Color(0f, 0.8f, 0.8f, 1f));
        turnBackground = accentObj.GetComponent<Image>();

        // Texto de Turno
        GameObject turnTextObj = new GameObject("TurnText");
        turnTextObj.transform.SetParent(headerPanel.transform, false);
        turnText = turnTextObj.AddComponent<Text>();
        turnText.font = uiFont;
        turnText.fontSize = 52;
        turnText.fontStyle = FontStyle.Bold;
        turnText.alignment = TextAnchor.MiddleCenter;
        turnText.text = "TURNO: JUGADOR 1";
        turnText.color = Color.white;
        ConfigureRectTransform(turnTextObj.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f), new Vector2(0.7f, 0.5f),
            new Vector2(30f, 0f), new Vector2(700f, 180f));

        // Botón de Reiniciar (en el header)
        GameObject restartBtnObj = CreateButton(headerPanel.transform, "RestartButton",
            new Vector2(0.9f, 0.5f), new Vector2(0.9f, 0.5f),
            new Vector2(-30f, 0f), new Vector2(220f, 100f),
            "Reiniciar", 32, 
            new Color(0.25f, 0.25f, 0.25f, 1f), Color.white,
            () => { JengaGameManager.Instance.RestartGame(); });

        // 3. Panel de Acción Inferior (Botón colocar)
        actionPanel = CreatePanel(uiCanvas.transform, "ActionPanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), // Bottom anchor
            new Vector2(0, 250f), new Vector2(1080f, 250f),
            Color.clear); // Transparente para no estorbar la vista AR

        GameObject placeBtnObj = CreateButton(actionPanel.transform, "PlaceButton",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(550f, 140f),
            "Colocar en la Cima", 38,
            new Color(0.18f, 0.64f, 0.6f, 1f), Color.white,
            () => { JengaGameManager.Instance.PlaceBlockOnTop(); });
        placeButton = placeBtnObj.GetComponent<Button>();
        actionPanel.SetActive(false);

        // 4. Panel de Seguimiento Inestable (Bloqueo y advertencia)
        trackingWarningPanel = CreatePanel(uiCanvas.transform, "TrackingWarningPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), // Center anchor
            Vector2.zero, new Vector2(1080f, 1920f),
            new Color(0.05f, 0.05f, 0.05f, 0.75f));

        GameObject warningTextObj = new GameObject("WarningText");
        warningTextObj.transform.SetParent(trackingWarningPanel.transform, false);
        Text warningText = warningTextObj.AddComponent<Text>();
        warningText.font = uiFont;
        warningText.fontSize = 44;
        warningText.fontStyle = FontStyle.Bold;
        warningText.alignment = TextAnchor.MiddleCenter;
        warningText.text = "SEGUIMIENTO AR INESTABLE\n\nMueve el teléfono despacio para escanear el plano y buscar una superficie estable.";
        warningText.color = new Color(0.95f, 0.65f, 0.22f, 1f);
        ConfigureRectTransform(warningTextObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(800f, 400f));
        
        trackingWarningPanel.SetActive(false);

        // 5. Panel de Game Over
        gameOverPanel = CreatePanel(uiCanvas.transform, "GameOverPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1080f, 1920f),
            new Color(0.08f, 0.08f, 0.08f, 0.95f));

        GameObject gameOverTextObj = new GameObject("GameOverText");
        gameOverTextObj.transform.SetParent(gameOverPanel.transform, false);
        gameOverText = gameOverTextObj.AddComponent<Text>();
        gameOverText.font = uiFont;
        gameOverText.fontSize = 50;
        gameOverText.fontStyle = FontStyle.Bold;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.text = "¡JUEGO TERMINADO!\n\nEl Jugador 1 derribó la torre.";
        gameOverText.color = new Color(0.92f, 0.40f, 0.33f, 1f);
        ConfigureRectTransform(gameOverTextObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
            Vector2.zero, new Vector2(900f, 400f));

        GameObject replayBtnObj = CreateButton(gameOverPanel.transform, "ReplayButton",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
            Vector2.zero, new Vector2(500f, 130f),
            "Jugar de Nuevo", 38,
            new Color(0.92f, 0.40f, 0.33f, 1f), Color.white,
            () => { 
                gameOverPanel.SetActive(false);
                JengaGameManager.Instance.RestartGame(); 
            });

        gameOverPanel.SetActive(false);
    }

    // --- Helpers de Creación UI ---

    private GameObject CreatePanel(Transform parent, string name, Vector2 minAnchor, Vector2 maxAnchor, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        
        Image img = panelObj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = (color.a > 0.01f); // Bloquear toques si tiene fondo opaco

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        ConfigureRectTransform(rect, minAnchor, maxAnchor, anchoredPos, size);

        return panelObj;
    }

    private GameObject CreateButton(Transform parent, string name, Vector2 minAnchor, Vector2 maxAnchor, Vector2 anchoredPos, Vector2 size, string label, int fontSize, Color btnColor, Color txtColor, System.Action onClickAction)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClickAction?.Invoke());

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        ConfigureRectTransform(rect, minAnchor, maxAnchor, anchoredPos, size);

        // Añadir Texto
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = uiFont;
        txt.text = label;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = txtColor;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    private void ConfigureRectTransform(RectTransform rect, Vector2 minAnchor, Vector2 maxAnchor, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = minAnchor;
        rect.anchorMax = maxAnchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    // --- Métodos Públicos de Control de UI ---

    public void UpdateTurnUI(int playerIndex, Color color)
    {
        if (turnText != null)
        {
            turnText.text = $"TURNO: JUGADOR {playerIndex + 1}";
            turnText.color = color;
        }

        if (turnBackground != null)
        {
            turnBackground.color = color;
        }
    }

    public void SetTrackingWarningActive(bool active)
    {
        if (trackingWarningPanel != null)
        {
            trackingWarningPanel.SetActive(active);
        }
    }

    public void SetActionButtonActive(bool active)
    {
        if (actionPanel != null)
        {
            actionPanel.SetActive(active);
        }
    }

    public void ShowGameOverUI(int losingPlayerIndex, Color color)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = $"¡JUEGO TERMINADO!\n\nEl Jugador {losingPlayerIndex + 1} derribó la torre.";
            gameOverText.color = color;
        }
    }

    void OnDestroy()
    {
        // Limpiar el canvas al destruirse la partida
        if (uiCanvas != null)
        {
            Destroy(uiCanvas);
        }
    }
}

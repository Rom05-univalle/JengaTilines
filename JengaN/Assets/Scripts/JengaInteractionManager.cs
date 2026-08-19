using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class JengaInteractionManager : MonoBehaviour
{
    private JengaTowerGenerator towerGenerator;
    private Plane dragPlane;
    private Vector3 dragStartIntersection;
    private Vector3 dragStartBlockPosition;
    private bool isDragging = false;

    void Awake()
    {
        towerGenerator = GetComponent<JengaTowerGenerator>();
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // Bloquear entrada si no estamos con tracking estable en AR
        if (JengaGameManager.Instance == null || !JengaGameManager.Instance.IsTrackingStable)
            return;

        Vector2 inputPosition = Vector2.zero;
        bool touchBegan = false;
        bool touchMoved = false;
        bool touchEnded = false;

        // 1. Detección de Entrada (Editor / Dispositivo Móvil)
        #if UNITY_EDITOR
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            inputPosition = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                touchBegan = true;
            }
            else if (mouse.leftButton.isPressed)
            {
                touchMoved = true;
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                touchEnded = true;
            }
        }
        #else
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            inputPosition = touch.screenPosition;
            
            if (touch.phase == TouchPhase.Began) touchBegan = true;
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) touchMoved = true;
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) touchEnded = true;
        }
        #endif

        // 2. Procesamiento de Interacciones
        if (touchBegan)
        {
            HandleTouchBegan(inputPosition);
        }
        else if (touchMoved && isDragging)
        {
            HandleTouchMoved(inputPosition);
        }
        else if (touchEnded && isDragging)
        {
            HandleTouchEnded();
        }
    }

    private void HandleTouchBegan(Vector2 screenPos)
    {
        if (JengaGameManager.Instance.CurrentState != JengaGameState.PlayerTurn)
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        
        // Raycast físico para detectar el bloque de Jenga
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            JengaBlock block = hit.collider.GetComponent<JengaBlock>();
            if (block != null)
            {
                // Intentar seleccionar en el GameManager
                if (JengaGameManager.Instance.IsValidBlockSelection(block))
                {
                    JengaGameManager.Instance.SelectBlock(block);
                    
                    // Definir un plano horizontal a la altura actual del bloque para el arrastre
                    dragPlane = new Plane(Vector3.up, block.transform.position);
                    
                    if (dragPlane.Raycast(ray, out float enter))
                    {
                        dragStartIntersection = ray.GetPoint(enter);
                        dragStartBlockPosition = block.transform.position;
                        isDragging = true;
                    }
                }
            }
        }
    }

    private void HandleTouchMoved(Vector2 screenPos)
    {
        JengaBlock activeBlock = JengaGameManager.Instance.SelectedBlock;
        if (activeBlock == null || JengaGameManager.Instance.CurrentState != JengaGameState.PlayerTurn)
        {
            isDragging = false;
            return;
        }

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 currentHitPoint = ray.GetPoint(enter);
            Vector3 totalDelta = currentHitPoint - dragStartIntersection;

            // El bloque solo puede deslizarse en su eje de longitud local (transform.right)
            Vector3 slidingAxis = activeBlock.transform.right;
            float slideDistance = Vector3.Dot(totalDelta, slidingAxis);

            // Limitar la distancia máxima de arrastre
            float maxDrag = towerGenerator.BlockLength * 1.1f;
            slideDistance = Mathf.Clamp(slideDistance, -maxDrag, maxDrag);

            // Mover el rigidbody cinemático para mantener interacción física realista
            Rigidbody rb = activeBlock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.MovePosition(dragStartBlockPosition + slidingAxis * slideDistance);
            }

            // Verificar si el bloque ha sido extraído completamente (75% o más fuera de la torre)
            float pullThreshold = towerGenerator.BlockLength * 0.75f;
            if (Mathf.Abs(slideDistance) >= pullThreshold)
            {
                isDragging = false;
                
                // Indicar al GameManager que se extrajo con éxito
                JengaGameManager.Instance.SetBlockPulled();

                // Colocar el bloque flotando encima de la torre para servir como indicador
                towerGenerator.GetNextTopPositionAndRotation(out Vector3 topLocalPos, out Quaternion topLocalRot, out _, out _);
                
                // Convertir posición local de la cima a posición mundial
                Vector3 topWorldPos = towerGenerator.transform.TransformPoint(topLocalPos);
                
                // Elevar 5 cm sobre la cima
                activeBlock.transform.position = topWorldPos + Vector3.up * 0.05f;
                activeBlock.transform.rotation = towerGenerator.transform.rotation * topLocalRot;
            }
        }
    }

    private void HandleTouchEnded()
    {
        isDragging = false;
        
        // Si el dedo se levanta y el bloque no ha sido extraído totalmente, regresarlo a su lugar
        if (JengaGameManager.Instance.CurrentState == JengaGameState.PlayerTurn)
        {
            JengaGameManager.Instance.DeselectAndResetBlock();
        }
    }
}

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceObjectOnPlane : MonoBehaviour
{
    [SerializeField]
    private GameObject objectToPlace; // el prefab que vas a instanciar

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;
    private float timeWaitingForPlane = 0f;
    private const float PLANE_DETECTION_TIMEOUT = 5f;  // Timeout de 5 segundos

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
        Debug.Log("[JengaAR] Awake ejecutado. raycastManager es null? " + (raycastManager == null));
        Debug.Log("[JengaAR] planeManager es null? " + (planeManager == null));
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Debug.Log("[JengaAR] OnEnable ejecutado, EnhancedTouch habilitado.");
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // Incrementar timeout
        timeWaitingForPlane += Time.deltaTime;

        // Logs de diagnóstico (reducir frecuencia)
        if (Time.frameCount % 30 == 0)  // Solo cada 30 frames para no saturar
        {
            Debug.Log("[JengaAR] Planos detectados: " + (planeManager != null ? planeManager.trackables.count : 0));
            Debug.Log("[JengaAR] Tiempo esperando plano: " + timeWaitingForPlane.ToString("F1") + "s");
        }

        Vector2 screenPosition = Vector2.zero;
        bool touchDetected = false;

        #if UNITY_EDITOR
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = mouse.position.ReadValue();
            touchDetected = true;
            Debug.Log($"[JengaAR] Editor Click detectado en: {screenPosition}");
        }
        #endif

        if (!touchDetected)
        {
            if (Touch.activeTouches.Count == 0)
                return;

            var touch = Touch.activeTouches[0];
            if (touch.phase != TouchPhase.Began)
                return;

            screenPosition = touch.screenPosition;
            touchDetected = true;
            Debug.Log($"[JengaAR] Toque de pantalla detectado en: {screenPosition}");
        }

        bool hitSuccessful = false;
        Pose hitPose = default;

        // OPCIÓN 1: Raycast a planos detectados (menos restrictivo)
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            hitSuccessful = true;
            hitPose = hits[0].pose;
            Debug.Log($"[JengaAR] Raycast pegó en un plano. Hits: {hits.Count} en {hitPose.position}");
            timeWaitingForPlane = 0f;  // Reset timeout cuando detecta
        }
        // OPCIÓN 2: Si no detecta plano, permitir colocación directa después de 5 segundos
        else if (timeWaitingForPlane >= PLANE_DETECTION_TIMEOUT)
        {
            if (Camera.main != null)
            {
                hitSuccessful = true;
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.2f + Vector3.down * 0.3f;
                hitPose = new Pose(spawnPos, Quaternion.identity);
                Debug.Log($"[JengaAR] Timeout: Torre apareció sin detección de plano en {hitPose.position}");
            }
        }
        #if UNITY_EDITOR
        else
        {
            // Fallback para editor
            if (Camera.main != null)
            {
                hitSuccessful = true;
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.2f + Vector3.down * 0.3f;
                hitPose = new Pose(spawnPos, Quaternion.identity);
                Debug.Log($"[JengaAR] Editor: Torre forzada en {hitPose.position}");
            }
        }
#endif

        if (hitSuccessful)
        {
            if (spawnedObject == null)
            {
                Debug.Log("[JengaAR] Instanciando Jenga por primera vez.");
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                
                this.enabled = false;
                Debug.Log("[JengaAR] PlaceObjectOnPlane deshabilitado - Juego iniciado.");
            }
            else
            {
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
            }
        }
    }

    public void ResetSpawning()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
        }
        this.enabled = true;
        Debug.Log("[JengaAR] PlaceObjectOnPlane re-habilitado para nuevo posicionamiento.");
    }
}
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
        // --- Logs de diagnóstico ---
        Debug.Log("[JengaAR] ARSession state: " + ARSession.state);

        if (planeManager != null)
        {
            Debug.Log("[JengaAR] Planos detectados: " + planeManager.trackables.count);
        }
        else
        {
            Debug.Log("[JengaAR] planeManager es null, no se puede contar planos. ¿Está el componente AR Plane Manager en el XR Origin?");
        }

        Debug.Log("[JengaAR] Update corriendo. Touches activos: " + Touch.activeTouches.Count);
        // --- Fin logs de diagnóstico ---

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

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            hitSuccessful = true;
            hitPose = hits[0].pose;
            Debug.Log($"[JengaAR] Raycast pegó en un plano AR. Hits: {hits.Count} en {hitPose.position}");
        }
        #if UNITY_EDITOR
        else
        {
            // Fallback directo: Aparecer 1.5 metros al frente y medio metro abajo de la cámara
            if (Camera.main != null)
            {
                hitSuccessful = true;
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Vector3.down * 0.5f;
                hitPose = new Pose(spawnPos, Quaternion.identity);
                Debug.Log($"[JengaAR] Editor Fallback: Torre forzada en {hitPose.position}");
            }
        }
#endif

        if (hitSuccessful)
        {
            if (spawnedObject == null)
            {
                Debug.Log("[JengaAR] Instanciando objeto por primera vez.");
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                
                // Deshabilitar este script para no mover la torre durante el juego
                this.enabled = false;
                Debug.Log("[JengaAR] PlaceObjectOnPlane deshabilitado para iniciar el juego.");
            }
            else
            {
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
            }
        }
        else
        {
            Debug.Log("[JengaAR] Raycast NO pegó en ningún plano válido.");
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
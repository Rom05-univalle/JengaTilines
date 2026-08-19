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

        if (Touch.activeTouches.Count == 0)
            return;

        var touch = Touch.activeTouches[0];

        if (touch.phase != TouchPhase.Began)
            return;

        Debug.Log($"[JengaAR] Toque detectado en: {touch.screenPosition}");

        if (raycastManager.Raycast(touch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log($"[JengaAR] Raycast pegó en un plano. Hits: {hits.Count}");
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                Debug.Log("[JengaAR] Instanciando objeto por primera vez.");
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
            }
            else
            {
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
            }
        }
        else
        {
            Debug.Log("[JengaAR] Raycast NO pegó en ningún plano.");
        }
    }
}
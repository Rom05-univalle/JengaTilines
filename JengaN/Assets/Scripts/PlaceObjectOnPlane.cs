using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceObjectOnPlane : MonoBehaviour
{
    [SerializeField]
    private GameObject objectToPlace; // el prefab que vas a instanciar

    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Detectar toque en pantalla (funciona en celular; en editor con mouse hay que simular)
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        // Lanzar el rayo desde el punto tocado hacia los planos detectados
        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            // Tomamos el primer hit (el más cercano)
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                // Primera vez: instanciamos el objeto
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
            }
            else
            {
                // Ya existe: lo movemos a la nueva posición tocada
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
            }
        }
    }
}
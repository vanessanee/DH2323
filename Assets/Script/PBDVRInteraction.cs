using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to PBD_Sphere alongside an XRSimpleInteractable component.
/// Uses the same grab pattern as your FFD XRInputHandler.
/// When the user grabs and drags, force is applied to nodes near the grab point.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class PBDVRInteraction : MonoBehaviour
{
    [Header("References")]
    public CPUPBD pbdSphere;

    [Header("Interaction Settings")]
    public float interactionRadius = 0.5f;
    public float forceStrength = 5.0f;
    public float maxForceMagnitude = 3.0f;

    private XRSimpleInteractable interactable;
    private Transform controllerTransform;
    private Vector3 grabPoint;       // world-space point where user grabbed
    private bool isDragging = false;
    private Vector3 lastControllerPos;

    void Start()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
{
    isDragging = true;
    controllerTransform = args.interactorObject.transform;
    lastControllerPos = controllerTransform.position;
    
    // Apply a one-shot force immediately on grab to test
    pbdSphere.ApplyForceToAll(new Vector3(0, 5, 0));
}

    void OnRelease(SelectExitEventArgs args)
    {
        isDragging          = false;
        controllerTransform = null;
    }

    void Update()
    {
        if (!isDragging || controllerTransform == null) return;

        Vector3 velocity = (controllerTransform.position - lastControllerPos) / Time.deltaTime;
        lastControllerPos = controllerTransform.position;

        Vector3 clampedVel = Vector3.ClampMagnitude(velocity, maxForceMagnitude);
        if (clampedVel.magnitude > 0.001f)
            pbdSphere.ApplyForceToAll(clampedVel * forceStrength);
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
        }
    }
}
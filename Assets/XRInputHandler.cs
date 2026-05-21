using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRInputHandler : MonoBehaviour
{
    [SerializeField] private XRSimpleInteractable interactable;
    private Transform controllerTransform;
    private Vector3 offset;
    private bool isDragging = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //fetches info from simple interactable component
        interactable.selectEntered.AddListener(OnGrab); //when someone grabs call OnGrab
        interactable.selectExited.AddListener(OnRelease); //when someone releases call OnRelease
    }


    //handles grabbing
    void OnGrab(SelectEnterEventArgs args) {
        isDragging = true;
        controllerTransform = args.interactorObject.transform; //is the controller that grabbed it — we save its transform so we can follow it
        offset = transform.position - controllerTransform.position;  //calculated once at grab time so the object stays in the same relative position to your hand, not snapping to the controller tip
    }


    //handles whatever happens when grabbing stopps
    void OnRelease(SelectExitEventArgs args) {
        isDragging = false;
        controllerTransform = null;
    }

    // Update is called once per frame
    void Update()
    {
        //If we're dragging, moves the control point to follow the controller position plus the original offset
        if (isDragging && controllerTransform != null) {
            transform.position = controllerTransform.position + offset;
        }
    }

    //cleanup code: runs when the GameObject is destroyed or the scene ends
    void OnDestroy() {
        if (interactable != null) {
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
        }
    }
}

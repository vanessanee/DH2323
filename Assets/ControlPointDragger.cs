using UnityEngine;

public class ControlPointDragger : MonoBehaviour {
    Vector3 offset;
    Camera cam;

    void Start() { cam = Camera.main; }

    void OnMouseDown() {
        offset = transform.position - MouseWorldPos();
    }

    void OnMouseDrag() {
        transform.position = MouseWorldPos() + offset;
    }

    Vector3 MouseWorldPos() {
        Vector3 mp = Input.mousePosition;
        mp.z = cam.WorldToScreenPoint(transform.position).z;
        return cam.ScreenToWorldPoint(mp);
    }
}
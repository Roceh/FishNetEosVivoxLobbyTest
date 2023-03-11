using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Allows orbiting of target with main camera
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Tooltip("Target that the camera will orbit.")]
        [SerializeField]
        public Transform target;

        [Tooltip("Distance from target the camera will orbit.")]
        [SerializeField]
        public float distanceFromTarget = 10f;

        [Tooltip("Scaler for x camera rotation.")]
        [SerializeField]
        public float xSpeed = 250.0f;

        [Tooltip("Scaler for y camera rotation.")]
        [SerializeField]
        public float ySpeed = 120.0f;

        [Tooltip("Minimum limit for y rotation.")]
        [SerializeField]
        public float yMinLimit = -20f;

        [Tooltip("Maximum limit for y rotation.")]
        [SerializeField]
        public float yMaxLimit = 80f;

        // private variables
        private float _cameraY;
        private float _cameraX;
        private bool _mouseDown = false;

        private bool IsPointerOverUIElement()
        {
            return IsPointerOverUIElement(GetEventSystemRaycastResults());
        }

        private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            for (int index = 0; index < eventSystemRaysastResults.Count; index++)
            {
                RaycastResult curRaysastResult = eventSystemRaysastResults[index];
                if (curRaysastResult.gameObject.layer == uiLayer)
                    return true;
            }
            return false;
        }

        private List<RaycastResult> GetEventSystemRaycastResults()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> raysastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raysastResults);
            return raysastResults;
        }

        private void SetupCamera(float x, float y)
        {
            _cameraY += x * xSpeed * 0.02f;
            _cameraX -= y * ySpeed * 0.02f;
            _cameraX = Mathf.Clamp(_cameraX, yMinLimit, yMaxLimit);
            
            transform.rotation = Quaternion.Euler(_cameraX, _cameraY, 0);
            transform.position = transform.rotation * new Vector3(0.0f, 0.0f, -distanceFromTarget) + target.position;
        }

        private void Start()
        {
            _cameraX = transform.eulerAngles.x;
            _cameraY = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target)
            {
                bool mouseIsPressed = (Input.GetMouseButton(0) || Input.GetMouseButton(1)) && !IsPointerOverUIElement();

                if (!_mouseDown && mouseIsPressed)
                {
                    _mouseDown = true;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else if (_mouseDown && !mouseIsPressed)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    _mouseDown = false;
                }

                float mouseX = 0f, mouseY = 0f;

                if (_mouseDown)
                {
                    mouseX = Input.GetAxis("Mouse X");
                    mouseY = Input.GetAxis("Mouse Y");
                }

                SetupCamera(mouseX, mouseY);
            }
        }
    }
}
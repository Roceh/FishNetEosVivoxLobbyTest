using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Animates wheel for wheeled vehicle
    /// </summary>
    public class WheelAnimator : MonoBehaviour
    {
        [Tooltip("Offset of wheel mesh from hit origin")]
        [SerializeField]
        public Vector3 wheelOffset = new Vector3(0, 0.5f, 0);

        [Tooltip("Scaling to apply to forward motion when converting to wheel rotation")]
        [SerializeField]
        public float wheelSpeedScaling = 1.0f;

        [Tooltip("How fast wheel returns back when no hit")]
        [SerializeField]
        public float returnSpeed = 8.0f;

        [Tooltip("Drive element that gives wheen position")]
        [SerializeField]
        public DriveElement raycast;

        [Tooltip("Root transform for animating steering")]
        [SerializeField]
        public Transform steerRoot;

        // private variables
        private Vector3 _lastPos = Vector3.zero;

        private void FixedUpdate()
        {
            // Obtain velocity of the wheel
            Vector3 instantV = (transform.position - _lastPos) / Time.fixedDeltaTime;
            float zVel = raycast.transform.InverseTransformDirection(instantV).z;
            _lastPos = transform.position;

            // inherited y rotation from ray
            steerRoot.localEulerAngles = new Vector3(steerRoot.localEulerAngles.x, raycast.transform.localEulerAngles.y, steerRoot.localEulerAngles.z);

            // Rotate the wheel according to speed
            transform.Rotate(Vector3.right, ((zVel * wheelSpeedScaling * Mathf.Rad2Deg) / raycast.radius) * Time.fixedDeltaTime);

            // Set the wheel position        
            if (raycast.IsGrounded())
            {
                transform.localPosition = new Vector3(transform.localPosition.x, (raycast.transform.InverseTransformPoint(raycast.GetCollisionPoint()) + wheelOffset).y, transform.localPosition.z);
            }
            else
            {
                transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(transform.localPosition.y, (raycast.castTo + wheelOffset).y, returnSpeed * Time.fixedDeltaTime), transform.localPosition.z);
            }            
        }
    }
}
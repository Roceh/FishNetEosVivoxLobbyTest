using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Animates the wheels for the tracked vehicle and distorts tracked (via bones) based upon wheel position
    /// </summary>
    public class SuspensionAnimator : MonoBehaviour
    {
        [Tooltip("Offset for wheel compared to hit origin.")]
        [SerializeField]
        public Vector3 wheelOffset = new Vector3(0, 0.62f, 0);

        [Tooltip("Y Offset to apply to wheel for track thickness")]
        [SerializeField]
        public float trackThickness = 0.05f;

        [Tooltip("How fast the wheel returns when no hit")]
        [SerializeField]
        public float returnSpeed = 6.0f;

        [Tooltip("Root bone for track wheel is in")]
        [SerializeField]
        public Transform trackRootBone;

        [Tooltip("Bone related to the wheel")]
        [SerializeField]
        public Transform trackBone;

        [Tooltip("Drive element that dictates wheel position")]
        [SerializeField]
        public DriveElement raycast;

        private void FixedUpdate()
        {
            // set the wheel position
            if (raycast.IsGrounded())
            {
                transform.localPosition = new Vector3(transform.localPosition.x, (raycast.transform.InverseTransformPoint(raycast.GetCollisionPoint()) + wheelOffset).y, transform.localPosition.z);
            }
            else
            {
                transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(transform.localPosition.y, (raycast.castTo + wheelOffset).y, returnSpeed * Time.fixedDeltaTime), transform.localPosition.z);
            }

            // deform the track based on wheel position
            trackBone.position = transform.parent.TransformPoint(new Vector3(transform.localPosition.x, transform.localPosition.y + trackThickness, transform.localPosition.z));
        }

    }
}
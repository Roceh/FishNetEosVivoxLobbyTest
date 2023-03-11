using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Moves the wheels in the track based upon velocity
    /// </summary>
    public class TrackAnimator : MonoBehaviour
    {
        [Tooltip("List of wheel transforms in the track (used to rotate)")]
        [SerializeField]
        public Transform[] roadWheelTransforms;

        [Tooltip("Sprocket transform (used to rotate)")]
        [SerializeField]
        public Transform sprocketTransform;

        [Tooltip("Idler transform (used to rotate)")]
        [SerializeField]
        public Transform idlerTransform;

        [Tooltip("Ammount to scale forward motion by for wheel rotation")]
        [SerializeField]
        public float wheelSpeedScaling = 1.0f;

        [Tooltip("Ammount to scale forward motion by for sprocket rotation")]
        [SerializeField]
        public float sprocketSpeedScaling = 1.6f;

        [Tooltip("Ammount to scale forward motion by for idler rotation")]
        [SerializeField]
        public float idlerSpeedScaling = 1.5f;

        [Tooltip("How fast to scale the UV texture adjustment")]
        [SerializeField]
        public float trackUVScaling = 1.0f;

        // private variables
        private Material _trackMat;
        private Vector3 _lastPos = Vector3.zero;
        private Vector3 _instantV = Vector3.zero;
        private Rigidbody _rb;

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();
            _trackMat = GetComponent<SkinnedMeshRenderer>().material;
        }

        private void FixedUpdate()
        {
            // obtain velocity of the track
            _instantV = (transform.position - _lastPos) / Time.fixedDeltaTime;
            float ZVel = _rb.transform.InverseTransformDirection(_instantV).z;
            _lastPos = transform.position;

            // animate wheels
            foreach (Transform wheel in roadWheelTransforms)
            {
                wheel.Rotate(Vector3.right, ZVel * wheelSpeedScaling * Mathf.Rad2Deg * Time.fixedDeltaTime);
            }

            // animate drive sprocket and idler
            sprocketTransform.Rotate(Vector3.right, ZVel * Mathf.Rad2Deg * sprocketSpeedScaling * Time.fixedDeltaTime);
            idlerTransform.Rotate(Vector3.right, ZVel * Mathf.Rad2Deg * idlerSpeedScaling * Time.fixedDeltaTime);

            // animate track texture
            Vector2 uvOffset = _trackMat.GetTextureOffset("_MainTex");
            uvOffset.y -= (ZVel * trackUVScaling) * Time.fixedDeltaTime;
            _trackMat.SetTextureOffset("_MainTex", uvOffset);

            // clamp UV offset of tracks
            if (uvOffset.y > 1.0f || uvOffset.y < -1.0f)
            {
                uvOffset.y = 0.0f;
                _trackMat.SetTextureOffset("_MainTex", uvOffset);
            }
        }
    }
}
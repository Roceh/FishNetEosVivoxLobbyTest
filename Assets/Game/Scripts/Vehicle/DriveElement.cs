using FishNet.Serializing;
using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Suspension and wheel script
    /// </summary>
    public class DriveElement : MonoBehaviour
    {
        [Tooltip("Ground layers.")]
        [SerializeField]
        public LayerMask mask;

        [Tooltip("Local vector direction for suspension.")]
        [SerializeField]
        public Vector3 castTo = new Vector3(0, -1f, 0);

        [Tooltip("Maximum ammount force to push up.")]
        [SerializeField]
        public float springMaxForce = 300.0f;

        [Tooltip("Ammount force to push up taking into account damping and stiffness.")]
        [SerializeField]
        public float springForce = 180.0f;

        [Tooltip("How stiff the suspension is.")]
        [SerializeField]
        public float stifness = 0.85f;

        [Tooltip("How damped the suspension is.")]
        [SerializeField]
        public float damping = 0.05f;

        [Tooltip("How much ground traction there is.")]
        [SerializeField]
        public float Xtraction = 1.0f;

        [Tooltip("For slow speeds below this counteract the suspension sliding effect.")]
        [SerializeField]
        public float staticSlideThreshold = 0.005f;

        [Tooltip("Scales the ground forces.")]
        [SerializeField]
        public float massKG = 100.0f;

        [Tooltip("Radius of sphere cast")]
        [SerializeField]
        public float radius = 1.0f;

        [Tooltip("How much force applied to rigidbodies under the wheel")]
        [SerializeField]
        public float forceUnderWheelScaler = 0.1f;

        [Tooltip("Minimum positional change on suspension hit before resync for observers.")]
        [Range(0.01f, 0.5f)]
        public float minHitPositionalChange = 0.01f;

        // private variables
        private Rigidbody _rb;

        // these are updated every simulate
        private Vector3 _collisionPoint = Vector3.zero;
        private bool _grounded = false;
        private float _ztraction = 0.15f;

        // these persist after each simulate
        public Vector3 _previousHitPosition;
        public float _previousHitDistance;

        public struct ShapeCastResult
        {
            public float hitDistance;
            public Vector3 hitPosition;
            public Vector3 hitNormal;
            public Vector3 hitVelocity;
            public GameObject hitObject;
        }

        public bool InSync(Reader reader)
        {
            var oldHitPosition = reader.Read<Vector3>();
            reader.ReadSingle();

            return (oldHitPosition - _previousHitPosition).magnitude < minHitPositionalChange;
        }

        public void GetState(Writer writer)
        {
            writer.Write(_previousHitPosition);
            writer.Write(_previousHitDistance);
        }

        public void SetState(Reader reader)
        {
            _previousHitPosition = reader.Read<Vector3>();
            _previousHitDistance = reader.ReadSingle();
        }

        // function to do sphere casting
        private ShapeCastResult ShapeCast(Vector3 origin, Vector3 offset)
        {
            ShapeCastResult result = new ShapeCastResult();

            RaycastHit hit;

            if (Physics.SphereCast(origin, radius, offset.normalized, out hit, offset.magnitude, mask))
            {
                result.hitDistance = hit.distance;
                result.hitPosition = origin + offset.normalized * result.hitDistance; 
                result.hitNormal = hit.normal;
                result.hitObject = hit.rigidbody?.gameObject;

                if (hit.rigidbody != null)
                {
                    result.hitVelocity = hit.rigidbody.GetPointVelocity(hit.point);
                }
                else
                {
                    result.hitVelocity = Vector3.zero;
                }
            }
            else
            {
                result.hitDistance = Mathf.Infinity;
                result.hitPosition = origin + offset.normalized * radius;
                result.hitNormal = Vector3.up;
                result.hitVelocity = Vector3.zero;
                result.hitObject = null;
            }

            return result;
        }

        public Vector3 GetCollisionPoint()
        {
            return _collisionPoint;
        }

        public bool IsGrounded()
        {
            return _grounded;
        }

        public void ApplyBrake(float amount = 0.0f)
        {
            _ztraction = Mathf.Max(0.0f, amount);
        }

        public void ApplyForce(Vector3 force)
        {
            if (IsGrounded())
            {
                _rb.AddForceAtPosition(force, GetCollisionPoint());

                // debug drive power
                Debug.DrawRay(GetCollisionPoint(), force / 1000f, new Color(1, 1, 0));
            }
        }

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();

            _grounded = false;
            _previousHitPosition = transform.position + castTo;
            _previousHitDistance = Mathf.Abs(castTo.y);
        }

        public void Simulate(float delta)
        {
            // perform sphere cast
            ShapeCastResult castResult = ShapeCast(transform.position, castTo.y * transform.up);
            _collisionPoint = castResult.hitPosition;

            Debug.DrawLine(transform.position, transform.position + castTo.y * transform.up, new Color(1, 0, 1));

            if (castResult.hitDistance != Mathf.Infinity)
            {
                // if grounded, handle forces
                _grounded = true;

                //Debug.DrawLine(transform.position, castResult.hit_position, new Color(1, 0, 0));

                // obtain instantaneaous linear velocity
                Vector3 instantLinearVelocity = (_collisionPoint - _previousHitPosition) / delta;

                // apply spring force with damping force
                float FSpring = stifness * (Mathf.Abs(castTo.y) - castResult.hitDistance);
                float FDamp = damping * (_previousHitDistance - castResult.hitDistance) / delta;
                float suspensionForce = Mathf.Clamp((FSpring + FDamp) * springForce, 0, springMaxForce);
                Vector3 suspensionForceVec = castResult.hitNormal * suspensionForce;

                // obtain axis velocity
                Vector3 localVelocity = transform.InverseTransformDirection(instantLinearVelocity - castResult.hitVelocity);

                // axis deceleration forces based on this drive elements mass and current acceleration
                float XAccel = (-localVelocity.x * Xtraction) / delta;
                float ZAccel = (-localVelocity.z * _ztraction) / delta;
                Vector3 XForce = transform.right * XAccel * massKG;
                Vector3 ZForce = transform.forward * ZAccel * massKG;

                // counter sliding by negating off axis suspension impulse at very low speed
                float vLimit = instantLinearVelocity.sqrMagnitude * delta;
                if (vLimit < staticSlideThreshold)
                {
                    XForce.x -= suspensionForceVec.x * Vector3.Dot(_rb.transform.up, Vector3.up);
                    ZForce.z -= suspensionForceVec.z * Vector3.Dot(_rb.transform.up, Vector3.up);
                }

                // final impulse force vector to be applied
                Vector3 finalForce = suspensionForceVec + XForce + ZForce;

                Debug.DrawRay(GetCollisionPoint(), suspensionForceVec / 1000f, new Color(0, 1, 0));
                Debug.DrawRay(GetCollisionPoint(), XForce / 1000f, new Color(1, 0, 0));
                Debug.DrawRay(GetCollisionPoint(), ZForce / 1000f, new Color(0, 0, 1));

                // apply forces relative to parent body
                _rb.AddForceAtPosition(finalForce, GetCollisionPoint());

                if (castResult.hitObject != null && castResult.hitObject.TryGetComponent<Rigidbody>(out Rigidbody hitRb))
                {
                    // move the thing the wheel has hit                    
                    hitRb.AddForceAtPosition(-finalForce * forceUnderWheelScaler, GetCollisionPoint());
                }

                _previousHitPosition = castResult.hitPosition;
                _previousHitDistance = castResult.hitDistance;
            }
            else
            {
                _grounded = false;
                _previousHitPosition = transform.position + castTo;
                _previousHitDistance = Mathf.Abs(castTo.y);
            }
        }
    }
}
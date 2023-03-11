﻿using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Alternative predictedobject for non player controlled rigidbodies - still finding this a bit smoother than default
    /// </summary>
    public partial class PredictedObjectCache : NetworkBehaviour
    {
        /// <summary>
        /// Transform which holds the graphical features of this object. This transform will be smoothed when desynchronizations occur.
        /// </summary>
        [Tooltip("Transform which holds the graphical features of this object. This transform will be smoothed when desynchronizations occur.")]
        [SerializeField]
        private Transform _graphicalObject;

        /// <summary>
        /// Rigidbody to predict.
        /// </summary>
        [Tooltip("Rigidbody to predict.")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [Tooltip("Duration to smooth desynchronizations over.")]
        [Range(0.01f, 0.5f)]
        [SerializeField]
        public float smoothingDuration = 0.05f;

        [Tooltip("Minimum positional change before resync for observers.")]
        [Range(0.01f, 0.5f)]
        [SerializeField]
        public float minPositionalChange = 0.05f;

        [Tooltip("Minimum rotational change before resync for observers.")]
        [Range(0.01f, 1f)]
        [SerializeField]
        public float minRotationalChange = 1f;

        /// <summary>
        /// True if subscribed to events.
        /// </summary>
        private bool _subscribed;

        /// <summary>
        /// World position before transform was predicted or reset.
        /// </summary>
        private Vector3 _previousPosition;

        /// <summary>
        /// World rotation before transform was predicted or reset.
        /// </summary>
        private Quaternion _previousRotation;

        /// <summary>
        /// Local position of transform when instantiated.
        /// </summary>
        private Vector3 _instantiatedLocalPosition;

        /// <summary>
        /// Local rotation of transform when instantiated.
        /// </summary>
        private Quaternion _instantiatedLocalRotation;

        /// <summary>
        /// Last sent state
        /// </summary>
        private RigidbodyState? _cachedRigidbodyState;

        /// <summary>
        /// Velocity for smoothing of position
        /// </summary>
        private Vector3 _smoothingPositionVelocity;

        /// <summary>
        /// Velocity for smoothing of rotation
        /// </summary>
        private float _smoothingRotationVelocity;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            base.TimeManager.OnPostTick += TimeManager_OnPostTick;
            _instantiatedLocalPosition = _graphicalObject.localPosition;
            _instantiatedLocalRotation = _graphicalObject.localRotation;
            _cachedRigidbodyState = null;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ChangeSubscriptions(true);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            ChangeSubscriptions(false);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            if (base.TimeManager != null)
            {
                base.TimeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }

        protected void TimeManager_OnPostTick()
        {
            if (base.IsClient)
            {
                ResetToTransformPreviousProperties();
            }

            if (base.IsServer && GlobalReconcilation.Instance.TicksBetweenReconcilation != 0 &&
                (base.TimeManager.LocalTick % GlobalReconcilation.Instance.TicksBetweenReconcilation) == 0)
            {
                SendRigidbodyState();
            }
        }

        /// <summary>
        /// Called before performing a reconcile on NetworkBehaviour.
        /// </summary>
        protected virtual void TimeManager_OnPreReconcile(NetworkBehaviour obj)
        {
            SetPreviousTransformProperties();

            if (_cachedRigidbodyState.HasValue)
            {
                _rigidbody.transform.position = _cachedRigidbodyState.Value.Position;
                _rigidbody.transform.rotation = _cachedRigidbodyState.Value.Rotation;
                _rigidbody.velocity = _cachedRigidbodyState.Value.Velocity;
                _rigidbody.angularVelocity = _cachedRigidbodyState.Value.AngularVelocity;
            }
        }

        private void TimeManager_OnPostReconcile(NetworkBehaviour obj)
        {
            ResetToTransformPreviousProperties();
        }

        private void Awake()
        {
            //Set in awake so they arent default.
            SetPreviousTransformProperties();

            if (Application.isPlaying)
                InitializeOnce();
        }

        private void Update()
        {
            MoveToTarget();
        }

        /// <summary>
        /// Sends current states of this object to client. This is always run on the server only
        /// </summary>
        private void SendRigidbodyState()
        {
            RigidbodyState state = new RigidbodyState
            {
                Position = _rigidbody.transform.position,
                Rotation = _rigidbody.transform.rotation,
                Velocity = _rigidbody.velocity,
                AngularVelocity = _rigidbody.angularVelocity
            };

            // Have we moved enough for resync ?
            if (!_cachedRigidbodyState.HasValue || (_cachedRigidbodyState.Value.Position - state.Position).magnitude > minPositionalChange || Quaternion.Angle(_cachedRigidbodyState.Value.Rotation, state.Rotation) > minRotationalChange)
            {
                // store the last state we sent
                _cachedRigidbodyState = state;

                // send state to the clients
                ObserversSendRigidbodyState(state);
            }
        }

        /// <summary>
        /// Sends transform and rigidbody state to spectators.
        /// </summary>
        /// <param name="state"></param>
        [ObserversRpc(ExcludeOwner = true, BufferLast = true)]
        private void ObserversSendRigidbodyState(RigidbodyState state, Channel channel = Channel.Unreliable)
        {
            if (!base.IsOwner && !base.IsServer)
            {
                // we just place in cache as this data is already old regards the client tick
                // so when the client reconcilates we will use this cache to fix up rb position

                // store state in cache slot
                _cachedRigidbodyState = state;
            }
        }

        private void TimeManager_OnPreTick()
        {
            SetPreviousTransformProperties();
        }

        /// <summary>
        /// Subscribes to events needed to function.
        /// </summary>
        /// <param name="subscribe"></param>
        private void ChangeSubscriptions(bool subscribe)
        {
            if (base.TimeManager == null)
                return;
            if (subscribe == _subscribed)
                return;

            if (subscribe)
            {
                base.TimeManager.OnPreTick += TimeManager_OnPreTick;
                base.PredictionManager.OnPreReconcile += TimeManager_OnPreReconcile;
                base.PredictionManager.OnPostReconcile += TimeManager_OnPostReconcile;
            }
            else
            {
                base.TimeManager.OnPreTick -= TimeManager_OnPreTick;
                base.PredictionManager.OnPreReconcile -= TimeManager_OnPreReconcile;
                base.PredictionManager.OnPostReconcile -= TimeManager_OnPostReconcile;
            }

            _subscribed = subscribe;
        }

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        private void InitializeOnce()
        {
            //No graphical object, cannot smooth.
            if (_graphicalObject == null)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Error))
                    Debug.LogError($"GraphicalObject is not set on {gameObject.name}. Initialization will fail.");
                return;
            }
        }

        private static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref float AngularVelocity, float smoothTime)
        {
            var delta = Quaternion.Angle(current, target);
            if (delta > 0.0f)
            {
                var t = Mathf.SmoothDampAngle(delta, 0.0f, ref AngularVelocity, smoothTime);
                t = 1.0f - t / delta;
                return Quaternion.Slerp(current, target, t);
            }

            return current;
        }

        /// <summary>
        /// Moves transform to target values.
        /// </summary>
        private void MoveToTarget()
        {
            Transform t = _graphicalObject.transform;
            t.localPosition = Vector3.SmoothDamp(t.localPosition, _instantiatedLocalPosition, ref _smoothingPositionVelocity, smoothingDuration);
            t.localRotation = SmoothDampQuaternion(t.localRotation, _instantiatedLocalRotation, ref _smoothingRotationVelocity, smoothingDuration);
        }

        /// <summary>
        /// Caches the transforms current position and rotation.
        /// </summary>
        private void SetPreviousTransformProperties()
        {
            _previousPosition = _graphicalObject.position;
            _previousRotation = _graphicalObject.rotation;
        }

        /// <summary>
        /// Resets the transform to cached position and rotation of the transform.
        /// </summary>
        private void ResetToTransformPreviousProperties()
        {
            _graphicalObject.position = _previousPosition;
            _graphicalObject.rotation = _previousRotation;
        }

        public struct RigidbodyState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
        }
    }
}
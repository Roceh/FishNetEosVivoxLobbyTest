using FishNet;
using FishNet.Connection;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Serializing;
using FishNet.Transporting;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Predication controller base class for both vehicles
    /// </summary>
    public abstract class PredictedVehicle : NetworkBehaviour
    {
        /// <summary>
        /// Root transform for visual car elements
        /// </summary>
        [Tooltip("Root transform holding all car visual game objects")]
        [SerializeField]
        public Transform vehicleVisualRootObject;

        [Tooltip("Duration to smooth desynchronizations over.")]
        [Range(0.01f, 0.5f)]
        [SerializeField]
        public float smoothingDuration = 0.05f;

        /// <summary>
        /// True if we have subcribed to time manager tick events
        /// </summary>
        private bool _subscribed = false;

        /// <summary>
        /// World position of visual object before transform was predicted or reset.
        /// </summary>
        private Vector3 _previousPosition;

        /// <summary>
        /// World rotation of visual object before transform was predicted or reset.
        /// </summary>
        private Quaternion _previousRotation;

        /// <summary>
        /// Local position of visual object of transform when instantiated.
        /// </summary>
        private Vector3 _instantiatedLocalPosition;

        /// <summary>
        /// Local rotation of visual object transform when instantiated.
        /// </summary>
        private Quaternion _instantiatedLocalRotation;

        /// <summary>
        /// Holds the last received state if this is an non player controlled vehicle
        /// </summary>
        private CachedStateInfo _cachedStateInfo = new CachedStateInfo();

        /// <summary>
        /// Used to cache last move on server and during replaying
        /// </summary>
        private byte[] _lastMove;

        /// <summary>
        /// Seems to be last bandwidth if we cache the array instead of creating a new one - not sure why..
        /// </summary>
        private byte[] _currentMove;

        /// <summary>
        /// Velocity for smoothing of position
        /// </summary>
        private Vector3 _smoothingPositionVelocity = Vector3.zero;

        /// <summary>
        /// Velocity for smoothing of rotation
        /// </summary>
        private float _smoothingRotationVelocity;

        public abstract ArraySegment<byte> GetMove();

        public abstract void SetMove(ArraySegment<byte> move);

        public abstract ArraySegment<byte> GetState();

        public abstract bool InSync(ArraySegment<byte> previous);

        public abstract void SetState(ArraySegment<byte> state);

        public abstract void Simulate(float tickDelta);

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _instantiatedLocalPosition = vehicleVisualRootObject.localPosition;
            _instantiatedLocalRotation = vehicleVisualRootObject.localRotation;
        }

        private void ChangeSubscriptions(bool subscribe)
        {
            if (base.TimeManager == null)
                return;
            if (subscribe == _subscribed)
                return;

            _subscribed = subscribe;

            if (subscribe)
            {
                base.TimeManager.OnTick += TimeManager_OnTick;
                base.TimeManager.OnPreTick += TimeManager_OnPreTick;
                base.TimeManager.OnPostTick += TimeManager_OnPostTick;

                base.PredictionManager.OnPreReconcile += TimeManager_OnPreReconcile;
                base.PredictionManager.OnPostReconcile += TimeManager_OnPostReconcile;
                base.PredictionManager.OnPreReplicateReplay += TimeManager_OnPreReplicateReplay;
            }
            else
            {
                base.TimeManager.OnTick -= TimeManager_OnTick;
                base.TimeManager.OnPreTick -= TimeManager_OnPreTick;
                base.TimeManager.OnPostTick -= TimeManager_OnPostTick;
                base.PredictionManager.OnPreReconcile -= TimeManager_OnPreReconcile;
                base.PredictionManager.OnPostReconcile -= TimeManager_OnPostReconcile;
                base.PredictionManager.OnPreReplicateReplay -= TimeManager_OnPreReplicateReplay;
            }
        }

        private void Start()
        {
            // we setup the tick subscription here otherwise TimeManger.Tick could fire before the simulation scripts have initialised
            ChangeSubscriptions(true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (base.IsOwner)
            {
                // client is controlling this - so setup camera
                var cfl = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<TrackCamera>();
                cfl.target = vehicleVisualRootObject;
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            ChangeSubscriptions(false);
        }

        private void Awake()
        {
            SetPreviousTransformProperties();
        }

        private void OnDestroy()
        {
            ChangeSubscriptions(false);
        }

        private void SimulateWithMove(ArraySegment<byte> moveData)
        {
            SetMove(moveData);
            Simulate((float)InstanceFinder.TimeManager.TickDelta);
        }


        [Replicate]
        private void Move(MoveData md, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
        {
            // this is so we can tell the other clients what our last move was
            if (base.IsServer && md.Move != null)
                _lastMove = md.Move;

            SimulateWithMove(md.Move);
        }

        [Reconcile]
        private void Reconciliation(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {
            SetState(rd.State);
        }

        public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref float AngularVelocity, float smoothTime)
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
            Transform t = vehicleVisualRootObject.transform;
            t.localPosition = Vector3.SmoothDamp(t.localPosition, _instantiatedLocalPosition, ref _smoothingPositionVelocity, smoothingDuration);
            t.localRotation = SmoothDampQuaternion(t.localRotation, _instantiatedLocalRotation, ref _smoothingRotationVelocity, smoothingDuration);
        }

        private void TimeManager_OnPreReplicateReplay(uint tick, PhysicsScene arg1, PhysicsScene2D arg2)
        {
            if (!base.IsOwner && !base.IsServer)
            {
                // for non server and non owner - we want to setup the forces before fishnet calls physics simulate
                SimulateWithMove(_lastMove);
            }
        }

        private void TimeManager_OnPreReconcile(NetworkBehaviour obj)
        {
            // this is so we can restore the visual state to what it was and lerp to new visual position/rotation after reconcile is done
            SetPreviousTransformProperties();

            // if this is a non player vehicle we want to simulate it using last received data (if available)
            if (!base.IsOwner && !base.IsServer)
            {
                // have we received info about the state of this vehicle on the server ?
                // if we haven't we just simulate from this point 
                if (_cachedStateInfo.State != null)
                {
                    SetState(_cachedStateInfo.State);
                    _lastMove = _cachedStateInfo.Move;
                }
            }
        }

        private void TimeManager_OnPostReconcile(NetworkBehaviour obj)
        {
            // Set transform back to where it was before reconcile so there's no visual disturbances.
            vehicleVisualRootObject.SetPositionAndRotation(_previousPosition, _previousRotation);
        }

        private void TimeManager_OnPreTick()
        {
            // used to smooth out physics simulation
            SetPreviousTransformProperties();
        }

        private void TimeManager_OnTick()
        {
            if (base.IsOwner)
            {
                Reconciliation(default, false);

                MoveData md = new MoveData(_currentMove);

                var moveArraySegment = GetMove();

                if (moveArraySegment != null && (_currentMove == null || _currentMove.Length != moveArraySegment.Count))
                {
                    _currentMove = new byte[moveArraySegment.Count];
                }

                if (moveArraySegment != null)
                {
                    moveArraySegment.CopyTo(_currentMove);
                }

                Move(md, false);
            }

            if (base.IsServer)
            {
                Move(default, true);
            }

            if (!base.IsOwner && !base.IsServer)
            {
                SimulateWithMove(_lastMove);
            }
        }

        private void TimeManager_OnPostTick()
        {
            if (base.IsServer)
            {
                uint localTick = base.TimeManager.LocalTick;

                // we reconcilate at reduce tick step for bandwidth saving!
                if ((localTick % GlobalReconcilation.Instance.TicksBetweenReconcilation) == 0)
                {
                    // get state as an array segment
                    var state = GetState();

                    // tell other clients the current state 
                    SendVehicleToObservers(state);

                    // tell owner the current servers simulation state
                    var rd = new ReconcileData { State = state };
                    Reconciliation(rd, true);
                }
            }

            // reset visual object to what it was before physics step
            ResetToTransformPreviousProperties();
        }

        private void SetPreviousTransformProperties()
        {
            _previousPosition = vehicleVisualRootObject.position;
            _previousRotation = vehicleVisualRootObject.rotation;
        }

        private void ResetToTransformPreviousProperties()
        {
            vehicleVisualRootObject.position = _previousPosition;
            vehicleVisualRootObject.rotation = _previousRotation;
        }

        private void Update()
        {
            MoveToTarget();
        }

        private void SendVehicleToObservers(ArraySegment<byte> state)
        {
            // send the move and state data to the other clients
            // we ask the vehicle controller to determine whether we need to update the observers here
            // its down to the controller to determine what that means
            if (_cachedStateInfo.State == null || !InSync(_cachedStateInfo.State))
            {
                ObserversSendVehicleState(_lastMove, state);

                // also sent state on server (using same variable)
                _cachedStateInfo.Update(_lastMove, state);
            }
        }

        [ObserversRpc(ExcludeOwner = true, BufferLast = true)]
        private void ObserversSendVehicleState(ArraySegment<byte> move, ArraySegment<byte> state, Channel channel = Channel.Unreliable)
        {
            // ignore if we are controlling this vehicle (owner and server)
            if (!base.IsServer && !base.IsOwner)
            {
                // we just place in cache as this data is already old regards the client tick
                // so when the client reconcilates we will use this cache to fix up this vehicle position

                // store state in cache slot
                _cachedStateInfo.Update(move, state);
            }
        }

        public class CachedStateInfo
        {
            public byte[] Move { get; private set; }
            public byte[] State { get; private set; }

            public void Update(ArraySegment<byte> move, ArraySegment<byte> state)
            {
                if (move.Count > 0)
                {
                    if (Move == null || Move.Length != move.Count)
                    {
                        Move = new byte[move.Count];
                    }
                    move.CopyTo(Move);
                }

                if (State == null || State.Length != state.Count)
                {
                    State = new byte[state.Count];
                }
                state.CopyTo(State);
            }
        }

        public struct MoveData : IReplicateData
        {
            public byte[] Move;

            public MoveData(byte[] move)
            {
                Move = move; 
                _tick = 0;
            }

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        public struct ReconcileData : IReconcileData
        {
            public ArraySegment<byte> State;

            public ReconcileData(ArraySegment<byte> state)
            {
                State = state;
                _tick = 0;
            }

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }
    }
}
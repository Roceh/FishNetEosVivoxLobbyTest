using FishNet.Serializing;
using System;
using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Predication controller for wheeled vehicle
    /// </summary>
    [RequireComponent(typeof(WheelRayVehicle))]
    public class PredictedWheelRayVehicle : PredictedVehicle
    {        
        private WheelRayVehicle _vc;
        private Writer _stateWriter = new Writer();
        private Writer _moveWriter = new Writer();

        [Tooltip("Minimum positional change before resync for observers.")]
        [Range(0.01f, 0.5f)]
        public float minPositionalChange = 0.05f;

        [Tooltip("Minimum rotational change before resync for observers.")]
        [Range(0.01f, 1f)]
        public float minRotationalChange = 1f;

        public struct RigidbodyState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
        }

        private void Awake()
        {
            _vc = GetComponent<WheelRayVehicle>();
        }

        public override ArraySegment<byte> GetMove()
        {
            _moveWriter.Reset(this.NetworkManager);
            _vc.GetMove(_moveWriter);
            return _moveWriter.GetArraySegment();
        }

        public override void SetMove(ArraySegment<byte> move)
        {
            if (move.Count > 0)
            {
                var reader = new Reader(move, this.NetworkManager);
                _vc.SetMove(reader);
            }
        }

        public override bool InSync(ArraySegment<byte> previous)
        {
            var reader = new Reader(previous, this.NetworkManager);
            return _vc.InSync(reader);
        }

        public override ArraySegment<byte> GetState()
        {
            _stateWriter.Reset(this.NetworkManager);
            _vc.GetState(_stateWriter);
            return _stateWriter.GetArraySegment();
        }

        public override void SetState(ArraySegment<byte> state)
        {
            var reader = new Reader(state, this.NetworkManager);
            _vc.SetState(reader);
        }

        public override void Simulate(float tickDelta)
        {
            _vc.Simulate(tickDelta);
        }
    }
}

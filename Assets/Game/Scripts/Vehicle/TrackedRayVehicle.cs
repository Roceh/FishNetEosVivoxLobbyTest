using FishNet.Serializing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Controller for tank like vehicles
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrackedRayVehicle : MonoBehaviour
    {
        [Tooltip("If true will invert steering when reversing")]
        [SerializeField]
        public bool invertSteerWhenReverse = false;

        [Tooltip("How much max torque engine has")]
        [SerializeField]
        public float enginePower = 150f;

        [Tooltip("Curve that adjust torque based upon current forward speed")]
        [SerializeField]
        public AnimationCurve torqueCurve;

        [Tooltip("Maximum forward speed that can be achieved")]
        [SerializeField]
        public float maxSpeedKph = 65f;

        [Tooltip("Maximum reverse speed that can be achieved")]
        [SerializeField]
        public float maxReverseSpeedKph = 20f;

        [Tooltip("Percentage adjust inverse input direction to avoid sudden stops")]
        [SerializeField]
        public float trackBrakePercent = 0.1f;

        [Tooltip("Friction when no power being given")]
        [SerializeField]
        public float rollingResistance = 0.02f;

        [Tooltip("Minimum speed to fully stop")]
        [SerializeField]
        public float autoStopSpeedMS = 1.0f;

        [Tooltip("Center of mass of vehicle")]
        [SerializeField]
        public Transform centerOfMass;

        [Tooltip("Minimum positional change before resync for observers.")]
        [Range(0.01f, 0.5f)]
        public float minPositionalChange = 0.05f;

        [Tooltip("Minimum rotational change before resync for observers.")]
        [Range(0.01f, 1f)]
        public float minRotationalChange = 1f;


        // private variables
        private float _drivePerRay;
        private Rigidbody _rb;
        private List<DriveElement> _leftDriveElements = new List<DriveElement>();
        private List<DriveElement> _rightDriveElements = new List<DriveElement>();
        private float _accelInput;
        private float _steerInput;


        public void GetMove(Writer writer)
        {
            writer.Write(Input.GetAxis("Vertical"));
            writer.Write(Input.GetAxis("Horizontal"));
        }

        public void SetMove(Reader reader)
        {
            _accelInput = reader.ReadSingle();
            _steerInput = reader.ReadSingle();
        }

        public bool InSync(Reader reader)
        {
            bool result = true;

            var oldPosition = reader.Read<Vector3>();
            var oldRotation = reader.Read<Quaternion>();
            reader.Read<Vector3>();
            reader.Read<Vector3>();

            result &= (oldPosition - _rb.transform.position).magnitude < minPositionalChange;
            result &= Quaternion.Angle(oldRotation, _rb.transform.rotation) < minRotationalChange;

            foreach (var driveElement in _leftDriveElements.Concat(_rightDriveElements))
            {
                result &= driveElement.InSync(reader);
            }

            return result;
        }

        public void GetState(Writer writer)
        {
            writer.Write(_rb.transform.position);
            writer.Write(_rb.transform.rotation);
            writer.Write(_rb.velocity);
            writer.Write(_rb.angularVelocity);

            foreach (var driveElement in _leftDriveElements.Concat(_rightDriveElements))
            {
                driveElement.GetState(writer);
            }
        }

        public void SetState(Reader reader)
        {
            _rb.transform.position = reader.Read<Vector3>();
            _rb.transform.rotation = reader.Read<Quaternion>();
            _rb.velocity = reader.Read<Vector3>();
            _rb.angularVelocity = reader.Read<Vector3>();

            foreach (var driveElement in _leftDriveElements.Concat(_rightDriveElements))
            {
                driveElement.SetState(reader);
            }
        }

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = _rb.transform.InverseTransformPoint(centerOfMass.position);

            var driveElements = GetComponentsInChildren<DriveElement>();

            foreach (DriveElement driveElement in driveElements)
            {                
                if (Mathf.Sign(driveElement.transform.localPosition.x) < 0)
                {
                    _leftDriveElements.Add(driveElement);
                }
                else
                {
                    _rightDriveElements.Add(driveElement);
                }
            }
            _drivePerRay = enginePower / (_leftDriveElements.Count + _rightDriveElements.Count);
            Debug.Log($"Found {_leftDriveElements.Count + _rightDriveElements.Count} track elements connected to vehicle. Each driveElement providing {_drivePerRay:F2} force each.");
        }

        public static float RangeLerp(float value, float istart, float istop, float ostart, float ostop, bool clamp = false)
        {
            float result = ostart + (ostop - ostart) * (value - istart) / (istop - istart);

            return clamp ? Mathf.Clamp(result, ostart, ostop) : result;
        }


        public void Simulate(float delta)
        {
            // calculate forward speed
            var currentSpeed = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).z;

            // Invert steering when reversing if enabled
            if (_accelInput < 0 && invertSteerWhenReverse)
            {
                _steerInput *= -1;
            }

            // Calculate speed interpolation
            float speedInterp = 0f;

            // Forward, use forward max speed
            if (_accelInput > 0)
            {
                speedInterp = RangeLerp(Mathf.Abs(currentSpeed), 0.0f, maxSpeedKph / 3.6f, 0.0f, 1.0f);
            }
            // Reverse, use reverse max speed
            else if (_accelInput < 0)
            {
                speedInterp = RangeLerp(Mathf.Abs(currentSpeed), 0.0f, maxReverseSpeedKph / 3.6f, 0.0f, 1.0f);
            }
            // Steering drive (always at start of curve)
            else if (_accelInput == 0 && _steerInput != 0)
            {
                speedInterp = 0f;
            }

            // Get force from torque curve (based on current speed)
            var currentDrivePower = torqueCurve.Evaluate(speedInterp) * _drivePerRay;

            // Double differential setup (steer with control of drive force)
            float braking = rollingResistance;

            // Calculate drive forces
            float LDriveFac = _accelInput + _steerInput;
            float RDriveFac = _accelInput + _steerInput * -1;

            Vector3 leftForce = transform.forward * currentDrivePower * LDriveFac;
            Vector3 rightForce = transform.forward * currentDrivePower * RDriveFac;

            // No brakes during normal driving
            if (LDriveFac != 0 || RDriveFac != 0)
            {
                braking = 0;
            }

            // Slow down if input opposite drive direction
            if (Mathf.Sign(currentSpeed) != Mathf.Sign(_accelInput))
            {
                braking = trackBrakePercent * Mathf.Abs(_accelInput);
            }

            // Apply parking brake if sitting still
            if (_accelInput == 0 && _steerInput == 0 && Mathf.Abs(currentSpeed) < autoStopSpeedMS)
            {
                braking = trackBrakePercent;
            }

            // Finally apply all forces and braking
            foreach (var element in _leftDriveElements)
            {
                element.ApplyForce(leftForce);
                element.ApplyBrake(braking);
                element.Simulate(delta);
            }

            foreach (var element in _rightDriveElements)
            {
                element.ApplyForce(rightForce);
                element.ApplyBrake(braking);
                element.Simulate(delta);
            }
        }
    }
}
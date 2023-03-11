using FishNet.Serializing;
using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Controller for wheeled vehicle
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WheelRayVehicle : MonoBehaviour
    {
        [Tooltip("How much max torque engine has")]
        [SerializeField]
        public float enginePower = 150.0f;

        [Tooltip("Curve that adjust torque based upon current forward speed")]
        [SerializeField]
        public AnimationCurve torqueCurve;

        [Tooltip("Maximum forward speed that can be achieved")]
        [SerializeField]
        public float maxSpeedKph = 100.0f;

        [Tooltip("Maximum reverse speed that can be achieved")]
        [SerializeField]
        public float maxReverseSpeedKph = 20.0f;

        [Tooltip("Percentage adjust inverse input direction to avoid sudden stops")]
        [SerializeField]
        public float maxBrakingCoef = 0.05f;

        [Tooltip("Friction when no power being given")]
        [SerializeField]
        public float rollingResistance = 0.001f;

        [Tooltip("Maximum steering angle wheels can be turned")]
        [SerializeField]
        public float steeringAngle = 25.0f;

        [Tooltip("Maximum speed wheels can be turned at")]
        [SerializeField]
        public float steerSpeed = 120.0f;

        [Tooltip("Ratio to reduce steering by as speed increases")]
        [SerializeField]
        public float maxSteerLimitRatio = 0.95f;

        [Tooltip("How fast wheels return to center after steering input stopped")]
        [SerializeField]
        public float steerReturnSpeed = 120.0f;

        [Tooltip("Minimum speed to fully stop")]
        [SerializeField]
        public float autoStopSpeedMS = 1.0f;

        [Tooltip("Front left drive element")]
        [SerializeField]
        public Transform frontLeftElement;

        [Tooltip("Front right drive element")]
        [SerializeField]
        public Transform frontRightElement;

        [Tooltip("Center of mass of vehicle")]
        [SerializeField]
        public Transform centerOfMass;

        [Tooltip("Minimum positional change before resync for observers.")]
        [Range(0.01f, 0.5f)]
        public float minPositionalChange = 0.05f;

        [Tooltip("Minimum rotational change before resync for observers.")]
        [Range(0.01f, 1f)]
        public float minRotationalChange = 1f;

        private Rigidbody _rb;
        private DriveElement[] _driveElements;
        private float _drivePerRay;
        private float _currentDrivePower;
        private float _currentSteerAngle;
        private float _maxSteerAngle;
        private float _currentSpeed;
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

            foreach (var driveElement in _driveElements)
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

            foreach (var driveElement in _driveElements)
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

            foreach (var driveElement in _driveElements)
            {
                driveElement.SetState(reader);
            }
        }

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = _rb.transform.InverseTransformPoint(centerOfMass.position);
            // setup array of drive elements and setup drive power
            _driveElements = GetComponentsInChildren<DriveElement>(); 
            _drivePerRay = enginePower / _driveElements.Length;
            Debug.Log($"Found {_driveElements.Length} drive elements connected to wheeled vehicle, setting to provide {_drivePerRay:f2} force each.");
        }

        public static float RangeLerp(float value, float istart, float istop, float ostart, float ostop, bool clamp = false)
        {
            float result = ostart + (ostop - ostart) * (value - istart) / (istop - istart);

            return clamp ? Mathf.Clamp(result, ostart, ostop) : result;
        }

        public void Simulate(float delta)
        {
            // Calculate forward speed
            _currentSpeed = transform.InverseTransformDirection(_rb.velocity).z;
            
            Vector3 finalForce;
            var finalBrake = rollingResistance;

            // steer wheels gradualy based on steering input
            if (_steerInput != 0)
            {
                var desiredAngle = -_steerInput * steeringAngle;
                _currentSteerAngle = Mathf.MoveTowards(_currentSteerAngle, -desiredAngle, steerSpeed * delta);
            }
            else
            {
                // return wheels to center with wheel return speed
                if (!Mathf.Approximately(_currentSteerAngle, 0.0f))
                {
                    if (_currentSteerAngle > 0.0f)
                    {
                        _currentSteerAngle -= steerReturnSpeed * delta;
                    }
                    else
                    {
                        _currentSteerAngle += steerReturnSpeed * delta;
                    }
                }
                else
                {
                    _currentSteerAngle = 0.0f;
                }
            }

            // limit steering based on speed and apply steering
            var maxSteerRatio = RangeLerp(_currentSpeed * 3.6f, 0f, maxSpeedKph, 0f, maxSteerLimitRatio, true);
            _maxSteerAngle = (1 - maxSteerRatio) * steeringAngle;
            _currentSteerAngle = Mathf.Clamp(_currentSteerAngle, -_maxSteerAngle, _maxSteerAngle);

            // front wheel steering
            frontRightElement.localRotation = Quaternion.Euler(0, _currentSteerAngle, 0);
            frontLeftElement.localRotation = Quaternion.Euler(0, _currentSteerAngle, 0);

            // no braking if we are driving
            if (_accelInput != 0)
            {
                finalBrake = 0;
            }

            // brake if movement opposite intended direction
            if (Mathf.Sign(_currentSpeed) != Mathf.Sign(_accelInput) && !Mathf.Approximately(_currentSpeed, 0) && _accelInput != 0)
            {
                finalBrake = maxBrakingCoef * Mathf.Abs(_accelInput);
            }

            // Apply parking brake if vehicle is sitting still with no inputs
            if (_accelInput == 0 && _steerInput == 0 && Mathf.Abs(_currentSpeed) < autoStopSpeedMS)
            {
                finalBrake = maxBrakingCoef;
            }

            // Calculate motor forces
            float speedInterp = 0f;

            if (_accelInput > 0)
            {
                speedInterp = RangeLerp(Mathf.Abs(_currentSpeed), 0.0f, maxSpeedKph / 3.6f, 0.0f, 1.0f, true);
            }
            else if (_accelInput < 0)
            {
                speedInterp = RangeLerp(Mathf.Abs(_currentSpeed), 0.0f, maxReverseSpeedKph / 3.6f, 0.0f, 1.0f, true);
            }

            _currentDrivePower = torqueCurve.Evaluate(speedInterp) * _drivePerRay;

            finalForce = transform.forward * _currentDrivePower * _accelInput;

            // 4WD 
            foreach (var driveElement in _driveElements)
            {
                // Apply drive force and braking
                driveElement.ApplyForce(finalForce);
                driveElement.ApplyBrake(finalBrake);
                driveElement.Simulate(delta);
            }
        }
    }
}
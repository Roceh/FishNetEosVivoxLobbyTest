using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Allows orbiting of target with main camera
    /// </summary>
    public class TrackCamera : MonoBehaviour
    {
        [Tooltip("Target that the camera will orbit.")]
        [SerializeField]
        public Transform target;

        [Tooltip("Distance from target the camera will orbit.")]
        [SerializeField]
        public float distanceFromTarget = 15f;

    
        private void LateUpdate()
        {
            if (target)
            {
                transform.position = transform.rotation * new Vector3(0.0f, 0.0f, -distanceFromTarget) + target.position;
            }
        }
    }
}
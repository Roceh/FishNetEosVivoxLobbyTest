using UnityEngine;

namespace UnityCustomRaycastVehicle
{
    /// <summary>
    /// Central point to control bandwidth reduction via reconcilation tick rate
    /// </summary>
    public class GlobalReconcilation : MonoBehaviour
    {
        public static GlobalReconcilation Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        /// <summary>
        /// How many ticks do we count before we do a reconcilation. 
        /// </summary>
        [Tooltip("How often (every n ticks) to send reconcilation to client.")]
        [Range(1, 50)]
        public uint TicksBetweenReconcilation = 10;
    }
}

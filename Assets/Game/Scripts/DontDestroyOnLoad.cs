using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EOSLobbyTest
{
    public class DontDestroyOnLoad : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);            
        }
    }
}

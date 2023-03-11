using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EOSLobbyTest
{
    public class UIPanelBusy : UIPanel<UIPanelBusy>, IUIPanel
    {
        [SerializeField]
        private RectTransform rectComponent;

        [SerializeField]
        private float rotateSpeed = 200f;

        private void Update()
        {
            rectComponent.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
        }
    }
}

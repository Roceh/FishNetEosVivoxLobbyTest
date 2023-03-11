using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class DropdownAutoScroll : Dropdown
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);

            if (eventData.selectedObject.GetComponentInChildren<Scrollbar>() != null || !IsActive() || !IsInteractable())
                return;

            Scrollbar scrollbar = gameObject.GetComponentInChildren<ScrollRect>()?.verticalScrollbar;
            if (options.Count > 1 && scrollbar != null)
            {
                if (scrollbar.direction == Scrollbar.Direction.TopToBottom)
                    scrollbar.value = Mathf.Max(0.001f, (float)(value) / (float)(options.Count - 1));
                else
                    scrollbar.value = Mathf.Max(0.001f, 1.0f - (float)(value) / (float)(options.Count - 1));
            }
        }
    }
}

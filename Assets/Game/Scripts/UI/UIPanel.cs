using System;
using System.Collections;
using UnityEngine;

namespace EOSLobbyTest
{
    public class UIPanel<T> : MonoBehaviourSingleton<T> where T : Component, IUIPanel
    {
        public string Id { get; }

        public bool UIResult { get; set; }

        // triggered whenever the panel is shown
        public event Action onShown;

        // triggered whenever the panel is hidden
        public event Action onHidden;

        // we use an explict bool to track panel visiblity 
        public bool IsVisible { get; set; }

        private void OnDisable()
        {
            // if we think we are visible then tidy up - normally occurs on scene change
            if (IsVisible)
            {
                DoHide(false);
            }
        }

        public UIPanel()
        {
            Id = GetType().Name;
        }

        public void DoShow()
        {
            OnShowing();
            
            gameObject.SetActive(true);

            IsVisible = true;

            StartCoroutine(OnShowDelayed());
        }

        protected virtual void OnShowing()
        {           
        }

        private IEnumerator OnShowDelayed()
        {
            yield return new WaitForEndOfFrame();

            OnShown();

            onShown?.Invoke();
        }

        protected virtual void OnShown()
        {
        }

        public void DoHide(bool result)
        {
            UIResult = result;

            OnHiding();

            IsVisible = false;

            gameObject.SetActive(false);

            OnHidden();

            onHidden?.Invoke();
        }

        protected virtual void OnHiding()
        {
        }

        protected virtual void OnHidden()
        {
        }
    }
}

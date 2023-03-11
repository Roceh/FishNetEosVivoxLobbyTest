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

        public UIPanel()
        {
            Id = GetType().Name;
        }

        public void DoShow()
        {
            OnShowing();
            
            gameObject.SetActive(true);

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

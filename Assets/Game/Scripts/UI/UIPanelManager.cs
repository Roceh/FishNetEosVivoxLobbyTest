using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EOSLobbyTest
{
    public class UIPanelManager : MonoBehaviourSingletonForScene<UIPanelManager>
    {
        private List<IUIPanel> _panels;

        [Tooltip("Panel we are going to show first")]
        public string initialPanel;

        public override void Awake()
        {
            base.Awake();

            Application.quitting += Application_quitting;

            _panels = GetComponentsInChildren<IUIPanel>(true).ToList();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Application.quitting -= Application_quitting;
        }

        private void Application_quitting()
        {
            foreach (var panel in _panels)
            {
                HidePanel(panel.Id);
            }
        }

        private void Start()
        {
            HideAllPanels();
            ShowPanel(initialPanel);
        }

        private void HideAllPanels()
        {
            foreach (var panel in _panels)
            {
                panel.gameObject.SetActive(false);
            }
        }

        public void HidePanel<T>(bool result = true)
        {
            HidePanel(typeof(T).Name, result);
        }

        public void HidePanel(string id, bool result = true)
        {
            IUIPanel panel = _panels.FirstOrDefault(x => x.Id == id);

            if (panel != null)
            {
                panel.DoHide(result);
            }
        }

        public void ShowPanel<T>()
        {
            ShowPanel(typeof(T).Name);
        }

        public bool PanelIsVisible<T>()
        {
            IUIPanel panel = _panels.FirstOrDefault(x => x.Id == typeof(T).Name);

            return panel != null && panel.gameObject.activeInHierarchy;
        }

        public IEnumerator ShowPanelAndWaitTillHidden<T>()
        {
            ShowPanel(typeof(T).Name);

            yield return new WaitUntil(() => !PanelIsVisible<T>());
        }

        public void ShowPanel(string id)
        {
            IUIPanel panel = _panels.FirstOrDefault(x => x.Id == id);

            if (panel != null)
            {
                panel.DoShow();
            }
        }
    }
}
using System;
using UnityEngine;

namespace EOSLobbyTest
{
    public interface IUIPanel
    {
        string Id { get; }

        GameObject gameObject { get; }

        bool UIResult { get; }

        void DoShow();

        void DoHide(bool result = true);
    }
}

using FishNet.Object;
using VivoxUnity;


namespace EOSLobbyTest
{
    public class VivoxPositionalVoice : NetworkBehaviour
    {
        private void Update()
        {
            if (IsOwner && VivoxManager.Instance.TransmittingSession != null && VivoxManager.Instance.TransmittingSession.AudioState == ConnectionState.Connected)
            {
                VivoxManager.Instance.TransmittingSession.Set3DPosition(transform.position, transform.position, transform.forward, transform.up);
            }
        }
    }
}
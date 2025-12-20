using UnityEngine;

namespace PurrLobby
{
    public struct LobbyUser
    {
        public string Id;
        public string DisplayName;
        public bool IsReady;
        public PlayerRole Role;
        public Texture2D Avatar;
    }

    public enum PlayerRole
    {
        None,
        Engineer,
        Technician
    }
}
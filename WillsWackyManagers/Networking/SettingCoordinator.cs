using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using UnityEngine;
using ExitGames.Client.Photon;

namespace WillsWackyManagers.Networking
{
    internal class SettingCoordinator : MonoBehaviourPunCallbacks
    {
        internal bool Synced { get; private set; }
        internal const string SettingsPropertyName = "WWM Settings";
        internal const string TableFlipSyncProperty = "Table Flip Sync Stats";
        internal const string RerollSyncProperty = "Reroll Sync Stats";

        public static SettingCoordinator instance;

        private void Start()
        {
            Synced = false;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                DestroyImmediate(this);
                return;
            }
        }

        public override void OnJoinedRoom()
        {
            Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            if (!customProperties.ContainsKey(TableFlipSyncProperty))
            {
                customProperties[TableFlipSyncProperty] = false;
            }
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            WillsWackyManagers.instance.OnHandShakeCompleted();
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            WillsWackyManagers.instance.OnHandShakeCompleted();
        }

        public override void OnLeftRoom()
        {
            Synced = false;
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(SettingsPropertyName))
            {
                Synced = true;
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    if (player.CustomProperties.TryGetValue(SettingsPropertyName, out var status))
                    {
                        if (!((bool) status))
                        {
                            Synced = false;
                        }
                    }
                    else
                    {
                        Synced = false;
                    }
                }

                if (!Synced)
                {
                    WillsWackyManagers.instance.OnHandShakeCompleted();
                }
            }
        }
    }
}

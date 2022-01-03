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
        internal const string PropertyName = "WWM Settings";

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
            //Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            //var settings = new bool[] { WillsWackyManagers.enableCurseRemovalConfig.Value, WillsWackyManagers.enableCurseSpawningConfig.Value, WillsWackyManagers.enableTableFlipConfig.Value, WillsWackyManagers.secondHalfTableFlipConfig.Value };
            //customProperties[PropertyName] = settings;
            //PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

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
            if (changedProps.ContainsKey(PropertyName))
            {
                Synced = true;
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    if (player.CustomProperties.TryGetValue(PropertyName, out var status))
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
                else
                {
                    UnityEngine.Debug.Log("[WWM][Settings] Settings are all synched.");
                }
            }
        }
    }
}

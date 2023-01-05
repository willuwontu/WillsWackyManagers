using UnityEngine;
using UnityEngine.UI;
using UnboundLib;
using System;
using Photon.Pun;
using ModdingUtils;

namespace WillsWackyManagers.MonoBehaviours
{
    [DisallowMultipleComponent]
    public class Misfire_Mono : MonoBehaviourPun, ModdingUtils.GameModes.IPointStartHookHandler
    {
        public int misfireChance = 0;
        private static System.Random random = new System.Random();
        private bool coroutineStarted;
        private Gun gun;
        private GunAmmo gunAmmo;
        private CharacterData data;
        private WeaponHandler weaponHandler;
        private Player player;

        private void Start()
        {
            data = GetComponentInParent<CharacterData>();
            ModdingUtils.GameModes.InterfaceGameModeHooksManager.instance.RegisterHooks(this);
        }

        private void Update()
        {
            if (!player)
            {
                if (!(data is null))
                {
                    player = data.player;
                    weaponHandler = data.weaponHandler;
                    gun = weaponHandler.gun;
                    gunAmmo = gun.GetComponentInChildren<GunAmmo>();
                    gun.ShootPojectileAction += OnShootProjectileAction;
                }
            }
        }

        private void OnShootProjectileAction(GameObject obj)
        {
            var roll = random.Next(100);
            if (roll < misfireChance && this.photonView.IsMine)
            {
                this.photonView.RPC(nameof(RPCA_Misfire), RpcTarget.All, roll );
            }
        }

        [PunRPC]
        private void RPCA_Misfire(int roll)
        {
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Hex] Player {player.playerID} Misfire Curse activated with a roll of {roll} and a chance of {misfireChance}%.");
            gunAmmo.SetFieldValue("currentAmmo", 0);
            for (int i = gunAmmo.populate.transform.childCount - 1; i >= 0; i--)
            {
                if (gunAmmo.populate.transform.GetChild(i).gameObject.activeSelf)
                {
                    Destroy(gunAmmo.populate.transform.GetChild(i).gameObject);
                }
            }
        }

        public void OnPointStart()
        {
            CheckIfValid();
        }

        private void CheckIfValid()
        {
            var haveMisfire = false;
            for (int i = 0; i < player.data.currentCards.Count; i++)
            {
                if (misfireChance <= 0)
                {
                    haveMisfire = true;
                    break;
                }
            }

            if (!haveMisfire)
            {
                UnityEngine.GameObject.Destroy(this);
            }
        }

        private void OnDestroy()
        {
            ModdingUtils.GameModes.InterfaceGameModeHooksManager.instance.RemoveHooks(this);
            gun.ShootPojectileAction -= OnShootProjectileAction;
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }
    }
}
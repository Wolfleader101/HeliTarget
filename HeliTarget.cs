using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("HeliTarget", "Wolfleader101", "1.6.5")]
	[Description("stop attack helicopter from targetting players")]
	class HeliTarget : RustPlugin
	{
		#region Variables

		private PluginConfig config;

		#endregion

		#region Hooks
		private void Init()
		{
			config = Config.ReadObject<PluginConfig>();

			permission.RegisterPermission(config.perm, this);
			if (config.bulletAccuracy > 0.0) ConVar.PatrolHelicopter.bulletAccuracy = config.bulletAccuracy; // lower = better

		}

		private void OnEntitySpawned(BaseHelicopter heli)
		{
			if(config.bulletDamage > 0.0) heli.bulletDamage = config.bulletDamage;
			heli.InvokeRepeating(() => FindTargets(heli.myAI), 5, 10);
		}

		bool CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
		{
			if (player != null && permission.UserHasPermission(player.UserIDString, config.perm))
			{
				return false;

			}
			else
			{
				return true;
			}
		}

		#endregion

		#region Custom Methods
		void FindTargets(PatrolHelicopterAI heli)
		{
			List<NPCPlayer> nearbyNPCPlayers = new List<NPCPlayer>();

			Vis.Entities(heli.transform.position, config.targetRadius, nearbyNPCPlayers);

			foreach (var player in nearbyNPCPlayers)
			{
				if (player.userID.IsSteamId() && permission.UserHasPermission(player.UserIDString, config.perm)) return;

				if (player is Scientist && !config.shootScientist) continue;
				if (player.ShortPrefabName == "scarecrow" || player.ShortPrefabName == "murderer" && !config.shootZombies) continue;
				heli._targetList.Add(new PatrolHelicopterAI.targetinfo(player, player));
			}

			if (config.shootAnimals)
			{
				List<BaseAnimalNPC> nearbyAnimals = new List<BaseAnimalNPC>();

				Vis.Entities(heli.transform.position, 100, nearbyAnimals);

				foreach (var animal in nearbyAnimals)
				{
					if (!config.shootAnimals) continue;
					heli.leftGun.SetTarget(animal);
					heli.rightGun.SetTarget(animal);
				}
			}

			if (heli._targetList.Any())
			{
				heli.timeBetweenRockets = config.timeBetweenRockets;
				ServerMgr.Instance.StartCoroutine(RocketTimer(heli));
			}


		}

		private void FireRocket(PatrolHelicopterAI heliAI)
		{
			if (heliAI == null || !(heliAI?.IsAlive() ?? false)) return;
			if (heliAI._targetList.Count == 0) return;

			var num1 = 4f;
			var strafeTarget = heliAI._targetList[0].ply.ServerPosition;
			if (strafeTarget == Vector3.zero) return;
			var vector3 = heliAI.transform.position + heliAI.transform.forward * 1f;
			var direction = (strafeTarget - vector3).normalized;
			if (num1 > 0.0) direction = Quaternion.Euler(UnityEngine.Random.Range((float)(-num1 * 0.5), num1 * 0.5f), UnityEngine.Random.Range((float)(-num1 * 0.5), num1 * 0.5f), UnityEngine.Random.Range((float)(-num1 * 0.5), num1 * 0.5f)) * direction;
			var flag = heliAI.leftTubeFiredLast;
			heliAI.leftTubeFiredLast = !flag;
			Effect.server.Run(heliAI.helicopterBase.rocket_fire_effect.resourcePath, heliAI.helicopterBase, StringPool.Get("rocket_tube_" + (!flag ? "right" : "left")), Vector3.zero, Vector3.forward, null, true);
			var entity = GameManager.server.CreateEntity(heliAI.rocketProjectile_Napalm.resourcePath, vector3, new Quaternion(), true);
			if (entity == null) return;
			var projectile = entity.GetComponent<ServerProjectile>();
			if (projectile != null) projectile.InitializeVelocity(direction * projectile.speed);
			entity.OwnerID = 1337; //assign ownerID so it doesn't infinitely loop on OnEntitySpawned
			entity.Spawn();
		}

		IEnumerator RocketTimer(PatrolHelicopterAI heli)
		{
			float time = Time.time;
			int i = 0;
			while (i <= config.rocketsToFire)
			{
				if (Time.time - time > 0.5f)
				{
					FireRocket(heli);
					time = Time.time;
					i++;
				}
				yield return null;
			}
			yield break;
		}


		#endregion

		#region Config

		private class PluginConfig
		{

			[JsonProperty("Permission")]
			public string perm { get; set; }

			[JsonProperty("Shoot Scientists")]
			public bool shootScientist { get; set; }

			[JsonProperty("Shoot Zombies")]
			public bool shootZombies { get; set; }

			[JsonProperty("Shoot Animals")]
			public bool shootAnimals { get; set; }

			[JsonProperty("Time(seconds) between rockets")]
			public float timeBetweenRockets { get; set; }

			[JsonProperty("Rockets to fire")]
			public int rocketsToFire { get; set; }

			[JsonProperty("Heli Target Radius")]
			public int targetRadius { get; set; }

			[JsonProperty("Heli bullet accuracy")]
			public float bulletAccuracy { get; set; }


			[JsonProperty("Heli bullet damage")]
			public float bulletDamage { get; set; }
		}

		private PluginConfig GetDefaultConfig()
		{
			return new PluginConfig
			{
				perm = "helitarget.ignore",
				shootScientist = true,
				shootAnimals = true,
				shootZombies = true,
				timeBetweenRockets = 15,
				rocketsToFire = 5,
				targetRadius = 100,
				bulletAccuracy = 2,
				bulletDamage = 20
			};
		}

		protected override void LoadDefaultConfig()
		{
			Config.WriteObject(GetDefaultConfig(), true);
		}
		#endregion
	}
}
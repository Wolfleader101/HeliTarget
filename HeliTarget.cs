using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("HeliTarget", "Wolfleader101", "1.1.1")]
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
		}

		private void OnEntitySpawned(BaseHelicopter heli)
		{
			heli.InvokeRepeating(() => FindTargets(heli.myAI), 5, 5);
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
			List<BasePlayer> nearbyNPCPlayers = new List<BasePlayer>();


			Vis.Entities(heli.transform.position, config.targetRadius, nearbyNPCPlayers);

			foreach (var player in nearbyNPCPlayers)
			{
				if (player.userID.IsSteamId() && permission.UserHasPermission(player.UserIDString, config.perm)) return;

				if (player is Scientist && !config.shootScientist) continue;
				if (player is HTNPlayer && !config.shootZombies) continue;

				heli._targetList.Add(new PatrolHelicopterAI.targetinfo(player, player));
			}

			if (heli._targetList.Any())
			{
				//var useNapalm = heli.CanUseNapalm(); // doesnt work
				//useNapalm = true;                    // doesnt work
				heli.timeBetweenRockets = config.timeBetweenRockets;
				for (int i = 1; i < config.rocketsToFire; i++)
				{
					heli.SetAimTarget(heli._targetList[0].ply.GetNetworkPosition(), false); // experimental
					heli.FireRocket();
				}
			}


			//if (config.shootAnimals)
			//{
			//	List<BaseAnimalNPC> nearbyAnimals = new List<BaseAnimalNPC>();


			//	Vis.Entities(heli.transform.position, 100, nearbyAnimals);

			//	foreach (var animal in nearbyAnimals)
			//	{
			//		if (!config.shootAnimals) continue;
			//		heli.SetAimTarget
			//PatrolHelicopterAI.leftGun.SetTarget
			//PatrolHelicopterAI.rightGun.SetTarget
			//	}
			//}



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
			};
		}

		protected override void LoadDefaultConfig()
		{
			Config.WriteObject(GetDefaultConfig(), true);
		}
		#endregion
	}
}
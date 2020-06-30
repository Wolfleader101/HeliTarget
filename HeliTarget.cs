using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("HeliTarget", "Wolfleader101", "1.0.0")]
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
			if (config.shootNPC)
			{
				List<NPCPlayerApex> nearbyScientist = new List<NPCPlayerApex>();


				Vis.Entities(heli.transform.position, 100, nearbyScientist);

				foreach (var player in nearbyScientist)
				{
					if (player is Scientist && !config.shootNPC) continue;
					if (player is NPCMurderer && !config.shootZombies) continue;
					if (player is NPCMurderer && !permission.UserHasPermission(player.UserIDString, config.perm)) continue;

					heli._targetList.Add(new PatrolHelicopterAI.targetinfo(player, player));
				}
				heli.timeBetweenRockets = config.timeBetweenRockets;
				for(int i = 1; i < config.rocketsToFire; i++)
				{
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
			//		heli._targetList.Add(new PatrolHelicopterAI.targetinfo(animal as BaseEntity));
			//	}
			//}
			


		}

		#endregion

		#region Config

		private class PluginConfig
		{

			[JsonProperty("Permission")]
			public string perm { get; set; }

			[JsonProperty("Shoot NPCs")]
			public bool shootNPC { get; set; }

			[JsonProperty("Shoot Zombies")]
			public bool shootZombies { get; set; }

			[JsonProperty("Shoot Animals")]
			public bool shootAnimals { get; set; }

			[JsonProperty("Time(seconds) between rockets")]
			public float timeBetweenRockets { get; set; }

			[JsonProperty("Rockets to fire")]
			public int rocketsToFire { get; set; }
		}

		private PluginConfig GetDefaultConfig()
		{
			return new PluginConfig
			{
				perm = "helitarget.ignore",
				shootNPC = true,
				shootAnimals = true,
				shootZombies = true,
				timeBetweenRockets = 15,
				rocketsToFire = 5,
			};
		}

		protected override void LoadDefaultConfig()
		{
			Config.WriteObject(GetDefaultConfig(), true);
		}
		#endregion
	}
}
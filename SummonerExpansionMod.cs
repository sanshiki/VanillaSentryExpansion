using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;
using Terraria;
using SummonerExpansionMod.Initialization;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria.GameContent.UI.States;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class SummonerExpansionMod : Mod
	{
		// internal static ModKeybind ToggleDynamicParamManagerUI;
		public override void Load()
		{
			// ToggleDynamicParamManagerUI = KeybindLoader.RegisterKeybind(this, "Toggle Dynamic Param Manager UI", "P");
			ModIDLoader.Load();
			// if (Main.netMode != NetmodeID.Server)
			// {
			// 	// First, you load in your shader file.
			// 	// You'll have to do this regardless of what kind of shader it is,
			// 	// and you'll have to do it for every shader file.
			// 	// This example assumes you have both armor and screen shaders.
			// 	Asset<Effect> ExampleShader = this.Assets.Request<Effect>("Effects/ShaderExample");

			// 	// To add a dye, simply add this for every dye you want to add.
			// 	// "PassName" should correspond to the name of your pass within the *technique*,
			// 	// so if you get an error here, make sure you've spelled it right across your effect file.
			// 	GameShaders.Armor.BindShader(ModContent.ItemType<MyDyeItem>(), new ArmorShaderData(ExampleShader, "ArmorBasic"));

			// 	// To bind a miscellaneous, non-filter effect, use this.
			// 	// If you're actually using this, you probably already know what you're doing anyway.
			// 	// This type of shader needs an additional parameter: float4 uShaderSpecificData;
			// 	GameShaders.Misc["SummonerExpansionMod:EffectName"] = new MiscShaderData(ExampleShader, "ArmorBasic");

			// 	// To bind a screen shader, use this.
			// 	// EffectPriority should be set to whatever you think is reasonable.   

			// 	Filters.Scene["SummonerExpansionMod:FilterName"] = new Filter(new ScreenShaderData(ExampleShader, "ArmorBasic"), EffectPriority.Medium);
			// }
		}
	}
}

// using Microsoft.Xna.Framework;
// using System;
// using Terraria;
// using Terraria.DataStructures;
// using Terraria.ID;
// using Terraria.ModLoader;
// using Terraria.Localization;

// using Microsoft.Xna.Framework.Graphics;
// using SummonerExpansionMod.Content.Buffs.Summon;
// using SummonerExpansionMod.ModUtils;
// using SummonerExpansionMod.Initialization;

// using SummonerExpansionMod.Content.Projectiles.Summon;
// using SummonerExpansionMod.Initialization;

// namespace SummonerExpansionMod.Content.Buffs.Summon
// {
//     public class ElectricBoostBuff : ModBuff
//     {
//         public override string Texture => ModGlobal.VANILLA_BUFF_TEXTURE_PATH + BuffID.Swiftness;
//         private const int MAX_DURATION = (int)(60 * 1f);
//         private const int MAX_LEVEL = 10;
//         private const float MAX_RUN_SPEED = 3f;
//         private const float MAX_RUN_ACCELERATION = 0.3f;
//         private const float MAX_JUMP_SPEED_BOOST = 4f;
//         private const float MAX_DAMAGE_INCREASE = 0.4f;
//         private int lvl = 1;
//         public override void SetStaticDefaults()
//         {
//             Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
//             Main.buffNoTimeDisplay[Type] = false; // The time remaining won't display on this buff
//             Main.debuff[Type] = false;    // 是负面效果

//             // DisplayName.SetDefault("Electric Boost");
//             // Description.SetDefault("Increases summon damage and movement speed.Current level: {0}/{1}", lvl, MAX_LEVEL);
//         }

//         public override bool ReApply(Player player, int time, int buffIndex)
//         {
//             lvl = Math.Min(lvl + 1, MAX_LEVEL);
//             return false;
//         }

//         public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
//         {
//             string tooltip = "Current level: " + lvl + "/" + MAX_LEVEL;
//             tip = tooltip;
//         }

//         public override void Update(Player player, ref int buffIndex)
//         {
//             float lvl_factor = (float)lvl / MAX_LEVEL;
//             player.maxRunSpeed += MathHelper.Lerp(0f, MAX_RUN_SPEED, lvl_factor);
//             player.runAcceleration += MathHelper.Lerp(0f, MAX_RUN_ACCELERATION, lvl_factor);

//             player.jumpSpeedBoost += MathHelper.Lerp(0f, MAX_JUMP_SPEED_BOOST, lvl_factor);

//             player.GetDamage(DamageClass.Summon) += MathHelper.Lerp(0f, MAX_DAMAGE_INCREASE, lvl_factor);

//             int buffTime = player.buffTime[buffIndex];
//             if(buffTime == 0)
//             {
//                 lvl -= 1;
//                 if(lvl <= 1)
//                 {
//                     lvl = 1;
//                 }
//                 else
//                 {
//                     player.buffTime[buffIndex] = MAX_DURATION;
//                 }
//             }
//         }
//     }
// }
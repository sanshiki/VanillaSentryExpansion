// using Microsoft.Xna.Framework;
// using System;
// using Terraria;
// using Terraria.DataStructures;
// using Terraria.ID;
// using Terraria.ModLoader;

// using SummonerExpansionMod.Content.Projectiles.Summon;

// namespace SummonerExpansionMod.Content.Buffs.Summon
// {
//     public class SentryEnhancementBuff : ModBuff
//     {
//         public override void SetStaticDefaults()
//         {
//             Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
//             Main.buffNoTimeDisplay[Type] = false; // The time remaining won't display on this buff
//             Main.debuff[Type] = false;    // 不是负面效果
//         }

//         public override void Update(Player player, ref int buffIndex)
//         {
//             player.GetDamage(DamageClass.Summon) += 0.1f;
//             // if (player.ownedProjectileCounts[ModContent.ProjectileType<Sentry>()] > 0)
//             // {
//             //     player.buffTime[buffIndex] = 18000;
//             // }
//             // else
//             // {
//             //     player.DelBuff(buffIndex);
//             //     buffIndex--;
//             // }
//         }
//     }
// }
// using Microsoft.Xna.Framework;
// using System;
// using Terraria;
// using Terraria.DataStructures;
// using Terraria.ID;
// using Terraria.ModLoader;

// using SummonerExpansionMod.Content.Projectiles.Summon;

// namespace SummonerExpansionMod.Content.Buffs.Summon
// {
//     public class SentryTargetBuff : ModBuff
//     {
//         public override void SetStaticDefaults()
//         {
//             Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
//             Main.buffNoTimeDisplay[Type] = false; // The time remaining won't display on this buff
//             Main.debuff[Type] = true;    // 是负面效果
//         }

//         // public override void Update(Player player, ref int buffIndex)
//         // {
//         //     // if (player.ownedProjectileCounts[ModContent.ProjectileType<Sentry>()] > 0)
//         //     // {
//         //     //     player.buffTime[buffIndex] = 18000;
//         //     // }
//         //     // else
//         //     // {
//         //     //     player.DelBuff(buffIndex);
//         //     //     buffIndex--;
//         //     // }
//         // }

//         public override void Update(NPC npc, ref int buffIndex)
//         {
//             // 给敌人加光
//             Lighting.AddLight(npc.Center, 1f, 0.3f, 0.3f);

//             // 概率生成粒子
//             if (Main.rand.NextBool(5))
//             {
//                 Dust.NewDust(npc.position, npc.width, npc.height, DustID.FireworkFountain_Red,
//                     Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
//             }
//         }
//     }
// }
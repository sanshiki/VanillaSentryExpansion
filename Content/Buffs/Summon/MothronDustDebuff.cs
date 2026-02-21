using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

using SummonerExpansionMod.Content.Projectiles.Summon;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Buffs.Summon
{
    public class MothronDustDebuff : ModBuff
    {
        public override string Texture => ModGlobal.VANILLA_BUFF_TEXTURE_PATH + BuffID.Swiftness;
        
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true; // This buff won't save when you exit the world
            Main.buffNoTimeDisplay[Type] = false; // The time remaining won't display on this buff
            Main.debuff[Type] = true;    // 是负面效果
        }

        // public override void Update(NPC npc, ref int buffIndex) {
        //     // 这里不处理逻辑，只是标记有这个debuff
        //     npc.GetGlobalNPC<MothronDustDebuffNPC>().hasDebuff = true;
        //     int lvl = npc.GetGlobalNPC<MothronDustDebuffNPC>().lvl;
        // }
    }

    // public class MothronDustDebuffNPC : GlobalNPC
    // {
    //     public override bool InstancePerEntity => true;
    //     private const int MAX_DURATION = (int)(60 * 0.15f);
    //     private const int MAX_LEVEL = 10;
    //     public int lvl = 0;
    //     public bool hasDebuff = false;
    //     private int decayTimer = 0;
        
    //     public override void ResetEffects(NPC npc) {
    //         hasDebuff = false;
    //     }

    //     public override void PostAI(NPC npc) {
    //         if (Main.netMode == NetmodeID.MultiplayerClient)
    //             return;


    //         if (hasDebuff) {
    //             // 每帧检测：防止堆叠层数超过上限
    //             if (lvl > MAX_LEVEL)
    //                 lvl = MAX_LEVEL;

    //             // 每60帧（1秒）减少一层
    //             decayTimer++;
    //             if (decayTimer >= MAX_DURATION) {
    //                 decayTimer = 0;
    //                 if (lvl > 0)
    //                 {
    //                     lvl--;
    //                     npc.netUpdate = true;
    //                 }
                        
    //             }

    //             // Main.NewText("MothronDustDebuff lvl: " + lvl);

    //         } else {
    //             // 没有debuff时自动清空
    //             lvl = 0;
    //             decayTimer = 0;
    //         }
    //     }

    //     public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    //     {
    //         binaryWriter.Write(lvl);
    //         binaryWriter.Write(hasDebuff);
    //         binaryWriter.Write(decayTimer);
    //     }

    //     public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    //     {
    //         lvl = binaryReader.ReadInt32();
    //         hasDebuff = binaryReader.ReadBoolean();
    //         decayTimer = binaryReader.ReadInt32();
    //     }
    // }
}
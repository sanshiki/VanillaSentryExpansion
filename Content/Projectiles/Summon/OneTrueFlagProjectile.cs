using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Items.Weapons.Summon;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class OneTrueFlagShadow
    {
        public Vector2 center;
        public float rotation;
        public int dir;
    }
    public class OneTrueFlagProjectile : FlagProjectile
    {
        protected override string FLAG_CLOTH_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/OneTrueFlag";
        protected override int FLAG_WIDTH => 120;
        protected override int FLAG_HEIGHT => 78;
        protected override float TAIL_OFFSET_X_1 => -33f;
        protected override float TAIL_OFFSET_Y_1 => -90f;  
        protected override float TAIL_OFFSET_X_2 => -33f;  
        protected override float TAIL_OFFSET_Y_2 => -63f;   
        protected override Color TAIL_COLOR => new Color(35, 54, 84, 100);
        protected override bool TAIL_DYNAMIC_DEBUG => false;
        // protected override bool TAIL_ENABLE_GLOBAL => false;
        protected override bool USE_CUSTOM_SENTRY_RECALL => true;
        protected override float GRAVITY => 2.0f;
        protected override float MAX_FALL_SPEED => 35f;
        protected override float RECALL_SPEED => 50f;
        protected override int FULLY_CHARGED_DUST => DustID.MushroomSpray;
        protected override int ENHANCE_BUFF_ID => ModBuffID.OneTrueFlagBuff;
        protected override int NPC_DEBUFF_ID => ModBuffID.OneTrueFlagDebuff;
        protected override int ENHANCE_BUFF_DURATION => 60*4;
        protected override bool AUTO_READD_BUFF_ON_PLANT => true;
        protected override bool USE_CURSOR_ASSISTED_PLANT => true;
        protected override float SENTRY_RECALL_SPEED => 52f;
        protected override float SENTRY_RECALL_THRESHOLD => 40f;
        protected override float SENTRY_RECALL_DECAY_DIST => 1000f;
        protected override float SENTRY_RECALL_MAX_DIST => 4200f;
        protected override int ONGROUND_CNT_THRESHOLD => 10;
        protected bool BladShotInited = false;
        protected bool SoundPlayed = false;
        protected Vector2 CursorPos;

        protected override void CustomSentryRecall(SentryRecallInfo info)
        {
            var sentry = Main.projectile[info.ID];
            if (!info.AnchorInited)
            {
                // Main.NewText("Sentry Recall Inited:"+info.ID);
                if(info.TileCollide) info.TargetPos = MinionAIHelper.SearchForGround(info.TargetPos+new Vector2(0, 100f), 10, 16, (int)(sentry.height * 0.5f));
                info.AnchorInited = true;
                if(Projectile.owner == Main.myPlayer)
                {
                    info.Anchor_ID = Projectile.NewProjectile(
                        Projectile.GetSource_FromAI(),
                        info.TargetPos,
                        Vector2.Zero,
                        ModProjectileID.OneTrueFlagAnchor,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                    Projectile proj = Main.projectile[info.Anchor_ID];
                    if (proj.ModProjectile is OneTrueFlagAnchor anchor_)
                    {
                        anchor_.sentryInfo = info;
                    }
                }
                if(!SoundPlayed)
                {
                    SoundEngine.PlaySound(ModSounds.HellpodSignal_2_1, Projectile.Center);
                    SoundPlayed = true;
                }
            }
        }

        public override void AI()
        {
            base.AI();
            if(State == WAVE_STATE)
            {
                // bool bladeShotExists = false;
                // for(int i = 0; i < Main.maxProjectiles; i++)
                // {
                //     Projectile projectile = Main.projectile[i];
                //     if(projectile.type == ModProjectileID.OneTrueFlagBladeShot && projectile.active && projectile.owner == Projectile.owner)
                //     {
                //         bladeShotExists = true;
                //         break;
                //     }
                // }
                Player player = Main.player[Projectile.owner];
                if(!BladShotInited/*  && !bladeShotExists */ && Projectile.owner == Main.myPlayer)
                {
                    Projectile bladeShot = Projectile.NewProjectileDirect(
                        Projectile.GetSource_FromAI(),
                        Projectile.Center,
                        Vector2.Zero,
                        ModProjectileID.OneTrueFlagBladeShot,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        (float)(TIME_LEFT_WAVE / AttackSpeed/* player.itemAnimationMax */),
                        Projectile.identity
                    );
                    BladShotInited = true;
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            // writer.Write(BladShotInited);
            writer.Write(CursorPos.X);
            writer.Write(CursorPos.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            // BladShotInited = reader.ReadBoolean();
            float CursorPosX = reader.ReadSingle();
            float CurosrPosY = reader.ReadSingle();
            CursorPos = new Vector2(CursorPosX, CurosrPosY);
        }
    }
}
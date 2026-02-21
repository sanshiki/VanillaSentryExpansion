using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
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
    public class SantaFlagProjectile : FlagProjectile
    {
        protected override string FLAG_CLOTH_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/SantaFlag";
        protected override int FLAG_WIDTH => 120;
        protected override int FLAG_HEIGHT => 78;
        protected override float TAIL_OFFSET_X_1 => -33f;
        protected override float TAIL_OFFSET_Y_1 => -90f;  
        protected override float TAIL_OFFSET_X_2 => -33f;  
        protected override float TAIL_OFFSET_Y_2 => -63f;
        protected override Color TAIL_COLOR => new Color(118, 49, 48, 100);
        protected override float GRAVITY => 1.75f;
        protected override float MAX_FALL_SPEED => 30f;
        protected override float SENTRY_RECALL_SPEED => 46f;
        protected override float SENTRY_RECALL_THRESHOLD => 40f;
        protected override float SENTRY_RECALL_DECAY_DIST => 1000f;
        protected override float SENTRY_RECALL_MAX_DIST => 4000f;
        protected override int ONGROUND_CNT_THRESHOLD => 10;
        protected override float RECALL_SPEED => 40f;
        protected override bool TAIL_DYNAMIC_DEBUG => false;
        // protected override bool TAIL_ENABLE_GLOBAL => false;
        protected override int FULLY_CHARGED_DUST => DustID.FrostHydra;
        protected override int ENHANCE_BUFF_ID => ModBuffID.SantaFlagBuff;
        protected override int NPC_DEBUFF_ID => BuffID.MaceWhipNPCDebuff;
        protected override int ENHANCE_BUFF_DURATION => 60*4;
        protected override int ENHANCE_BUFF_DURATION_PLANTED => 60*5;
        protected override bool AUTO_READD_BUFF_ON_PLANT => true;
        protected override bool USE_CURSOR_ASSISTED_PLANT => true;
        protected bool isCharged = false;
        protected bool BladShotInited = false;
        protected Vector2 CursorPos;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (isCharged)
            {
                target.AddBuff(BuffID.Frostburn, 3 * 60);
            }
            base.OnHitNPC(target, hit, damageDone);
        }
        public override void AI()
        {
            if (!BladShotInited)
            {
                if(Projectile.owner == Main.myPlayer)
                    CursorPos = Main.MouseWorld;
                BladShotInited = true;
                Projectile.netUpdate = true;
            }

            base.AI();
            Player player = Main.player[Projectile.owner];
            if (State == WAVE_STATE && Projectile.timeLeft == TIME_LEFT_WAVE / 2 && isCharged)
            {
                Vector2 direction = Vector2.Normalize(CursorPos - player.Center);
                if(Projectile.owner == Main.myPlayer)
                {
                    Projectile bladeShot = Projectile.NewProjectileDirect(
                        Projectile.GetSource_FromAI(),
                        player.Center + direction * PoleLength * 0.8f,
                        Vector2.Normalize(CursorPos - player.Center) * 6f * player.whipRangeMultiplier,
                        ModProjectileID.SantaFlagBladeShot,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                    if(isCharged) bladeShot.ai[0] = 1f;
                }
            }
            if (player.HasBuff(ModBuffID.SantaFlagBuff))
            {
                if(!isCharged) Projectile.netUpdate = true;
                isCharged = true;
            }
            else
            {
                if(isCharged) Projectile.netUpdate = true;
                isCharged = false;
            }
        }
        protected override void CreateDustEffect(Player player)
        {
            if (isCharged && (State == WAVE_STATE || State == RECALL_STATE))
            {
                int dustType = 135;
                if (Main.rand.NextFloat() < 0.75f)
                {
                    Vector2 DustCenter = Projectile.Center + new Vector2(0, -(PoleLength - FLAG_HEIGHT) / 2f).RotatedBy(Projectile.rotation);
                    Dust dust = Dust.NewDustDirect(DustCenter - new Vector2(FLAG_HEIGHT / 2f, FLAG_HEIGHT / 2f), FLAG_HEIGHT, FLAG_HEIGHT, dustType, 0f, 0f, 0, default, 2f);
                    dust.noGravity = true;
                    dust.velocity = (DustCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * MinionAIHelper.RandomFloat(0f, 3f);
                    dust.fadeIn = 1f;
                }
            }
            base.CreateDustEffect(player);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(isCharged);
            writer.Write(BladShotInited);
            writer.Write(CursorPos.X);
            writer.Write(CursorPos.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            isCharged = reader.ReadBoolean();
            BladShotInited = reader.ReadBoolean();
            float CursorPosX = reader.ReadSingle();
            float CurosrPosY = reader.ReadSingle();
            CursorPos = new Vector2(CursorPosX, CurosrPosY);
        }
    }
}
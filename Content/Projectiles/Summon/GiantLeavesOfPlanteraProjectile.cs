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
    public class GiantLeavesOfPlanteraProjectile : FlagProjectile
    {
        protected override string FLAG_CLOTH_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/GiantLeavesOfPlantera";
        protected override int FLAG_WIDTH => 112;
        protected override int FLAG_HEIGHT => 66;
        protected override float TAIL_OFFSET_X_1 => -33f;
        protected override float TAIL_OFFSET_Y_1 => -90f;  
        protected override float TAIL_OFFSET_X_2 => -33f;  
        protected override float TAIL_OFFSET_Y_2 => -63f;   
        protected override Color TAIL_COLOR => new Color(200, 246, 5, 60);
        protected override bool TAIL_DYNAMIC_DEBUG => false;
        protected override float GRAVITY => 1.6f;
        protected override float MAX_FALL_SPEED => 27.5f;
        // protected override bool TAIL_ENABLE_GLOBAL => false;
        protected override int FULLY_CHARGED_DUST => 110;
        protected override int ENHANCE_BUFF_ID => ModBuffID.TikiFlagBuff;
        protected override int NPC_DEBUFF_ID => ModBuffID.TikiFlagDebuff;
        protected override int ENHANCE_BUFF_DURATION => 60*4;
        protected override bool AUTO_READD_BUFF_ON_PLANT => true;
        protected override bool USE_CURSOR_ASSISTED_PLANT => true;
        protected override bool USE_CUSTOM_SENTRY_RECALL => true;
        protected override float SENTRY_RECALL_SPEED => 42f;
        protected override float SENTRY_RECALL_THRESHOLD => 40f;
        protected override float SENTRY_RECALL_DECAY_DIST => 900f;
        protected override float SENTRY_RECALL_MAX_DIST => 3700f;
        protected override int ONGROUND_CNT_THRESHOLD => 15;
        protected bool isCharged = false;

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
                        ModProjectileID.GiantLeavesOfPlanteraAnchor,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                    Projectile proj = Main.projectile[info.Anchor_ID];
                    if (proj.ModProjectile is GiantLeavesOfPlanteraAnchor anchor_)
                    {
                        anchor_.sentryInfo = info;
                    }
                }
            }
        }

        public override void AI()
        {
            base.AI();
            Player player = Main.player[Projectile.owner];
            if (State == WAVE_STATE && isCharged)
            {
                float wave_ratio = Projectile.timeLeft / (float)player.itemAnimationMax;
                float wave_ratio_mean = 0.6f; // DynamicParamManager.QuickGet("WaveRatioMean", 0.6f, 0f, 1f).value;
                float wave_ratio_range = 0.1f; //DynamicParamManager.QuickGet("WaveRatioRange", 0.2f, 0f, 1f).value;
                if(/* Projectile.timeLeft % 2 == 0 &&  */wave_ratio >= wave_ratio_mean - wave_ratio_range && wave_ratio <= wave_ratio_mean + wave_ratio_range)
                {
                    Vector2 direction = Vector2.Normalize(Projectile.Center - player.Center);
                    if(Projectile.owner == Main.myPlayer)
                    {
                        Projectile bullet = Projectile.NewProjectileDirect(
                            Projectile.GetSource_FromAI(),
                            player.Center + direction * PoleLength * 0.8f,
                            Vector2.Normalize(Projectile.Center - player.Center) * 6f,
                            MinionAIHelper.RandomBool() ? ModProjectileID.GiantLeavesOfPlanteraBullet1 : ModProjectileID.GiantLeavesOfPlanteraBullet2,
                            (int)(Projectile.damage * 0.75f),
                            2,
                            Projectile.owner
                        );
                    }
                }
            }
            if (player.HasBuff(ModBuffID.TikiFlagBuff))
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
            if (State == WAVE_STATE || State == RECALL_STATE)
            {
                if(isCharged)
                {
                    int dustType = MinionAIHelper.RandomBool() ? 40 : 145;
                    if (Main.rand.NextFloat() < 0.65f)
                    {
                        Vector2 DustCenter = Projectile.Center + new Vector2(0, -(PoleLength - FLAG_HEIGHT) / 2f).RotatedBy(Projectile.rotation);
                        Dust dust = Dust.NewDustDirect(DustCenter - new Vector2(FLAG_HEIGHT / 2f, FLAG_HEIGHT / 2f), FLAG_HEIGHT, FLAG_HEIGHT, dustType, 0f, 0f, 0, default, 1f);
                        dust.scale = MinionAIHelper.RandomFloat(1f, 1.5f);
                        dust.velocity = (DustCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * MinionAIHelper.RandomFloat(0f, 3f);
                    }
                }
                else
                {
                    int dustType = MinionAIHelper.RandomBool() ? 40 : 145;
                    if (Main.rand.NextFloat() < 0.15f)
                    {
                        Vector2 DustCenter = Projectile.Center + new Vector2(0, -(PoleLength - FLAG_HEIGHT) / 2f).RotatedBy(Projectile.rotation);
                        Dust dust = Dust.NewDustDirect(DustCenter - new Vector2(FLAG_HEIGHT / 2f, FLAG_HEIGHT / 2f), FLAG_HEIGHT, FLAG_HEIGHT, dustType, 0f, 0f, 0, default, 1f);
                        dust.scale = MinionAIHelper.RandomFloat(1f, 1.5f);
                        dust.velocity = (DustCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * MinionAIHelper.RandomFloat(0f, 3f);
                    }
                }
            }
            base.CreateDustEffect(player);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(isCharged);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            isCharged = reader.ReadBoolean();
        }
    }
}
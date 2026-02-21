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
using Humanizer;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class HellFlagProjectile : FlagProjectile
    {
        protected override string FLAG_CLOTH_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/HellFlag";
        protected override int FLAG_WIDTH => 90;
        protected override int FLAG_HEIGHT => 58;
        protected override float TAIL_OFFSET_X_1 => -33f;
        protected override float TAIL_OFFSET_Y_1 => -90f;  
        protected override float TAIL_OFFSET_X_2 => -33f;  
        protected override float TAIL_OFFSET_Y_2 => -63f;   
        protected override Color TAIL_COLOR => new Color(190, 32, 3, 100);
        protected override bool TAIL_DYNAMIC_DEBUG => false;
        protected override float GRAVITY => 1.0f;
        protected override float MAX_FALL_SPEED => 20f;
        protected override bool USE_CURSOR_ASSISTED_PLANT => true;
        protected override bool USE_CUSTOM_SENTRY_RECALL => true;
        protected override float SENTRY_RECALL_SPEED => 35f;
        protected override float SENTRY_RECALL_THRESHOLD => 40f;
        protected override float SENTRY_RECALL_DECAY_DIST => 600f;
        protected override float SENTRY_RECALL_MAX_DIST => 3250f;
        protected override int ONGROUND_CNT_THRESHOLD => 15;
        // protected override bool TAIL_ENABLE_GLOBAL => false;
        protected override int FULLY_CHARGED_DUST => 182;
        protected override int ENHANCE_BUFF_ID => ModBuffID.HellFlagBuff;
        protected override int NPC_DEBUFF_ID => BuffID.BoneWhipNPCDebuff;
        private bool isCharged = false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (isCharged)
            {
                target.AddBuff(BuffID.OnFire, 3 * 60);
            }
            base.OnHitNPC(target, hit, damageDone);
        }

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
                        ModProjectileID.HellFlagAnchor,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                    Projectile proj = Main.projectile[info.Anchor_ID];
                    if (proj.ModProjectile is HellFlagAnchor anchor_)
                    {
                        anchor_.sentryInfo = info;
                    }
                }
            }
        }

        public override void AI()
        {
            base.AI();
            Player owner = Main.player[Projectile.owner];
            if (owner.HasBuff(ModBuffID.HellFlagBuff))
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
                int dustType = DustID.FlameBurst;
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
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            isCharged = reader.ReadBoolean();
        }
    }
}
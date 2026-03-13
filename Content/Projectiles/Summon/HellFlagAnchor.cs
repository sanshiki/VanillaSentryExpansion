using Microsoft.Xna.Framework;
using System;
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
using Terraria.Graphics.CameraModifiers;
using System.IO;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class HellFlagAnchor : ModProjectile, IRecallSentryAnchor
    {
        private const bool DEBUG_RECALL_SYNC = false;
        public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.JimsDrone;

        private const int BASE_WAIT_TIME = 35;
        private const float DIST_FACTOR = 0.0075f;
        private const int ANCHOR_TIMELEFT = 60*10;

        // dust: 259 235

        private int WaitTimer = 0;
        private int RandomWaitTime = 0;

        public ProjectileReference SentryRef;
        public Vector2 TargetPos;
        public bool OriginalTileCollide;
        public bool Configured;
        private bool LoggedConfigured;
        private bool LoggedUnconfigured;
        private bool LoggedTeleport;
        /// <summary> 是否已根据 RandomWaitTime 与距离设置过 timeLeft，避免重复设置且保证多人一致 </summary>
        private bool TeleportTimeLeftSet;

        private void LogDebug(string message)
        {
            if (!DEBUG_RECALL_SYNC)
            {
                return;
            }
            Mod.Logger.Info($"[HellFlagAnchor] {message}");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.JimsDrone);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = ANCHOR_TIMELEFT;
            Projectile.alpha = 255;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            SentryRef.Clear();
            TargetPos = Vector2.Zero;
            OriginalTileCollide = true;
            Configured = false;
        }

        public void Configure(ProjectileReference sentryRef, Vector2 targetPos, bool originalTileCollide)
        {
            SentryRef = sentryRef;
            TargetPos = targetPos;
            OriginalTileCollide = originalTileCollide;
            Configured = true;
            LogDebug(
                $"Configure anchorWho={Projectile.whoAmI} owner={Projectile.owner} mode={Main.netMode} " +
                $"sentryIdentity={SentryRef.Identity} sentryWho={SentryRef.WhoAmI} target={TargetPos} tile={OriginalTileCollide}");
        }

        public override void OnSpawn(IEntitySource source)
        {
            // 仅 owner 生成随机数，再通过 SendExtraAI/ReceiveExtraAI 同步，保证多人一致
            if (Projectile.owner == Main.myPlayer)
                RandomWaitTime = Main.rand.Next(0, 20);
        }

        public override void AI()
        {
            if(Projectile.owner == Main.myPlayer)
            {
                if(ANCHOR_TIMELEFT - Projectile.timeLeft == 2) Projectile.netUpdate = true;
            }
            if (!Configured)
            {
                if (!LoggedUnconfigured && WaitTimer % 30 == 0)
                {
                    LogDebug($"WaitingConfig anchorWho={Projectile.whoAmI} owner={Projectile.owner} mode={Main.netMode}");
                    LoggedUnconfigured = true;
                }
                WaitTimer++;
                return;
            }

            if (!LoggedConfigured)
            {
                LogDebug(
                    $"ConfiguredStart anchorWho={Projectile.whoAmI} owner={Projectile.owner} mode={Main.netMode} " +
                    $"sentryIdentity={SentryRef.Identity} sentryWho={SentryRef.WhoAmI} target={TargetPos}");
                LoggedConfigured = true;
            }

            if (Configured)
            {
                Projectile sentry = SentryRef.Get();
                int sentryWidth = sentry != null && sentry.active ? sentry.width : 32;
                int sentryHeight = sentry != null && sentry.active ? sentry.height : 32;
                WaitTimer++;
                float visualDist = sentry != null && sentry.active ? sentry.Center.Distance(TargetPos) : 0f;

                // 仅 owner 设置一次 timeLeft，保证多人用同一随机数；其他客户端通过 projectile 同步得到 timeLeft
                if (Projectile.owner == Main.myPlayer && !TeleportTimeLeftSet)
                {
                    int totalWait = BASE_WAIT_TIME + (int)(visualDist * DIST_FACTOR) + RandomWaitTime;
                    Projectile.timeLeft = totalWait;
                    TeleportTimeLeftSet = true;
                    Projectile.netUpdate = true;
                }

                // create dust effect
                Dust dust3;
                int dust_id3 = MinionAIHelper.RandomBool() ? 235 : 259;
                Vector2 position3 = TargetPos + new Vector2(-sentryWidth * 0.5f, -sentryHeight);
                dust3 = Main.dust[Terraria.Dust.NewDust(position3, sentryWidth, sentryHeight, dust_id3, 0f, -4.6511626f, 0, new Color(255,255,255), 1f)];
                dust3.noGravity = true;
                dust3.fadeIn = 0.6f;
                dust3.velocity.X = 0f;
            }

            

        
        }

        public override void OnKill(int timeLeft)
        {
            if (!Configured)
                return;
            Projectile sentry = SentryRef.Get();
            int sentryWidth = sentry != null && sentry.active ? sentry.width : 32;
            int sentryHeight = sentry != null && sentry.active ? sentry.height : 32;

            for (int i = 0; i < 6; i++)
            {
                int dust_id1 = MinionAIHelper.RandomBool() ? 235 : 259;
                Vector2 position1 = TargetPos + new Vector2(-sentryWidth * 0.5f, -sentryHeight);
                Dust dust1 = Main.dust[Terraria.Dust.NewDust(position1, sentryWidth, sentryHeight, dust_id1, 0f, 0f, 0, new Color(255, 255, 255), 1f)];
                dust1.noGravity = true;
            }

            if (sentry != null && sentry.active)
            {
                for (int i = 0; i < 6; i++)
                {
                    int dust_id2 = MinionAIHelper.RandomBool() ? 235 : 259;
                    Vector2 position2 = sentry.Center + new Vector2(-sentry.width * 0.5f, -sentry.height);
                    Dust dust2 = Main.dust[Terraria.Dust.NewDust(position2, sentry.width, sentry.height, dust_id2, 0f, 0f, 0, new Color(255, 255, 255), 1f)];
                    dust2.noGravity = true;
                }
            }

            if (!LoggedTeleport)
            {
                LogDebug($"VisualComplete anchorWho={Projectile.whoAmI} mode={Main.netMode} target={TargetPos}");
                LoggedTeleport = true;
            }

            SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/dd2_flameburst_tower_shot_2") with { Volume = .47f, Pitch = .74f, PitchVariance = .72f };
            SoundEngine.PlaySound(style, Projectile.Center);

        }

        public override bool MinionContactDamage()
		{
			return false;
		}

        public override void SendExtraAI(BinaryWriter writer)
        {
            SentryRef.SendExtraAI(writer);
            writer.Write(TargetPos.X);
            writer.Write(TargetPos.Y);
            writer.Write(OriginalTileCollide);
            writer.Write(Configured);
            writer.Write((short)RandomWaitTime);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SentryRef.ReceiveExtraAI(reader);
            float targetPosX = reader.ReadSingle();
            float targetPosY = reader.ReadSingle();
            TargetPos = new Vector2(targetPosX, targetPosY);
            OriginalTileCollide = reader.ReadBoolean();
            Configured = reader.ReadBoolean();
            RandomWaitTime = reader.ReadInt16();
            LogDebug(
                $"ReceiveExtraAI anchorWho={Projectile.whoAmI} owner={Projectile.owner} mode={Main.netMode} " +
                $"configured={Configured} sentryIdentity={SentryRef.Identity} sentryWho={SentryRef.WhoAmI} target={TargetPos} tile={OriginalTileCollide}");
        }
    }
}
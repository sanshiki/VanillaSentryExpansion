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

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class GiantLeavesOfPlanteraAnchor : ModProjectile
    {
        public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.JimsDrone;

        private const int BASE_WAIT_TIME = 20;
        private const float DIST_FACTOR = 0.0075f;

        // dust: 259 235

        private int WaitTimer = 0;
        private int RandomWaitTime = 0;

        public SentryRecallInfo sentryInfo;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.JimsDrone);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 60*10;
            Projectile.alpha = 255;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            RandomWaitTime = Main.rand.Next(0, 20);
        }

        public override void AI()
        {
            if(sentryInfo != null)
            {
                var sentry = Main.projectile[sentryInfo.ID];
                WaitTimer++;
                if (WaitTimer >= BASE_WAIT_TIME + (int)(sentry.Center.Distance(sentryInfo.TargetPos) * DIST_FACTOR) + RandomWaitTime)
                {

                    // create teleport dust effect
                    for(int i = 0; i < 6; i++)
                    {
                        int dust_id1 = MinionAIHelper.RandomBool() ? 40 : 145;
                        Vector2 position1 = sentryInfo.TargetPos + new Vector2(-sentry.width * 0.5f, -sentry.height * 0.5f);
                        Dust dust1 = Main.dust[Terraria.Dust.NewDust(position1, sentry.width, sentry.height, dust_id1, 0f, 0f, 0, new Color(255,255,255), 1f)];
                        // dust1.noGravity = true;
                    }

                    for(int i = 0; i < 6; i++)
                    {
                        int dust_id2 = MinionAIHelper.RandomBool() ? 40 : 145;
                        Vector2 position2 = sentry.Center + new Vector2(-sentry.width * 0.5f, -sentry.height * 0.5f);
                        Dust dust2 = Main.dust[Terraria.Dust.NewDust(position2, sentry.width, sentry.height, dust_id2, 0f, 0f, 0, new Color(255,255,255), 1f)];
                        // dust2.noGravity = true;
                    }
                    
                    if (sentry != null && sentry.active)
                    {
                        sentry.Center = sentryInfo.TargetPos + new Vector2(0, -sentry.height * 0.5f);
                        sentry.velocity = sentryInfo.TileCollide ? new Vector2(0, 20f) : Vector2.Zero;
                        sentryInfo.IsRecalled = true;
                        sentry.netUpdate = true;
                    }

                    SoundEngine.PlaySound(SoundID.Grass,Projectile.Center);

                    Projectile.Kill();
                }

                float factor = 0.3f; // DynamicParamManager.QuickGet("DustSinFactor", 0.3f, 0.1f, 1f).value;
                float spd_factor = 0.13f; // DynamicParamManager.QuickGet("DustSpeedFactor", 0.13f, 0.1f, 10f).value;

                // create dust effect
                Dust dust3;
                int dust_id3 = MinionAIHelper.RandomBool() ? 40 : 145;
                float dust_x = (float)Math.Sin(WaitTimer * factor) * sentry.width * 0.5f;
                float dust_y = /* sentry.height * 0.5f */ 0f;
                float dust_speed = (float)Math.Min(sentry.height, 60f) * spd_factor;
                Vector2 position3 = sentryInfo.TargetPos + new Vector2(-dust_x, -dust_y);
                dust3 = Terraria.Dust.NewDustPerfect(position3, dust_id3, new Vector2(0f, -dust_speed), 0, new Color(255,255,255), 1f);
                dust3.noGravity = true;
                dust3.fadeIn = 0.6f;

                Dust dust4;
                int dust_id4 = MinionAIHelper.RandomBool() ? 40 : 145;
                float dust_x2 = (float)Math.Cos(WaitTimer * factor) * sentry.width * 0.5f;
                float dust_y2 = /* sentry.height * 0.5f */ 0f;
                float dust_speed2 = (float)Math.Min(sentry.height, 60f) * spd_factor;
                Vector2 position4 = sentryInfo.TargetPos + new Vector2(-dust_x2, -dust_y2);
                dust4 = Terraria.Dust.NewDustPerfect(position4, dust_id4, new Vector2(0f, -dust_speed2), 0, new Color(255,255,255), 1f);
                dust4.noGravity = true;
                dust4.fadeIn = 0.6f;

            }

            

        
        }

        public override bool MinionContactDamage()
		{
			return false;
		}
    }
}
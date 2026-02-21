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
    public class HellFlagAnchor : ModProjectile
    {
        public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.JimsDrone;

        private const int BASE_WAIT_TIME = 35;
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
                        int dust_id1 = MinionAIHelper.RandomBool() ? 235 : 259;
                        Vector2 position1 = sentryInfo.TargetPos + new Vector2(-sentry.width * 0.5f, -sentry.height);
                        Dust dust1 = Main.dust[Terraria.Dust.NewDust(position1, sentry.width, sentry.height, dust_id1, 0f, 0f, 0, new Color(255,255,255), 1f)];
                        dust1.noGravity = true;
                    }

                    for(int i = 0; i < 6; i++)
                    {
                        int dust_id2 = MinionAIHelper.RandomBool() ? 235 : 259;
                        Vector2 position2 = sentry.Center + new Vector2(-sentry.width * 0.5f, -sentry.height);
                        Dust dust2 = Main.dust[Terraria.Dust.NewDust(position2, sentry.width, sentry.height, dust_id2, 0f, 0f, 0, new Color(255,255,255), 1f)];
                        dust2.noGravity = true;
                    }
                    
                    if (sentry != null && sentry.active)
                    {
                        sentry.Center = sentryInfo.TargetPos + new Vector2(0, -sentry.height * 0.5f);
                        sentry.velocity = sentryInfo.TileCollide ? new Vector2(0, 20f) : Vector2.Zero;
                        sentryInfo.IsRecalled = true;
                        sentry.netUpdate = true;
                    }

                    SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/dd2_flameburst_tower_shot_2") with { Volume = .47f,  Pitch = .74f,  PitchVariance = .72f, };
                    SoundEngine.PlaySound(style,Projectile.Center);

                    Projectile.Kill();
                }

                // create dust effect
                Dust dust3;
                int dust_id3 = MinionAIHelper.RandomBool() ? 235 : 259;
                Vector2 position3 = sentryInfo.TargetPos + new Vector2(-sentry.width * 0.5f, -sentry.height);
                dust3 = Main.dust[Terraria.Dust.NewDust(position3, sentry.width, sentry.height, dust_id3, 0f, -4.6511626f, 0, new Color(255,255,255), 1f)];
                dust3.noGravity = true;
                dust3.fadeIn = 0.6f;
                dust3.velocity.X = 0f;
            }

            

        
        }

        public override bool MinionContactDamage()
		{
			return false;
		}
    }
}
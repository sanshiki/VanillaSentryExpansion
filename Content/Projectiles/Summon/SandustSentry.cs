using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class SandustSentry : ModProjectile
    {
        /* -------------------- constants -------------------- */
        // frame speed constants
        private const int NORMAL_FRAME_SPEED = 15;
        private const int SHOOT_FRAME_SPEED = 5;
        private const int FRAME_COUNT = 4;

        // shoot interval constants
        private const int SHOOT_INTERVAL = 120;
        private const int SHOOT_INTERVAL_FAST = 90;
        
        // gravity constants
        public static float Gravity = ModGlobal.SENTRY_GRAVITY;
        public static float MaxGravity = 20f;

        // bullet constants
        private const float BULLET_SPEED_Y = 55f;
        private const float BULLET_SPEED_X = 20f;
        private const float BULLET_SPEED = 60f;
        private const float BULLET_GRAVITY = 2.0f;
        private const float MAX_LEGAL_HEIGHT = BULLET_SPEED_Y * BULLET_SPEED_Y / (2 * BULLET_GRAVITY)*0.8f;
        
        public override string Texture => "SummonerExpansionMod/Assets/Textures/Projectiles/SandustSentryV2";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = FRAME_COUNT;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 58;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.sentry = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            int shootTimer = (int)Projectile.ai[0];
            int shootInterval = SHOOT_INTERVAL;
            Player owner = Main.player[Projectile.owner];
            
            // apply gravity
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxGravity)
            {
                Projectile.velocity.Y = MaxGravity;
            }

            // Targeting
            NPC target = MinionAIHelper.SearchForTargets(
                owner, 
                Projectile, 
                1000f, 
                false, 
                n => n.Center.Y < Projectile.Center.Y + MAX_LEGAL_HEIGHT).TargetNPC;

            if (target != null)
            {
                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if (shootTimer == 0)
                {
                    // Fire!
                    Vector2 ShootOffset = new Vector2(0f, -15f);
                    Vector2 ShootCenter = Projectile.Center + ShootOffset;
                    
                    Vector2 velocity = MinionAIHelper.PredictVelocityWithGravity(Projectile.Center, target.Center, target.velocity, BULLET_SPEED_Y, BULLET_GRAVITY, 60, 1);

                    if(velocity.X > BULLET_SPEED_X)
                    {
                        velocity.X = BULLET_SPEED_X;
                    }
                    if(velocity.X < -BULLET_SPEED_X)
                    {
                        velocity.X = -BULLET_SPEED_X;
                    }

                    if(Projectile.owner == Main.myPlayer)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(
                            Projectile.GetSource_FromAI(),
                            ShootCenter,
                            velocity,
                            ModProjectileID.SandustSentryBullet,
                            Projectile.damage,
                            Projectile.knockBack,
                            Projectile.owner);
                    }

                    shootTimer = 0; // Reset shoot animation
                    Projectile.netUpdate = true;


                    SoundEngine.PlaySound(SoundID.Item95, Projectile.position);
                }
            }

            shootTimer++;
            if(shootTimer >= shootInterval)
                shootTimer = shootInterval;

            // Animation
            UpdateAnimation(target, shootTimer);

            Projectile.ai[0] = (float)shootTimer;
        }

        private void UpdateAnimation(NPC target, int shootTimer)
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > NORMAL_FRAME_SPEED)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= FRAME_COUNT)
                {
                    Projectile.frame = 0;
                }
            }

            if (Main.rand.NextFloat() < 0.1f)
            {
                Dust dust;
                int dustType = MinionAIHelper.RandomBool() ? 283 : 133;
                float offset = dustType == 283 ? -12f : -21f;
                Vector2 position = Projectile.position+new Vector2(0f, offset);
                dust = Main.dust[Terraria.Dust.NewDust(position, Projectile.width, 5, dustType, 0f, -4.6511626f, 0, new Color(255,255,255), 1f)];
            }


        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity.X = 0f;
            Projectile.netUpdate = true;

            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override bool MinionContactDamage()
		{
			return false;
		}


    }
}
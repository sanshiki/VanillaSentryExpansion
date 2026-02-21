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
    public class BunnySentry : ModProjectile
    {
        /* ----------------- constants ----------------- */
        // frame speed constants
        private const int NORMAL_FRAME_SPEED = 20;
        private const int SHOOT_FRAME_SPEED = 5;

        // shoot interval
        private const int SHOOT_INTERVAL = 35;
        private const int INIT_SHOOT_CNT = 4;

        // gravity constants
        public const float Gravity = ModGlobal.SENTRY_GRAVITY;
        public const float MaxGravity = 20f;

        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/BunnySentry";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 26;
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

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.ai[0] = (float)INIT_SHOOT_CNT;
        }

        public override void AI()
        {
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
                    600f, 
                    true, 
                    null).TargetNPC;

            int shootTimer = (int)Projectile.ai[0];

            // Animation
            UpdateAnimation(target, shootTimer);

            int shootInterval = SHOOT_INTERVAL;
            if (target != null)
            {
                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if (shootTimer == 0)
                {
                    // Fire!
                    Vector2 bulletOffset = new Vector2(-18f * Projectile.spriteDirection, 7f);
                    Vector2 direction = target.Center - Projectile.Center - bulletOffset;
                    float distance = direction.Length();
                    direction.Normalize();
                    direction *= 10f; // Bullet speed
                    direction.Y -= distance * 0.002f;

                    
                    if (Projectile.owner == Main.myPlayer)
                    {
                        Projectile seed = Projectile.NewProjectileDirect(
                            Projectile.GetSource_FromAI(),
                            Projectile.Center + bulletOffset,
                            direction,
                            ModProjectileID.BunnySentryBullet,
                            Projectile.damage,
                            Projectile.knockBack,
                            Projectile.owner);
                    }


                    shootTimer = 0; // Reset shoot animation

                    SoundStyle style = new SoundStyle("Terraria/Sounds/Item_11") with { Volume = .7f,  Pitch = .72f,  PitchVariance = .26f, };
                    SoundEngine.PlaySound(style, Projectile.Center);

                    Projectile.netUpdate = true;
                }
            }

            shootTimer++;
            if(shootTimer >= shootInterval)
                shootTimer = shootInterval;

            Projectile.ai[0] = (float)shootTimer;
        }

        private void UpdateAnimation(NPC target, int shootTimer)
        {
            // Projectile.frameCounter++;
            if (target != null)
            {
                if (shootTimer >= 0 && shootTimer <= 3)
                {
                    // Shooting animation
                    Projectile.frame = 1; // Frame 3
                }
                else
                {
                    Projectile.frame = 0;
                }
                // face towards target
                Vector2 direction = target.Center - Projectile.Center;
                direction.Normalize();
                Projectile.spriteDirection = direction.X > 0 ? -1 : 1;
            }
            else
            {
                Projectile.frame = 0;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Projectile.velocity = Vector2.Zero;
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
using Microsoft.Xna.Framework;
using System;
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
    public class QueenSpiderOverride : IProjectileOverride
    {
        private const int SHOOT_INTERVAL = 60;
        private const float BULLET_SPEED = 17f;
        private const float COMPENSATE_ANGLE = 20f * ModGlobal.DEG_TO_RAD_FLOAT;
        private const float ANGLE_STEP = 22.5f * ModGlobal.DEG_TO_RAD_FLOAT;

        private int shootTimer = 0;

        private Vector2 direction = new Vector2(0, -1);

        public QueenSpiderOverride()
        {
            RegisterFlags["SetDefaults"] = true;
            RegisterFlags["PreAI"] = true;
            RegisterFlags["OnTileCollide"] = true;
            RegisterFlags["TileCollideStyle"] = true;
        }

        public override void SetDefaults(Projectile projectile)
        {
			projectile.width = 66;
			projectile.height = 50;
			projectile.aiStyle = -1;
			projectile.timeLeft = Projectile.SentryLifeTime;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.manualDirectionChange = true;
            projectile.sentry = true;
            projectile.netImportant = true;
            projectile.DamageType = DamageClass.Summon;
        }

        private void UpdateAnimation(Projectile projectile)
        {
            float angle = direction.ToRotation();
            if(angle >= -ANGLE_STEP/2f && angle < ModGlobal.PI_FLOAT/2f)
            {
                projectile.frame = 0;
            }
            else if (angle > ModGlobal.PI_FLOAT/2f && angle <= ModGlobal.PI_FLOAT || (angle > -ModGlobal.PI_FLOAT && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f))
            {
                projectile.frame = 8;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + ANGLE_STEP)
            {
                projectile.frame = 7;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + ANGLE_STEP && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 2*ANGLE_STEP)
            {
                projectile.frame = 6;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 2*ANGLE_STEP && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 3*ANGLE_STEP)
            {
                projectile.frame = 5;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 3*ANGLE_STEP && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 4*ANGLE_STEP)
            {
                projectile.frame = 4;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 4*ANGLE_STEP && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 5*ANGLE_STEP)
            {
                projectile.frame = 3;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 5*ANGLE_STEP && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 6*ANGLE_STEP)
            {
                projectile.frame = 2;
            }
            else if (angle > -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 6*ANGLE_STEP && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f + 7*ANGLE_STEP)
            {
                projectile.frame = 1;
            }

            // Main.NewText("Current Frame:" + projectile.frame + " angle:" + angle);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            // projectile.velocity = Vector2.Zero;
            if(projectile.velocity.X != 0f) projectile.netUpdate = true;
            projectile.velocity.X = 0f;
            return true;
        }

        public override bool TileCollideStyle(Projectile projectile, ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override bool PreAI(Projectile projectile)
        {
            // apply gravity
            MinionAIHelper.ApplyGravity(projectile, ModGlobal.SENTRY_GRAVITY, ModGlobal.SENTRY_MAX_FALL_SPEED);

            // search for target
            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[projectile.owner], 
                projectile, 
                800f, 
                true, 
                n => (n.Center - projectile.Center).ToRotation() <= ModGlobal.PI_FLOAT/6f || (n.Center - projectile.Center).ToRotation() >= 5f*ModGlobal.PI_FLOAT/6f).TargetNPC;

            if(target != null)
            {
                // calculate direction
                direction = target.Center - projectile.Center;
                direction.Normalize();

                if(direction.ToRotation() <= -ModGlobal.PI_FLOAT/2f - COMPENSATE_ANGLE || direction.ToRotation() >= -ModGlobal.PI_FLOAT/2f + COMPENSATE_ANGLE)
                {
                    direction = direction.RotatedBy(COMPENSATE_ANGLE * (direction.X > 0f ? -1f : 1f));
                }
                // float predictedAngle = MinionAIHelper.PredictParabolaAngle(projectile, target, 0.2f, BULLET_SPEED);
                // direction = new Vector2(1, 0).RotatedBy(predictedAngle);
                if(shootTimer >= SHOOT_INTERVAL)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0)
                {

                    Vector2 ShootOffset = new Vector2(28f, 0).RotatedBy(direction.ToRotation()) + new Vector2(0, 12.5f);

                    Projectile.NewProjectileDirect(
                        projectile.GetSource_FromThis(),
                        projectile.Center + ShootOffset,
                        direction * BULLET_SPEED,
                        ProjectileID.SpiderEgg,
                        projectile.damage,
                        projectile.knockBack,
                        projectile.owner);
                }
            }

            shootTimer++;
            if(shootTimer >= SHOOT_INTERVAL)
            {
                shootTimer = SHOOT_INTERVAL;
            }

            UpdateAnimation(projectile);

            return false;
        }
    }

    public class SpiderEggOverride : IProjectileOverride
    {
        private const float GRAVITY = 0.2f;
        private const float COMPENSATE_ANGLE = 10f * ModGlobal.DEG_TO_RAD_FLOAT;
        private const float TRIGGER_RANGE = 200f;

        public SpiderEggOverride()
        {
            RegisterFlags["SetDefaults"] = true;
            RegisterFlags["PreAI"] = true;
        }

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 16;
            projectile.height = 16;
            projectile.friendly = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 60;
            projectile.scale = 0.9f;
        }

        public override bool PreAI(Projectile projectile)
        {
            MinionAIHelper.ApplyGravity(projectile, GRAVITY, 20f);

            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[projectile.owner],
                projectile,
                TRIGGER_RANGE,
                true).TargetNPC;

            if(target != null)
            {
                Vector2 BabySpiderDir = (target.Center - projectile.Center).SafeNormalize(Vector2.Zero);
                float targetAngle = BabySpiderDir.ToRotation();
                if(targetAngle <= -ModGlobal.PI_FLOAT/2f - COMPENSATE_ANGLE || targetAngle >= -ModGlobal.PI_FLOAT/2f + COMPENSATE_ANGLE)
                {
                    BabySpiderDir = BabySpiderDir.RotatedBy(COMPENSATE_ANGLE * (BabySpiderDir.X > 0f ? -1f : 1f));
                }

                projectile.velocity = BabySpiderDir * projectile.velocity.Length();
                projectile.Kill();
            }

            return true;
        }
    }
}
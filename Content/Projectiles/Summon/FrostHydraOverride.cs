using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System.IO;
using Terraria.ModLoader.IO;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class FrostHydraOverrdie : IProjectileOverride
    {
        private const int SHOOT_INTERVAL = 60;
        private const int SHOOT_ANIMATION_SPEED = 20;
        private const float REAL_BULLET_SPEED = 10f;
        private const float ANGLE_STEP = 22.5f * ModGlobal.DEG_TO_RAD_FLOAT;
        private int shootTimer = 0;
        private bool isShooting = false;

        private float directionAngle = 0f;

        public FrostHydraOverrdie()
        {
            RegisterFlags["SetDefaults"] = true;
            RegisterFlags["PreAI"] = true;
            RegisterFlags["OnTileCollide"] = true;
            RegisterFlags["TileCollideStyle"] = true;
            RegisterFlags["OnSpawn"] = true;
            RegisterFlags["SendExtraAI"] = true;
            RegisterFlags["ReceiveExtraAI"] = true;
        }

        public override void SetDefaults(Projectile projectile)
        {
			projectile.width = 80;
			projectile.height = 74;
			projectile.aiStyle = -1;
			projectile.light = 0.25f;
			projectile.timeLeft = Projectile.SentryLifeTime;
			projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.sentry = true;
            projectile.netImportant = true;
            projectile.DamageType = DamageClass.Summon;
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item46, projectile.Center);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            // projectile.velocity = Vector2.Zero;
            if(projectile.velocity.X != 0f) MinionAIHelper.SetProjectileNetUpdate(projectile);
            projectile.velocity.X = 0f;
            return true;
        }

        public override bool TileCollideStyle(Projectile projectile, ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        private void UpdateAnimation(Projectile projectile)
        {
            Vector2 direction = directionAngle.ToRotationVector2();
            float angle = directionAngle;
            int frameIdx = 0;
            projectile.spriteDirection =  direction.X > 0 ? 1 : -1;
            if((angle > -ANGLE_STEP/2f && angle <= ModGlobal.PI_FLOAT) || (angle > -ModGlobal.PI_FLOAT && angle <= -ModGlobal.PI_FLOAT + ANGLE_STEP/2f))
            {
                frameIdx = 0;
            }
            else
            {
                float shoot_angle = (float)Math.Atan2(Math.Abs(direction.Y), Math.Abs(direction.X));
                if(shoot_angle > ANGLE_STEP/2f && shoot_angle <= ANGLE_STEP/2f + ANGLE_STEP)
                {
                    frameIdx = 1;
                }
                else if(shoot_angle > ANGLE_STEP/2f + ANGLE_STEP && shoot_angle <= ANGLE_STEP/2f + 2*ANGLE_STEP)
                {
                    frameIdx = 2;
                }
                else if(shoot_angle > ANGLE_STEP/2f + 2*ANGLE_STEP && shoot_angle <= ANGLE_STEP/2f + 3*ANGLE_STEP)
                {
                    frameIdx = 3;
                }
                else if(shoot_angle > ANGLE_STEP/2f + 3*ANGLE_STEP && shoot_angle <= ANGLE_STEP/2f + 4*ANGLE_STEP)
                {
                    frameIdx = 4;
                }
            }
            if(isShooting)
            {
                projectile.frameCounter++;
                projectile.frame = frameIdx * 2 + 1;
                if(projectile.frameCounter >= SHOOT_ANIMATION_SPEED)
                {
                    projectile.frameCounter = 0;
                    isShooting = false;
                }
            }
            else
            {
                projectile.frame = frameIdx * 2;
                projectile.frameCounter = 0;
            }
        }

        public override bool PreAI(Projectile projectile)
        {
            // apply gravity
            MinionAIHelper.ApplyGravity(projectile, ModGlobal.SENTRY_GRAVITY, ModGlobal.SENTRY_MAX_FALL_SPEED);

            // search for target
            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[projectile.owner], 
                projectile, 
                1200f, 
                true, 
                n => (n.Center - projectile.Center).ToRotation() <= ModGlobal.PI_FLOAT/6f || (n.Center - projectile.Center).ToRotation() >= 5f*ModGlobal.PI_FLOAT/6f).TargetNPC;

            if(target != null)
            {
                Vector2 ShootCenter = projectile.Center;
                Vector2 direction = target.Center - projectile.Center;
                direction.Normalize();
                directionAngle = direction.ToRotation();

                if(shootTimer >= SHOOT_INTERVAL)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0)
                {
                    if(projectile.owner == Main.myPlayer)
                    {
                        Projectile bullet = Projectile.NewProjectileDirect(
                            projectile.GetSource_FromThis(),
                            ShootCenter,
                            direction * REAL_BULLET_SPEED,
                            ProjectileID.FrostBlastFriendly,
                            projectile.damage,
                            projectile.knockBack, 
                            projectile.owner);
                    }

                    isShooting = true;
                    MinionAIHelper.SetProjectileNetUpdate(projectile);
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

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(shootTimer);
            binaryWriter.Write(isShooting);
            binaryWriter.Write(directionAngle);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            shootTimer = binaryReader.ReadInt32();
            isShooting = binaryReader.ReadBoolean();
            directionAngle = binaryReader.ReadSingle();
        }
    }

    public class FrostBlastOverride : IProjectileOverride
    {
        private const float HOMING_RANGE = 1000f;
        private const float CONTROL_P = 1f;
        private const float CONTROL_D = 0.05f;
        private const float MAX_TURN_SPEED = 1.0f;

        private float lastError = 0f;

        private const float DAMAGE_DECAY_FACTOR = 0.8f;
        private int hitCount = 0;

        private int TargetId = -1;

        private bool HomeinEnabled = true;

        public FrostBlastOverride()
        {
            RegisterFlags["SetDefaults"] = true;
            RegisterFlags["AI"] = true;
            RegisterFlags["ModifyHitNPC"] = true;
            RegisterFlags["SendExtraAI"] = true;
            RegisterFlags["ReceiveExtraAI"] = true;
        }

        public override void SetDefaults(Projectile projectile)
        {
			projectile.width = 14;
			projectile.height = 14;
			projectile.aiStyle = 28;
			projectile.alpha = 255;
			projectile.penetrate = 3;
			projectile.friendly = true;
			projectile.extraUpdates = 3;
			projectile.coldDamage = true;
			projectile.usesIDStaticNPCImmunity = false;
			// projectile.idStaticNPCHitCooldown = 10;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 60;
        }

        public override void AI(Projectile projectile)
        {
            // get target
            // int targetId = (int)projectile.ai[0];
            // NPC target = targetId != -1 ? Main.npc[targetId] : null;

            // if(target == null) return;
            // if(!target.active) return;

            // if((target.Center - projectile.Center).Length() > HOMING_RANGE || (target.Center - projectile.Center).Length() < 50f || projectile.penetrate < 3)
            // {
            //     return;
            // }

            if(TargetId == -1)
            {
                NPC targetNPC = MinionAIHelper.SearchForTargets(
                    Main.player[projectile.owner], 
                    projectile, 
                    HOMING_RANGE, 
                    true, 
                    null).TargetNPC;

                TargetId = targetNPC != null ? targetNPC.whoAmI : -1;
            }

            NPC target = TargetId != -1 ? Main.npc[TargetId] : null;
            if(target == null || !target.active || (target.Center - projectile.Center).Length() > HOMING_RANGE || projectile.penetrate < 3)
            {
                if(HomeinEnabled) MinionAIHelper.SetProjectileNetUpdate(projectile);
                HomeinEnabled = false;
            }
            
            if(!HomeinEnabled) return;


            float direction = (target.Center - projectile.Center).ToRotation();
            float dir_err = direction - projectile.velocity.ToRotation();
            dir_err = MinionAIHelper.NormalizeAngle(dir_err);
            float turn_speed = dir_err * CONTROL_P + (dir_err - lastError) * CONTROL_D;
            lastError = dir_err;
            turn_speed = MathHelper.Clamp(turn_speed, -MAX_TURN_SPEED, MAX_TURN_SPEED);
            Vector2 velocity = projectile.velocity;
            projectile.velocity = velocity.RotatedBy(turn_speed);
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[projectile.owner];
            modifiers.HitDirectionOverride = (target.Center - player.Center).X > 0 ? 1 : -1;

            float multiplier = (float)Math.Pow(DAMAGE_DECAY_FACTOR, hitCount);

            modifiers.FinalDamage *= multiplier;

            hitCount++;
            MinionAIHelper.SetProjectileNetUpdate(projectile);
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(TargetId);
            binaryWriter.Write(hitCount);
            binaryWriter.Write(HomeinEnabled);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            TargetId = binaryReader.ReadInt32();
            hitCount = binaryReader.ReadInt32();
            HomeinEnabled = binaryReader.ReadBoolean();
        }
    }
}
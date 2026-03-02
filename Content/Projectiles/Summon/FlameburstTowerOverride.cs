using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ModLoader.IO;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class FlameburstTowerOverride : IProjectileOverride
    {
        // constants
        protected const float BULLET_SPEED_1 = 10f;
        protected const float BULLET_SPEED_2 = 15f;
        protected const float BULLET_SPEED_3 = 20f;
        protected const float SPEED_FACTOR = 1.4f;
        protected const float PRED_BULLET_SPEED = 15f;
        protected const bool USE_PREDICTION = true;
        protected const float MAX_RANGE_1 = 50f*16f;
        protected const float MAX_RANGE_2 = 60f*16f;
        protected const float MAX_RANGE_3 = 60f*16f;
        protected const float RANGE_FACTOR = 1.5f;
        protected const int SHOOT_ANIMATION_SPEED = 5;
        protected const int SHOOT_INTERVAL = (int)(60 * 0.5f);
        protected const float ATTACK_SPEED_ENHANCEMENT_FACTOR = 0.75f;
        protected virtual int FRAME_COUNT => 9;

        protected Vector2 direction = new Vector2(1f, 0f);
        protected int shootTimer = SHOOT_INTERVAL / 2;
        protected int TimerDuringShoot = -1;

        protected bool enchanced = false;

        public FlameburstTowerOverride()
        {
            RegisterFlags["SetDefaults"] = true;
            RegisterFlags["PreAI"] = true;
        }

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 30;
			projectile.height = 54;
            projectile.aiStyle = -1;
            projectile.timeLeft = Projectile.SentryLifeTime;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.manualDirectionChange = true;
            projectile.sentry = true;
            projectile.netImportant = true;
            projectile.DamageType = DamageClass.Summon;
        }

        protected bool CheckArmorSet(Player player)
        {
            bool SquireArmorSet = player.armor[0].type == 3797 &&
                   player.armor[1].type == 3798 &&
                   player.armor[2].type == 3799;
            bool SquireAltArmorSet = player.armor[0].type == 3874 &&
                   player.armor[1].type == 3875 &&
                   player.armor[2].type == 3876;
            return SquireArmorSet || SquireAltArmorSet;
        }

        protected bool GetEnhanced(Projectile projectile, Player player)
        {
            bool new_enchanced = CheckArmorSet(player);
            if(enchanced ^ new_enchanced)
            {
                MinionAIHelper.SetProjectileNetUpdate(projectile);
            }
            enchanced = new_enchanced;
            return enchanced;
        }

        protected void UpdateAnimation(Projectile projectile)
        {
            // face towards target
            projectile.spriteDirection = direction.X > 0 ? 1 : -1;

            // play shoot animation
            if (TimerDuringShoot >= 0 || projectile.frame > FRAME_COUNT - 1)
            {
                projectile.frameCounter++;
                if(projectile.frameCounter >= SHOOT_ANIMATION_SPEED)
                {
                    projectile.frameCounter = 0;
                    projectile.frame++;
                }
            }
            else
            {
                projectile.frame = 0;
                projectile.frameCounter = 0;
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(direction.X);
            binaryWriter.Write(direction.Y);
            binaryWriter.Write(shootTimer);
            binaryWriter.Write(TimerDuringShoot);
            bitWriter.WriteBit(enchanced);
        }
		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            float directionX = binaryReader.ReadSingle();
            float directionY = binaryReader.ReadSingle();
            direction = new Vector2(directionX, directionY);
            shootTimer = binaryReader.ReadInt32();
            TimerDuringShoot = binaryReader.ReadInt32();
            enchanced = bitReader.ReadBit();
        }
    }

    public class FlameburstShotOverride : IProjectileOverride
    {
        protected virtual float HOMING_RANGE => 500f;
        protected virtual float CONTROL_P => 0.15f;
        protected virtual float CONTROL_D => 0.01f;
        protected virtual float MAX_TURN_SPEED => 0.02f;
        protected virtual float HOMING_SPEED => 10f;
        protected virtual float HOMING_INERTIA => 10f;

        protected virtual int lvl => 1;

        protected float lastError = 0f;

        protected int TargetId = -1;

        public FlameburstShotOverride()
        {
            RegisterFlags["AI"] = true;

            // DynamicParamManager.Register("InertiaT1", 10f, 0.1f, 100f);
            // DynamicParamManager.Register("InertiaT2", 10f, 0.1f, 100f);
            // DynamicParamManager.Register("InertiaT3", 10f, 0.1f, 100f);
        }

        public override void AI(Projectile projectile)
        {
            // // get target
            // int targetId = (int)projectile.ai[0];
            // NPC target = targetId != -1 ? Main.npc[targetId] : null;

            // if(target == null || !target.active || (target.Center - projectile.Center).Length() > HOMING_RANGE)
            // {
            //     float spd = (float)Math.Min(projectile.velocity.Length() + 0.5f, HOMING_SPEED);
            //     projectile.velocity = projectile.velocity.SafeNormalize(Vector2.Zero) * spd;
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
            if(target == null || !target.active || (target.Center - projectile.Center).Length() > HOMING_RANGE)
            {
                float spd = (float)Math.Min(projectile.velocity.Length() + 0.5f, HOMING_SPEED);
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.Zero) * spd;
                return;
            }

            MinionAIHelper.HomeinToTarget(projectile, target.Center, HOMING_SPEED, HOMING_INERTIA);
        }
    }

    public class FlameburstShotT1Override : FlameburstShotOverride
    {
        protected override float HOMING_RANGE => 500f;
        protected override float CONTROL_P => 0.15f;
        protected override float CONTROL_D => 0.01f;
        protected override float MAX_TURN_SPEED => 0.02f;
        protected override float HOMING_SPEED => 10f;
        protected override float HOMING_INERTIA => 80f;

        protected override int lvl => 1;
    }

    public class FlameburstTowerT1Override : FlameburstTowerOverride
    {
        protected override int FRAME_COUNT => 7;
        public override bool PreAI(Projectile projectile)
        {
            // apply gravity
            MinionAIHelper.ApplyGravity(projectile, ModGlobal.SENTRY_GRAVITY, ModGlobal.SENTRY_MAX_FALL_SPEED);

            // calculate range
            bool Enhanced = GetEnhanced(projectile, Main.player[projectile.owner]);
            float maxRange = MAX_RANGE_1 * (Enhanced ? RANGE_FACTOR : 1f);

            // targeting
            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[projectile.owner], 
                projectile, 
                maxRange, 
                true, 
                n => (n.Center - projectile.Center).ToRotation() <= ModGlobal.PI_FLOAT/4f || (n.Center - projectile.Center).ToRotation() >= 3f*ModGlobal.PI_FLOAT/4f).TargetNPC;


            int shootInterval = (int)((SHOOT_INTERVAL + FRAME_COUNT * SHOOT_ANIMATION_SPEED) * (Enhanced ? ATTACK_SPEED_ENHANCEMENT_FACTOR : 1f));


            // shooting
            if(target != null)
            {
                // calculate target direction
                Vector2 TargetPredictedPos = MinionAIHelper.PredictTargetPosition(projectile, target, BULLET_SPEED_1 * (Enhanced ? SPEED_FACTOR : 1f), 60, 3);
                direction = TargetPredictedPos - projectile.Center;
                direction.Normalize();

                // fire!
                if(shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0)
                {
                    TimerDuringShoot = 0;
                }
                
            }

            // keep shooting even lose target
            if(TimerDuringShoot >= 0)
            {
                TimerDuringShoot++;
                if(TimerDuringShoot >= SHOOT_ANIMATION_SPEED * FRAME_COUNT)
                {
                    TimerDuringShoot = -1;
                }
                else if (TimerDuringShoot == SHOOT_ANIMATION_SPEED * FRAME_COUNT / 2)
                {
                    Vector2 BulletVelocity = direction * BULLET_SPEED_1 * (Enhanced ? SPEED_FACTOR : 1f);
                    Vector2 bulletOffset = new Vector2(0, -20f);

                    if(projectile.owner == Main.myPlayer)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), 
                                                projectile.Center + bulletOffset, 
                                                BulletVelocity, 
                                                ProjectileID.DD2FlameBurstTowerT1Shot, 
                                                projectile.damage, 
                                                projectile.knockBack, 
                                                projectile.owner);
                    }
                }
            }
            else if (target == null)
            {
                direction = new Vector2(1f, 0);
            }

            shootTimer++;
            if(shootTimer >= shootInterval)
            {
                shootTimer = shootInterval;
            }

            UpdateAnimation(projectile);

            return false;
        }
    }

    public class FlameburstShotT2Override : FlameburstShotOverride
    {
        protected override float HOMING_RANGE => 750f;
        protected override float CONTROL_P => 0.15f;
        protected override float CONTROL_D => 0.1f;
        protected override float MAX_TURN_SPEED => 0.08f;
        protected override float HOMING_SPEED => 15f;
        protected override float HOMING_INERTIA => 45f;
        protected override int lvl => 2;
    }

    public class FlameburstTowerT2Override : FlameburstTowerOverride
    {
        public override bool PreAI(Projectile projectile)
        {
            // apply gravity
            MinionAIHelper.ApplyGravity(projectile, ModGlobal.SENTRY_GRAVITY, ModGlobal.SENTRY_MAX_FALL_SPEED);

            // calculate range
            bool Enhanced = GetEnhanced(projectile, Main.player[projectile.owner]);
            float maxRange = MAX_RANGE_2 * (Enhanced ? RANGE_FACTOR : 1f);

            // targeting
            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[projectile.owner], 
                projectile, 
                maxRange, 
                true, 
                n => (n.Center - projectile.Center).ToRotation() <= ModGlobal.PI_FLOAT/4f || (n.Center - projectile.Center).ToRotation() >= 3f*ModGlobal.PI_FLOAT/4f).TargetNPC;


            int shootInterval = (int)((SHOOT_INTERVAL + FRAME_COUNT * SHOOT_ANIMATION_SPEED) * (Enhanced ? ATTACK_SPEED_ENHANCEMENT_FACTOR : 1f));


            // shooting
            if(target != null)
            {
                // calculate target direction
                Vector2 TargetPredictedPos = MinionAIHelper.PredictTargetPosition(projectile, target, BULLET_SPEED_2 * (Enhanced ? SPEED_FACTOR : 1f), 60, 3);
                direction = TargetPredictedPos - projectile.Center;
                direction.Normalize();

                // fire!
                if(shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0)
                {
                    TimerDuringShoot = 0;
                }
                
            }

            // keep shooting even lose target
            if(TimerDuringShoot >= 0)
            {
                TimerDuringShoot++;
                if(TimerDuringShoot >= SHOOT_ANIMATION_SPEED * FRAME_COUNT)
                {
                    TimerDuringShoot = -1;
                }
                else if (TimerDuringShoot == SHOOT_ANIMATION_SPEED * FRAME_COUNT / 2)
                {
                    Vector2 BulletVelocity = direction * BULLET_SPEED_2 * (Enhanced ? SPEED_FACTOR : 1f);
                    Vector2 bulletOffset = new Vector2(0, -20f);

                    if(projectile.owner == Main.myPlayer)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), 
                                                projectile.Center + bulletOffset, 
                                                BulletVelocity, 
                                                ProjectileID.DD2FlameBurstTowerT2Shot, 
                                                projectile.damage, 
                                                projectile.knockBack, 
                                                projectile.owner);
                    }
                }
            }
            else if (target == null)
            {
                direction = new Vector2(1f, 0);
            }

            shootTimer++;
            if(shootTimer >= shootInterval)
            {
                shootTimer = shootInterval;
            }

            UpdateAnimation(projectile);

            return false;
        }
    }

    public class FlameburstShotT3Override : FlameburstShotOverride
    {
        protected override float HOMING_RANGE => 1000f;
        protected override float CONTROL_P => 3f;
        protected override float CONTROL_D => 0.2f;
        protected override float MAX_TURN_SPEED => 3.0f;
        protected override float HOMING_SPEED => 25f;
        protected override float HOMING_INERTIA => 10f;
        protected override int lvl => 3;
    }

    public class FlameburstTowerT3Override : FlameburstTowerOverride
    {
        public override bool PreAI(Projectile projectile)
        {
            // apply gravity
            MinionAIHelper.ApplyGravity(projectile, ModGlobal.SENTRY_GRAVITY, ModGlobal.SENTRY_MAX_FALL_SPEED);

            // calculate range
            bool Enhanced = GetEnhanced(projectile, Main.player[projectile.owner]);
            float maxRange = MAX_RANGE_3 * (Enhanced ? RANGE_FACTOR : 1f);

            // targeting
            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[projectile.owner], 
                projectile, 
                maxRange, 
                true, 
                n => (n.Center - projectile.Center).ToRotation() <= ModGlobal.PI_FLOAT/4f || (n.Center - projectile.Center).ToRotation() >= 3f*ModGlobal.PI_FLOAT/4f).TargetNPC;


            int shootInterval = (int)((SHOOT_INTERVAL + FRAME_COUNT * SHOOT_ANIMATION_SPEED) * (Enhanced ? ATTACK_SPEED_ENHANCEMENT_FACTOR : 1f));


            // shooting
            if(target != null)
            {
                // calculate target direction
                // Vector2 TargetPredictedPos = MinionAIHelper.PredictTargetPosition(projectile, target, BULLET_SPEED_3 * (Enhanced ? SPEED_FACTOR : 1f), 60, 3);
                direction = target.Center - projectile.Center;
                direction.Normalize();

                // fire!
                if(shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0)
                {
                    TimerDuringShoot = 0;
                }
                
            }

            // keep shooting even lose target
            if(TimerDuringShoot >= 0)
            {
                TimerDuringShoot++;
                if(TimerDuringShoot >= SHOOT_ANIMATION_SPEED * FRAME_COUNT)
                {
                    TimerDuringShoot = -1;
                }
                else if (TimerDuringShoot == SHOOT_ANIMATION_SPEED * FRAME_COUNT / 2)
                {
                    Vector2 BulletVelocity = direction * BULLET_SPEED_3 * (Enhanced ? SPEED_FACTOR : 1f);
                    Vector2 bulletOffset = new Vector2(0, -20f);

                    if(projectile.owner == Main.myPlayer)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), 
                                                projectile.Center + bulletOffset, 
                                                BulletVelocity, 
                                                ProjectileID.DD2FlameBurstTowerT3Shot, 
                                                projectile.damage, 
                                                projectile.knockBack, 
                                                projectile.owner);
                    }
                }
            }
            else if (target == null)
            {
                direction = new Vector2(1f, 0);
            }

            shootTimer++;
            if(shootTimer >= shootInterval)
            {
                shootTimer = shootInterval;
            }

            UpdateAnimation(projectile);

            return false;
        }
    }
}
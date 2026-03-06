using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Microsoft.Xna.Framework.Graphics;

using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;
using Terraria.Audio;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class HoneyCombSentry : ModProjectile
    {
        private const int NORMAL_FRAME_SPEED = 20;
        private const int SHOOT_FRAME_SPEED = 5;

        private const int SHOOT_INTERVAL_1 = 60*2;
        private const int SHOOT_INTERVAL_2 = (int)(60*1.5f);
        private const int MAX_BEES_PER_SHOT = 6;
        private const int MIN_BEES_PER_SHOT = 3;
        private const float ENHANCEMENT_FACTOR = 0.75f;

        public static float Gravity = ModGlobal.SENTRY_GRAVITY;
        public static float MaxGravity = 20f;

        private const string TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/HoneyCombSentry";
        public override string Texture => TEXTURE_PATH;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 34;
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
            for(int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, 150);
                d.noGravity = true;
            }
            
        }

        private bool CheckHiveBackpack(Player owner)
        {
            for(int idx = 3; idx <= 9; idx++)
            {
                Item accessory = owner.armor[idx];
                if(accessory.type == ItemID.HiveBackpack && accessory.stack > 0)
                {
                    return true;
                }
            }
            return false;
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

            int shootTimer = (int)Projectile.ai[0];

            // Targeting
            NPC target = MinionAIHelper.SearchForTargets(
                    owner, 
                    Projectile, 
                    600f, 
                    true, 
                    null).TargetNPC;

            bool hasHiveBackpack = CheckHiveBackpack(owner);
            int shootInterval = hasHiveBackpack ? SHOOT_INTERVAL_2 : SHOOT_INTERVAL_1;
            if (target != null)
            {

                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if (shootTimer == 0)
                {
                    // Fire!
                    Vector2 direction = target.Center - Projectile.Center;
                    direction.Normalize();
                    // direction *= 10f;
                    float dir = direction.ToRotation();
                    Vector2 BaseVel = new Vector2(3f, 0f);
                    
                    Random beeNumRandom = new Random();
                    int bees_per_shot = beeNumRandom.Next(MIN_BEES_PER_SHOT, MAX_BEES_PER_SHOT + 1);
                    for (int i = 0; i < bees_per_shot; i++)
                    {
                        Random random = new Random();
                        float random_seed_dir = (float)random.NextDouble();
                        float random_seed_vel = (float)random.NextDouble();
                        float dir_offset = (random_seed_dir*2-1) * ModGlobal.PI_FLOAT / 8f;
                        float vel_offset = (random_seed_vel*2-1) * 0.5f + 1f;
                        // Main.NewText("random_seed: " + random_seed + " dir_offset: " + dir_offset);
                        Vector2 Velocity = (BaseVel * vel_offset).RotatedBy(dir + dir_offset);


                        if(Projectile.owner == Main.myPlayer)
                        {
                            Projectile bee = Projectile.NewProjectileDirect(
                                    Projectile.GetSource_FromAI(),
                                    Projectile.Center,
                                    Velocity,
                                    ModProjectileID.HoneyCombSentryBullet,
                                    // ProjectileID.Bee,
                                    Projectile.damage,
                                    0,
                                    Projectile.owner);
                        }
                        Projectile.netUpdate = true;

                        // bee.DamageType = DamageClass.Summon;
                        // ProjectileID.Sets.SentryShot[bee.type] = true;

                        SoundEngine.PlaySound(SoundID.Item109, Projectile.Center);
                    }

                    // if player has hive backpack, may create giant bee
                    if(hasHiveBackpack)
                    {
                        Random random = new Random();
                        float random_seed = MinionAIHelper.RandomFloat(0f, 1f);
                        if(random_seed < 0.5f)
                        {
                            if(Projectile.owner == Main.myPlayer)
                            {
                                Projectile bee = Projectile.NewProjectileDirect(
                                    Projectile.GetSource_FromAI(),
                                    Projectile.Center,
                                    BaseVel.RotatedBy(dir),
                                    ModProjectileID.HoneyCombSentryGiantBullet,
                                    Projectile.damage+2,
                                    0.5f,
                                    Projectile.owner);
                            }
                            Projectile.netUpdate = true;

                            // bee.DamageType = DamageClass.Summon;
                            // ProjectileID.Sets.SentryShot[bee.type] = true;
                        }
                    }

                    // create smoke dust
                    for(int i = 0;i < 5;i++)
                    {
                        Random random = new Random();
                        float random_seed = (float)random.NextDouble();
                        float scale = random_seed * 3f + 0.5f;
                        Vector2 DustVel = BaseVel.RotatedBy(dir);
                        int dust = Dust.NewDust(Projectile.position - Projectile.Size/2f + new Vector2(-2f, 5f), Projectile.width, Projectile.height, DustID.Smoke, DustVel.X, DustVel.Y, 0, default, 1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].scale = scale;
                    }
                }
            }

            shootTimer++;
            if (shootTimer >= shootInterval)
            {
                shootTimer = shootInterval;
            }

            Projectile.ai[0] = (float)shootTimer;

            // Animation
            // UpdateAnimation(target, shootTimer);
        }

        // private NPC FindTarget(float range)
        // {
        //     NPC closest = null;
        //     float closestDist = range;

        //     for (int i = 0; i < Main.maxNPCs; i++)
        //     {
        //         NPC npc = Main.npc[i];
        //         if (npc.CanBeChasedBy(this) && Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
        //         {
        //             float distance = Vector2.Distance(Projectile.Center, npc.Center);
        //             if (distance < closestDist)
        //             {
        //                 closestDist = distance;
        //                 closest = npc;
        //             }
        //         }
        //     }

        //     return closest;
        // }



        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Projectile.velocity = Vector2.Zero;
            if(Projectile.velocity.X != 0f) Projectile.netUpdate = true;
            Projectile.velocity.X = 0f;
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
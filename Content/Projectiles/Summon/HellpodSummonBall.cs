using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;


namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class HellpodSummonBall : ModProjectile
    {
        // animation
        private const int FRAME_COUNT = 2;
        private const float BOUNCE_DECAY = 0.5f;
        private const int SIGNAL_TIME = 60*4;
        private const int SIGNAL_HEIGHT = 1000;
        private const int HELLPOD_SUMMON_TIME = 60*2;
        private const int HELLPOD_SUMMON_HEIGHT = 1000;

        private const int HELLPOD_DAMAGE = 100;
        private const float HELLPOD_KNOCKBACK = 10f;

        private NonUniformFloatIntPacker extraPacker = new NonUniformFloatIntPacker(
            2, // SignalEnable
            2, // SignalSpawned
            2 // HellpodSpawned
        );

        // private variables

        private int SignalProjectileId = 0;
        private int HellpodProjectileId = 0;

        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/HellpodSummonBall";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FRAME_COUNT;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
            Projectile.netImportant = true;
            // Projectile.sentry = true;
        }

        public override void AI()
        {
            // decode
            int SignalTimer = (int)Projectile.ai[0];
            int[] decode_values = extraPacker.Decode(Projectile.ai[1]);
            bool SignalEnable = decode_values[0] != 0;
            bool SignalSpawned = decode_values[1] != 0;
            bool HellpodSpawned = decode_values[2] != 0;
            int SummonTargetID = (int)Projectile.ai[2];


            MinionAIHelper.ApplyGravity(Projectile);
            
            if(SignalEnable)
            {
                if(!SignalSpawned)
                {
                    if(Projectile.owner == Main.myPlayer)
                    {
                        SignalProjectileId = Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center + new Vector2(0, -SIGNAL_HEIGHT/2f),
                            Vector2.Zero,
                            ModContent.ProjectileType<HellpodSummonSignal>(),
                            0,
                            0,
                            Projectile.owner
                        );
                    }
                    SoundEngine.PlaySound(ModSounds.HellpodSignal_1, Projectile.Center);
                    SignalSpawned = true;
                    Projectile.netUpdate = true;
                }
                Projectile.velocity = Vector2.Zero;
                

                SignalTimer++;
                if(SignalTimer >= HELLPOD_SUMMON_TIME)
                {
                    if(!HellpodSpawned)
                    {
                        if(Projectile.owner == Main.myPlayer)
                        {
                            HellpodProjectileId = Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center + new Vector2(0, -HELLPOD_SUMMON_HEIGHT),
                                new Vector2(0, 10f),
                                ModContent.ProjectileType<Hellpod>(),
                                HELLPOD_DAMAGE,
                                HELLPOD_KNOCKBACK,
                                Projectile.owner
                            );
                        }
                        SoundEngine.PlaySound(ModSounds.HellpodSignal_2_1, Projectile.Center);
                        HellpodSpawned = true;
                        Projectile.netUpdate = true;
                    }
                    Projectile HellpodProjectile = Main.projectile[HellpodProjectileId];
                    Vector2 Ball2Hellpod = HellpodProjectile.Center - Projectile.Center;
                    // hellpod is arrived
                    if(Ball2Hellpod.Length() < 10f || Ball2Hellpod.Y > 0)
                    {
                        // create dust
                        for(int i = 0; i < 10; i++)
                        {
                            // dirt
                            float dust_ang = -(float)i * 180f / 10f;
                            float dust_speed = MinionAIHelper.RandomFloat(3f, 5f);
                            Vector2 vel = new Vector2(dust_speed, 0f).RotatedBy(dust_ang * MathHelper.ToRadians(1));
                            Dust dirtDust = Dust.NewDustDirect(Projectile.Center, 10, 10, DustID.Dirt, vel.X, vel.Y, 0, Color.White, 1.0f);
                            // smoke
                            Dust smokeDust = Dust.NewDustDirect(Projectile.Center, 10, 10, DustID.Smoke, 0, 0, 0, Color.White, 1f);
                            smokeDust.noGravity = true;
                            float smoke_ang = -(float)i * 180f / 10f;
                            float smoke_speed = MinionAIHelper.RandomFloat(3f, 6f);
                            float smoke_size = MinionAIHelper.RandomFloat(3f, 5f);
                            int smoke_alpha = (int)(MinionAIHelper.RandomFloat(0.0f, 1.0f) * 255);
                            smokeDust.velocity = new Vector2(1, 0f).RotatedBy(smoke_ang * MathHelper.ToRadians(1));
                            smokeDust.velocity *= smoke_speed;
                            smokeDust.scale = smoke_size;
                            smokeDust.alpha = smoke_alpha;

                            
                            float flame_ang = -((float)i * 90f / 10f + 45f);
                            float flame_speed = MinionAIHelper.RandomFloat(5f, 8f);
                            float flame_size = MinionAIHelper.RandomFloat(1f, 1f);
                            // int flame_alpha = (int)(MinionAIHelper.RandomFloat(0.5f, 1.5f) * 255);
                            Vector2 flame_vel = new Vector2(1, 0f).RotatedBy(flame_ang * MathHelper.ToRadians(1));
                            // Main.NewText("flame_ang: " + flame_ang + " flame_speed: " + flame_speed + " flame_size: " + flame_size);
                            flame_vel *= flame_speed;
                            Dust flameDust = Dust.NewDustDirect(Projectile.Center, 10, 10, DustID.Torch, flame_vel.X, flame_vel.Y, 0, Color.White, flame_size);
                            // flameDust.noGravity = true;
                        }

                        // create sentry
                        Vector2 SpawnOffset = new Vector2(0, 25f-4f);
                        if(Projectile.owner == Main.myPlayer)
                        {
                            Projectile.NewProjectileDirect(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center - SpawnOffset,
                                Vector2.Zero,
                                SummonTargetID,
                                Projectile.damage,
                                Projectile.knockBack,
                                Projectile.owner
                                
                            );
                        }
                        Projectile.netUpdate = true;
                        // Main.NewText("sentry created: " + SummonTargetID);

                        Player player = Main.player[Projectile.owner];
                        player.UpdateMaxTurrets();

                        SoundEngine.PlaySound(ModSounds.HellpodSignal_3_2, Projectile.Center);

                        // kill self
                        Projectile.Kill();

                    }
                    // create dust
                    Dust normalSmokeDust = Dust.NewDustDirect(Projectile.Center, 10, 10, DustID.Smoke, 0, 0, 0, Color.White, 1f);
                    normalSmokeDust.noGravity = true;
                    bool dir = MinionAIHelper.RandomBool();
                    float ang = (dir ? MinionAIHelper.RandomFloat(0f, 45f) : 180 - MinionAIHelper.RandomFloat(0f, 45f)) + 180f;
                    float speed = MinionAIHelper.RandomFloat(8f, 12f);
                    float size = MinionAIHelper.RandomFloat(1f, 4f);
                    int alpha = (int)(MinionAIHelper.RandomFloat(0.5f, 1.5f) * 255);
                    normalSmokeDust.velocity = new Vector2(1, 0f).RotatedBy(ang * MathHelper.ToRadians(1));
                    normalSmokeDust.velocity *= speed;
                    normalSmokeDust.scale = size;
                    normalSmokeDust.alpha = alpha;

                    
                }
                if(SignalTimer >= SIGNAL_TIME)
                {
                    // kill self
                    Projectile.Kill();

                    return;
                }
            }
            // Main.NewText("SignalEnable: " + SignalEnable + " SignalTimer: " + SignalTimer);

            UpdateAnimation(SignalEnable);

            Projectile.ai[0] = (float)SignalTimer;
            Projectile.ai[1] = extraPacker.Encode(SignalEnable?1:0,SignalSpawned?1:0,HellpodSpawned?1:0);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[1] = extraPacker.Set(Projectile.ai[1], 0, 0);
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 0.1f)
            {
                Projectile.velocity.X = -oldVelocity.X * BOUNCE_DECAY;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 0.1f)
            {
                if (oldVelocity.Y > 0f)
                {
                    Projectile.ai[1] = extraPacker.Set(Projectile.ai[1], 0, 1);
                    // Projectile.velocity = Vector2.Zero;
                    Projectile.velocity.X = 0f;
                }
                else
                {
                    Projectile.velocity.Y = -oldVelocity.Y * BOUNCE_DECAY;
                }
            }
            Projectile.netUpdate = true;
            
            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        private void UpdateAnimation(bool SignalEnable)
        {
            if(SignalEnable)
            {
                Projectile.frame = FRAME_COUNT - 1;
                 Projectile.rotation = 0f;
            }
            else
            {
                Projectile.frame = 0;
                Projectile.rotation += Projectile.velocity.X * 0.05f;
            }
        }

        public void SetSummonTarget(int targetID)
        {
            Projectile.ai[2] = targetID;
            // Main.NewText("summon target: " + SummonTargetID);
        }

        public override void Kill(int timeLeft)
        {
            Projectile SignalProjectile = Main.projectile[SignalProjectileId];
            Projectile HellpodProjectile = Main.projectile[HellpodProjectileId];
            if(SignalProjectile != null && SignalProjectile.type == ModContent.ProjectileType<HellpodSummonSignal>()) SignalProjectile.Kill();
            if(HellpodProjectile != null && HellpodProjectile.type == ModContent.ProjectileType<Hellpod>()) HellpodProjectile.Kill();
            SignalProjectile.netUpdate = true;
            HellpodProjectile.netUpdate = true;
        }
    }
}
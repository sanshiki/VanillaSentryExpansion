using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class StardustSentry : ModProjectile
    {
        /* -------------------- constants -------------------- */
        // animation
        private const int FRAME_COUNT = 5;
        private const int MAX_FRAME_SPEED = 12;
        private const int MIN_FRAME_SPEED = 3;
        // private int fireCooldown = 30;

        // shoot interval
        private const int FIRE_INTERVAL = 120;
        private const int SIGNAL_TIME = 50;
        private const int BULLET_NUM = 3;
        private const int BULLET_INTERVAL = 5;

        // bullet 
        private const float REAL_BULLET_SPEED = 15f;
        private const float PRED_BULLET_SPEED = 15f;
        private const float DEACCELERATION = 0.5f;
        private const bool USE_PREDICTION = true;

        // teleport
        private const int TELEPORT_COOLDOWN = 60*5;
        private const int TELEPORT_TRIGGER_DISTANCE = 2000;
        private const int TELEPORT_MAX_DISTANCE = 4000;
        
        // texture
        private string TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/StardustSentry";
        public override string Texture => TEXTURE_PATH;

        // float packer
        private NonUniformFloatIntPacker timerPacker = new NonUniformFloatIntPacker(
            FIRE_INTERVAL, // fireTimer
            SIGNAL_TIME, // signalTimer
            BULLET_INTERVAL, // bulletTimer
            TELEPORT_COOLDOWN // teleportTimer
        );

        private NonUniformFloatIntPacker extraPacker = new NonUniformFloatIntPacker(
            BULLET_NUM, // bulletCnt
            2 // canShoot
        );

        /* -------------------- variables -------------------- */
        private float currentFrameSpeed = (float)MAX_FRAME_SPEED;
        private float targetCenterX = 0f;
        private float targetCenterY = 0f;
        private ProjectileReference SignalRef;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = FRAME_COUNT;
        }

        public override void SetDefaults()
        {
            Projectile.width = 68;
            Projectile.height = 98;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.sentry = true;
            Projectile.netImportant = true;
            Projectile.light = 1f;
        }

        public override void AI()
        {
            // decode
            int[] timer_decode_values = timerPacker.Decode(Projectile.ai[0]);
            int fireTimer = timer_decode_values[0];
            int signalTimer = timer_decode_values[1];
            int bulletTimer = timer_decode_values[2];
            int teleportTimer = timer_decode_values[3];
            int[] extra_decode_values = extraPacker.Decode(Projectile.ai[1]);
            int bulletCnt = extra_decode_values[0];
            bool canShoot = extra_decode_values[1] != 0;
            // Float in the air
            Vector2 vel = Projectile.velocity;
            Vector2 vel_dir = vel.SafeNormalize(Vector2.Zero);
            if(vel.Length() > DEACCELERATION)
            {
                Projectile.velocity -= vel_dir * DEACCELERATION;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            float FloatAmplitude = 0.5f;
            FloatAmplitude = Math.Min(FloatAmplitude, 2f / vel.Length());
            
            float FloatOffset = (float)(Math.Sin(Projectile.localAI[0] * 0.05f) * FloatAmplitude);
            Projectile.localAI[0]++;
            Projectile.Center += new Vector2(0, FloatOffset);

            // teleport to owner if needed
            Player owner = Main.player[Projectile.owner];
            if (Vector2.Distance(Projectile.Center, owner.Center) > TELEPORT_TRIGGER_DISTANCE && Vector2.Distance(Projectile.Center, owner.Center) < TELEPORT_MAX_DISTANCE && teleportTimer >= TELEPORT_COOLDOWN)
            {
                TryTeleportNearPlayer(owner);
                teleportTimer = 0;
            }
            teleportTimer += teleportTimer >= TELEPORT_COOLDOWN ? 0 : 1;

            // search for targets and emit signal
            int fireInterval = FIRE_INTERVAL;
            NPC target = MinionAIHelper.SearchForTargets(
                owner, 
                Projectile, 
                1700f, 
                false, 
                null).TargetNPC;
            if (target != null)
            {
                targetCenterX = target.Center.X;
                targetCenterY = target.Center.Y;
                if (fireTimer >= fireInterval)
                {
                    EmitSignal(target);
                    fireTimer = 0;
                    signalTimer = 0;
                    MinionAIHelper.SetProjectileNetUpdate(Projectile);
                }
                currentFrameSpeed -= 0.1f;
                if (currentFrameSpeed < MIN_FRAME_SPEED) currentFrameSpeed = MIN_FRAME_SPEED;
            }
            else
            {
                currentFrameSpeed += 0.1f;
                if (currentFrameSpeed > MAX_FRAME_SPEED) currentFrameSpeed = MAX_FRAME_SPEED;
            }

            // check if signal available
            Projectile signalProj = SignalRef.Get();
            if(signalProj != null)
            {
                if(signalProj.active && signalProj.type == ModProjectileID.StardustSentrySignal)
                {
                    signalProj.Center = Projectile.Center + new Vector2(0, -Projectile.height / 2f - 1000f / 2f);
                    signalProj.velocity = Projectile.velocity;

                    signalTimer++;
                    if (signalTimer >= SIGNAL_TIME && target != null)
                    {
                        signalTimer = 0;
                        canShoot = true;
                    }
                }
            }
            
            if(canShoot)
            {
                bulletTimer++;
                if(bulletTimer >= BULLET_INTERVAL)
                {
                    bulletCnt++;
                    bulletTimer = 0;
                    Vector2 target_center = new Vector2(targetCenterX, targetCenterY);
                    Vector2 ProjOffset = new Vector2(MinionAIHelper.RandomFloat(-1000f, 1000f), -1000f);
                    Vector2 ProjSpawnPos = target_center + ProjOffset;
                    Vector2 PredictedPos = target_center; 
                    if(target != null)
                        PredictedPos = MinionAIHelper.PredictTargetPosition(ProjSpawnPos, target_center, target.velocity, 50f);
                    Vector2 direction = (PredictedPos - ProjSpawnPos).SafeNormalize(Vector2.Zero);

                    if(Projectile.owner == Main.myPlayer)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(
                            Projectile.GetSource_FromAI(),
                            ProjSpawnPos,
                            direction * 50f,
                            ModProjectileID.StardustSentryBullet,
                            Projectile.damage,
                            Projectile.knockBack,
                            Projectile.owner,
                            (float)(target.whoAmI)
                        );
                    }

                    if(bulletCnt >= BULLET_NUM)
                    {
                        bulletCnt = 0;
                        canShoot = false;
                    }

                    MinionAIHelper.SetProjectileNetUpdate(Projectile);
                }
            }

            fireTimer++;
            if(fireTimer >= fireInterval)
                fireTimer = fireInterval;

            UpdateAnimation(target);

            Projectile.ai[0] = timerPacker.Encode(fireTimer, signalTimer, bulletTimer, teleportTimer);
            Projectile.ai[1] = extraPacker.Encode(bulletCnt, canShoot ? 1 : 0);
        }

        private void EmitSignal(NPC target)
        {
            Vector2 SignalOffset = new Vector2(0, -Projectile.height/2f-1000f/2f);
            if(Projectile.owner == Main.myPlayer)
            {
                Projectile proj = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center + SignalOffset,
                    new Vector2(0, 0),
                    ModProjectileID.StardustSentrySignal,
                    0,
                    0,
                    Projectile.owner
                );
                SignalRef.Set(proj);
            }
        }

        private void TryTeleportNearPlayer(Player player)
        {
            Vector2 bestPosition = Projectile.Center;
            int bestTileCount = int.MaxValue;

            for (int xOffset = -5; xOffset <= 5; xOffset++)
            {
                for (int yOffset = -5; yOffset <= 5; yOffset++)
                {
                    Vector2 checkPos = player.Center + new Vector2(xOffset * 16, yOffset * 16);
                    if (!Collision.SolidCollision(checkPos, Projectile.width, Projectile.height))
                    {
                        int tileCount = CountNearbySolidTiles(checkPos);
                        if (tileCount < bestTileCount)
                        {
                            bestTileCount = tileCount;
                            bestPosition = checkPos;
                        }
                    }
                }
            }

            // add random offset to best position
            bestPosition += new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10));

            // Teleport if we found a better position
            Projectile.position = bestPosition;
            MinionAIHelper.SetProjectileNetUpdate(Projectile);

            // Optional: Visual or sound effect
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(bestPosition, Projectile.width, Projectile.height, DustID.MagicMirror, Scale: 1.5f);
                }
                SoundEngine.PlaySound(SoundID.Item8, bestPosition);
            }
        }

        private int CountNearbySolidTiles(Vector2 center)
        {
            int tileCount = 0;
            Point tileCenter = center.ToTileCoordinates();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Tile tile = Framing.GetTileSafely(tileCenter.X + x, tileCenter.Y + y);
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        tileCount++;
                    }
                }
            }

            return tileCount;
        }

        public override bool MinionContactDamage()
		{
			return false;
		}

        private void UpdateAnimation(NPC target)
        {
            Projectile.frameCounter++;
            Projectile.localAI[1]++;
            if (Projectile.frameCounter >= currentFrameSpeed)
            {
                Projectile.frame = (Projectile.frame + 1) % (FRAME_COUNT - 1);
                Projectile.frameCounter = 0;
            }

            if (target != null)
            {
                Vector2 direction = target.Center - Projectile.Center;
                direction.Normalize();
                Projectile.spriteDirection = direction.X > 0 ? -1 : 1;
            }

            if (Main.rand.NextFloat() < 0.05f)
            {
                Dust dust;
                Vector2 position = Projectile.Center - Projectile.Size / 2f;
                dust = Dust.NewDustDirect(position, Projectile.width, Projectile.height, 111, 0f, 0f, 0, new Color(0,67,255), 1f);
                dust.velocity = new Vector2(0f, MinionAIHelper.RandomFloat(-0.3f, 0f));
                // dust.shader = GameShaders.Armor.GetSecondaryShader(30, Main.LocalPlayer);
            }
        }

        public override void Kill(int timeLeft)
        {
            int[] extra_decode_values = extraPacker.Decode(Projectile.ai[1]);
            Projectile signalProj = SignalRef.Get();
            if (signalProj != null)
            {
                if(signalProj.active)
                    signalProj.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(TEXTURE_PATH).Value;
            int width = texture.Width;
            int FrameHeight = Projectile.height;
            int CurrentFrameHeight = texture.Height / FRAME_COUNT * (Projectile.frame+1);

            // draw the turret part
            Rectangle TurretRect = new Rectangle(0, 0, width, FrameHeight);
            Vector2 TurretWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0f, 0f));
            Vector2 TurretOrigin = new Vector2(width / 2f, FrameHeight / 2f);
            MinionAIHelper.DrawPart(
                Projectile,
                texture,
                TurretWorldPos,
                TurretRect,
                lightColor,
                Projectile.rotation,
                TurretOrigin
            );

            // draw the floating deck part
            float FloatAmplitude = 5f;
            float FloatOffset = (float)(Math.Cos(Projectile.localAI[1] * 0.03f) * FloatAmplitude);
            // Main.NewText("FloatOffset:" + FloatOffset);
            Rectangle DeckRect = new Rectangle(0, CurrentFrameHeight, width, FrameHeight);
            Vector2 DeckWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0f, CurrentFrameHeight+FloatOffset));
            Vector2 DeckOrigin = new Vector2(width / 2f, CurrentFrameHeight + FrameHeight / 2f);
            MinionAIHelper.DrawPart(
                Projectile,
                texture,
                DeckWorldPos,
                DeckRect,
                lightColor,
                Projectile.rotation,
                DeckOrigin
            );

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(currentFrameSpeed);
            writer.Write(targetCenterX);
            writer.Write(targetCenterY);
            SignalRef.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            currentFrameSpeed = reader.ReadSingle();
            targetCenterX = reader.ReadSingle();
            targetCenterY = reader.ReadSingle();
            SignalRef.ReceiveExtraAI(reader);
        }
    }
}
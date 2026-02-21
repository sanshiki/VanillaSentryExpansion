using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
    public class CursedFireExtractorSentry : ModProjectile
    {

        private const int IDLE_STATE = 0;
        private const int PRESS_STATE = 1;
        private const int SHOOT_STATE = 2;
        private const int RELEASE_STATE = 3;

        private const int SHOOT_INTERVAL = 120;
        private const int PRESS_TIME = 14;
        private const int SHOOT_TIME = 6;
        private const int RELEASE_TIME = 11;
        private const int NORMAL_FRAME_SPEED = 10;
        private const int SHOOT_FRAME_SPEED = 2;
        private const int MAX_BULLET_NUM = 3;

        public static float Gravity = ModGlobal.SENTRY_GRAVITY;
        public static float MaxGravity = 20f;
        private const float BULLET_SPEED = 30;
        private const int FRAME_COUNT = 21;

        private float direction = 0f;
        private float lastTargetPosX = 0f;
        private float lastTargetPosY = 0f;

        public override string Texture => "SummonerExpansionMod/Assets/Textures/Projectiles/CursedFireExtractorSentry";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = FRAME_COUNT;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 70;
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
            Projectile.ai[0] = 0f;
            Projectile.ai[1] = -1f;
            Projectile.ai[2] = IDLE_STATE;
        }

        public override void AI()
        {
            int shootTimer = (int)Projectile.ai[0];
            int shootAnimationTimer = (int)Projectile.ai[1];
            int ExtractorState = (int)Projectile.ai[2];

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
                false, 
                null).TargetNPC;


            Vector2 BulletOffset = new Vector2(-21f, 24f);
            if(target != null)
            {
                Vector2 PredictedPos = MinionAIHelper.PredictTargetPosition(Projectile.Center + BulletOffset, target.Center, target.velocity, BULLET_SPEED, 60, 3);
                lastTargetPosX = PredictedPos.X;
                lastTargetPosY = PredictedPos.Y;
            }

            Vector2 lastTargetPos = new Vector2(lastTargetPosX, lastTargetPosY);
            
            direction = (lastTargetPos - Projectile.Center - BulletOffset).ToRotation();

            switch (ExtractorState)
            {
                case IDLE_STATE:
                {
                    if(target != null && shootTimer == SHOOT_INTERVAL)
                    {
                        ExtractorState = PRESS_STATE;
                        shootAnimationTimer = 0;
                    }
                } break;
                case PRESS_STATE:
                {
                    if(shootAnimationTimer != -1)
                    {
                        shootAnimationTimer++;
                    }
                    if(shootAnimationTimer >= PRESS_TIME * SHOOT_FRAME_SPEED)
                    {
                        ExtractorState = SHOOT_STATE;
                        shootAnimationTimer = 0;
                    }
                } break;
                case SHOOT_STATE:
                {
                    if(shootAnimationTimer != -1)
                    {
                        shootAnimationTimer++;
                    }
                    if(shootAnimationTimer % (SHOOT_TIME * SHOOT_FRAME_SPEED / MAX_BULLET_NUM) == 0)
                    {
                        if(Projectile.owner == Main.myPlayer)
                        {
                            Projectile bullet = Projectile.NewProjectileDirect(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center + BulletOffset,
                                direction.ToRotationVector2() * BULLET_SPEED,
                                ModProjectileID.CursedFireExtractorSentryBullet,
                                Projectile.damage,
                                Projectile.knockBack,
                                Projectile.owner);
                            bullet.usesLocalNPCImmunity = true;
                            bullet.localNPCHitCooldown = 10;
                            ProjectileID.Sets.SentryShot[bullet.type] = true;
                            Projectile.usesIDStaticNPCImmunity = true;
                            Projectile.idStaticNPCHitCooldown = 20;

                            SoundEngine.PlaySound(SoundID.Item73, Projectile.position);
                        }
                        Projectile.netUpdate = true;
                    }
                    if(shootAnimationTimer >= SHOOT_TIME * SHOOT_FRAME_SPEED)
                    {
                        ExtractorState = RELEASE_STATE;
                        shootAnimationTimer = 0;
                    }
                } break;
                case RELEASE_STATE:
                {
                    if(shootAnimationTimer != -1)
                    {
                        shootAnimationTimer++;
                    }
                    if(shootAnimationTimer >= RELEASE_TIME * SHOOT_FRAME_SPEED)
                    {
                        ExtractorState = IDLE_STATE;
                        shootAnimationTimer = -1;
                        shootTimer = 0;
                    }
                } break;
            }

            shootTimer++;
            if(shootTimer >= SHOOT_INTERVAL)
                shootTimer = SHOOT_INTERVAL;

            // Animation
            UpdateAnimation(target);

            Projectile.ai[0] = (float)shootTimer;
            Projectile.ai[1] = (float)shootAnimationTimer;
            Projectile.ai[2] = (float)ExtractorState;
        }

        private void UpdateAnimation(NPC target)
        {
            Projectile.frameCounter++;
            int ExtractorState = (int)Projectile.ai[2];
            switch (ExtractorState)
            {
                case IDLE_STATE:
                {
                    Projectile.frame += (int)(Projectile.frameCounter / NORMAL_FRAME_SPEED);
                    if (Projectile.frameCounter >= NORMAL_FRAME_SPEED)
                    {
                        Projectile.frameCounter = 0;
                    }
                    if (Projectile.frame >= 5)
                    {
                        Projectile.frame = 0;
                    }
                } break;
                case PRESS_STATE:
                {
                    Projectile.frame += (int)(Projectile.frameCounter / SHOOT_FRAME_SPEED);
                    Projectile.frame = (int)MathHelper.Clamp(Projectile.frame, 6, 19);
                    if (Projectile.frameCounter >= SHOOT_FRAME_SPEED)
                    {
                        Projectile.frameCounter = 0;
                    }
                    if(Projectile.frame == 8 || Projectile.frame == 10 || Projectile.frame == 12)
                    {
                        SoundStyle style = new SoundStyle("Terraria/Sounds/Item_83") with { Volume = .68f,  Pitch = 1f,  PitchVariance = 1.05f, };
                        SoundEngine.PlaySound(style, Projectile.position);
                    }
                } break;
                case SHOOT_STATE:
                {
                    Projectile.frame = 19;
                } break;
                case RELEASE_STATE:
                {
                    Projectile.frame -= Projectile.frameCounter / SHOOT_FRAME_SPEED > 0 ? 1 : 0;
                    Projectile.frame = (int)MathHelper.Clamp(Projectile.frame, 6, 19);

                    if(Projectile.frame == 16) Projectile.frame = 13;
                    if (Projectile.frameCounter >= SHOOT_FRAME_SPEED)
                    {
                        Projectile.frameCounter = 0;
                    }
                    
                } break;
            }

            // Main.NewText("frame: " + Projectile.frame + " frameCounter: " + Projectile.frameCounter);

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int width = texture.Width;
            int FrameHeight = Projectile.height;
            int CurrentFrameHeight = texture.Height / FRAME_COUNT * Projectile.frame;

            // draw the base part
            Rectangle BaseRect = new Rectangle(0, CurrentFrameHeight, width, FrameHeight);
            Vector2 BaseWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0f, CurrentFrameHeight));
            Vector2 BaseOrigin = new Vector2(width / 2f, CurrentFrameHeight + FrameHeight / 2f);
            MinionAIHelper.DrawPart(
                Projectile,
                texture,
                BaseWorldPos,
                BaseRect,
                lightColor,
                Projectile.rotation,
                BaseOrigin
            );

            // draw the tip part
            int TipTextureHeight = texture.Height / FRAME_COUNT * (FRAME_COUNT - 1);
            int TipWidth = 22;
            int TipHeight = 10;
            Rectangle TipRect = new Rectangle(0, TipTextureHeight, TipWidth, TipHeight);
            Vector2 TipWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(-21f, 24f));
            Vector2 TipOrigin = new Vector2(3f, 3f);
            MinionAIHelper.DrawPart(
                Projectile,
                texture,
                TipWorldPos,
                TipRect,
                lightColor,
                direction,
                TipOrigin
            );

            return false;
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

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(direction);
            writer.Write(lastTargetPosX);
            writer.Write(lastTargetPosY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            direction = reader.ReadSingle();
            lastTargetPosX = reader.ReadSingle();
            lastTargetPosY = reader.ReadSingle();
        }

    }
}
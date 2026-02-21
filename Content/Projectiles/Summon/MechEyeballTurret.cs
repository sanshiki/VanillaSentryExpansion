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
    public class MechEyeballTurret : ModProjectile
    {
        public const float Gravity = ModGlobal.SENTRY_GRAVITY;
        public const float MaxGravity = 20f;

        // animation constants
        private const int FRAME_NUM = 11;
        private const int REAL_FRAME_NUM = 17;
        private const int SWTICH_FRAME_SPEED = 2;
        private const float SWITCH_ROTATE_SPEED = ModGlobal.PI_FLOAT / (3 * SWTICH_FRAME_SPEED);
        private const float STICK_DOWN_DIST = 22f;
        private const float STICK_DOWN_SPEED = STICK_DOWN_DIST / (3 * SWTICH_FRAME_SPEED);
        private Vector2 ShootOffset = new Vector2(0, -22f);
        

        // predraw constants
        // size坐标系：碰撞箱
        // origin坐标系：矩形
        private const string TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/MechEyeballTurret";
        private Vector2 BaseSize = new Vector2(46, 46);
        private Vector2 BaseOrigin = new Vector2(22, 43);
        private Vector2 RedEyeSize = new Vector2(34, 22);
        private Vector2 RedEyeOrigin = new Vector2(11, 11);
        private Vector2 GreenEyeSize = new Vector2(22, 22);
        private Vector2 GreenEyeOrigin = new Vector2(11, 11);
        private Vector2 StickSize = new Vector2(10, 14);
        private Vector2 StickOrigin = new Vector2(5, 7);

        // states constants
        private const int RED_STATE = 0;
        private const int RED_TO_GREEN = 1;
        private const int GREEN_STATE = 2;
        private const int GREEN_TO_RED = 3;
        private const float SEARCH_RANGE = 1000f;
        private const float SWTICH_STATE_THRESHOLD = 220f;
        private const int STATE_MAINTAIN_DURATION = 60;

        // damage constants
        private const int RED_SHOOT_INTERVAL = 20;
        private const int GREEN_SHOOT_INTERVAL = 7;
        private const float RED_DMG_FACTOR = 0.7f;
        private const float GREEN_DMG_FACTOR = 1.0f;
        private const float RED_BULLET_SPEED = 12f;
        private const float PRED_RED_BULLET_SPEED = 35f;
        private const float GREEN_BULLET_SPEED = 4f;

        // variables
        private int State = RED_STATE;
        private float direction = 0f;
        private float AngleBeforeSwitch;
        private float CurrentStickDownDist;

        public override string Texture => TEXTURE_PATH;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = FRAME_NUM;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 66;
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
            Projectile.ai[0] = 0f; // shootTimer
            Projectile.ai[1] = 1f; // RealFrame
            Projectile.ai[2] = 0f; // StateMaintainCnt
        }

        public override void AI()
        {
            int shootTimer = (int)Projectile.ai[0];
            int RealFrame = (int)Projectile.ai[1];
            int StateMaintainCnt = (int)Projectile.ai[2];

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
                    SEARCH_RANGE, 
                    true, 
                    null).TargetNPC;

            // calculate direction
            float distance = 99999;
            if(target != null && (State == RED_STATE || State == GREEN_STATE))
            {
                Vector2 dirVec;
                if(State == RED_STATE)  // only use prediction when in red state
                {
                    dirVec = MinionAIHelper.PredictTargetPosition(Projectile.Center + ShootOffset, target.Center, target.velocity, PRED_RED_BULLET_SPEED, 60, 3) - Projectile.Center - ShootOffset;
                }
                else // otherwise, use target position
                {
                    dirVec = target.Center - Projectile.Center - ShootOffset;
                }
                distance = dirVec.Length();
                if (distance > 0f)
                    direction = dirVec.ToRotation();
            }

            switch (State)
            {
                case RED_STATE:
                {
                    if (target != null)
                    {
                        shootTimer++;

                        int shootInterval = RED_SHOOT_INTERVAL;

                        if (shootTimer >= shootInterval)
                        {
                            // Fire!
                            
                            Vector2 velocity = direction.ToRotationVector2() * RED_BULLET_SPEED;

                            if(Projectile.owner == Main.myPlayer)
                            {
                                Projectile.NewProjectileDirect(
                                    Projectile.GetSource_FromAI(),
                                    Projectile.Center + ShootOffset,
                                    velocity,
                                    // ProjectileID.Seed,
                                    ProjectileID.MiniRetinaLaser,
                                    (int)(Projectile.damage * RED_DMG_FACTOR),
                                    Projectile.knockBack,
                                    Projectile.owner);
                            }
                            shootTimer = 0; // Reset shoot animation
                        }
                    }
                    else
                    {
                        shootTimer = 0; // Reset if no target
                    }

                    RealFrame = 1;
                    Projectile.frame = RealFrame;
                    CurrentStickDownDist = 0;

                    // swtich state
                    if(StateMaintainCnt++ > STATE_MAINTAIN_DURATION && distance < SWTICH_STATE_THRESHOLD && target != null)
                    {
                        AngleBeforeSwitch = direction;
                        StateMaintainCnt = 0;
                        State = RED_TO_GREEN;
                        Projectile.netUpdate = true;
                    }
                } break;
                case RED_TO_GREEN:
                {
                    bool finished = Red2GreenAnimation();
                    if(finished)
                    {
                        State = GREEN_STATE;
                        Projectile.netUpdate = true;
                    }
                } break;
                case GREEN_STATE:
                {
                    if (target != null)
                    {
                        shootTimer++;

                        int shootInterval = GREEN_SHOOT_INTERVAL;

                        if (shootTimer >= shootInterval)
                        {
                            // Fire!
                            
                            Vector2 velocity = direction.ToRotationVector2() * GREEN_BULLET_SPEED;

                            if(Projectile.owner == Main.myPlayer)
                            {
                                Projectile.NewProjectileDirect(
                                Projectile.GetSource_FromAI(),
                                Projectile.Center + ShootOffset,
                                velocity,
                                // ProjectileID.Seed,
                                ModProjectileID.MechEyeballTurretEyeFire,
                                (int)(Projectile.damage * GREEN_DMG_FACTOR),
                                0,
                                Projectile.owner);
                            }
                            shootTimer = 0; // Reset shoot animation

                            SoundEngine.PlaySound(SoundID.Item34, Projectile.position);
                        }
                    }
                    else
                    {
                        shootTimer = 0; // Reset if no target
                    }

                    RealFrame = REAL_FRAME_NUM - 1;
                    Projectile.frame = FRAME_NUM - 1;
                    CurrentStickDownDist = 0;

                    // swtich state
                    if(StateMaintainCnt++ > STATE_MAINTAIN_DURATION && distance > SWTICH_STATE_THRESHOLD && target != null)
                    {
                        StateMaintainCnt = 0;
                        AngleBeforeSwitch = direction;
                        State = GREEN_TO_RED;
                        Projectile.netUpdate = true;
                    }
                } break;
                case GREEN_TO_RED:
                {
                    bool finished = Green2RedAnimation();
                    if(finished)
                    {
                        State = RED_STATE;
                        Projectile.netUpdate = true;
                    }
                } break;
            }

            // frame mapping
            if(RealFrame <= 3)
                Projectile.frame = RealFrame;
            else if(RealFrame > 3 && RealFrame <= 6)
                Projectile.frame = 3;
            else if(RealFrame > 6 && RealFrame <= 9)
                Projectile.frame = RealFrame - 3;
            else if(RealFrame > 9 && RealFrame <= 12)
                Projectile.frame = 6;
            else if(RealFrame > 12 && RealFrame <= 15)
                Projectile.frame = RealFrame - 6;

            // Main.NewText("State: " + State + ", RealFrame: " + RealFrame + ", CurrentStickDownDist: " + CurrentStickDownDist);

            Projectile.ai[0] = (float)shootTimer;
            Projectile.ai[2] = (float)StateMaintainCnt;
        }

        private bool Red2GreenAnimation()
        {
            int RealFrame = (int)Projectile.ai[1];
            bool finished = false;
            Projectile.frameCounter++;
            if(Projectile.frameCounter >= SWTICH_FRAME_SPEED)
            {
                Projectile.frameCounter = 0;
                RealFrame++;
            }
            if(RealFrame >= REAL_FRAME_NUM-2)
            {
                RealFrame = REAL_FRAME_NUM-2;
                finished = true;
            }

            if (RealFrame > 3 && RealFrame <= 6)
            {
                CurrentStickDownDist += STICK_DOWN_SPEED;
                CurrentStickDownDist = Math.Clamp(CurrentStickDownDist, 0, STICK_DOWN_DIST);
            }
            else if (RealFrame > 9 && RealFrame <= 12)
            {
                CurrentStickDownDist += STICK_DOWN_SPEED * -1;
                CurrentStickDownDist = Math.Clamp(CurrentStickDownDist, 0, STICK_DOWN_DIST);
            }

            Projectile.ai[1] = (float)RealFrame;
            return finished;
        }

        private bool Green2RedAnimation()
        {
            int RealFrame = (int)Projectile.ai[1];
            bool finished = false;
            Projectile.frameCounter++;
    
            if(Projectile.frameCounter >= SWTICH_FRAME_SPEED)
            {
                Projectile.frameCounter = 0;
                RealFrame--;
            }
            if(RealFrame <= 1)
            {
                RealFrame = 1;
                finished = true;
            }

            if (RealFrame > 3 && RealFrame <= 6)
            {
                CurrentStickDownDist += STICK_DOWN_SPEED * -1;
                CurrentStickDownDist = Math.Clamp(CurrentStickDownDist, 0, STICK_DOWN_DIST);
            }
            else if (RealFrame > 9 && RealFrame <= 12)
            {
                CurrentStickDownDist += STICK_DOWN_SPEED;
                CurrentStickDownDist = Math.Clamp(CurrentStickDownDist, 0, STICK_DOWN_DIST);
            }


            Projectile.ai[1] = (float)RealFrame;
            return finished;
        }

        private float CalculateRotation(float cur_angle, float target_angle, float speed)
        {
            // Normalize delta to [-pi, pi] to ensure shortest rotation direction
            float delta = MathHelper.WrapAngle(target_angle - cur_angle);

            // No movement if speed is zero or negative
            float maxStep = Math.Abs(speed);
            if (maxStep <= 0f)
            {
                return MathHelper.WrapAngle(cur_angle);
            }

            // Step toward target by at most maxStep, without overshooting
            float step = MathHelper.Clamp(delta, -maxStep, maxStep);
            float next = cur_angle + step;

            // Keep angle wrapped for consistency
            return MathHelper.WrapAngle(next);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int RealFrame = (int)Projectile.ai[1];
            int width = (int)Projectile.width;
            int height = (int)Projectile.height;
            Texture2D TextureSprite = ModContent.Request<Texture2D>(TEXTURE_PATH).Value;
            Rectangle RedTextureRect = new Rectangle(12, 0, (int) RedEyeSize.X, (int) RedEyeSize.Y);
            Rectangle GreenTextureRect = new Rectangle(12, TextureSprite.Height - height, (int) GreenEyeSize.X, (int) GreenEyeSize.Y);
            Rectangle StickTextureRect = new Rectangle(0, 0, (int) StickSize.X, (int) StickSize.Y);
            int CurrentFrameHeight = height * Projectile.frame;
            Vector2 CurrentFramePos = new Vector2(width, CurrentFrameHeight);
            Rectangle CurrentBaseTextureRect = new Rectangle(0, CurrentFrameHeight + height - (int) BaseSize.Y, width, (int) BaseSize.Y);

            // always draw stick
            Vector2 StickWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, -4));
            MinionAIHelper.DrawPart(
                Projectile,
                TextureSprite,
                StickWorldPos,
                StickTextureRect,
                lightColor,
                Projectile.rotation,
                StickOrigin
            );

            switch (State)
            {
                case RED_STATE:
                {
                    Vector2 ReadEyeWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f));
                    MinionAIHelper.DrawPart(
                        Projectile,
                        TextureSprite,
                        ReadEyeWorldPos,
                        RedTextureRect,
                        lightColor,
                        direction,
                        RedEyeOrigin
                    );
                } break;
                case RED_TO_GREEN:
                case GREEN_TO_RED:
                {
                    Vector2 NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f);
                    float NextAngle = direction;
                    int CurrentFrame = RealFrame;
                    bool isRed2Green = State == RED_TO_GREEN;

                    if(CurrentFrame <= 3)
                    {
                        NextAngle = CalculateRotation(direction, isRed2Green ? ModGlobal.PI_FLOAT/2 : AngleBeforeSwitch, SWITCH_ROTATE_SPEED);
                        direction = NextAngle;
                    }
                    else if(CurrentFrame > 3 && CurrentFrame <= 6)
                    {
                        NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f + CurrentStickDownDist);
                    }
                    else if(CurrentFrame > 6 && CurrentFrame <= 9)
                    {
                        NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f + STICK_DOWN_DIST);
                    }
                    else if(CurrentFrame > 9 && CurrentFrame <= 12)
                    {
                        NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f + CurrentStickDownDist);
                    }
                    else if(CurrentFrame > 12 && CurrentFrame <= 15)
                    {
                        NextAngle = CalculateRotation(direction, isRed2Green ? AngleBeforeSwitch : ModGlobal.PI_FLOAT/2, SWITCH_ROTATE_SPEED);
                        direction = NextAngle;
                    }
                    if(CurrentFrame < 8)
                    {
                        Vector2 ReadEyeWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, NextPos);
                        MinionAIHelper.DrawPart(
                            Projectile,
                            TextureSprite,
                            ReadEyeWorldPos,
                            RedTextureRect,
                            lightColor,
                            direction,
                            RedEyeOrigin
                        );
                    }
                    else
                    {
                        Vector2 GreenEyeWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, NextPos);
                        MinionAIHelper.DrawPart(
                            Projectile,
                            TextureSprite,
                            GreenEyeWorldPos,
                            GreenTextureRect,
                            lightColor,
                            NextAngle,
                            GreenEyeOrigin
                        );
                    }
                } break;
                case GREEN_STATE:
                {
                    Vector2 GreenEyeWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, -(height - GreenEyeSize.Y - 4f) / 2f));
                    MinionAIHelper.DrawPart(
                        Projectile,
                        TextureSprite,
                        GreenEyeWorldPos,
                        GreenTextureRect,
                        lightColor,
                        direction,
                        GreenEyeOrigin
                    );
                } break;
                // case GREEN_TO_RED:
                // {
                //     Vector2 NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f);
                //     float NextAngle = ModGlobal.PI_FLOAT/2;
                //     int CurrentFrame = RealFrame;

                //     if(CurrentFrame <= 3)
                //     {
                //         NextAngle = CalculateRotation(Projectile.rotation, ModGlobal.PI_FLOAT/2, SWITCH_ROTATE_SPEED);
                //     }
                //     else if(CurrentFrame > 3 && CurrentFrame <= 6)
                //     {
                //         float step = (float)(CurrentFrame - 3) * 7;
                //         NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f + step);
                //     }
                //     else if(CurrentFrame > 6 && CurrentFrame <= 9)
                //     {
                //         NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f + 22f);
                //     }
                //     else if(CurrentFrame > 9 && CurrentFrame <= 12)
                //     {
                //         float step = (float)(12 - CurrentFrame) * 7;
                //         NextPos = new Vector2(0, -(height - RedEyeSize.Y - 4f) / 2f + step);
                //     }
                //     else if(CurrentFrame > 12 && CurrentFrame <= 15)
                //     {
                //         NextAngle = CalculateRotation(Projectile.rotation, AngleBeforeSwitch, SWITCH_ROTATE_SPEED);
                //     }
                //     if(CurrentFrame < 8)
                //     {
                //         Vector2 ReadEyeWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, NextPos);
                //         MinionAIHelper.DrawPart(
                //             Projectile,
                //             TextureSprite,
                //             ReadEyeWorldPos,
                //             RedTextureRect,
                //             lightColor,
                //             NextAngle,
                //             RedEyeOrigin
                //         );
                //     }
                //     else
                //     {
                //         Vector2 GreenEyeWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, NextPos);
                //         MinionAIHelper.DrawPart(
                //             Projectile,
                //             TextureSprite,
                //             GreenEyeWorldPos,
                //             GreenTextureRect,
                //             lightColor,
                //             NextAngle,
                //             GreenEyeOrigin
                //         );
                //     }
                // } break;
            }

            // always draw base
            Vector2 BaseOriginCurrent = new Vector2(BaseOrigin.X, BaseOrigin.Y + CurrentFrameHeight);
            Vector2 BaseWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, CurrentFrameHeight + height / 2f));
            MinionAIHelper.DrawPart(
                Projectile,
                TextureSprite,
                BaseWorldPos,
                CurrentBaseTextureRect,
                lightColor,
                Projectile.rotation,
                BaseOriginCurrent
            );

            

            return false;
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

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(direction);
            writer.Write(AngleBeforeSwitch);
            writer.Write(CurrentStickDownDist);
        }
        
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            direction = reader.ReadSingle();
            AngleBeforeSwitch = reader.ReadSingle();
            CurrentStickDownDist = reader.ReadSingle();
        }

    }
}
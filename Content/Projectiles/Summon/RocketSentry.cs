using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.CameraModifiers;

using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class RocketSentry : SentryWithSpawnAnime
    {
        /* ------------------ variables ------------------ */
        private NonUniformFloatIntPacker timerPacker = new NonUniformFloatIntPacker(
            TOTAL_SHOOT_INTERVAL, // shootTimer
            SPAWN_TIME // spawnTimer
        );

        // textures
        private const string BASE_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/RocketSentryV2Base";
        private const string GUN_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/RocketSentryV2Gun";
        private const string FRONT_BOARD_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/SentryFrontBoard";
        private const string BACK_BOARD_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/SentryBackBoard";
        public override string Texture => BASE_TEXTURE_PATH;

        // sentry state
        private const bool USE_PREDICTION = false;

        // sentry parameters
        private const float REAL_BULLET_SPEED = 25f;
        private const float PRED_BULLET_SPEED = 25f;

        // buff constants
        private const float ENHANCEMENT_FACTOR = 0.75f;

        // animation frame parameters
        private const int FRAME_COUNT = 37;
        private const int NORMAL_FRAME_SPEED = 20;
        private const int SHOOT_FRAME_SPEED = 5;

        // private const int SHOOT_INTERVAL = 6;
        private const int TOTAL_SHOOT_INTERVAL = 80;
        // private const int INTERVAL_BETWEEN_ROCKETS = 30;
        // private const int ROCKET_NUM = 2;
        private const int SPAWN_TIME = 2*26;
        private const float MAX_RANGE = 1500f;
        // gravity
        public static float Gravity = ModGlobal.SENTRY_GRAVITY;
        public static float MaxGravity = 20f;

        // direction
        private Vector2 direction = new Vector2(0, -1);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 52;
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
            PunchCameraModifier modifier = new PunchCameraModifier(Projectile.Center, (-MathHelper.PiOver2).ToRotationVector2(), 10f, 6f, 10, 1000f, "RocketSentry");
            Main.instance.CameraModifiers.Add(modifier);
            Projectile.ai[1] = -ModGlobal.PI_FLOAT/2f;
        }

        public override void AI()
        {
            // decode
            int[] timer_decode_values = timerPacker.Decode(Projectile.ai[0]);
            int shootTimer = timer_decode_values[0];
            int spawnTimer = timer_decode_values[1];
            float directionAngle = Projectile.ai[1];
            direction = directionAngle.ToRotationVector2();
            bool onGround = Projectile.ai[2] != 0;

            Player owner = Main.player[Projectile.owner];

            // if on ground, spawnTimer++
            if(onGround)
            {
                spawnTimer++;
            }

            // apply gravity
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxGravity)
            {
                Projectile.velocity.Y = MaxGravity;
            }

            // adjust gun
            if (spawnTimer >= SPAWN_TIME / 2 && spawnTimer < SPAWN_TIME)
            {
                // (0, -1) -> (-1, 0)
                float GunAdjustProcess = (float)Math.Min(Math.Max((float)(spawnTimer - SPAWN_TIME/2) / SPAWN_TIME * 2, 0.0f), 1.0f);
                float ang = GunAdjustProcess * ModGlobal.PI_FLOAT / 2;
                direction = new Vector2(-(float)Math.Sin(ang), -(float)Math.Cos(ang));
                direction.Normalize();
            }


            // Targeting
            // NPC target = FindTarget(1500f); // Range: 1500 pixels
            NPC target = MinionAIHelper.SearchForTargets(
                owner, 
                Projectile, 
                MAX_RANGE, 
                true, 
                null).TargetNPC;

            if (target != null && spawnTimer > SPAWN_TIME)
            {
                // calculate direction
                Vector2 PredictedPos = target.Center;
                if(USE_PREDICTION)
                {

                    PredictedPos = MinionAIHelper.PredictTargetPosition(
                        Projectile, target, PRED_BULLET_SPEED, 60, 5);
                }
                direction = PredictedPos - Projectile.Center;
                direction.Normalize();

                // if ((shootTimer >= INTERVAL_BETWEEN_ROCKETS && rocketCount == 0) ||
                //     (shootTimer >= INTERVAL_BETWEEN_ROCKETS*2 && rocketCount == 1))
                if (shootTimer >= TOTAL_SHOOT_INTERVAL)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0)
                {
                    // Fire!
                    Vector2 bulletVelocity = direction * REAL_BULLET_SPEED;

                    Vector2 bulletOffset = new Vector2(0f, -5f) + direction * 27f;

                    if(Projectile.owner == Main.myPlayer)
                    {
                        Projectile rocket = Projectile.NewProjectileDirect(
                            Projectile.GetSource_FromAI(),
                            Projectile.Center + bulletOffset,
                            bulletVelocity,
                            // ProjectileID.RocketI,
                            ModProjectileID.RocketSentryBullet,
                            Projectile.damage,
                            Projectile.knockBack,
                            Projectile.owner);


                        // rocket.DamageType = DamageClass.Summon;
                        // rocket.hostile = false;
                        // rocket.friendly = true;
                        // rocket.usesLocalNPCImmunity = true;
                        // rocket.usesIDStaticNPCImmunity = true;
                        // rocket.localNPCHitCooldown = 20;
                        // Main.NewText("Damage: " + Projectile.damage + "Rocket Damage: " + rocket.damage);
                        rocket.ai[0] = target.whoAmI;
                    }

                    Projectile.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Item11, Projectile.Center);
                }

                // int shootInterval = TOTAL_SHOOT_INTERVAL;
                // if(owner.HasBuff(BUFF_ID))
                // {
                //     shootInterval = (int)(shootInterval * ENHANCEMENT_FACTOR);
                // }
                


            }

            shootTimer++;
            if (shootTimer >= TOTAL_SHOOT_INTERVAL)
            {
                shootTimer = TOTAL_SHOOT_INTERVAL;
            }

            // Animation
            // UpdateAnimation(target);
            Projectile.spriteDirection = direction.X > 0 ? 1 : -1;

            Projectile.ai[0] = timerPacker.Encode(
                shootTimer,
                spawnTimer >= SPAWN_TIME ? SPAWN_TIME : spawnTimer
            );
            Projectile.ai[1] = direction.ToRotation();
            Projectile.ai[2] = onGround ? 1f : 0f;
        }


        // private void UpdateAnimation(NPC target)
        // {
        //     Projectile.frameCounter++;
            
        //     if(spawnTimer < SPAWN_TIME)
        //     {
        //         Projectile.frame = FRAME_COUNT - spawnTimer / 2 - 1;
        //     }
        //     else if (target != null)
        //     {
        //         // face towards target
        //         float angle = (float)Math.Atan2(direction.Y, Math.Abs(direction.X));    // -PI/2 to PI/2
        //         int frame = 0;

        //         float step = (float)(Math.PI / 36f);

        //         if (Math.Abs(angle) < step)
        //         {
        //             frame = 0;
        //         }
        //         else if (angle > step)
        //         {
        //             frame = (int)(Math.Abs(angle) / step);
        //             if (frame > 8)
        //             {
        //                 frame = 8;
        //             }
        //         }
        //         else if (angle < -step)
        //         {
        //             frame = (int)(Math.Abs(angle) / step) + 9;
        //             if (frame >= 25)
        //             {
        //                 frame = 25;
        //             }
        //         }
        //         Projectile.frame = frame;

        //         // face towards target
        //         Projectile.spriteDirection = direction.X > 0 ? 1 : -1;
        //     }

        //     // Main.NewText("frame: " + Projectile.frame + " spawnTimer: " + spawnTimer);
        // }

        public override bool PreDraw(ref Color lightColor)
        {
            int[] timer_decode_values = timerPacker.Decode(Projectile.ai[0]);
            int spawnTimer = timer_decode_values[1];
            float directionAngle = Projectile.ai[1];
            direction = directionAngle.ToRotationVector2();

            Texture2D BaseTexture = ModContent.Request<Texture2D>(BASE_TEXTURE_PATH).Value;
            Texture2D GunTexture = ModContent.Request<Texture2D>(GUN_TEXTURE_PATH).Value;
            Texture2D FrontBoardTexture = ModContent.Request<Texture2D>(FRONT_BOARD_TEXTURE_PATH).Value;
            Texture2D BackBoardTexture = ModContent.Request<Texture2D>(BACK_BOARD_TEXTURE_PATH).Value;

            float SpawnProcess = (float)Math.Min((float)spawnTimer / SPAWN_TIME * 2, 1.0f);
            float GunAdjustProcess = (float)Math.Min(Math.Max((float)(spawnTimer - SPAWN_TIME/2) / SPAWN_TIME * 2, 0.0f), 1.0f);
            float SpawnStartY = (BaseTexture.Height + 22f) * (1 - SpawnProcess);

            
            // always draw base
            Vector2 BaseWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0));
            Vector2 BaseOrigin = new Vector2(BaseTexture.Width / 2, BaseTexture.Height / 2);
            float ClipThreshold = BaseWorldPos.Y + 21f;
            MinionAIHelper.DrawPart(
                Projectile,
                BaseTexture,
                BaseWorldPos,
                new Rectangle(0, 0, BaseTexture.Width, BaseTexture.Height),
                lightColor,
                Projectile.rotation,
                BaseOrigin
            );

            // draw back board
            float BackBoardWorldY = SpawnStartY;
            Vector2 BackBoardWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, BackBoardWorldY)); // 1 3
            Vector2 BackBoardOrigin = new Vector2(BackBoardTexture.Width / 2, BackBoardTexture.Height / 2);
            Rectangle BackBoardRect = MinionAIHelper.CalculateClipRect(new Rectangle(0, 0, BackBoardTexture.Width, BackBoardTexture.Height), BackBoardWorldPos, BackBoardOrigin, -1f, ClipThreshold);
            // Main.NewText("BackBoardRect:" + BackBoardRect + " WorldPos:" + BackBoardWorldPos + " Origin:" + BackBoardOrigin + " ClipThreshold:" + ClipThreshold);
            MinionAIHelper.DrawPart(
                Projectile,
                BackBoardTexture,
                BackBoardWorldPos,
                BackBoardRect,
                lightColor,
                Projectile.rotation,
                BackBoardOrigin
            );

            // draw gun
            float GunWorldY = SpawnStartY-10f;
            Vector2 GunWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0f, GunWorldY));
            Vector2 GunOrigin = new Vector2(GunTexture.Width / 2 - 6 * Projectile.spriteDirection, 16);
            Vector2 GunWorldPosTemp = new Vector2(GunWorldPos.Y, GunWorldPos.X);
            Vector2 GunOriginTemp = new Vector2(GunOrigin.Y, GunOrigin.X);
            Rectangle GunRectClip = MinionAIHelper.CalculateClipRect(new Rectangle(0, 0, GunTexture.Width, GunTexture.Height), GunWorldPosTemp, GunOrigin, ClipThreshold, -1f);
            // Main.NewText("GunRectClip:" + GunRectClip + " GunWorldPos:" + GunWorldPosTemp + " GunOrigin:" + GunOriginTemp + " ClipThreshold:" + ClipThreshold);
            Rectangle GunRect = new Rectangle(GunTexture.Width - GunRectClip.Width, GunRectClip.Y, GunRectClip.Width , GunTexture.Height);
            MinionAIHelper.DrawPart(
                Projectile,
                GunTexture,
                GunWorldPos,
                GunRect,
                lightColor,
                // Projectile.rotation,
                direction.ToRotation() + (Projectile.spriteDirection == 1 ? 0 : MathHelper.Pi),
                GunOrigin
            );

            // draw front board
            float FrontBoardWorldY = SpawnStartY-6f;
            Vector2 FrontBoardWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(-7 * Projectile.spriteDirection, FrontBoardWorldY)); // 7 9
            Vector2 FrontBoardOrigin = new Vector2(FrontBoardTexture.Width / 2, FrontBoardTexture.Height / 2);
            Rectangle FrontBoardRect = MinionAIHelper.CalculateClipRect(new Rectangle(0, 0, FrontBoardTexture.Width, FrontBoardTexture.Height), FrontBoardWorldPos, FrontBoardOrigin, -1f, ClipThreshold);
            
            MinionAIHelper.DrawPart(
                Projectile,
                FrontBoardTexture,
                FrontBoardWorldPos,
                FrontBoardRect,
                lightColor,
                Projectile.rotation,
                FrontBoardOrigin
            );


            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool onGround = true;
            Projectile.ai[2] = onGround ? 1f : 0f;
            // Projectile.velocity = Vector2.Zero;
            Projectile.velocity.X = 0f;
            Projectile.netUpdate = true;
            return false;
        }

        public override void SetAttached(bool attached)
        {
            bool onGround = attached;
            Projectile.ai[2] = onGround ? 1f : 0f;
            // Projectile.tileCollide = !attached;
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
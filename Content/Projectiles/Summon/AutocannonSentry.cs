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
using SummonerExpansionMod.Content.Dusts;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class AutocannonSentry : SentryWithSpawnAnime
    {
        /* ------------------ variables ------------------ */
        private NonUniformFloatIntPacker timerPacker = new NonUniformFloatIntPacker(
            TOTAL_SHOOT_INTERVAL, // shootTimer
            SPAWN_TIME, // spawnTimer
            1 // isShooting
        );

        // textures
        private const string BASE_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/RocketSentryV2Base";
        private const string GUN_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/AutocannonSentryGun";
        private const string FRONT_BOARD_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/SentryFrontBoard";
        private const string BACK_BOARD_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/SentryBackBoard";
        public override string Texture => BASE_TEXTURE_PATH;

        // sentry state
        private const bool USE_PREDICTION = true;

        // sentry parameters
        private const float REAL_BULLET_SPEED = 80f;
        private const float PRED_BULLET_SPEED = 65f;

        // buff constants
        private const float ENHANCEMENT_FACTOR = 0.75f;

        // animation frame parameters
        private const int FRAME_COUNT = 4;
        private const int NORMAL_FRAME_SPEED = 20;
        private const int SHOOT_FRAME_SPEED = 3;

        // private const int SHOOT_INTERVAL = 6;
        private const int TOTAL_SHOOT_INTERVAL = 130;
        private const int INVERVAL_BETWEEN_SHOTS = 30;
        private const int SHOT_NUM = 3;
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
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            PunchCameraModifier modifier = new PunchCameraModifier(Projectile.Center, (-MathHelper.PiOver2).ToRotationVector2(), 10f, 6f, 10, 1000f, "AutocannonSentry");
            Main.instance.CameraModifiers.Add(modifier);

            Projectile.ai[1] = -ModGlobal.PI_FLOAT/2f;
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

            // DynamicParamManager.Register("GunTipDistOffset", 0f, -50f, 50f);
            // DynamicParamManager.Register("GunTipPosOffset", 0f, -50f, 50f);
            // DynamicParamManager.Register("GunTipOriginOffsetX", 0f, -50f, 50f);
            // DynamicParamManager.Register("GunTipOriginOffsetY", 0f, -50f, 50f);
        }

        public override void AI()
        {
            // decode
            int[] timer_decode_values = timerPacker.Decode(Projectile.ai[0]);
            int shootTimer = timer_decode_values[0];
            int spawnTimer = timer_decode_values[1];
            bool isShooting = timer_decode_values[2] != 0;
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
                Vector2 ShootPos = Projectile.Center + new Vector2(0, -17f);
                if(USE_PREDICTION)
                {

                    PredictedPos = MinionAIHelper.PredictTargetPosition(
                        ShootPos, target.Center, target.velocity, PRED_BULLET_SPEED, 60, 1);
                }
                direction = PredictedPos - ShootPos;
                direction.Normalize();

                // if ((shootTimer >= INTERVAL_BETWEEN_ROCKETS && rocketCount == 0) ||
                //     (shootTimer >= INTERVAL_BETWEEN_ROCKETS*2 && rocketCount == 1))
                if (shootTimer >= TOTAL_SHOOT_INTERVAL)
                {
                    shootTimer = 0;
                }
                if(shootTimer == 0 || shootTimer == INVERVAL_BETWEEN_SHOTS || shootTimer == INVERVAL_BETWEEN_SHOTS*2)
                {
                    // Whip add damage
                    int addDamage = MinionAIHelper.AccumulateWhipDamage(target);
                    int totalDamage = Projectile.damage + addDamage;

                    Vector2 BulletShellVelocity = direction.RotatedBy(-2f/3f*ModGlobal.PI_FLOAT*Projectile.spriteDirection) * 3.5f;
                    Dust BulletShellDust = Dust.NewDustDirect(Projectile.Center + new Vector2(-5f*Projectile.spriteDirection, -18f), 1, 1, ModContent.DustType<BigBulletShell>(), BulletShellVelocity.X, BulletShellVelocity.Y);
                    BulletShellDust.scale = 0.65f;
                    BulletShellDust.alpha = 0;

                    // Fire!
                    Vector2 bulletVelocity = direction * REAL_BULLET_SPEED;

                    Vector2 bulletOffset = direction * 50f;

                    if(Projectile.owner == Main.myPlayer)
                    {
                        Projectile bullet = Projectile.NewProjectileDirect(
                            Projectile.GetSource_FromAI(),
                            ShootPos + bulletOffset,
                            bulletVelocity,
                            // ProjectileID.RocketI,
                            // ModProjectileID.RocketSentryBullet,
                            // ProjectileID.ExplosiveBullet,
                            ModProjectileID.AutocannonSentryBullet,
                            // ProjectileID.BulletHighVelocity,
                            totalDamage,
                            Projectile.knockBack,
                            Projectile.owner,
                            0,
                            70 // self damage
                        );
                    }

                    isShooting = true;

                    Projectile.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Item38, Projectile.Center);
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
            UpdateAnimation(ref isShooting);
            Projectile.spriteDirection = direction.X > 0 ? 1 : -1;

            Projectile.ai[0] = timerPacker.Encode(
                shootTimer,
                spawnTimer >= SPAWN_TIME ? SPAWN_TIME : spawnTimer,
                isShooting ? 1 : 0
            );
            Projectile.ai[1] = direction.ToRotation();
            Projectile.ai[2] = onGround ? 1f : 0f;
        }


        private void UpdateAnimation(ref bool isShooting)
        {
            if(isShooting)
            {
                if(Projectile.frameCounter >= SHOOT_FRAME_SPEED)
                {
                    Projectile.frame = (Projectile.frame + 1) % FRAME_COUNT;
                    Projectile.frameCounter = 0;
                    if(Projectile.frame >= FRAME_COUNT - 1)
                    {
                        isShooting = false;
                        Projectile.frame = FRAME_COUNT - 1;
                    }
                }
            }
            else
            {
                Projectile.frame = FRAME_COUNT - 1;
                Projectile.frameCounter = 0;
            }

            Projectile.frameCounter++;
            if(Projectile.frameCounter >= SHOOT_FRAME_SPEED)
            {
                Projectile.frameCounter = SHOOT_FRAME_SPEED;
            }
        }

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
            int GunWidth = GunTexture.Width - (GunAdjustProcess >= 1.0f ? 0 : 42);
            int GunHeight = GunTexture.Height / FRAME_COUNT;
            int CurrentFrameHeight = GunHeight * Projectile.frame;
            float GunWorldY = SpawnStartY-17f;
            Vector2 GunWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0f, GunWorldY));
            Vector2 GunOrigin = new Vector2(GunWidth / 2 + (GunAdjustProcess >= 1.0f ? -14 : 8) * Projectile.spriteDirection, 9);
            Vector2 GunWorldPosTemp = new Vector2(GunWorldPos.Y, GunWorldPos.X);
            Vector2 GunOriginTemp = new Vector2(GunOrigin.Y, GunOrigin.X);
            Rectangle GunRectClip = MinionAIHelper.CalculateClipRect(new Rectangle(0, 0, GunWidth, GunHeight), GunWorldPosTemp, GunOrigin, ClipThreshold, -1f);
            // Main.NewText("GunRectClip:" + GunRectClip + " GunWorldPos:" + GunWorldPosTemp + " GunOrigin:" + GunOriginTemp + " ClipThreshold:" + ClipThreshold);
            if(SpawnProcess >= 1.0f)
            {
                GunRectClip = new Rectangle(0, 0, GunWidth, GunHeight);
            }
            Rectangle GunRect = new Rectangle(GunWidth - GunRectClip.Width, GunRectClip.Y + CurrentFrameHeight, GunRectClip.Width , GunHeight);
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

            // draw gun tip
            if(SpawnProcess >= 1.0f && GunAdjustProcess < 1.0f)
            {
                // float GunTipOffset = DynamicParamManager.Get("GunTipDistOffset").value; // 17f
                // float GunTipPosOffset = DynamicParamManager.Get("GunTipPosOffset").value; // 5.2f
                // float GunTipOriginOffsetX = DynamicParamManager.Get("GunTipOriginOffsetX").value; // 3f
                // float GunTipOriginOffsetY = DynamicParamManager.Get("GunTipOriginOffsetY").value; // 13f
                float GunTipOffset = 17f;
                float GunTipPosOffset = 5.2f;
                float GunTipOriginOffsetX = 3f;
                float GunTipOriginOffsetY = 13f;
                Vector2 GunTipWorldPos = GunWorldPos + direction * GunTipOffset + new Vector2(0, GunTipPosOffset);
                Vector2 GunTipOrigin = new Vector2(GunTexture.Width/2f + GunTipOriginOffsetX * Projectile.spriteDirection, GunTipOriginOffsetY);
                Rectangle GunTipRect = new Rectangle(48, 0, 42, GunHeight);
                MinionAIHelper.DrawPart(
                    Projectile,
                    GunTexture,
                    GunTipWorldPos,
                    GunTipRect,
                    lightColor,
                    (1-GunAdjustProcess) * ModGlobal.PI_FLOAT*3f/2f,
                    GunTipOrigin
                );
            }

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
            if(Projectile.ai[2]==0) Projectile.netUpdate = true;
            Projectile.ai[2] = onGround ? 1f : 0f;
            // Projectile.velocity = Vector2.Zero;
            Projectile.velocity.X = 0f;
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
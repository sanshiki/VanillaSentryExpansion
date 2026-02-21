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
    public class TempleSentry : ModProjectile
    {
        private const bool USE_PREDICTION = true;
        private const int PRED_BULLET_SPEED = 40;
        private const int REAL_BULLET_SPEED = 40;

        private const int EYEBEAM_STATE = 0;
        private const int HEATRAY_STATE = 1;
        private const int REST_STATE = 2;
        

        private const int EYEBEAM_MIN_SHOOT_INTERVAL = 60;
        private const int EYEBEAM_MAX_SHOOT_INTERVAL = 10;
        private const int HEATRAY_SHOOT_INTERVAL = 10;
        private const int EYEBEAM_MAX_CHARGE_NUM = 10;
        private const int HEATRAY_MAX_CHARGE_NUM = 20;
        private const float HEAYRAY_DAMAGE_FACTOR = 0.75f;
        private const int REST_TIME = 120;
        private const int TIMEOUT_MAX = 60*4;
        
        public const float Gravity = ModGlobal.SENTRY_GRAVITY;
        public const float MaxGravity = 20f;

        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/TempleSentry";

        private const string HEATRAY_TEXTURE = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/TempleSentryHeatRay";

        private const string CORE_TEXTURE = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/TempleSentryCore";

        private int State = EYEBEAM_STATE;
        private int shootInterval = EYEBEAM_MIN_SHOOT_INTERVAL;
        private bool hasTarget = false;

        private int CoreLightCnt = 30;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 118;
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
            Projectile.ai[0] = 50; // shootTimer
            Projectile.ai[1] = 0; // chargeCnt
            Projectile.ai[2] = 0; // timeoutCnt
        }

        public override void AI()
        {
            int shootTimer = (int)Projectile.ai[0];
            int chargeCnt = (int)Projectile.ai[1];
            int timeoutCnt = (int)Projectile.ai[2];
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
                    1500f, 
                    true, 
                    null).TargetNPC;

            // Animation
            UpdateAnimation(target, shootTimer);

            Vector2 bulletOffset = new Vector2(0, -30f);
            if (target != null)
            {
                Vector2 ShootCenter = Projectile.Center + bulletOffset;
                hasTarget = true;
                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                }
                if (shootTimer == 0)
                {
                    chargeCnt++;
                    // Main.NewText("chargeCnt: " + chargeCnt);
                    switch (State)
                    {
                        case EYEBEAM_STATE:
                            {
                                shootInterval = (int)MathHelper.Lerp(EYEBEAM_MIN_SHOOT_INTERVAL, EYEBEAM_MAX_SHOOT_INTERVAL, (float)chargeCnt / EYEBEAM_MAX_CHARGE_NUM);
                                ShootEyeBeam(target, ShootCenter);
                                if (chargeCnt >= EYEBEAM_MAX_CHARGE_NUM)
                                {
                                    shootInterval = HEATRAY_SHOOT_INTERVAL;
                                    chargeCnt = 0;
                                    State = HEATRAY_STATE;
                                    SoundEngine.PlaySound(SoundID.Item45, Projectile.position);
                                }
                            }
                            break;
                        case HEATRAY_STATE:
                            {
                                ShootHeatRay(target, ShootCenter);
                                if (chargeCnt >= HEATRAY_MAX_CHARGE_NUM)
                                {
                                    shootInterval = 1;
                                    chargeCnt = 0;
                                    State = REST_STATE;
                                }
                            }
                            break;
                        case REST_STATE:
                            {
                                if (chargeCnt >= REST_TIME)
                                {
                                    chargeCnt = 0;
                                    shootTimer = EYEBEAM_MIN_SHOOT_INTERVAL - 10;
                                    State = EYEBEAM_STATE;
                                }
                            }
                            break;
                        default:
                            break;
                    }

                    Projectile.netUpdate = true;
                }
            }
            else
            {
                timeoutCnt++;
                hasTarget = false;
                if(timeoutCnt > TIMEOUT_MAX)
                {
                    State = EYEBEAM_STATE;
                    chargeCnt = 0;
                    timeoutCnt = 0;
                    shootTimer = EYEBEAM_MIN_SHOOT_INTERVAL - 10;
                    Projectile.netUpdate = true;
                }
            }
            

            shootTimer++;
            if(shootTimer >= shootInterval)
                shootTimer = shootInterval;

            Projectile.ai[0] = (float)shootTimer;
            Projectile.ai[1] = (float)chargeCnt;
            Projectile.ai[2] = (float)timeoutCnt;
            

        }

        private void UpdateAnimation(NPC target, int shootTimer)
        {
            int chargeCnt = (int)Projectile.ai[1];
            switch (State)
            {
                case HEATRAY_STATE:
                {
                    Vector2 position = Projectile.Center + new Vector2(20, -32f);
                    Dust dust1 = Dust.NewDustPerfect(position, 170, new Vector2(4f, 0f), 0, new Color(255,255,255), 1.5f);
                    dust1.noGravity = true;
                    position = Projectile.Center + new Vector2(0, -60f);
                    Dust dust2 = Dust.NewDustPerfect(position, 170, new Vector2(0, -4f), 0, new Color(255,255,255), 1.5f);
                    dust2.noGravity = true;
                    position = Projectile.Center + new Vector2(-20, -32f);
                    Dust dust3 = Dust.NewDustPerfect(position, 170, new Vector2(-4f, 0f), 0, new Color(255,255,255), 1.5f);
                    dust3.noGravity = true;
                } break;
                case REST_STATE:
                {
                    if (chargeCnt == 1)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                float AngVelOffset = MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/6f, ModGlobal.PI_FLOAT/6f);
                                Vector2 position = Projectile.Center + new Vector2(20, -32f);
                                Dust dust1 = Dust.NewDustDirect(position, 1, 1, 31, 4.5f, 0f, 0, new Color(255, 255, 255), 2.5f);
                                dust1.noGravity = true;
                                dust1.fadeIn = 3f;
                                dust1.velocity = dust1.velocity.RotatedBy(AngVelOffset);
                                position = Projectile.Center + new Vector2(0, -60f);
                                Dust dust2 = Dust.NewDustDirect(position, 1, 1, 31, 0, -4.5f, 0, new Color(255, 255, 255), 2.5f);
                                dust2.noGravity = true;
                                dust2.fadeIn = 3f;
                                position = Projectile.Center + new Vector2(-20, -32f);
                                Dust dust3 = Dust.NewDustDirect(position, 1, 1, 31, -4.5f, 0f, 0, new Color(255, 255, 255), 2.5f);
                                dust3.noGravity = true;
                                dust3.fadeIn = 3f;
                            }
                    }
                } break;
                default:
                    break;
            }
                    

        }

        private void ShootEyeBeam(NPC target, Vector2 ShootCenter)
        {
            // Fire!
            Vector2 PredictedPos = target.Center;
            if (USE_PREDICTION)
            {
                PredictedPos = MinionAIHelper.PredictTargetPosition(ShootCenter, target.Center, target.velocity, PRED_BULLET_SPEED, 60, 2);
            }
            Vector2 direction = PredictedPos - ShootCenter;
            float distance = direction.Length();
            direction.Normalize();
            direction *= REAL_BULLET_SPEED; // Bullet speed

            if(Projectile.owner == Main.myPlayer)
            {
                Projectile beam = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    ShootCenter,
                    direction,
                    ModProjectileID.TempleSentryEyeBeamBullet,
                    // ProjectileID.EyeBeam,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);
            }

            // beam.DamageType = DamageClass.Summon;
            // beam.friendly = true;
            // beam.hostile = false;
            // beam.tileCollide = true;
            // beam.usesLocalNPCImmunity = true;
            // beam.localNPCHitCooldown = 20;
            // ProjectileID.Sets.SentryShot[beam.type] = true;
        }

        private void ShootHeatRay(NPC target, Vector2 ShootCenter)
        {
            // Fire!
            Vector2 PredictedPos = target.Center;
            Vector2 direction = PredictedPos - ShootCenter;
            float distance = direction.Length();
            direction.Normalize();
            direction *= (target.Center - ShootCenter).Length() / 50f; // Bullet speed

            if(Projectile.owner == Main.myPlayer)
            {
                Projectile ray = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    ShootCenter,
                    direction,
                    // ProjectileID.HeatRay,
                    ModProjectileID.TempleSentryHeatRay,
                    (int)(Projectile.damage * HEAYRAY_DAMAGE_FACTOR),
                    Projectile.knockBack,
                    Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item12, Projectile.position);
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

        public override bool PreDraw(ref Color lightColor)
        {
            int chargeCnt = (int)Projectile.ai[1];
            // draw sentry
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int width = texture.Width;
            int height = texture.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            Vector2 worldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0));
            Vector2 origin = new Vector2(width / 2, height / 2);
            MinionAIHelper.DrawPart(Projectile, texture, worldPos, rect, lightColor, Projectile.rotation, origin);

            // draw core
            Texture2D coreTexture = ModContent.Request<Texture2D>(CORE_TEXTURE).Value;
            int coreWidth = coreTexture.Width;
            int coreHeight = coreTexture.Height;
            Rectangle coreRect = new Rectangle(0, 0, coreWidth, coreHeight);
            Vector2 coreWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0));
            Vector2 coreOrigin = new Vector2(coreWidth / 2, coreHeight / 2);
            Color coreColor = lightColor;
            if(State == EYEBEAM_STATE)
            {
                coreColor = Color.Lerp(lightColor, Color.White, (float)chargeCnt / EYEBEAM_MAX_CHARGE_NUM);
            }
            else if(State == HEATRAY_STATE)
            {
                coreColor = Color.White;
                CoreLightCnt = 30;
            }
            else if(State == REST_STATE)
            {
                coreColor = Color.Lerp(lightColor, Color.White, (float)CoreLightCnt / 30f);
                CoreLightCnt--;
                if(CoreLightCnt <= 0)
                {
                    CoreLightCnt = 0;
                }
            }
            MinionAIHelper.DrawPart(Projectile, coreTexture, coreWorldPos, coreRect, coreColor, Projectile.rotation, coreOrigin);

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(State);
            writer.Write(shootInterval);
            writer.Write(hasTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = reader.ReadInt32();
            shootInterval = reader.ReadInt32();
            hasTarget = reader.ReadBoolean();
        }

    }
}
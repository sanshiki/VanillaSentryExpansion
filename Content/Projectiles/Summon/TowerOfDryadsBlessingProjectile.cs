using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class TowerOfDryadsBlessingProjectile : ModProjectile
    {
        // animation parameters
        private const int FRAME_SPEED = 10;
        private const int FRAME_NUM = 5;
        private const int LIVE_TIME = 40*10;
        private const int FIRST_STAGE = (int)(0.167 * LIVE_TIME);
        private const int SECOND_STAGE = (int)(0.333 * LIVE_TIME + FIRST_STAGE);
        private const int THIRD_STAGE = (int)(0.333 * LIVE_TIME + SECOND_STAGE);
        private const int FOURTH_STAGE = (int)(0.167 * LIVE_TIME + THIRD_STAGE);
        private const int FADE_TIME = 60;

        // projectile state
        private bool initialized = false;
        public Vector2 RotateCenter;
        public float RotateRadius;
        private float BaseRotateRadius;
        public float RotateAngle;
        public float RotateSpeed;
        private static int BuffProjectileID = -1;
        private ProjectileReference TowerReference;

        // public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DryadsWardCircle;
        public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.DryadsWardCircle;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FRAME_NUM;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14; // 尺寸可以和沙块差不多
            Projectile.height = 14;
            Projectile.aiStyle = 0; 
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = 1; // 击中一次就消失
            Projectile.timeLeft = LIVE_TIME; // 存活时间
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.alpha = 255;
            TowerReference.Clear();

        }

        public override void AI()
        {
            if (!initialized)
            {
                Initialize();
            }

            if(LIVE_TIME - Projectile.timeLeft == 2)
            {
                MinionAIHelper.SetProjectileNetUpdate(Projectile);
            }

            // Main.NewText("TowerOfDryadsBlessingProjectile AI");


            Projectile tower = TowerReference.Get();
            if (tower == null || !tower.active || tower.type != ModProjectileID.TowerOfDryadsBlessing)
            {
                // string reason = tower == null ? "tower is null" : !tower.active ? "tower is not active" : tower.type != ModProjectileID.TowerOfDryadsBlessing ? "tower is not a TowerOfDryadsBlessing" : "unknown reason";
                // Main.NewText("TowerOfDryadsBlessingProjectile AI: Tower is not valid," + reason);
                Projectile.Kill();
                return;
            }

            RotateCenter = tower.Center + new Vector2(0, -10);
            
            // fade in and out
            int alpha = Projectile.alpha;
            if (Projectile.timeLeft > LIVE_TIME - FADE_TIME)
            {
                alpha -= (int)(255 / (float)FADE_TIME);
            }
            else if (Projectile.timeLeft < FADE_TIME)
            {
                alpha += (int)(255 / (float)FADE_TIME);
            }

            CalculateRadius();
            RePosition();
            UpdateAnimation(alpha);
        }

        private void Initialize()
        {
            RotateCenter = Projectile.Center;
            RotateAngle = Projectile.ai[0];
            BaseRotateRadius = Projectile.ai[1];
            RotateRadius = BaseRotateRadius;
            RotateSpeed = Projectile.ai[2];
            initialized = true;
        }

        private void RePosition()
        {
            Projectile.Center = RotateCenter + new Vector2(RotateRadius, 0).RotatedBy(RotateAngle);
            RotateAngle += RotateSpeed;
        }

        private void UpdateAnimation(int alpha)
        {
            // cycle through the frames
            Projectile.frame = Projectile.frameCounter / FRAME_SPEED % FRAME_NUM;
            Projectile.frameCounter++;
            
            Projectile.alpha = alpha > 255 ? 255 : alpha < 0 ? 0 : alpha;

            // add rotation
            Projectile.rotation = RotateAngle + MathHelper.PiOver2;

            // add dust
            Random ran = new Random();
            float rate = ran.Next(100) / 100.0f;
            if (rate < 0.01)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DryadsWard, 0f, 0f);
                Dust dust = Main.dust[dustIndex];
                dust.velocity = (RotateCenter - dust.position) * 0.038f;
                dust.noGravity = true;
            }
        }

        private void CalculateRadius()
        {
            int elapsedTick = LIVE_TIME - Projectile.timeLeft;

            // second stage: radius increase to double
            if (elapsedTick > FIRST_STAGE && elapsedTick <= SECOND_STAGE)
            {
                int tick = elapsedTick - FIRST_STAGE;
                RotateRadius = BaseRotateRadius + tick * BaseRotateRadius / (float) (SECOND_STAGE - FIRST_STAGE);
                RotateRadius = RotateRadius > BaseRotateRadius * 2 ? BaseRotateRadius * 2 : RotateRadius;
            }
            // third stage: radius remain double
            else if (elapsedTick > SECOND_STAGE && elapsedTick <= THIRD_STAGE)
            {
                RotateRadius = BaseRotateRadius * 2;
            }
            // fourth stage: radius increase to four times
            else if (elapsedTick > THIRD_STAGE && elapsedTick <= FOURTH_STAGE)
            {
                int tick = elapsedTick - THIRD_STAGE;
                RotateRadius = BaseRotateRadius * 2 + tick * BaseRotateRadius * 2 / (float) (FOURTH_STAGE - THIRD_STAGE);
                RotateRadius = RotateRadius > BaseRotateRadius * 4 ? BaseRotateRadius * 4 : RotateRadius;
            }
            else if (elapsedTick > FOURTH_STAGE)
            {
                RotateRadius = BaseRotateRadius * 4;
            }
            else
            {
                RotateRadius = BaseRotateRadius;
            }
        }

        public void SetTowerReference(Projectile tower)
        {
            TowerReference.Set(tower);
            // Main.NewText("TowerOfDryadsBlessingProjectile SetTowerReference: TowerReference set");
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            TowerReference.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            TowerReference.ReceiveExtraAI(reader);
        }

    }
}

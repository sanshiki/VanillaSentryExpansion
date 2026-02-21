using Microsoft.Xna.Framework;
using System;
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
    public class TempleSentryHeatRay : ModProjectile
    {
        private const string TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Projectiles/TempleSentryHeatRay";
        public override string Texture => TEXTURE_PATH;

        private const int SIGNAL_WIDTH = 7;
        private const int SIGNAL_HEIGHT = 1500;
        private const int SIGNAL_SLICE_HEIGHT = 4;
        private const int SIGNAL_BASE_HEIGHT = 6;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.HeatRay);
            Projectile.aiStyle = -1;
            Projectile.extraUpdates = 50;
            Projectile.alpha = 150;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 3f)
            {
                if (Main.rand.NextFloat() < 0.5f)
                {
                    Vector2 HeatRayPos = Projectile.position;
                    Projectile.alpha = 255;
                    Vector2 HeatRayDir = Projectile.velocity.SafeNormalize(Vector2.Zero);

                    // HeatRayPos -= Projectile.velocity * ((float)i * 0.25f);	
                    Dust HeatRayDust = Dust.NewDustDirect(HeatRayPos, 1, 1, DustID.TheDestroyer);   // 162
                    HeatRayDust.position = HeatRayPos;
                    HeatRayDust.position.X += Projectile.width / 2;
                    HeatRayDust.position.Y += Projectile.height / 2;
                    HeatRayDust.scale = (float)Main.rand.Next(70, 110) * 0.013f;
                    HeatRayDust.noGravity = true;
                    HeatRayDust.velocity = HeatRayDir * 5f;
                    HeatRayDust.shader = GameShaders.Armor.GetSecondaryShader(90, Main.LocalPlayer);
                    // HeatRayDust
                    // HeatRayDust.fadeIn = 1.0f;
                }


                // if (Main.rand.NextFloat() < 0.5116279f)
                // {
                //     Dust dust;
                //     // You need to set position depending on what you are doing. You may need to subtract width/2 and height/2 as well to center the spawn rectangle.
                //     Vector2 position = HeatRayPos;
                //     dust = Terraria.Dust.NewDustPerfect(position, 173, HeatRayDir, 255, new Color(255,255,255), 2f);
                //     dust.shader = GameShaders.Armor.GetSecondaryShader(61, Main.LocalPlayer);
                //     dust.scale = (float)Main.rand.Next(70, 110) * 0.013f;
                //     dust.velocity = HeatRayDir * 2f;
                //     dust.fadeIn = 1.0f;
                // }

            }
        }

        // public override bool PreDraw(ref Color lightColor)
        // {
        //     Player player = Main.player[Projectile.owner];
        //     Texture2D texture = ModContent.Request<Texture2D>(TEXTURE_PATH).Value;
        //     int width = SIGNAL_WIDTH;
        //     int height = SIGNAL_HEIGHT;
        //     int TextureHeight = texture.Height;
        //     Vector2 origin = new Vector2(width / 2, height / 2);

        //     // draw base part
        //     Rectangle basePart = new Rectangle(0, 0, width, SIGNAL_BASE_HEIGHT);
        //     MinionAIHelper.DrawPart(
        //         Projectile,
        //         texture,
        //         MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0)),
        //         basePart,
        //         Color.White,
        //         Projectile.rotation,
        //         origin
        //     );

        //     // draw repeat part
        //     for(int i = 0; i < SIGNAL_HEIGHT / SIGNAL_SLICE_HEIGHT; i++)
        //     {
        //         float alpha = 1f - (float)i / (SIGNAL_HEIGHT / SIGNAL_SLICE_HEIGHT);
        //         int repeatY = SIGNAL_BASE_HEIGHT + i * SIGNAL_SLICE_HEIGHT;
        //         // Main.NewText("repeatY: " + repeatY + " alpha: " + alpha);
        //         Rectangle slicePart = new Rectangle(0, SIGNAL_BASE_HEIGHT, width, SIGNAL_SLICE_HEIGHT);
        //         Vector2 repeatLocalPos = new Vector2(0, repeatY);
        //         Vector2 repeatWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, repeatLocalPos);
        //          MinionAIHelper.DrawPart(
        //             Projectile,
        //             texture,
        //             repeatWorldPos,
        //             slicePart,
        //             Color.White * alpha,
        //             Projectile.rotation,
        //             origin
        //         );
        //     }

        //     return false;
        // }
    }
}
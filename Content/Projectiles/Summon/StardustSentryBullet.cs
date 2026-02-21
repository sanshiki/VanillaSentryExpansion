using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class StardustSentryBullet : ModProjectile
    {
        private string TEXTURE_PATH = "Terraria/Images/Projectile_" + ProjectileID.StardustJellyfishSmall;
        private const string TRAIL_VERTEX_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Vertexes/TriangleVertex";
        private string EXTRA_TEXURE_PATH = "Terraria/Images/Extra_46";
        private const int MAX_PENETRATE = 3;
        public override string Texture => TEXTURE_PATH;

        private const int FRAME_COUNT = 4;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FRAME_COUNT;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 90;

            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = MAX_PENETRATE;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.timeLeft = 60 * 3;
            Projectile.light = 1f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.ToRotation() + (float)Math.PI / 2f, 0.4f);

            // try get target
            int TargetID = (int)Projectile.ai[0];
            bool canHomein = Projectile.ai[1] == 0f;
            NPC target = null;
            if(TargetID >= 0 && TargetID < Main.maxNPCs)
            {
                NPC npc = Main.npc[TargetID];
                if (npc.active && npc.CanBeChasedBy(Projectile))
                {
                    target = npc;
                }
            }

            if (target != null && target.active && !target.dontTakeDamage && !target.immortal)
            {
                if (Projectile.penetrate == MAX_PENETRATE && Projectile.Center.Distance(target.Center) > 80f && canHomein)
                {
                    MinionAIHelper.HomeinToTarget(Projectile, target.Center, 50f, 10f);
                }
                else
                {
                    canHomein = false;
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 50f;
                }
            }
            else
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 50f;

            if (++Projectile.frameCounter >= 2)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
            if (Main.rand.Next(12) == 0)
            {
                int num845 = Dust.NewDust(Projectile.Center, 8, 8, 180);
                Main.dust[num845].position = Projectile.Center;
                Dust dust150 = Main.dust[num845];
                Dust dust3 = dust150;
                dust3.velocity *= 0.2f;
                Main.dust[num845].noGravity = true;
            }
            if (Main.rand.Next(2) == 0)
            {
                int num851 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 180, 0f, 0f, 100);
                Dust dust151 = Main.dust[num851];
                Dust dust3 = dust151;
                dust3.scale += (float)Main.rand.Next(50) * 0.01f;
                Main.dust[num851].noGravity = true;
                dust151 = Main.dust[num851];
                dust3 = dust151;
                dust3.velocity *= 0.1f;
                Main.dust[num851].fadeIn = Main.rand.NextFloat() * 1.5f;
            }
            if (Main.rand.Next(3) == 0)
            {
                int num852 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 176, 0f, 0f, 100);
                Dust dust152 = Main.dust[num852];
                Dust dust3 = dust152;
                dust3.scale += 0.3f + (float)Main.rand.Next(50) * 0.01f;
                Main.dust[num852].noGravity = true;
                dust152 = Main.dust[num852];
                dust3 = dust152;
                dust3.velocity *= 0.1f;
                Main.dust[num852].fadeIn = Main.rand.NextFloat() * 1.5f;
            }

            if (Main.rand.Next(4) == 0)
            {
                int num853 = Dust.NewDust(Projectile.Center, 8, 8, 242);
                Main.dust[num853].position = Projectile.Center;
                Dust dust153 = Main.dust[num853];
                Dust dust3 = dust153;
                dust3.velocity *= 0.2f;
                Main.dust[num853].noGravity = true;
                Main.dust[num853].scale = 1.5f;
            }
            Projectile.alpha = 0;

            Projectile.ai[1] = canHomein? 0 : 1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制顶点拖尾效果
            MinionAIHelper.DrawVertexTrail(
                Projectile,
                TRAIL_VERTEX_TEXTURE_PATH,
                new Color(70, 160, 255, 200),  // 起始颜色：亮蓝色
                new Color(30, 80, 150, 0),     // 结束颜色：暗蓝色并透明
                20f,                            // 起始宽度
                20f                              // 结束宽度
            );
            Texture2D extraTexture = ModContent.Request<Texture2D>(EXTRA_TEXURE_PATH).Value;

            Main.EntitySpriteDraw(
                extraTexture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                new Vector2(extraTexture.Width / 2, extraTexture.Height / 4),
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            Texture2D texture = ModContent.Request<Texture2D>(TEXTURE_PATH).Value;

            Rectangle rectangle = new Rectangle(
                0,
                texture.Height / Main.projFrames[Type] * Projectile.frame,
                texture.Width, 
                texture.Height / Main.projFrames[Type]
                );

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                rectangle,
                lightColor,
                Projectile.rotation,
                new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            
            return false; // 阻止默认绘制
        }
    }
}

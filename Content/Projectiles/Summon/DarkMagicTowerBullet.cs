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
    public class DarkMagicTowerBullet : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SapphireBolt;

        private const int DUST_INTERVAL = 10;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.SapphireBolt);

            Projectile.aiStyle = -1;
            Projectile.alpha = 250;

            // 关键：召唤伤害
            Projectile.DamageType = DamageClass.Summon;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.penetrate = 3;
            Projectile.tileCollide = true;
        }


        public override void AI()
        {
            // int BlueDustID = 41;
            // // int BlueDustID = DustIDs[BlueDustIDIdx];
            // Dust BlueDust = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, BlueDustID, Projectile.velocity.X, Projectile.velocity.Y);
            // BlueDust.noGravity = true;
            // BlueDust.scale = MinionAIHelper.RandomFloat(0.8f, 2.0f);
            Dust dust;
            // You need to set position depending on what you are doing. You may need to subtract width/2 and height/2 as well to center the spawn rectangle.
            Vector2 position = Projectile.Center - Projectile.Size/2f;
            float ang_offset = MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f);
            dust = Main.dust[Terraria.Dust.NewDust(position, Projectile.width, Projectile.height, 109, Projectile.velocity.X*0.3f, Projectile.velocity.Y*0.3f, 0, new Color(255,255,255), 1.4534883f)];
            dust.noGravity = true;
            dust.fadeIn = 1.4f;
            dust.velocity = Projectile.velocity.RotatedBy(ang_offset) * 0.7f;

            ang_offset = MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f);
            dust = Main.dust[Terraria.Dust.NewDust(position, Projectile.width, Projectile.height, 172, Projectile.velocity.X*0.3f, Projectile.velocity.Y*0.3f, 0, new Color(255,255,255), 1.0f)];
            dust.noGravity = true;
            dust.fadeIn = 1.4f;
            dust.velocity = Projectile.velocity.RotatedBy(ang_offset) * 0.7f;

            

        }

        // public override bool PreDraw(ref Color lightColor)
        // {
        //     // 先获取贴图
        //     Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

        //     // 设定你想要的颜色，比如粉色
        //     Color drawColor = new Color(255, 100, 200, 150);  

        //     // 绘制
        //     Main.EntitySpriteDraw(
        //         texture,
        //         Projectile.Center - Main.screenPosition,
        //         null,
        //         drawColor,
        //         Projectile.rotation,
        //         texture.Size() / 2f,
        //         Projectile.scale,
        //         SpriteEffects.None,
        //         0
        //     );
        //     return false; // 阻止默认绘制
        // }
    }
}

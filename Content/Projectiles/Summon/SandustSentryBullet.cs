using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class SandustSentryBullet : ModProjectile
    {

        private const float GRAVITY = 2.0f;
        private const float MAX_GRAVITY = 200f;
        private const float EXPLOSION_RADIUS = 60f;

        public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.SandBallFalling;

        private const float SANDNADO_DAMAGE_FACTOR = 0.3f;
        private const float SANDNADO_SPAWN_CHANCE = 0.25f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20; // 尺寸可以和沙块差不多
            Projectile.height = 20;
            Projectile.aiStyle = 0; // 自定义AI，不用原版沙块的aiStyle
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1; // 击中一次就消失
            Projectile.timeLeft = 600; // 存活时间
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // 模拟重力：Y方向速度逐渐增加
            Projectile.velocity.Y += GRAVITY; // 比原版重力大（原版沙块大概 0.2f）
            // 旋转，增加视觉效果
            Projectile.rotation += Projectile.velocity.X * 0.05f;

            for(int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 87, 0f, 0f);
                d.noGravity = true;
            }
        }

        // 击中NPC时
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Explosion(target);
        }

        // 撞击地面时
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explosion(null);
            return false; // Kill() 在 Explosion 内处理
        }

        private void Explosion(NPC target)
        {
            // 范围半径（像素）
            float radius = EXPLOSION_RADIUS; // 大约5格范围
            Vector2 center = Projectile.Center;

            // 伤害范围内的NPC
            if(MinionAIHelper.IsServer())
            {
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.Distance(center) < radius && npc != target && !npc.dontTakeDamage && !npc.immortal)
                    {
                        // 计算伤害
                        int damage = (int)(Projectile.damage * 0.8f); // 爆炸伤害稍低
                        // Main.NewText("damage:" + damage.ToString());
                        NPC.HitInfo hitInfo = new NPC.HitInfo();
                        hitInfo.Crit = false;
                        hitInfo.DamageType = DamageClass.Summon;
                        hitInfo.HitDirection = 0;
                        hitInfo.Knockback = 0f;
                        hitInfo.Damage = damage;
                        npc.StrikeNPC(hitInfo, false, false);
                    }
                }
            }

            Projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            if(MinionAIHelper.RandomFloat(0f, 1f) < SANDNADO_SPAWN_CHANCE && Projectile.owner == Main.myPlayer)
            {
                Projectile sandado = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModProjectileID.SandustSentrySandnadoFriendly, (int)(Projectile.damage * SANDNADO_DAMAGE_FACTOR), 2, Projectile.owner);
            }

            if(!MinionAIHelper.IsServer())
            {
                // 生成Dust特效
                for (int i = 0; i < 5; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 133, 0f, 0f);
                }
                for (int i = 0; i < 5; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 283, 0f, 0f);
                }

                // 播放爆炸音效
                SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            }
        }

    }
}

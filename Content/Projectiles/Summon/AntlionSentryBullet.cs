using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System.IO;

using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class AntlionSentryBullet : ModProjectile
    {

        private const float GRAVITY = 1.0f;
        private const float MAX_GRAVITY = 30f;
        private const float EXPLOSION_RADIUS = 60f;
        private const int SPLIT_COUNT = 4;

        public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.SandBallFalling;

        public override void SetDefaults()
        {
            Projectile.width = 14; // 尺寸可以和沙块差不多
            Projectile.height = 14;
            Projectile.aiStyle = 0; // 自定义AI，不用原版沙块的aiStyle
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1; // 击中一次就消失
            Projectile.timeLeft = 600; // 存活时间
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Summon;
            // Projectile.netImportant = true;
        }

        public override void AI()
        {
            // 模拟重力：Y方向速度逐渐增加
            Projectile.velocity.Y += GRAVITY; // 比原版重力大（原版沙块大概 0.2f）

            // 限制最大下落速度，避免太快
            if (Projectile.velocity.Y > MAX_GRAVITY)
                Projectile.velocity.Y = MAX_GRAVITY;

            // 旋转，增加视觉效果
            Projectile.rotation += Projectile.velocity.X * 0.05f;

            bool hasSplit = Projectile.ai[0] == 0f ? false : true;


            if(Projectile.velocity.Y > 3f && !hasSplit)
            {
                // split into 4 bullets
                for(int i = 0; i < SPLIT_COUNT; i++)
                {
                    if(Projectile.owner == Main.myPlayer)
                    {
                        Vector2 vel = new Vector2(Projectile.velocity.X + (float)(i-SPLIT_COUNT/2)/SPLIT_COUNT*8f, Projectile.velocity.Y * MinionAIHelper.RandomFloat(0.5f, 1.0f));
                        Projectile bullet = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, vel, ModProjectileID.AntlionSentryBullet, (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 1f);
                    }
                }
                Projectile.Kill();
                
            }

            // Main.NewText("proj id:" + Projectile.identity + " hasSplit:" + hasSplit.ToString());

            // 粒子效果（可选）
            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Sand, 0f, 0f);
            }
        }

        // 击中NPC时
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Main.NewText("onhitnpc explosion triggerred.");
            Explosion(target);
        }

        // 撞击地面时
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hasSplit = Projectile.ai[0] == 0f ? false : true;
            if(!hasSplit)
            {
                // Main.NewText("ontilecollide non hassplit case");
                if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 0.1f)
                {
                    for(int i = 0; i < SPLIT_COUNT-1; i++)
                    {
                        float velY = MinionAIHelper.RandomFloat(-3f, 3f) + oldVelocity.Y * 0.5f;                     
                        Projectile bullet = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(-oldVelocity.X*MinionAIHelper.RandomFloat(0.3f, 0.7f), velY), ModProjectileID.AntlionSentryBullet, (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 1f);
                    }

                    Projectile.Kill();
                }
                if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 0.1f)
                {
                    if (oldVelocity.Y <= 0f)
                    {
                        for(int i = 0; i < SPLIT_COUNT-1; i++)
                        {
                            float velX = MinionAIHelper.RandomFloat(-3f, 3f) + oldVelocity.X * 0.5f;
                            Projectile bullet = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(velX, -oldVelocity.Y*MinionAIHelper.RandomFloat(0.3f, 0.7f)), ModProjectileID.AntlionSentryBullet, (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 1f);
                        }

                        Projectile.Kill();
                    }
                }
            }
            else
            {
                // Main.NewText("ontilecollide hassplit case");
                Explosion(null);
            }

            Projectile.netUpdate = true;
            
            return false;
        }

        private void Explosion(NPC target)
        {
            // 范围半径（像素）
            float radius = EXPLOSION_RADIUS; // 大约5格范围
            Vector2 center = Projectile.Center;

            Player owner = Main.player[Projectile.owner];

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
                        
                        // npc.StrikeNPC(hitInfo, false, false);
                        owner.StrikeNPCDirect(npc, hitInfo);
                    }
                }
            }

            Projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            bool hasSplit = Projectile.ai[0] == 0f ? false : true;
            
            if(!MinionAIHelper.IsServer())
            {
                if(hasSplit)
                {
                    // 生成Dust特效
                    for (int i = 0; i < 10; i++)
                    {
                        // Vector2 dustVel = Main.rand.NextVector2Circular(3f, 3f);
                        // Dust.NewDustPerfect(center, DustID.Sand, dustVel).scale = 1.5f;
                        Dust d = Dust.NewDustDirect(Projectile.Center-Projectile.Size/2, Projectile.width, Projectile.height, DustID.Sand, 0f, 0f);
                        d.scale = 1.2f;
                    }
                }
                // 播放爆炸音效
                SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            }
        }

    }
}

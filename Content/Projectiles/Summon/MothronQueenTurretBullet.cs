using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.Content.Buffs.Summon;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class MothronBabyFriendly : ModProjectile
    {
        public override string Texture => ModGlobal.VANILLA_NPC_TEXTURE_PATH + NPCID.MothronSpawn;

        private const float MAX_SPEED = 15f;
        private const float ACC = 0.2f;
        private const float INERTIA = 15f;
        private const float EXPLOSION_RADIUS = 80f;
        private const int DEBUFF_DURATION_PER_LEVEL = (int)(60 * 0.15f);
        private const int MAX_DEBUFF_LEVEL = 10;
        private const float DAMAGE_INCREASE_PER_LEVEL = 0.02f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 66;
            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            // search for target
            NPC target = MinionAIHelper.SearchForTargets(
                owner,
                Projectile,
                1500f,
                false,
                null,
                false).TargetNPC;
            if(target != null)
            {
                MinionAIHelper.HomeinToTarget(Projectile, target.Center, MAX_SPEED, INERTIA);
            }
            else
            {
                float spd = Projectile.velocity.Length();
                if (spd < MAX_SPEED)
                {
                    Projectile.velocity += Projectile.velocity.SafeNormalize(Vector2.Zero) * ACC;
                }
                else
                {
                    Projectile.velocity -= Projectile.velocity.SafeNormalize(Vector2.Zero) * ACC;
                }
            }

            
            // add mothron dust debuff to targets in radius
            if (Projectile.timeLeft % 60 == 0)
            {
                List<NPC> targets = MinionAIHelper.SearchTargetsInRadius(Projectile.Center, EXPLOSION_RADIUS);
                if(MinionAIHelper.IsServer())
                {
                    foreach (NPC targ in targets)
                    {
                        int buffType = ModBuffID.MothronDustDebuff;
                        int index = targ.FindBuffIndex(buffType);
                        if(index == -1)
                        {
                            targ.AddBuff(buffType, DEBUFF_DURATION_PER_LEVEL);
                            // Mod.Logger.Info("MothronBabyFriendly: AI: Add mothron dust debuff to target: " + targ.type);
                        }
                        else
                        {
                            targ.buffTime[index] += DEBUFF_DURATION_PER_LEVEL;
                            if(targ.buffTime[index] > DEBUFF_DURATION_PER_LEVEL * MAX_DEBUFF_LEVEL)
                            {
                                targ.buffTime[index] = DEBUFF_DURATION_PER_LEVEL * MAX_DEBUFF_LEVEL;
                            }
                            // Mod.Logger.Info("MothronBabyFriendly: AI: Update mothron dust debuff to target: " + targ.type + " lvl: " + targ.buffTime[index] / DEBUFF_DURATION_PER_LEVEL);
                        }
                    }
                }
                for (int i = 0; i < 15; i++)
                {
                        Dust dust;
                        Vector2 position = Projectile.Center - Projectile.Size/2f;
                        dust = Main.dust[Terraria.Dust.NewDust(position, Projectile.width, Projectile.height, 233, 0f, 0f, 0, new Color(255,255,255), 1.2f)];
                        dust.noGravity = true;
                        dust.velocity = new Vector2(1, 0).RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT, ModGlobal.PI_FLOAT)) * MinionAIHelper.RandomFloat(1f, 5f);
                }
            }

            // update animation
            Projectile.frameCounter++;
            if(Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 3;
            }
            // slightly rotate towards target
            // 检查速度是否为 NaN，避免异常
            if (float.IsNaN(Projectile.velocity.X) || float.IsNaN(Projectile.velocity.Y))
            {
                Projectile.velocity = Vector2.Zero;
            }
            Projectile.rotation = Projectile.velocity.X * 0.05f;
            // 如果速度为零或NaN，使用默认方向
            if (Math.Abs(Projectile.velocity.X) < 0.01f)
            {
                Projectile.spriteDirection = 1;
            }
            else
            {
                Projectile.spriteDirection = -Math.Sign(Projectile.velocity.X);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // bounce
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 0.1f)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 0.1f)
            {

                Projectile.velocity.Y = -oldVelocity.Y;
            }
            // Projectile.penetrate--;
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for(int i = 0; i < 10; i++)
            {
                Dust dust;
                Vector2 position = Projectile.Center - Projectile.Size/2f;
                dust = Main.dust[Terraria.Dust.NewDust(position, Projectile.width, Projectile.height, 233, 0f, 0f, 0, new Color(255,255,255), 1f)];
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int buffType = ModBuffID.MothronDustDebuff;
            int index = target.FindBuffIndex(buffType);

            if (index != -1)
            {
                int lvl = target.buffTime[index] / DEBUFF_DURATION_PER_LEVEL; // 每60tick一层

                if (lvl > 0)
                {
                    float bonus = 1f + (lvl * DAMAGE_INCREASE_PER_LEVEL);
                    modifiers.FinalDamage *= bonus;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int width = texture.Width;
            int FrameHeight = Projectile.height * Projectile.frame;
            Rectangle rect = new Rectangle(0, FrameHeight, width, Projectile.height);
            Vector2 worldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0));
            Vector2 origin = new Vector2(width / 2, Projectile.height / 2);
            MinionAIHelper.DrawPart(Projectile, texture, worldPos, rect, lightColor, Projectile.rotation, origin);


            for(int i = Projectile.oldPos.Length-1;i >= 0; i-=2)
            {
                Vector2 pos = MinionAIHelper.ConvertToWorldPos(Projectile.oldPos[i] + Projectile.Size / 2f, Projectile.oldRot[i] , new Vector2(0, 0));
                Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);

                MinionAIHelper.DrawPart(Projectile, texture, pos, rect, color, Projectile.rotation, origin);
            }

            return false;
        }   
    }

    public class MothronQueenTurretBullet : ModProjectile
    {

        private const float GRAVITY = 0.2f;
        private const float MAX_GRAVITY = 30f;
        private const float EXPLOSION_RADIUS = 60f;

        public override string Texture => ModGlobal.VANILLA_NPC_TEXTURE_PATH + NPCID.MothronEgg;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 42;
            Projectile.aiStyle = 0; // 自定义AI，不用原版沙块的aiStyle
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1; // 击中一次就消失
            Projectile.timeLeft = 600; // 存活时间
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Summon;
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

            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[Projectile.owner],
                Projectile,
                300f,
                false,
                null).TargetNPC;
            if(target != null)
            {
                Projectile.Kill();
            }
        }

        public override void Kill(int timeLeft)
        {
            if(Projectile.owner == Main.myPlayer)
            {
                int num = MinionAIHelper.RandomInt(1, 3);
                for(int i = 0; i < num; i++)
                {
                    float directionOffset = MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/4f, ModGlobal.PI_FLOAT/4f);
                    Vector2 vel = Projectile.velocity.RotatedBy(directionOffset);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, vel, ModProjectileID.MothronBabyFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                	Dust dust;
                    Vector2 position = Projectile.Center - Projectile.Size/2f;
                    dust = Main.dust[Terraria.Dust.NewDust(position, Projectile.width, Projectile.height, 236, 0f, 0f, 0, new Color(255,255,255), 1.2f)];
                    // dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int width = texture.Width;
            int FrameHeight = Projectile.height * Projectile.frame;
            Rectangle rect = new Rectangle(0, FrameHeight, width, Projectile.height);
            Vector2 worldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0));
            Vector2 origin = new Vector2(width / 2, Projectile.height / 2);
            MinionAIHelper.DrawPart(Projectile, texture, worldPos, rect, lightColor, Projectile.rotation, origin);


            for(int i = Projectile.oldPos.Length-1;i >= 0; i-=2)
            {
                Vector2 pos = MinionAIHelper.ConvertToWorldPos(Projectile.oldPos[i] + Projectile.Size / 2f, Projectile.oldRot[i] , new Vector2(0, 0));
                Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);

                MinionAIHelper.DrawPart(Projectile, texture, pos, rect, color, Projectile.rotation, origin);
            }

            return false;
        }   

    }
}

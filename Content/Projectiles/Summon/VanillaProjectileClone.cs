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
using SummonerExpansionMod.Content.Buffs.Summon;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    /// <summary>
    /// 一个可继承的基类，用于快速制作“召唤系版本”的原版射弹。
    /// 继承此类后只需指定 BaseProjectileID 即可自动完成 CloneDefaults、
    /// AIType、DamageType 和 SentryShot 标记。
    /// </summary>
    public abstract class ClonedSentryProjectile : ModProjectile
    {
        /// <summary>
        /// 你希望克隆的原版 ProjectileID（例如 ProjectileID.Bee）
        /// </summary>
        public abstract int BaseProjectileID { get; }

        /// <summary>
        /// 可选：覆写贴图路径。例如 "YourMod/Projectiles/SentryBee"
        /// 如果未重写，将使用默认 ModProjectile 路径。
        /// </summary>
        public virtual string TexturePath => base.Texture;

        public override string Texture => TexturePath;

        public override void SetStaticDefaults()
        {
            // 复制原版帧数、拖尾、动画等静态参数（可选增强）
            Main.projFrames[Type] = Main.projFrames[BaseProjectileID];
            ProjectileID.Sets.TrailCacheLength[Type] = ProjectileID.Sets.TrailCacheLength[BaseProjectileID];
            ProjectileID.Sets.TrailingMode[Type] = ProjectileID.Sets.TrailingMode[BaseProjectileID];

            // 标记为哨兵弹，使鞭子增益生效
            ProjectileID.Sets.SentryShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(BaseProjectileID);
            AIType = BaseProjectileID;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.friendly = true;
            Projectile.hostile = false;
        }
    }


    public class BunnySentryBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.Seed;

        public override string TexturePath => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.Seed;
    }



    public class HoneyCombSentryBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.Bee;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.Bee;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // 蜂巢哨兵子弹使用4帧动画
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.5f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 0.1f)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 0.1f)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }

            Projectile.penetrate --;
            
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for(int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, 150);
                d.noGravity = true;
            }
        }
    }

    public class HoneyCombSentryGiantBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.GiantBee;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.GiantBee;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // 蜂巢哨兵子弹使用4帧动画
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.5f;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.tileCollide = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 0.1f)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 0.1f)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }

            Projectile.penetrate --;
            
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for(int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, 150);
                d.noGravity = true;
            }
        }
    }


    public class IchorPressorSentryBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.GoldenShowerFriendly;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.GoldenShowerFriendly;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            // Projectile.tileCollide = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if(MinionAIHelper.IsServer())
            {
                target.AddBuff(BuffID.Ichor, 3*60);
            }
        }
    }

    public class CursedFireExtractorSentryBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.CursedFlameFriendly;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.CursedFlameFriendly;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.penetrate = 3;
            // Projectile.tileCollide = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.CursedInferno, 3*60);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }
    }

    public class MechEyeballTurretEyeFire : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.EyeFire;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.EyeFire;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.CursedInferno, 3*60);
        }
    }

    public class TempleSentryEyeBeamBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.EyeBeam;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.EyeBeam;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.tileCollide = true;
        }
    }

    public class GiantLeavesOfPlanteraBullet1 : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.SeedPlantera;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.SeedPlantera;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.projFrames[Type] = 2;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 20;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.ownerHitCheck = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModBuffID.TikiFlagDebuff, 5*60);
        }

        public override void Kill(int timeLeft)
        {
            for(int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, 145);
            }
        }
    }

    public class GiantLeavesOfPlanteraBullet2 : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.PoisonSeedPlantera;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.PoisonSeedPlantera;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.projFrames[Type] = 2;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 20;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.ownerHitCheck = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModBuffID.TikiFlagDebuff, 5*60);
            target.AddBuff(BuffID.Poisoned, 3*60);
        }

        public override void Kill(int timeLeft)
        {
            for(int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, 40);
            }
        }
    }
    
}

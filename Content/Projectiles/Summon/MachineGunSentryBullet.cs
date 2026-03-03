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
using SummonerExpansionMod.Content.Items.Accessories;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class MachineGunSentryBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.Bullet;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.Bullet;

        public int SelfDamage = 10;
        public int SelfArmorPenetration = 10;

        private bool DamageDebug = false;


        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.25f;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.friendly = true;
            Projectile.hostile = false;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            SelfDamage = (int)Projectile.ai[1];
            if (MinionAIHelper.DoHarmToSelf(player, Projectile, SelfDamage, Projectile.knockBack))
            {
                // Main.NewText("SelfDamage: " + SelfDamage);
                Projectile.Kill(); // 避免每帧重复触发
            }
        }
    }

    public class AutocannonSentryBullet : ClonedSentryProjectile
    {
        public override int BaseProjectileID => ProjectileID.BulletHighVelocity;

        public override string TexturePath => "Terraria/Images/Projectile_" + ProjectileID.BulletHighVelocity;

        private bool DamageDebug = false;

        private const float DAMAGE_DECAY_FACTOR = 0.8f;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.ai[0] = 0f;  // hitCount
        }


        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            int SelfDamage = (int)Projectile.ai[1];
            if (MinionAIHelper.DoHarmToSelf(player, Projectile, SelfDamage, Projectile.knockBack))
            {
                Projectile.penetrate--;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            modifiers.HitDirectionOverride = (target.Center - player.Center).X > 0 ? 1 : -1;

            int hitCount = (int)Projectile.ai[0];

            float multiplier = (float)Math.Pow(DAMAGE_DECAY_FACTOR, hitCount);

            modifiers.FinalDamage *= multiplier;

            hitCount++;

            Projectile.ai[0] = (float)hitCount;
        }
    }
}
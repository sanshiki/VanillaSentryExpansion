using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{

    public class TowerOfDryadsBlessing : ModProjectile
    {

        /* ----------------- constants ----------------- */
        // leaf orbit parameters
        private const int LEAF_ORBIT_RADIUS_OUTER = 300;
        private const int LEAF_ORBIT_RADIUS_INNER = 100;
        private const int LEAF_NUM = 10;
        private const int LEAF_RESPAWN_INTERVAL = 40*17;
        private const int LIVE_TIME = 40*10;
        private const int INIT_LEAF_CNT = LEAF_RESPAWN_INTERVAL - 40*3;
        private const int LEAF_FIRST_STAGE = (int)(0.167f * LIVE_TIME);
        private const int LEAF_SECOND_STAGE = (int)(0.333f * LIVE_TIME + LEAF_FIRST_STAGE);
        private const int LEAF_THIRD_STAGE = (int)(0.333f * LIVE_TIME + LEAF_SECOND_STAGE);
        private const int LEAF_FOURTH_STAGE = (int)(0.167f * LIVE_TIME + LEAF_THIRD_STAGE);

        // gravity constants
        public const float Gravity = ModGlobal.SENTRY_GRAVITY;
        public const float MaxGravity = 20f;

        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/TowerOfDryadsBlessing";

        /* ----------------- variables ----------------- */
        Vector2 CenterOffset = new Vector2(0, -10);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 46;
            Projectile.friendly = false;
            Projectile.sentry = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.ai[0] = (float)INIT_LEAF_CNT;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // apply gravity
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxGravity)
            {
                Projectile.velocity.Y = MaxGravity;
            }

            int LeafTimer = (int)Projectile.ai[0];


            if (LeafTimer <= LIVE_TIME)
            {
                int radius = (int)CalculateAuraRadius(LeafTimer);

                // 给范围内玩家加树妖祝福
                foreach(Player player in Main.player)
                {
                    if (player.active && !player.dead && Vector2.Distance(player.Center, Projectile.Center) < radius)
                    {
                        player.AddBuff(BuffID.DryadsWard, 30);
                    }
                }

                // 给范围内敌人加树妖祸害
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage &&
                        Vector2.Distance(npc.Center, Projectile.Center) < radius && Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    {
                        npc.AddBuff(BuffID.DryadsWardDebuff, 30);
                    }
                }

                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DryadsWard, 0f, 0f);
                Dust dust = Main.dust[dustIndex];
                Random ran = new Random();
                float rate = ran.Next(100) / 100.0f;
                float velFactor = rate > 0.2 ? 0.2f : -0.6f;
                dust.velocity = (Projectile.Center + CenterOffset - dust.position) * velFactor;
                dust.noGravity = true;
                // Main.NewText("dust triggerred");
            }

            // 持续生成环绕的叶子
            int leafRespawnInterval = LEAF_RESPAWN_INTERVAL;

            if (LeafTimer >= leafRespawnInterval)
            {
                if(Projectile.owner == Main.myPlayer)
                {
                    SpawnOrbitingLeaves();
                }
                Projectile.netUpdate = true;
                LeafTimer = 0;
            }
            LeafTimer++;

            Projectile.ai[0] = (float)LeafTimer;
        }

        private void SpawnOrbitingLeaves()
        {
            // 按当前旋转角度生成外环叶子
            for (int i = 0; i < LEAF_NUM; i++)
            {
                float angle = MathHelper.TwoPi / LEAF_NUM * i;
                Vector2 spawnVelocity = Vector2.Zero;

                // 接口重映射
                int LeaftOuter = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + CenterOffset, // 投射物坐标->叶圆环中心
                    spawnVelocity, // 投射物速度
                    ModProjectileID.TowerOfDryadsBlessingProjectile,
                    0, // 投射物伤害
                    0, // 投射物击退
                    Projectile.owner,
                    angle, // ai[0] -> 叶片角度
                    LEAF_ORBIT_RADIUS_OUTER, // ai[1] -> 叶圆环半径
                    0.02f // ai[2] -> 叶片角速度
                );
                if (Main.projectile.IndexInRange(LeaftOuter) &&
                    Main.projectile[LeaftOuter].ModProjectile is TowerOfDryadsBlessingProjectile outerLeaf)
                {
                    outerLeaf.SetTowerReference(Projectile);
                    Main.projectile[LeaftOuter].netUpdate = true;
                }

                int LeafInner = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + CenterOffset, // 投射物坐标->叶圆环中心
                    spawnVelocity, // 投射物速度
                    ModProjectileID.TowerOfDryadsBlessingProjectile,
                    0, // 投射物伤害
                    0, // 投射物击退
                    Projectile.owner,
                    angle, // ai[0] -> 叶片角度
                    LEAF_ORBIT_RADIUS_INNER, // ai[1] -> 叶圆环半径
                    -0.01f // ai[2] -> 叶片角速度
                );
                if (Main.projectile.IndexInRange(LeafInner) &&
                    Main.projectile[LeafInner].ModProjectile is TowerOfDryadsBlessingProjectile innerLeaf)
                {
                    innerLeaf.SetTowerReference(Projectile);
                    Main.projectile[LeafInner].netUpdate = true;
                }
            }
        }

        private float CalculateAuraRadius(int leafTimer)
        {
            float radius = LEAF_ORBIT_RADIUS_OUTER;

            // 二阶段：半径扩大到 2 倍
            if (leafTimer > LEAF_FIRST_STAGE && leafTimer <= LEAF_SECOND_STAGE)
            {
                int tick = leafTimer - LEAF_FIRST_STAGE;
                radius = LEAF_ORBIT_RADIUS_OUTER + tick * LEAF_ORBIT_RADIUS_OUTER / (float)(LEAF_SECOND_STAGE - LEAF_FIRST_STAGE);
                return Math.Min(radius, LEAF_ORBIT_RADIUS_OUTER * 2f);
            }

            // 四阶段：半径扩大到 4 倍
            if (leafTimer > LEAF_THIRD_STAGE && leafTimer <= LEAF_FOURTH_STAGE)
            {
                int tick = leafTimer - LEAF_THIRD_STAGE;
                radius = LEAF_ORBIT_RADIUS_OUTER * 2f + tick * LEAF_ORBIT_RADIUS_OUTER * 2f / (float)(LEAF_FOURTH_STAGE - LEAF_THIRD_STAGE);
                return Math.Min(radius, LEAF_ORBIT_RADIUS_OUTER * 4f);
            }

            if (leafTimer > LEAF_FOURTH_STAGE)
            {
                return LEAF_ORBIT_RADIUS_OUTER * 4f;
            }

            return radius;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Projectile.velocity = Vector2.Zero;
            if(Projectile.velocity.X != 0f) Projectile.netUpdate = true;
            Projectile.velocity.X = 0f;
            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }


    }
}

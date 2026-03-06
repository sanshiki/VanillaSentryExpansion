using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
    public class CursedMagicTowerBulletNPC : ModNPC
    {
        private const float MAX_SPEED = 20f;
        private const float INERTIA = 20f;
        private const int TIME_LEFT = 60*5;

        public override string Texture => ModGlobal.VANILLA_NPC_TEXTURE_PATH + NPCID.CursedSkull;

        public float damage = 0f;
        public float knockBack = 0f;
        public int targetId = 0;
        public int ownerId = 0;
        
        private float MouseWroldPosX = 0f;
        private float MouseWroldPosY = 0f;
        private bool Dying = false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }
        public override void SetDefaults()
        {
            NPC.width = 26;
            NPC.height = 28;
            NPC.friendly = false;
            NPC.lifeMax = 1;
            NPC.defense = 0;
            NPC.damage = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.dontTakeDamageFromHostiles = true;
        }

        public override void AI()
        {
            int timeLeftCnt = (int)NPC.ai[0];
            if(timeLeftCnt >= TIME_LEFT)
            {
                Dying = true;
                NPC.netUpdate = true;
            }
            timeLeftCnt++;
            
            Player owner = Main.player[ownerId];
            NPC ownerTarget = Main.npc[targetId];
            if(ownerTarget != null && owner != null)
            {
                float factor = 0.8f;
                Vector2 targetPos = factor * owner.Center + (1-factor) * ownerTarget.Center;
                targetPos = owner.Center + (targetPos - owner.Center).SafeNormalize(Vector2.UnitX) * MathHelper.Clamp(targetPos.Distance(owner.Center), 32f, 150f);
                float spd_factor = (NPC.Center - targetPos).Length() / 300f;
                float real_speed = MAX_SPEED * Math.Min(spd_factor, 1f);
                Vector2 direction = (targetPos - NPC.Center).SafeNormalize(Vector2.UnitX);
                direction *= real_speed;
                NPC.velocity = (NPC.velocity * (INERTIA - 1) + direction) / INERTIA;

                // Main.NewText("[" + DateTime.UtcNow.Ticks + "] CursedMagicTowerBulletNPC: AI checkpoint 2");
            }
            else
            {
                Dying = true;
                NPC.netUpdate = true;
            }

            UpdateAnimation();

            if(Dying) NPC.netUpdate = true;

            if(Dying && MinionAIHelper.IsServer())
            {
                NPC.StrikeInstantKill();
                NPC.checkDead();
            }

            NPC.ai[0] = timeLeftCnt;
        }

        private void UpdateAnimation()
        {
            NPC.rotation = MinionAIHelper.NormalizeAngle(NPC.velocity.ToRotation() + ( NPC.velocity.X > 0 ? 0f : ModGlobal.PI_FLOAT));
            NPC.spriteDirection = NPC.velocity.X > 0 ? -1 : 1;

            if (Main.rand.Next(12) == 0)
            {
                int num845 = Dust.NewDust(NPC.Center, 8, 8, 172);
                Main.dust[num845].position = NPC.Center;
                Dust dust150 = Main.dust[num845];
                Dust dust3 = dust150;
                dust3.velocity *= 0.2f;
                Main.dust[num845].noGravity = true;
            }
            if (Main.rand.Next(2) == 0)
            {
                int num851 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 172, 0f, 0f, 100);
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
                int num852 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 176, 0f, 0f, 100);
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
                int num853 = Dust.NewDust(NPC.Center, 8, 8, 242);
                Main.dust[num853].position = NPC.Center;
                Dust dust153 = Main.dust[num853];
                Dust dust3 = dust153;
                dust3.velocity *= 0.2f;
                Main.dust[num853].noGravity = true;
                Main.dust[num853].scale = 1.5f;
            }
        }

        public override void FindFrame(int frameHeight) {
            NPC.frameCounter++;

            if (NPC.frameCounter >= 5) {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type]) {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.npcFrameCount[NPC.type];
            Rectangle frame = new Rectangle(0, frameHeight * (int)(NPC.frame.Y / frameHeight), texture.Width, frameHeight);
            Vector2 drawOrigin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            Vector2 drawPos = NPC.Center - screenPos;
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // draw trail
            for(int i = NPC.oldPos.Length-1;i >= 0; i-=1)
            {
                Vector2 pos = NPC.oldPos[i] + NPC.Size / 2f - screenPos;
                Color color = NPC.GetAlpha(drawColor) * ((NPC.oldPos.Length - i) / (float)NPC.oldPos.Length);
                float scale = MathHelper.Lerp(0.5f, NPC.scale, (NPC.oldPos.Length - i) / (float)NPC.oldPos.Length);
                spriteBatch.Draw(texture, pos, frame, color, NPC.rotation, drawOrigin, scale, effects, 0f);

                // Main.NewText("[" + DateTime.UtcNow.Ticks + "] CursedMagicTowerBulletNPC: Draw trail: " + pos + " color: " + color);
                Dust.QuickDust(new Point((int)pos.X, (int)pos.Y), Color.Red);
            }

            spriteBatch.Draw(
                texture,
                drawPos,
                frame,
                drawColor,
                NPC.rotation,
                drawOrigin,
                NPC.scale,
                effects,
                0f
            );

            return false;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            // 只允许玩家的鞭子触发
            if (projectile.owner == ownerId &&
                projectile.DamageType == DamageClass.SummonMeleeSpeed)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.HideCombatText();
            // modifiers.SetMaxDamage(0);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[ownerId];
            bool HitByOwner = TIME_LEFT - (int)NPC.ai[0] > 0;
            if(projectile.owner == Main.myPlayer && HitByOwner)
            {
                Vector2 MousePos = Main.MouseWorld;
                Vector2 PlayerMouseDir = (MousePos - player.Center).SafeNormalize(Vector2.UnitX);
                Vector2 NPCMouseDir = player.Center + PlayerMouseDir * 750f;
                Vector2 dir = (NPCMouseDir - NPC.Center).SafeNormalize(Vector2.UnitX);

                int ExtraDamage = (int)Math.Min(damageDone*0.5f, damage*2);

                Projectile.NewProjectile(
                    NPC.GetSource_Death(),
                    NPC.Center,
                    dir * 40f,
                    ModContent.ProjectileType<CursedMagicTowerBulletV3>(),
                    (int)(damage + ExtraDamage/* *HitDamage */), (int)knockBack, player.whoAmI);

                NPC targetNPC = null;
                float minDist = float.MaxValue;
                foreach (var npc in Main.ActiveNPCs)
				{
					bool canBeChased = npc.CanBeChasedBy(player);
					if (canBeChased && npc.active && npc.type != NPC.type)
					{
						if (Vector2.Distance(npc.Center, Main.MouseWorld) < 500f && 
                            Vector2.Distance(npc.Center, Main.MouseWorld) < minDist &&
                            npc.type != ModContent.NPCType<CursedMagicTowerBulletNPC>() &&
                            Collision.CanHitLine(player.Center, 8, 8, npc.Center, npc.width, npc.height)
                            )
						{
							targetNPC = npc;
							minDist = Vector2.Distance(npc.Center, Main.MouseWorld);
						}
					}
				}
                // Dust.QuickDustLine(targetNPC.Center, MouseWroldPos, 10f, Color.Red);
                if(player != null && targetNPC != null)
                {
                    player.MinionAttackTargetNPC = targetNPC.whoAmI;
                }
            }
        }

        public override bool CheckDead()
        {
            Player player = Main.player[ownerId];
            bool HitByOwner = TIME_LEFT - (int)NPC.ai[0] > 0;
            // int HitDamage = (int)NPC.ai[2];
            // Main.NewText("HitByOwner: " + HitByOwner + " HitDamage: " + HitDamage);
            // if (player.whoAmI == Main.myPlayer && HitByOwner)
            // {
            //     // Vector2 MousePos = Main.MouseWorld;
            //     // Vector2 PlayerMouseDir = (MousePos - player.Center).SafeNormalize(Vector2.UnitX);
            //     // Vector2 NPCMouseDir = player.Center + PlayerMouseDir * 750f;
            //     // Vector2 dir = (NPCMouseDir - NPC.Center).SafeNormalize(Vector2.UnitX);

            //     // Projectile.NewProjectile(
            //     //     NPC.GetSource_Death(),
            //     //     NPC.Center,
            //     //     dir * 40f,
            //     //     ModContent.ProjectileType<CursedMagicTowerBulletV3>(),
            //     //     (int)(damage/* *HitDamage */), (int)knockBack, player.whoAmI);


            //     NPC targetNPC = null;
            //     float minDist = float.MaxValue;
            //     foreach (var npc in Main.ActiveNPCs)
			// 	{
			// 		bool canBeChased = npc.CanBeChasedBy(player);
			// 		if (canBeChased && npc.active && npc.type != NPC.type)
			// 		{
			// 			if (Vector2.Distance(npc.Center, Main.MouseWorld) < 500f && Vector2.Distance(npc.Center, Main.MouseWorld) < minDist)
			// 			{
			// 				targetNPC = npc;
			// 				minDist = Vector2.Distance(npc.Center, Main.MouseWorld);
			// 			}
			// 		}
			// 	}
            //     // Dust.QuickDustLine(targetNPC.Center, MouseWroldPos, 10f, Color.Red);
            //     if(player != null && targetNPC != null)
            //     {
            //         player.MinionAttackTargetNPC = targetNPC.whoAmI;
            //     }
            // }

            if(!MinionAIHelper.IsServer())
            {
                // create dead dust
                for(int i = 0; i < 3; i++)
                {
                    Dust dust1 = Dust.NewDustDirect(NPC.Center, 8, 8, 172);
                    Dust dust2 = Dust.NewDustDirect(NPC.Center, 8, 8, 176);
                    Dust dust3 = Dust.NewDustDirect(NPC.Center, 8, 8, 242);
                    dust1.noGravity = true;
                    dust2.noGravity = true;
                    dust3.noGravity = true;
                    dust1.scale = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                    dust2.scale = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                    dust3.scale = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                    dust1.fadeIn = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                    dust2.fadeIn = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                    dust3.fadeIn = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                    dust1.velocity = NPC.velocity.RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f)) * 0.7f;
                    dust2.velocity = NPC.velocity.RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f)) * 0.7f;
                    dust3.velocity = NPC.velocity.RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f)) * 0.7f;
                }
            }
            
            SoundEngine.PlaySound(SoundID.Item110, NPC.Center);
            return true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(damage);
            writer.Write(knockBack);
            writer.Write(targetId);
            writer.Write(ownerId);
            writer.Write(MouseWroldPosX);
            writer.Write(MouseWroldPosY);
            writer.Write(Dying);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            damage = reader.ReadSingle();
            knockBack = reader.ReadSingle();
            targetId = reader.ReadInt32();
            ownerId = reader.ReadInt32();
            MouseWroldPosX = reader.ReadSingle();
            MouseWroldPosY = reader.ReadSingle();
            Dying = reader.ReadBoolean();
        }
    }

    public class CursedMagicTowerBulletV3 : ModProjectile
    {
        // public override string Texture => ModGlobal.VANILLA_PROJECTILE_TEXTURE_PATH + ProjectileID.SapphireBolt;

        private const int DUST_INTERVAL = 10;

        private const float MAX_SPEED = 40f;
        private const float ACC = 1.0f;

        public override string Texture => ModGlobal.VANILLA_NPC_TEXTURE_PATH + NPCID.CursedSkull;
        private const string TRAIL_VERTEX_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Vertexes/TriangleVertex";

        /* 
         * 29： dark blue small
         * 41： similar to 29, little brighter
         * 42： normal blue large
         * 45： simailar to 42, little brighter
         * 54:  black large
         * 59:  light blue small
         * 62:  pink + purple small
         * 65:  similar to 62, little darker
         * 71:  light pink large
         * 86:  similar to 71, little brighter
         * 88:  white + light blue large
         * 109:  dark black large
         * 113:  similar to 88
         * 164:  white + pink small
         * 173:  white + purple small
         */

        private List<int> DustIDs = new List<int> { 29, 41, 42, 45, 54, 59, 62, 65, 71, 86, 88, 109, 113, 164, 173 };

        public Vector2 Target;


        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 30;

            Projectile.aiStyle = -1;

            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

            Projectile.DamageType = DamageClass.Summon;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;

            // Projectile.alpha = 250;
        }

        public override void AI()
        {
            // 27 29 41 42 45 54 59 62 65 71 86 88 109 113 164 173
            // int BlueDustIDIdx = (int)DynamicParamManager.Get("DustIDIdx").value;
            int BlueDustID = 29;
            // int BlueDustID = DustIDs[BlueDustIDIdx];
            // Dust BlueDust = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, BlueDustID, Projectile.velocity.X, Projectile.velocity.Y);
            // BlueDust.noGravity = true;
            // BlueDust.scale = MinionAIHelper.RandomFloat(2.6f, 3.2f);
            // BlueDust.fadeIn = 1.4f;

            NPC target = MinionAIHelper.SearchForTargets(
                Main.player[Projectile.owner],
                Projectile,
                500f,
                true,
                n => n.type != ModContent.NPCType<CursedMagicTowerBulletNPC>()).TargetNPC;

            if(target != null)
            {
                MinionAIHelper.HomeinToTarget(Projectile, target.Center, MAX_SPEED, 10f);
            }
            else{
                Projectile.velocity += Projectile.velocity.SafeNormalize(Vector2.UnitX) * ACC;
            }


            UpdateAnimation();
            
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);

            // create dead dust
            for(int i = 0; i < 3; i++)
            {
                Dust dust1 = Dust.NewDustDirect(Projectile.Center, 8, 8, 172);
                Dust dust2 = Dust.NewDustDirect(Projectile.Center, 8, 8, 176);
                Dust dust3 = Dust.NewDustDirect(Projectile.Center, 8, 8, 242);
                dust1.noGravity = true;
                dust2.noGravity = true;
                dust3.noGravity = true;
                dust1.scale = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                dust2.scale = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                dust3.scale = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                dust1.fadeIn = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                dust2.fadeIn = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                dust3.fadeIn = MinionAIHelper.RandomFloat(0.8f, 1.5f);
                dust1.velocity = Projectile.velocity.RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f)) * 0.7f;
                dust2.velocity = Projectile.velocity.RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f)) * 0.7f;
                dust3.velocity = Projectile.velocity.RotatedBy(MinionAIHelper.RandomFloat(-ModGlobal.PI_FLOAT/32f, ModGlobal.PI_FLOAT/32f)) * 0.7f;
            }
        }

        private void UpdateAnimation()
        {
            Projectile.rotation = MinionAIHelper.NormalizeAngle(Projectile.velocity.ToRotation() + (Projectile.velocity.X > 0 ? 0f : ModGlobal.PI_FLOAT));
            Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            if (Main.rand.Next(12) == 0)
            {
                int num845 = Dust.NewDust(Projectile.Center, 8, 8, 172);
                Main.dust[num845].position = Projectile.Center;
                Dust dust150 = Main.dust[num845];
                Dust dust3 = dust150;
                dust3.velocity *= 0.2f;
                Main.dust[num845].noGravity = true;
            }
            if (Main.rand.Next(2) == 0)
            {
                int num851 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 172, 0f, 0f, 100);
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
        }

        public override bool PreDraw(ref Color lightColor)
        {

            

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int width = texture.Width;
            int frameHeight = Projectile.height;
            Rectangle rect = new Rectangle(0, frameHeight * Projectile.frame, width, frameHeight);
            Vector2 worldPos = MinionAIHelper.ConvertToWorldPos(Projectile, new Vector2(0, 0));
            Vector2 origin = new Vector2(width / 2, Projectile.height / 2);

            // 绘制拖尾
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i -= 1)
            {
                Vector2 pos = MinionAIHelper.ConvertToWorldPos(Projectile.oldPos[i] + Projectile.Size / 2f, Projectile.oldRot[i], new Vector2(0, 0));
                Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);
                MinionAIHelper.DrawPart(Projectile, texture, pos, rect, color, Projectile.rotation, origin);
            }

            MinionAIHelper.DrawVertexTrail(
                Projectile,
                TRAIL_VERTEX_TEXTURE_PATH,
                new Color(70, 160, 255, 200),  // 起始颜色：亮蓝色
                new Color(30, 80, 150, 0),     // 结束颜色：暗蓝色并透明
                20f,                            // 起始宽度
                20f                              // 结束宽度
            );

            // 绘制主体
            MinionAIHelper.DrawPart(Projectile, texture, worldPos, rect, lightColor, Projectile.rotation, origin);

            return false;
        }
    }
}

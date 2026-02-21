using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Items.Weapons.Summon;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public class OneTrueFlagBladeShot : ModProjectile
    {
        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/OneTrueFlagShadow";
        private const string FLAG_TAIL_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Vertexes/SwordTail4";

        private const float SHOOT_DIST = 500f;
        private const float ROT_SPEED = 1.2f;
        private const int FLAG_WIDTH = 120;
        private const int FLAG_HEIGHT = 78;
        private const float FLAG_TAIL_LENGTH = 6;
        private const float PoleLength = 280;
        private const int TAIL_FIT_INSERT_SIZE = 2;  // polar 插值时每两个点之间插入的点数
        private const float DAMAGE_DECAY_FACTOR = 0.8f;  // 伤害衰减系数
        private const int RADIUS = 140;

        private Vector2 ShootDirection;
        private float ShootSpeed;
        private Vector2 RelativeDisplacement;
        private Vector2 RelativeVelocity;
        private int TimeLeft;
        private int hitCount = 0;  // 记录击中敌人的次数，用于伤害衰减
        protected Vector2 CursorPos;
        private bool Inited = false;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;//这一项赋值2可以记录运动轨迹和方向（用于制作拖尾）
            ProjectileID.Sets.TrailCacheLength[Type] = 10;//这一项代表记录的轨迹最多能追溯到多少帧以前
        }

        public override void SetDefaults()
        {
            Projectile.width = 130;
            Projectile.height = 290;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1000;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.ownerHitCheck = true;
            Projectile.alpha = 255;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.timeLeft = (int)Projectile.ai[0];
            TimeLeft = Projectile.timeLeft;
            ShootSpeed = SHOOT_DIST / TimeLeft * player.whipRangeMultiplier * 2f;
            if(Projectile.owner == Main.myPlayer)
                CursorPos = Main.MouseWorld;
            Projectile.netUpdate = true;
            // Main.NewText("ShootSpeed: " + ShootSpeed + " TimeLeft: " + TimeLeft + " ShootDist: " + SHOOT_DIST);
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile FlagProjectile = Main.projectile[(int)Projectile.ai[1]];
            if(FlagProjectile.type != ModProjectileID.OneTrueFlagProjectile)
            {
                Projectile.Kill();
                return;
            }
            else
            {
                if(!FlagProjectile.active || FlagProjectile.owner != Projectile.owner)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if(!Inited)
            {
                ShootDirection = Vector2.Normalize(CursorPos - player.Center);
                RelativeVelocity = ShootDirection * ShootSpeed;
                Projectile.timeLeft = (int)Projectile.ai[0];
                Inited = true;
            }

            // Main.NewText("onetrueflagblade ai.alpha:" + Projectile.alpha+"TimeLeft:"+TimeLeft);

            // update relative velocity and displacement
            float de_acc = ShootSpeed / TimeLeft;
            RelativeVelocity -= ShootDirection * de_acc;
            RelativeDisplacement += RelativeVelocity;
            Projectile.Center = player.Center + RelativeDisplacement;

            float ratio = MathHelper.Lerp(0.1f, 1.1f, (float)Projectile.timeLeft / (float)TimeLeft);

            Projectile.rotation += ROT_SPEED * FlagProjectile.spriteDirection * ratio;

            UpdateAnimation();
            CreateDustEffect(player);
        }
        private void UpdateAnimation()
        {
            if(Projectile.timeLeft > TimeLeft / 4 * 3)
            {
                Projectile.alpha = (int)MathHelper.Lerp(0, 255, (float)(Projectile.timeLeft - TimeLeft / 4 * 3) / (float)(TimeLeft / 4));
            }
            else if (Projectile.timeLeft > TimeLeft / 2)
            {
                Projectile.alpha = 0;
            }
            else
            {
                Projectile.alpha = (int)MathHelper.Lerp(0, 290, (float)(TimeLeft / 2 - Projectile.timeLeft) / (float)TimeLeft * 2f);
            }
            // Main.NewText("Projectile.alpha: " + Projectile.alpha + "timeLeft: " + Projectile.timeLeft);

            Vector3 lightColor = new Vector3(0.2f, 0.2f, 0.8f) * Projectile.alpha / 255f * 2f;
            Lighting.AddLight(Projectile.Center, lightColor);

            for(int i=0;i<2;i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Vector2.One * RADIUS, 2 * RADIUS, 2 * RADIUS, 59, 0f, 0f);
                d.noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float height = texture.Height / Main.projFrames[Projectile.type];
            float width = texture.Width;
            // int color_r = (int)DynamicParamManager.QuickGet("TrailColor.R", 35, 0, 255).value;
            // int color_g = (int)DynamicParamManager.QuickGet("TrailColor.G", 54, 0, 255).value;
            // int color_b = (int)DynamicParamManager.QuickGet("TrailColor.B", 84, 0, 255).value;
            // int color_a = (int)DynamicParamManager.QuickGet("TrailColor.A", 255, 0, 255).value;
            // Color TailColor = new Color(color_r, color_g, color_b, color_a);
            Color TailColor = new Color(43, 144, 255, 255);

            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, 
                                    BlendState.Additive, 
                                    SamplerState.AnisotropicClamp, 
                                    DepthStencilState.None, 
                                    RasterizerState.CullNone, 
                                    null, 
                                    Main.GameViewMatrix.
                                    TransformationMatrix);

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition,
                new Rectangle(0, (int)(0 * height), (int)width, (int)height),
                // Projectile.GetAlpha(new Color(212, 205, 189, 200)),
                // Projectile.GetAlpha(lightColor),
                Projectile.GetAlpha(TailColor),
                Projectile.rotation,
                new Vector2(123, 146),
                Projectile.scale,
                SpriteEffects.None,
                0
            );


            // draw trail
            int OldPosSize = Math.Min(TimeLeft - Projectile.timeLeft, 5);
            for(int i = OldPosSize-1;i >= 0; i-=1)
            {
                Vector2 pos = MinionAIHelper.ConvertToWorldPos(Projectile.oldPos[i] + Projectile.Size / 2f, Projectile.oldRot[i], new Vector2(0, 0));
                Color color = Projectile.GetAlpha(TailColor) * ((OldPosSize - i) / (float)OldPosSize);
                float scale = MathHelper.Lerp(0.5f, Projectile.scale, (OldPosSize - i) / (float)OldPosSize);
                Main.EntitySpriteDraw(texture, pos, new Rectangle(0, (int)(0 * height), (int)width, (int)height), color, Projectile.oldRot[i], new Vector2(123, 146), scale, SpriteEffects.None, 0f);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, 
                                    BlendState.AlphaBlend, 
                                    SamplerState.AnisotropicClamp, 
                                    DepthStencilState.None, 
                                    RasterizerState.CullNone, 
                                    null, 
                                    Main.GameViewMatrix.
                                    TransformationMatrix);


            // PreDrawFlagCloth(ref lightColor, Projectile.Center);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // 定义圆心和半径
            Vector2 circleCenter = Projectile.Center;
            float circleRadius = (float)RADIUS; // 你想要的碰撞范围半径

            // 找出矩形内最接近圆心的点
            float closestX = MathHelper.Clamp(circleCenter.X, targetHitbox.Left, targetHitbox.Right);
            float closestY = MathHelper.Clamp(circleCenter.Y, targetHitbox.Top, targetHitbox.Bottom);

            // 计算圆心到该点的距离
            float distanceSquared = (circleCenter.X - closestX) * (circleCenter.X - closestX) +
                                    (circleCenter.Y - closestY) * (circleCenter.Y - closestY);

            // 如果距离小于半径平方 => 碰撞
            return distanceSquared < (circleRadius * circleRadius);
        }

        private void CreateDustEffect(Player player)
        {
            int dustType = -1;
            if (MinionAIHelper.IsPartyImbue(player))
            {
                dustType = Main.rand.Next(4) + 139;
            }
            else
            {
                dustType = MinionAIHelper.GetImbueDust(player);
            }
            if (dustType != -1 && Main.rand.NextFloat() < 0.75f)
            {
                Vector2 DustCenter = Projectile.Center + new Vector2(0, -(PoleLength - FLAG_HEIGHT) / 2f).RotatedBy(Projectile.rotation);
                Dust dust = Dust.NewDustDirect(DustCenter - new Vector2(FLAG_HEIGHT / 2f, FLAG_HEIGHT / 2f), FLAG_HEIGHT, FLAG_HEIGHT, dustType, 0f, 0f, 0, default, 2f);
                dust.noGravity = true;
                dust.velocity = (DustCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * MinionAIHelper.RandomFloat(0f, 3f);
                dust.fadeIn = 1f;

            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            // 设置击退方向
            modifiers.HitDirectionOverride = (target.Center - player.Center).X > 0 ? 1 : -1;

            // 根据击中次数计算伤害衰减
            float multiplier = (float)Math.Pow(DAMAGE_DECAY_FACTOR, hitCount);
            modifiers.FinalDamage *= multiplier;

            // 增加击中计数
            hitCount++;

            int ImbueDeBuffID = MinionAIHelper.GetImbueDebuff(player);
            if (ImbueDeBuffID != -1) target.AddBuff(ImbueDeBuffID, 3 * 60);
            if(MinionAIHelper.IsPartyImbue(player))
            {
                Projectile.NewProjectile(new EntitySource_Misc("WeaponEnchantment_Confetti"), target.Center.X, target.Center.Y, target.velocity.X, target.velocity.Y, 289, 0, 0f, player.whoAmI);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            // 设置召唤物攻击目标标记
            player.MinionAttackTargetNPC = target.whoAmI;

            // 应用召唤物灌注效果
            int ImbueDeBuffID = MinionAIHelper.GetImbueDebuff(player);
            if (ImbueDeBuffID != -1)
            {
                target.AddBuff(ImbueDeBuffID, 3 * 60);
            }

            // 派对灌注特效
            if (MinionAIHelper.IsPartyImbue(player))
            {
                Projectile.NewProjectile(new EntitySource_Misc("WeaponEnchantment_Confetti"),
                    target.Center.X, target.Center.Y, target.velocity.X, target.velocity.Y,
                    289, 0, 0f, player.whoAmI);
            }
        }
    
        protected void PreDrawFlagCloth(ref Color lightColor, Vector2 ClothCenter)
        {
            Player player = Main.player[Projectile.owner];

            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, 
                                    BlendState.AlphaBlend, 
                                    SamplerState.AnisotropicClamp, 
                                    DepthStencilState.None, 
                                    RasterizerState.CullNone, 
                                    null, 
                                    Main.GameViewMatrix.
                                    TransformationMatrix);

            List<Vertex> ve = new List<Vertex>();
            Color tailColor = new Color(35, 54, 84, 100);

            int CurrentTime = TimeLeft - Projectile.timeLeft;
            int OldPosSize = (int)Math.Min(CurrentTime, FLAG_TAIL_LENGTH);

            // 构造 polar 点列表
            List<MinionAIHelper.PolarCurveFitter.Polar> PolarPoints = new List<MinionAIHelper.PolarCurveFitter.Polar>();
            for(int i = 0; i < OldPosSize; i++)
            {
                // 使用固定半径 PoleLength，以及记录的旋转角度
                PolarPoints.Add(new MinionAIHelper.PolarCurveFitter.Polar(PoleLength, Projectile.oldRot[i]));
            }

            // 进行 polar 插值
            if(PolarPoints.Count > 0)
            {
                PolarPoints = MinionAIHelper.PolarCurveFitter.FitAndInsert(PolarPoints, TAIL_FIT_INSERT_SIZE);
            }

            float TailLength = FLAG_TAIL_LENGTH * (TAIL_FIT_INSERT_SIZE + 1) - 1;

            // 使用插值后的点生成顶点
            for(int i = 0; i < PolarPoints.Count; i++)
            {
                float ratio = i / (float)TailLength;
                Color b = Projectile.GetAlpha(tailColor);

                Vector2 SpinCenter = Projectile.Center;
                float OldRot = (float)PolarPoints[i].theta;

                Vector2 UpperVertexPffset = new Vector2(-FLAG_WIDTH * 0.9f * Projectile.spriteDirection, -PoleLength / 2f * 0.95f);
                Vector2 LowerVertexPffset = new Vector2(-FLAG_WIDTH * 0.9f * Projectile.spriteDirection, (-PoleLength / 2f + FLAG_HEIGHT) * 0.95f);

                ve.Add(new Vertex(SpinCenter - Main.screenPosition + UpperVertexPffset.RotatedBy(OldRot),
                        new Vector3(ratio, 1, 1),
                        b));
                ve.Add(new Vertex(SpinCenter - Main.screenPosition + LowerVertexPffset.RotatedBy(OldRot),
                        new Vector3(ratio, 0, 1),
                        b));
            }
    
            if(ve.Count >= 3)//因为顶点需要围成一个三角形才能画出来 所以需要判顶点数>=3 否则报错
            {
                gd.Textures[0] = ModContent.Request<Texture2D>(FLAG_TAIL_TEXTURE_PATH).Value;//获取刀光的拖尾贴图
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);//画
            }

            //结束顶点绘制
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(CursorPos.X);
            writer.Write(CursorPos.Y);
            writer.Write(TimeLeft);
            writer.Write(ShootSpeed);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            float CursorPosX = reader.ReadSingle();
            float CurosrPosY = reader.ReadSingle();
            CursorPos = new Vector2(CursorPosX, CurosrPosY);
            TimeLeft = reader.ReadInt32();
            ShootSpeed = reader.ReadSingle();
        }
    }
}
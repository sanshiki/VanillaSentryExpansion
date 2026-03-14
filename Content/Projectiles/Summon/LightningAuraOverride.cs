using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.WorldBuilding;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.Content.Buffs.Summon;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;
using Terraria.Audio;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
	public class LightningAuraOverride : IProjectileOverride
	{
		private Vector2 lastVelocity = new Vector2(0, 0);
		public LightningAuraOverride()
		{
			RegisterFlags["PreAI"] = false;
			RegisterFlags["OnTileCollide"] = true;
			RegisterFlags["PostAI"] = true;
		}

		public override bool PreAI(Projectile projectile)
		{
			// apply velocity
            // projectile.Center += new Vector2(lastVelocity.X, 0f);
            // if(!(projectile.velocity.X == 0f && lastVelocity.X != 0f))
            //     lastVelocity = projectile.velocity;

			// projectile.velocity = Vector2.Zero;

			lastVelocity = projectile.velocity;

            return true;
		}

		public override void PostAI(Projectile projectile)
		{
			if(projectile.velocity.X != lastVelocity.X) projectile.tileCollide = false;
			else projectile.tileCollide = true;
			lastVelocity = projectile.velocity;
		}

		public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
		{
			if(projectile.velocity.X != 0f) projectile.netUpdate = true;
			projectile.velocity.X = 0f;
			lastVelocity = new Vector2(0, 0);
			return true;
		}
	}
    // public class LightningAuraOverrideAborted : IProjectileOverride
    // {
    //     // constants
    //     protected const int HIT_COOLDOWN_LV1 = (int)(60 * 0.5f);
    //     protected const int HIT_COOLDOWN_LV2 = (int)(60 * 0.25f);
	// 	protected const float CRIT_RATE_LV1 = 0f;
	// 	protected const float CRIT_RATE_LV2 = 0.16f;
	// 	protected const float CRIT_RATE_LV3 = 0.25f;
    //     protected const int FRAME_SPEED = 5;
    //     protected const int FRAME_COUNT = 6;
	// 	protected virtual int MAX_RADIUS => 15; 	// unit:tile(s)
	// 	protected const float RADIUS_FACTOR = 1.2f;
	// 	protected const int MAX_FRAME_SPEED = 20;
	// 	protected const int MIN_FRAME_SPEED = 3;

	// 	// buff constants
	// 	protected const int BUFF_DURATION = (int)(60 * 1f);
	// 	protected const int BUFF_THRESHOLD = 60;
	// 	protected const int BUFF_NORMAL_RESET_INTERVAL = 15;
	// 	protected const int BUFF_HITNPC_RESET_INTERVAL = 3;
	// 	protected int BuffResetCnt = 0;
	// 	protected int BuffCntAddTimer = 0;
	// 	protected int BuffHitNPCAddTimer = 0;

    //     protected Vector2 TargetDirection = new Vector2(1f, 0f);
    //     protected int shootTimer = HIT_COOLDOWN_LV1 / 2;
    //     protected int TimerDuringShoot = -1;

    //     protected bool SquireArmorSet = false;
    //     protected bool SquireAltArmorSet = false;

    //     public LightningAuraOverrideAborted()
    //     {
    //         RegisterFlags["SetDefaults"] = true;
    //         RegisterFlags["PreAI"] = true;
    //         RegisterFlags["Colliding"] = true;
	// 		RegisterFlags["OnHitNPC"] = true;
    //     }

    //     public override void SetDefaults(Projectile projectile)
    //     {
    //         projectile.width = 16;
    //         projectile.height = 16;
    //         projectile.aiStyle = -1;
    //         projectile.timeLeft = Projectile.SentryLifeTime;
    //         projectile.friendly = true;
    //         projectile.usesLocalNPCImmunity = true;
    //         projectile.localNPCHitCooldown = 30;
    //         projectile.penetrate = -1;
    //         projectile.netImportant = true;
    //         projectile.ignoreWater = true;
    //         projectile.tileCollide = true;
    //         projectile.manualDirectionChange = true;
    //         projectile.sentry = true;
    //         projectile.DamageType = DamageClass.Summon;
    //     }

	// 	public override bool? Colliding(Projectile projectile, Rectangle myRect, Rectangle targetRect)
	// 	{
	// 		if (myRect.Intersects(targetRect) && targetRect.Distance(projectile.Center) < (float)(projectile.height / 2 - 20))
	// 		{
	// 			if (projectile.AI_137_CanHit(targetRect.Center.ToVector2()) || projectile.AI_137_CanHit(targetRect.TopLeft() + new Vector2(targetRect.Width / 2, 0f)))
	// 			{
	// 				return true;
	// 			}
	// 		}
	// 		return false;
	// 	}

	// 	protected void CreateLightningDustEffect(Vector2 start, Vector2 end, float displacement, float minDisplacement)
	// 	{
	// 		Vector2 origin = start;
	// 		Vector2 direction = start - end;
	// 		List<Vector2> LightningPoints = new List<Vector2>();
	// 		MinionAIHelper.GenerateLightning(origin, end, displacement, minDisplacement, LightningPoints);

	// 		for(int i = 0; i < LightningPoints.Count; i++)
	// 		{
	// 			Vector2 point = LightningPoints[i];
	// 			direction = end - point;
	// 			direction.Normalize();
	// 			if(i < LightningPoints.Count - 1)
	// 			{
	// 				Vector2 nextPoint = LightningPoints[i + 1];
	// 				direction = nextPoint - point;
	// 				direction.Normalize();
	// 			}
	// 			float seed = MinionAIHelper.RandomFloat(0f, 1f);
	// 			if(seed > 0.8f)
	// 			{
	// 				continue;
	// 			}

	// 			Vector2 targetPosition = point;

	// 			Dust dust = Dust.NewDustDirect(targetPosition, 5, 5, 226, 0f, 0f, 100);
	// 			dust.position = targetPosition;
	// 			dust.velocity = direction * 3f * (i / (float)LightningPoints.Count);
	// 			dust.scale = MinionAIHelper.RandomFloat(0.5f, 0.7f);
	// 			dust.fadeIn = 1f * (i / (float)LightningPoints.Count);
	// 			dust.noGravity = true;
	// 			dust.noLight = true;
	// 		}
	// 	}

    //     public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    //     {
	// 		// create electric dust effect
	// 		// CreateLightningDustEffect(projectile.Center - new Vector2(0, projectile.height/2), target.Center, (float)Math.Max(direction.Length() / 7f, 4f), 4f);

	// 		BuffHitNPCAddTimer++;
	// 		if(BuffHitNPCAddTimer >= BUFF_HITNPC_RESET_INTERVAL)
	// 		{
	// 			BuffHitNPCAddTimer = 0;
	// 			BuffResetCnt++;
	// 		}
    //     }

    //     protected void CheckArmorSet(Player player)
    //     {
    //         SquireArmorSet = player.armor[0].type == 3806 &&
    //                player.armor[1].type == 3807 &&
    //                player.armor[2].type == 3808;
    //         SquireAltArmorSet = player.armor[0].type == 3880 &&
    //                player.armor[1].type == 3881 &&
    //                player.armor[2].type == 3882;
    //     }

    //     protected bool VanillaFind(Point origin, GenSearch search, out Point result)
    //     {
    //         result = search.Find(origin);
    //         if (result == GenSearch.NOT_FOUND)
    //         {
    //             return false;
    //         }
    //         return true;
    //     }

	// 	protected void VanillaLightningAuraAI(Projectile projectile)
	// 	{
	// 		// apply gravity
	// 		MinionAIHelper.ApplyGravity(projectile, ModGlobal.SENTRY_GRAVITY, ModGlobal.SENTRY_MAX_FALL_SPEED);

	// 		int MaxHeight = 20;
	// 		int num2 = 999;
	// 		int HitCooldown = 30;
	// 		int num4 = 40;
	// 		int MinHeight = 4;
	// 		projectile.knockBack = 0f;
	// 		if (Main.player[projectile.owner].setMonkT2)
	// 		{
	// 			HitCooldown -= 5;
	// 		}
	// 		if (Main.player[projectile.owner].setMonkT3)
	// 		{
	// 			MaxHeight = 24;
	// 			MinHeight = 8;
	// 		}
	// 		projectile.ai[0] += 1f;
	// 		if (projectile.ai[0] >= (float)HitCooldown)
	// 		{
	// 			projectile.ai[0] = 0f;
	// 		}
	// 		if (projectile.ai[0] == 0f)
	// 		{
	// 			bool flag = false;
	// 			for (int i = 0; i < 200; i++)
	// 			{
	// 				NPC nPC = Main.npc[i];
	// 				if (nPC.CanBeChasedBy(projectile) && nPC.Hitbox.Distance(projectile.Center) < (float)(projectile.width / 2) && projectile.Colliding(projectile.Hitbox, nPC.Hitbox))
	// 				{
	// 					flag = true;
	// 					break;
	// 				}
	// 			}
	// 			if (flag)
	// 			{
	// 				SoundEngine.PlaySound(in SoundID.DD2_LightningAuraZap, projectile.Center);
	// 			}
	// 		}
	// 		if (projectile.localAI[0] == 0f)
	// 		{
	// 			projectile.localAI[0] = 1f;
	// 			projectile.velocity = Vector2.Zero;
	// 			Point origin = projectile.Center.ToTileCoordinates();
	// 			bool flag2 = true;
	// 			if (!VanillaFind(origin, Searches.Chain(new Searches.Down(500), new Conditions.NotNull(), new Conditions.IsSolid()), out var result))
	// 			{
	// 				flag2 = false;
	// 				projectile.position.Y += 16f;
	// 				return;
	// 			}
	// 			if (!VanillaFind(new Point(result.X, result.Y - 1), Searches.Chain(new Searches.Up(MaxHeight), new Conditions.NotNull(), new Conditions.IsSolid()), out var result2))
	// 			{
	// 				result2 = new Point(origin.X, origin.Y - MaxHeight - 1);
	// 			}
	// 			int num6 = 0;
	// 			if (flag2 && Main.tile[result.X, result.Y] != null && Main.tile[result.X, result.Y].BlockType == BlockType.HalfBlock)
	// 			{
	// 				num6 += 8;
	// 			}
	// 			Vector2 center = result.ToWorldCoordinates(8f, num6);
	// 			Vector2 vector = result2.ToWorldCoordinates(8f, 0f);
	// 			projectile.Size = new Vector2(1f, center.Y - vector.Y);
	// 			if (projectile.height > MaxHeight * 16)
	// 			{
	// 				projectile.height = MaxHeight * 16;
	// 			}
	// 			if (projectile.height < MinHeight * 16)
	// 			{
	// 				projectile.height = MinHeight * 16;
	// 			}
	// 			projectile.height *= 2;
	// 			projectile.width = (int)((float)projectile.height * 1f);
	// 			if (projectile.width > num2)
	// 			{
	// 				projectile.width = num2;
	// 			}
	// 			projectile.Center = center;
	// 		}
	// 		// calculate frame speed
	// 		int FrameSpeed = (int)((BUFF_THRESHOLD - BuffResetCnt) / (float) BUFF_THRESHOLD * (MAX_FRAME_SPEED - MIN_FRAME_SPEED) + MIN_FRAME_SPEED);
	// 		if (++projectile.frameCounter >= FrameSpeed)
	// 		{
	// 			projectile.frameCounter = 0;
	// 			if (++projectile.frame >= Main.projFrames[projectile.type])
	// 			{
	// 				projectile.frame = 0;
	// 			}
	// 		}
	// 		DelegateMethods.v3_1 = new Vector3(0.2f, 0.7f, 1f);
	// 		Utils.PlotTileLine(projectile.Center + Vector2.UnitX * -40f, projectile.Center + Vector2.UnitX * 40f, 80f, DelegateMethods.CastLightOpen);
	// 		Vector2 vector2 = new Vector2(projectile.Top.X, projectile.position.Y + (float)num4);
	// 		for (int j = 0; j < 4; j++)
	// 		{
	// 			if (Main.rand.Next(6) != 0)
	// 			{
	// 				continue;
	// 			}
	// 			Vector2 vector3 = Main.rand.NextVector2Unit();
	// 			// Radius dust effect
	// 			if (!(Math.Abs(vector3.X) < 0.12f))
	// 			{
	// 				Vector2 targetPosition = projectile.Center + vector3 * new Vector2((projectile.height - num4) / 2);
	// 				if (!WorldGen.SolidTile((int)targetPosition.X / 16, (int)targetPosition.Y / 16) && projectile.AI_137_CanHit(targetPosition))
	// 				{
	// 					Dust dust = Dust.NewDustDirect(targetPosition, 0, 0, 226, 0f, 0f, 100);
	// 					dust.position = targetPosition;
	// 					dust.velocity = (vector2 - dust.position).SafeNormalize(Vector2.Zero);
	// 					dust.scale = 0.7f;
	// 					dust.fadeIn = 1f;
	// 					dust.noGravity = true;
	// 					dust.noLight = true;
	// 				}
	// 			}
	// 		}
	// 		for (int k = 0; k < 0; k++)
	// 		{
	// 			if (Main.rand.Next(10) != 0)
	// 			{
	// 				continue;
	// 			}
	// 			Vector2 vector4 = Main.rand.NextVector2Unit();
	// 			if (!(Math.Abs(vector4.X) < 0.12f))
	// 			{
	// 				Vector2 targetPosition2 = projectile.Center + vector4 * new Vector2((projectile.height - num4) / 2) * Main.rand.NextFloat();
	// 				if (!WorldGen.SolidTile((int)targetPosition2.X / 16, (int)targetPosition2.Y / 16) && projectile.AI_137_CanHit(targetPosition2))
	// 				{
	// 					Dust dust2 = Dust.NewDustDirect(targetPosition2, 0, 0, 226, 0f, 0f, 100);
	// 					dust2.velocity *= 0.6f;
	// 					dust2.velocity += Vector2.UnitY * -2f;
	// 					dust2.noGravity = true;
	// 					dust2.noLight = true;
	// 				}
	// 			}
	// 		}
	// 		// Central pillar dust effect
	// 		for (int l = 0; l < 4; l++)
	// 		{
	// 			if (Main.rand.Next(10) == 0)
	// 			{
	// 				Dust dust3 = Dust.NewDustDirect(vector2 - new Vector2(8f, 0f), 16, projectile.height / 2 - 40, 226, 0f, 0f, 100);
	// 				dust3.velocity *= 0.6f;
	// 				dust3.velocity += Vector2.UnitY * -2f;
	// 				dust3.scale = 0.7f;
	// 				dust3.noGravity = true;
	// 				dust3.noLight = true;
	// 			}
	// 		}
	// 		// projectile.tileCollide = true;
	// 		// projectile.velocity.Y += 0.2f;

	// 		BuffCntAddTimer++;
	// 		if(BuffCntAddTimer >= BUFF_NORMAL_RESET_INTERVAL)
	// 		{
	// 			BuffCntAddTimer = 0;
	// 			BuffResetCnt++;
	// 		}

	// 		if(BuffResetCnt >= BUFF_THRESHOLD)
	// 		{
	// 			BuffResetCnt = 0;
	// 			Player owner = Main.player[projectile.owner];
	// 			owner.AddBuff(ModContent.BuffType<ElectricBoostBuff>(), BUFF_DURATION);
	// 			Vector2 direction = owner.Center - projectile.Center;
	// 			float displacement = MathHelper.Clamp(direction.Length() / 7f, 4f, 100f);
	// 			CreateLightningDustEffect(projectile.Center - new Vector2(0, projectile.height/2), owner.Center, displacement, 4f);
	// 		}
	// 	}
    // }

    // public class LightningAuraT1Override : LightningAuraOverride
    // {
    //     public override bool PreAI(Projectile projectile)
    //     {
	// 		VanillaLightningAuraAI(projectile);
    //         // UpdateAnimation(projectile);
    //         return false;
    //     }
    // }
}
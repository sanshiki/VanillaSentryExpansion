using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using System;
using System.IO;
using Terraria.ModLoader.IO;

using System.Collections.Generic;

using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.Content.Projectiles.Summon;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
	public abstract class IProjectileOverride
	{
		// virturl entries
		public virtual void SetDefaults(Projectile projectile) {}
		public virtual bool PreAI(Projectile projectile) => true;
		public virtual void AI(Projectile projectile) {}
		public virtual void PostAI(Projectile projectile) {}
		public virtual bool OnTileCollide(Projectile projectile, Vector2 oldVelocity) => true;
		public virtual bool? Colliding(Projectile projectile, Rectangle myRect, Rectangle targetRect) => null;
		public virtual void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) {}
		public virtual void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers) {}
		public virtual bool TileCollideStyle(Projectile projectile, ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) => true;
		public virtual void OnSpawn(Projectile projectile, IEntitySource source) {}
		public virtual void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) {}
		public virtual void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) {}
		// register flags
		public Dictionary<string, bool> RegisterFlags = new()
		{
			{ "SetDefaults", false },
			{ "PreAI", false },
			{ "AI", false },
			{ "PostAI", false },
			{ "OnTileCollide", false },
			{ "Colliding", false },
			{ "OnHitNPC", false },
			{ "ModifyHitNPC", false },
			{ "TileCollideStyle", false },
			{ "OnSpawn", false},
			{ "SendExtraAI", false},
			{ "ReceiveExtraAI", false}
		};
	}
	
	public class VanillaSentryOverride : GlobalProjectile
	{

		public override bool InstancePerEntity => true;

		private readonly Dictionary<int, IProjectileOverride> overrides = new()
		{
			{ ProjectileID.DD2BallistraTowerT1, new BallistaTowerT1Override() },
			{ ProjectileID.DD2BallistraTowerT2, new BallistaTowerT2Override() },
			{ ProjectileID.DD2BallistraTowerT3, new BallistaTowerT3Override() },
			{ ProjectileID.DD2FlameBurstTowerT1, new FlameburstTowerT1Override() },
			{ ProjectileID.DD2FlameBurstTowerT2, new FlameburstTowerT2Override() },
			{ ProjectileID.DD2FlameBurstTowerT3, new FlameburstTowerT3Override() },
			{ ProjectileID.DD2FlameBurstTowerT1Shot, new FlameburstShotT1Override() },
			{ ProjectileID.DD2FlameBurstTowerT2Shot, new FlameburstShotT2Override() },
			{ ProjectileID.DD2FlameBurstTowerT3Shot, new FlameburstShotT3Override() },
			{ ProjectileID.DD2LightningAuraT1, new LightningAuraOverride() },
			{ ProjectileID.DD2LightningAuraT2, new LightningAuraOverride() },
			{ ProjectileID.DD2LightningAuraT3, new LightningAuraOverride() },
			{ ProjectileID.DD2ExplosiveTrapT1, new ExplosiveTrapOverride() },
			{ ProjectileID.DD2ExplosiveTrapT2, new ExplosiveTrapOverride() },
			{ ProjectileID.DD2ExplosiveTrapT3, new ExplosiveTrapOverride() },
			{ ProjectileID.SpiderHiver, new QueenSpiderOverride() },
			{ ProjectileID.SpiderEgg, new SpiderEggOverride() },
			{ ProjectileID.HoundiusShootius, new EyeBallTurretOverride() },
			{ ProjectileID.HoundiusShootiusFireball, new EyeBallTurretFireballOverride() },
			{ ProjectileID.FrostHydra, new FrostHydraOverrdie() },
			{ ProjectileID.FrostBlastFriendly, new FrostBlastOverride() },
			{ ProjectileID.MoonlordTurret, new MoonlordTurretOverride() },
			{ ProjectileID.RainbowCrystal, new RainbowCrystalOverride() },
			// 以后只需要在这里加映射
		};

		public override void SetDefaults(Projectile projectile)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
			{
				if (handler.RegisterFlags["SetDefaults"])
					handler.SetDefaults(projectile);
				else
					base.SetDefaults(projectile);
			}
				
			else
				base.SetDefaults(projectile);
		}

		public override bool PreAI(Projectile projectile)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
			{
				if (handler.RegisterFlags["PreAI"])
					return handler.PreAI(projectile);
				else
					return base.PreAI(projectile);
			}
			return base.PreAI(projectile);
		}

		public override void AI(Projectile projectile)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
			{
				if (handler.RegisterFlags["AI"])
					handler.AI(projectile);
				else
					base.AI(projectile);
			}
			else
				base.AI(projectile);
		}

		public override void PostAI(Projectile projectile)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				handler.PostAI(projectile);
			else
				base.PostAI(projectile);
		}

		public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
			{
				if (handler.RegisterFlags["OnTileCollide"])
					return handler.OnTileCollide(projectile, oldVelocity);
				else
					return base.OnTileCollide(projectile, oldVelocity);
			}
			return base.OnTileCollide(projectile, oldVelocity);
		}

		public override bool? Colliding(Projectile projectile, Rectangle myRect, Rectangle targetRect)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
			{
				if (handler.RegisterFlags["Colliding"])
					return handler.Colliding(projectile, myRect, targetRect);
				else
					return base.Colliding(projectile, myRect, targetRect);
			}
			return base.Colliding(projectile, myRect, targetRect);
		}

		public override bool TileCollideStyle(Projectile projectile, ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				return handler.TileCollideStyle(projectile, ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
			else
				return base.TileCollideStyle(projectile, ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				handler.OnHitNPC(projectile, target, hit, damageDone);
			else
				base.OnHitNPC(projectile, target, hit, damageDone);
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				handler.ModifyHitNPC(projectile, target, ref modifiers);
			else
				base.ModifyHitNPC(projectile, target, ref modifiers);
		}
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				handler.OnSpawn(projectile, source);
			else
				base.OnSpawn(projectile, source);
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				handler.SendExtraAI(projectile, bitWriter, binaryWriter);
			else
				base.SendExtraAI(projectile, bitWriter, binaryWriter);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			if (overrides.TryGetValue(projectile.type, out var handler))
				handler.ReceiveExtraAI(projectile, bitReader, binaryReader);
			else
				base.ReceiveExtraAI(projectile, bitReader, binaryReader);
		}
	}
}
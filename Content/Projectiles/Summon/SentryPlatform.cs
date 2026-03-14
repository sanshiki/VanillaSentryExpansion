// using Microsoft.Xna.Framework;
// using System;
// using System.Collections.Generic;
// using Terraria;
// using Terraria.DataStructures;
// using Terraria.ID;
// using Terraria.ModLoader;
// using System.Linq;

// using SummonerExpansionMod.Content.Buffs.Summon;
// using SummonerExpansionMod.ModUtils;
// using SummonerExpansionMod.Initialization;

// namespace SummonerExpansionMod.Content.Projectiles.Summon
// {    
// 	public class SentryPlatform : ModProjectile
// 	{

// 		private static Dictionary<SentryPlatform, int> attachedSentryDict = new Dictionary<SentryPlatform, int>();

// 		public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/SentryPlatform";

// 		public override void SetStaticDefaults()
// 		{
// 			// Sets the amount of frames this minion has on its spritesheet
// 			Main.projFrames[Projectile.type] = 1;
// 			// This is necessary for right-click targeting
// 			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

// 			Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion

// 			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
// 			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.
// 		}

// 		public sealed override void SetDefaults()
// 		{
// 			Projectile.width = 32;
// 			Projectile.height = 36;
// 			Projectile.tileCollide = false; // Makes the minion go through tiles freely

// 			// These below are needed for a minion weapon
// 			Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
// 			Projectile.minion = true; // Declares this as a minion (has many effects)
// 			Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
// 			Projectile.minionSlots = 1f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
// 			Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles

// 			attachedSentryDict.Add(this, -1);	// -1 means no sentry attached
// 		}

// 		// Here you can decide if your minion breaks things like grass or pots
// 		public override bool? CanCutTiles()
// 		{
// 			return false;
// 		}

// 		// This is mandatory if your minion deals contact damage (further related stuff in AI() in the Movement region)
// 		public override bool MinionContactDamage()
// 		{
// 			return false;
// 		}

// 		// The AI of this minion is split into multiple methods to avoid bloat. This method just passes values between calls actual parts of the AI.
// 		public override void AI()
// 		{
// 			Player owner = Main.player[Projectile.owner];

// 			if (!CheckActive(owner))
// 			{
// 				return;
// 			}

// 			GeneralBehavior(owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition);
// 			// SearchForTargets(owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
// 			Movement(false, 0, Vector2.Zero, distanceToIdlePosition, vectorToIdlePosition);
// 			// Visuals();
//             AttachSentry(owner);

//             // Main.NewText(attachedSentryDict[platformIndex]);
// 			string attachedSentryDictString = "";
// 			foreach(var entry in attachedSentryDict)
// 			{
// 				attachedSentryDictString += entry.Value + " ";
// 			}
// 			// Main.NewText("attachedSentryDict:" + attachedSentryDictString);
// 		}

// 		// This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
// 		private bool CheckActive(Player owner)
// 		{
// 			if (owner.dead || !owner.active)
// 			{
// 				owner.ClearBuff(ModContent.BuffType<SentryPlatformBuff>());

// 				return false;
// 			}

// 			if (owner.HasBuff(ModContent.BuffType<SentryPlatformBuff>()))
// 			{
// 				Projectile.timeLeft = 2;
// 			}

// 			return true;
// 		}

//         private void AttachSentry(Player owner)
//         {
//             // if the platform haven't attached to a sentry, find the closest sentry
//             if (attachedSentryDict[this] == -1)
//             {
//                 for (int i = 0; i < Main.maxProjectiles; i++)
//                 {
//                     Projectile other = Main.projectile[i];

// 					bool hasAttached = attachedSentryDict.Any(entry => entry.Value == i);
//                     if (!other.active || other.owner != owner.whoAmI || !IsSentry(other) || hasAttached)
//                     {
//                         continue;
//                     }

//                     float distance = Vector2.Distance(Projectile.Center, other.Center);
//                     if (distance < 48f)
//                     {
//                         attachedSentryDict[this] = i;
// 						if(other.ModProjectile is SentryWithSpawnAnime sentryWithSpawnAnime)
// 						{
// 							sentryWithSpawnAnime.SetAttached(true);
// 						}
// 						// Main.NewText("attached to " + other.type);
//                         break;
//                     }
//                 }
//             }
            
//             // if a available sentry is found, attach to it
//             if (attachedSentryDict[this] != -1)
//             {
//                 Projectile sentry = Main.projectile[attachedSentryDict[this]];
//                 if (sentry.active && sentry.owner == owner.whoAmI)
//                 {
//                     sentry.Center = Projectile.Center + new Vector2(0, -sentry.height / 2 - 7);
//                     sentry.velocity = Projectile.velocity;
					
                    
//                     sentry.tileCollide = false;
//                 }
//                 else
//                 {
//                     attachedSentryDict[this] = -1;
//                 }
//             }
//         }

//         private bool IsSentry(Projectile proj)
//         {
//             // return proj.minion && proj.sentry && !proj.netImportant && proj.friendly && proj.damage > 0;
//             return proj.sentry;
//         }

// 		private void GeneralBehavior(Player owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
// 		{
// 			Vector2 idlePosition = owner.Center;
// 			idlePosition.Y -= 48f; // Go up 48 coordinates (three tiles from the center of the player)

// 			// If your minion doesn't aimlessly move around when it's idle, you need to "put" it into the line of other summoned minions
// 			// The index is projectile.minionPos
// 			float minionPositionOffsetX = (10 + Projectile.minionPos * 40) * -owner.direction;
// 			idlePosition.X += minionPositionOffsetX; // Go behind the player

// 			// All of this code below this line is adapted from Spazmamini code (ID 388, aiStyle 66)

// 			// Teleport to player if distance is too big
// 			vectorToIdlePosition = idlePosition - Projectile.Center;
// 			distanceToIdlePosition = vectorToIdlePosition.Length();

// 			if (Main.myPlayer == owner.whoAmI && distanceToIdlePosition > 2000f)
// 			{
// 				// Whenever you deal with non-regular events that change the behavior or position drastically, make sure to only run the code on the owner of the projectile,
// 				// and then set netUpdate to true
// 				Projectile.position = idlePosition;
// 				Projectile.velocity *= 0.1f;
// 				Projectile.netUpdate = true;
// 			}

// 			// If your minion is flying, you want to do this independently of any conditions
// 			float overlapVelocity = 0.04f;

// 			// Fix overlap with other minions
// 			foreach (var other in Main.ActiveProjectiles)
// 			{
// 				if (other.whoAmI != Projectile.whoAmI && other.owner == Projectile.owner && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
// 				{
// 					if (Projectile.position.X < other.position.X)
// 					{
// 						Projectile.velocity.X -= overlapVelocity;
// 					}
// 					else
// 					{
// 						Projectile.velocity.X += overlapVelocity;
// 					}

// 					if (Projectile.position.Y < other.position.Y)
// 					{
// 						Projectile.velocity.Y -= overlapVelocity;
// 					}
// 					else
// 					{
// 						Projectile.velocity.Y += overlapVelocity;
// 					}
// 				}
// 			}
// 		}

// 		private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
// 		{
// 			// Default movement parameters (here for attacking)
// 			float speed = 8f;
// 			float inertia = 10f;

// 			if (foundTarget)
// 			{
// 				// Minion has a target: attack (here, fly towards the enemy)
// 				if (distanceFromTarget > 40f)
// 				{
// 					// The immediate range around the target (so it doesn't latch onto it when close)
// 					Vector2 direction = targetCenter - Projectile.Center;
// 					direction.Normalize();
// 					direction *= speed;

// 					Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
// 				}
// 			}
// 			else
// 			{
// 				// Minion doesn't have a target: return to player and idle
// 				if (distanceToIdlePosition > 600f)
// 				{
// 					// Speed up the minion if it's away from the player
// 					speed = 12f;
// 					inertia = 60f;
// 				}
// 				else
// 				{
// 					// Slow down the minion if closer to the player
// 					speed = 4f;
// 					inertia = 80f;
// 				}

// 				if (distanceToIdlePosition > 20f)
// 				{
// 					// The immediate range around the player (when it passively floats about)

// 					// This is a simple movement formula using the two parameters and its desired direction to create a "homing" movement
// 					vectorToIdlePosition.Normalize();
// 					// vectorToIdlePosition *= distanceToIdlePosition > 600f ? speed : distanceToIdlePosition;
// 					// Projectile.velocity = vectorToIdlePosition * 0.05f + Projectile.velocity * 0.03f;
// 					vectorToIdlePosition *= speed;
// 					Projectile.velocity = (Projectile.velocity * (inertia - 1) + vectorToIdlePosition) / inertia;
// 				}
// 				else if (Projectile.velocity == Vector2.Zero)
// 				{
// 					// If there is a case where it's not moving at all, give it a little "poke"
// 					Projectile.velocity.X = -0.15f;
// 					Projectile.velocity.Y = -0.05f;
// 				}
// 			}
// 		}

// 		private void Visuals()
// 		{
// 			// So it will lean slightly towards the direction it's moving
// 			Projectile.rotation = Projectile.velocity.X * 0.05f;

// 			// This is a simple "loop through all frames from top to bottom" animation
// 			int frameSpeed = 5;

// 			Projectile.frameCounter++;

// 			if (Projectile.frameCounter >= frameSpeed)
// 			{
// 				Projectile.frameCounter = 0;
// 				Projectile.frame++;

// 				if (Projectile.frame >= Main.projFrames[Projectile.type])
// 				{
// 					Projectile.frame = 0;
// 				}
// 			}

// 			// Some visuals here
// 			Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
// 		}

// 		public override void Kill(int timeLeft)
// 		{
// 			int sentryIndex = attachedSentryDict[this];
// 			if(sentryIndex != -1)
// 			{
// 				Projectile sentry = Main.projectile[sentryIndex];
// 				if (sentry != null && sentry.active && sentry.owner == Projectile.owner)
// 				{
// 					sentry.tileCollide = true;
// 					if(sentry.ModProjectile is SentryWithSpawnAnime sentryWithSpawnAnime)
// 					{
// 						sentryWithSpawnAnime.SetAttached(false);
// 					}
// 				}
// 			}
// 			attachedSentryDict.Remove(this);
// 		}
// 	}
// }

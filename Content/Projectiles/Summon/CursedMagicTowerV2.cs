// using Microsoft.Xna.Framework;
// using System;
// using System.Collections.Generic;
// using Terraria;
// using Terraria.DataStructures;
// using Terraria.ID;
// using Terraria.ModLoader;
// using Terraria.Audio;

// using Microsoft.Xna.Framework.Graphics;
// using SummonerExpansionMod.Content.Buffs.Summon;
// using SummonerExpansionMod.ModUtils;
// using SummonerExpansionMod.Initialization;

// namespace SummonerExpansionMod.Content.Projectiles.Summon
// {
//     public class CursedMagicTowerV2 : ModProjectile
//     {
//         // animation
//         private const int FRAME_COUNT = 3;
//         private int FRAME_SPEED = 10;
//         // private int fireCooldown = 30;
//         private const int FIRE_INTERVAL = 120;
//         private int fireTimer = 0;
//         private long floatCnt = 0;

//         private const float REAL_BULLET_SPEED = 9.5f;
//         private const float PRED_BULLET_SPEED = 15f;
//         private const float DEACCELERATION = 0.5f;
//         private const bool USE_PREDICTION = false;

//         private const float ENHANCEMENT_FACTOR = 0.75f;
//         private int BUFF_ID = -1;

//         public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/CursedMagicTower";

//         public override void SetStaticDefaults()
//         {
//             ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
//             ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
//             Main.projFrames[Projectile.type] = FRAME_COUNT;
//         }

//         public override void SetDefaults()
//         {
//             Projectile.width = 40;
//             Projectile.height = 58;
//             Projectile.friendly = false;
//             Projectile.DamageType = DamageClass.Summon;
//             Projectile.ignoreWater = true;
//             Projectile.tileCollide = false;
//             Projectile.penetrate = -1;
//             Projectile.timeLeft = Projectile.SentryLifeTime;
//             Projectile.sentry = true;
//             Projectile.netImportant = true;
            
//             BUFF_ID = ModBuffID.SentryEnhancement;

//             // DynamicParamManager.Register("CursedMagicTower.RealBulletSpeed", REAL_BULLET_SPEED, 0f, 30f, null);

//         }


//         public override void AI()
//         {
//             // Float in the air
//             Vector2 vel = Projectile.velocity;
//             Vector2 vel_dir = vel.SafeNormalize(Vector2.Zero);
//             if(vel.Length() > DEACCELERATION)
//             {
//                 Projectile.velocity -= vel_dir * DEACCELERATION;
//             }
//             else
//             {
//                 Projectile.velocity = Vector2.Zero;
//             }

//             float FloatAmplitude = 0.5f;
//             FloatAmplitude = Math.Min(FloatAmplitude, 2f / vel.Length());
            
//             float FloatOffset = (float)(Math.Sin(floatCnt * 0.05f) * FloatAmplitude);
//             floatCnt++;
//             Projectile.Center += new Vector2(0, FloatOffset);

//             // teleport to owner if needed
//             float maxDistance = 2000f;
//             Player owner = Main.player[Projectile.owner];
//             if (Vector2.Distance(Projectile.Center, owner.Center) > maxDistance)
//             {
//                 TryTeleportNearPlayer(owner);
//             }

//             int fireInterval = FIRE_INTERVAL;
//             if(owner.HasBuff(BUFF_ID))
//             {
//                 fireInterval = (int)(fireInterval * ENHANCEMENT_FACTOR);
//             }

//             // find target and fire, as well as cooldown control
//             // NPC target = FindTarget();
//             NPC target = MinionAIHelper.SearchForTargets(
//                 owner, 
//                 Projectile, 
//                 1500f, 
//                 false, 
//                 null).TargetNPC;
            
//             fireTimer++;
//             if(fireTimer >= fireInterval)
//             {
//                 if(target != null)
//                 {
//                     FireAt(target);
//                     fireTimer = 0;
//                 }
//                 else
//                 {
//                     fireTimer = fireInterval;
//                 }
//             }

//             UpdateAnimation(target);
//         }

//         private void FireAt(NPC target)
//         {
//             Vector2 PredictedPos = target.Center;
            
//             Vector2 direction = PredictedPos - Projectile.Center;
//             direction.Normalize();

//             float AngleOffset = MinionAIHelper.RandomFloat(-0.1f, 0.1f);
//             direction = direction.RotatedBy(AngleOffset);

//             Vector2 ShootOffset = new Vector2(0, -8f);

//             // float RealBulletSpeed = (float)DynamicParamManager.Get("CursedMagicTower.RealBulletSpeed").value;
//             float RealBulletSpeed = REAL_BULLET_SPEED;

//             float SpeedOffset = MinionAIHelper.RandomFloat(-1f, 1f);

//             Projectile proj = Projectile.NewProjectileDirect(
//                 Projectile.GetSource_FromAI(),
//                 Projectile.Center + ShootOffset,
//                 direction * (RealBulletSpeed + SpeedOffset),
//                 // ProjectileID.WaterStream, // Reusing vanilla projectile
//                 ModProjectileID.CursedMagicTowerBulletSmall,
//                 // ProjectileID.SapphireBolt,
//                 Projectile.damage,
//                 Projectile.knockBack,
//                 Projectile.owner
//             );

//             proj.ai[0] = target.whoAmI;

//             if(proj.ModProjectile is CursedMagicTowerBulletSmall bullet)
//             {
//                 bullet.Target = PredictedPos;
//             }

//             SoundEngine.PlaySound(SoundID.Item43, Projectile.Center);
//         }

//         private void TryTeleportNearPlayer(Player player)
//         {
//             Vector2 bestPosition = Projectile.Center;
//             int bestTileCount = int.MaxValue;

//             if(player.Center.Distance(Projectile.Center) > 4000f) return;

//             for (int xOffset = -5; xOffset <= 5; xOffset++)
//             {
//                 for (int yOffset = -5; yOffset <= 5; yOffset++)
//                 {
//                     Vector2 checkPos = player.Center + new Vector2(xOffset * 16, yOffset * 16);
//                     if (!Collision.SolidCollision(checkPos, Projectile.width, Projectile.height))
//                     {
//                         int tileCount = CountNearbySolidTiles(checkPos);
//                         if (tileCount < bestTileCount)
//                         {
//                             bestTileCount = tileCount;
//                             bestPosition = checkPos;
//                         }
//                     }
//                 }
//             }

//             // add random offset to best position
//             bestPosition += new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10));

//             // Teleport if we found a better position
//             Projectile.position = bestPosition;
//             Projectile.netUpdate = true;

//             // Optional: Visual or sound effect
//             if (Main.netMode != NetmodeID.Server)
//             {
//                 for (int i = 0; i < 10; i++)
//                 {
//                     Dust.NewDust(bestPosition, Projectile.width, Projectile.height, DustID.MagicMirror, Scale: 1.5f);
//                 }
//                 SoundEngine.PlaySound(SoundID.Item8, bestPosition);
//             }
//         }

//         private int CountNearbySolidTiles(Vector2 center)
//         {
//             int tileCount = 0;
//             Point tileCenter = center.ToTileCoordinates();

//             for (int x = -1; x <= 1; x++)
//             {
//                 for (int y = -1; y <= 1; y++)
//                 {
//                     Tile tile = Framing.GetTileSafely(tileCenter.X + x, tileCenter.Y + y);
//                     if (tile.HasTile && Main.tileSolid[tile.TileType])
//                     {
//                         tileCount++;
//                     }
//                 }
//             }

//             return tileCount;
//         }

//         public override bool MinionContactDamage()
// 		{
// 			return false;
// 		}

//         private void UpdateAnimation(NPC target)
//         {
//             Projectile.frameCounter++;
//             if (Projectile.frameCounter >= FRAME_SPEED)
//             {
//                 Projectile.frame = (Projectile.frame + 1) % FRAME_COUNT;
//                 Projectile.frameCounter = 0;
//             }

//             if (target != null)
//             {
//                 Vector2 direction = target.Center - Projectile.Center;
//                 direction.Normalize();
//                 Projectile.spriteDirection = direction.X > 0 ? -1 : 1;
//             }

//             // add light
//             Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 0.8f);

//             // add sparkle dust
//             float seed = MinionAIHelper.RandomFloat(0f,1f);
//             if(seed > 0.95f)
//             {
//                 int BlueDustID = 29;
//                 Dust BlueDust = Dust.NewDustDirect(Projectile.Center - Projectile.Size/2f, Projectile.width, Projectile.height, BlueDustID, 0f, 0f);
//                 BlueDust.noGravity = true;
//                 BlueDust.noLight = true;
//             }
//         }
//     }
// }
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using SummonerExpansionMod.Content.Buffs.Summon;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System.Collections.Generic;
using SummonerExpansionMod.Initialization;
using Terraria.ID;
using System.Linq;
using Terraria.WorldBuilding;
using Terraria.DataStructures;

using SummonerExpansionMod.Content.Items.Accessories;


namespace SummonerExpansionMod.ModUtils
{
	/// <summary>
	/// 召唤物AI通用工具类
	/// </summary>
	public static class MinionAIHelper
	{
		#region Constants
		// 默认距离常量
		public const float DEFAULT_TELEPORT_DISTANCE = 2500f;
		public const float DEFAULT_TARGET_SEARCH_RANGE = 700f;
		public const float DEFAULT_MAX_TARGET_DISTANCE = 2000f;
		public const float DEFAULT_CLOSE_THROUGH_WALL_DISTANCE = 100f;
		public const float DEFAULT_MINION_SPACING = 40f;
		public const float DEFAULT_OVERLAP_VELOCITY = 0.04f;
		#endregion

		#region Position and Movement
		/// <summary>
		/// 计算召唤物的理想位置
		/// </summary>
		/// <param name="owner">召唤物主人</param>
		/// <param name="projectile">召唤物</param>
		/// <param name="spacing">召唤物间距</param>
		/// <returns>理想位置</returns>
		public static Vector2 CalculateIdlePosition(Player owner, Projectile projectile, float spacing = DEFAULT_MINION_SPACING)
		{
			Vector2 idlePosition = owner.Center;
			float minionOffset = (spacing + projectile.minionPos * spacing) * -owner.direction;
			idlePosition.X += minionOffset;
			return idlePosition;
		}

		/// <summary>
		/// 处理距离过远时的传送
		/// </summary>
		/// <param name="owner">召唤物主人</param>
		/// <param name="projectile">召唤物</param>
		/// <param name="idlePosition">理想位置</param>
		/// <param name="teleportDistance">传送距离</param>
		public static void HandleTeleportation(Player owner, Projectile projectile, Vector2 idlePosition, float teleportDistance = DEFAULT_TELEPORT_DISTANCE)
		{
			float distanceToIdle = Vector2.Distance(projectile.Center, idlePosition);
			
			if (Main.myPlayer == owner.whoAmI && distanceToIdle > teleportDistance)
			{
				projectile.position = idlePosition;
				projectile.velocity *= 0.1f;
				projectile.netUpdate = true;
			}
		}

		/// <summary>
		/// 处理与其他召唤物的重叠
		/// </summary>
		/// <param name="projectile">当前召唤物</param>
		/// <param name="overlapVelocity">重叠修正速度</param>
		public static void HandleMinionOverlap(Projectile projectile, float overlapVelocity = DEFAULT_OVERLAP_VELOCITY)
		{
			foreach (var other in Main.ActiveProjectiles)
			{
				if (other.whoAmI != projectile.whoAmI && 
					other.owner == projectile.owner && 
					IsOverlapping(projectile, other))
				{
					ApplyOverlapCorrection(projectile, other, overlapVelocity);
				}
			}
		}

		/// <summary>
		/// 检查是否与其他召唤物重叠
		/// </summary>
		/// <param name="projectile">当前召唤物</param>
		/// <param name="other">其他召唤物</param>
		/// <returns>是否重叠</returns>
		public static bool IsOverlapping(Projectile projectile, Projectile other)
		{
			return Math.Abs(projectile.position.X - other.position.X) + 
				   Math.Abs(projectile.position.Y - other.position.Y) < projectile.width;
		}

		/// <summary>
		/// 应用重叠修正
		/// </summary>
		/// <param name="projectile">当前召唤物</param>
		/// <param name="other">其他召唤物</param>
		/// <param name="overlapVelocity">重叠修正速度</param>
		public static void ApplyOverlapCorrection(Projectile projectile, Projectile other, float overlapVelocity)
		{
			if (projectile.position.X < other.position.X)
				projectile.velocity.X -= overlapVelocity;
			else
				projectile.velocity.X += overlapVelocity;

			if (projectile.position.Y < other.position.Y)
				projectile.velocity.Y -= overlapVelocity;
			else
				projectile.velocity.Y += overlapVelocity;
		}
		#endregion

		#region Target Search
		/// <summary>
		/// 搜索目标的结果
		/// </summary>
		public struct TargetSearchResult
		{
			public bool FoundTarget;
			public float DistanceFromTarget;
			public Vector2 TargetCenter;
			public NPC TargetNPC;

			public TargetSearchResult(bool foundTarget, float distance, Vector2 center, NPC npc = null)
			{
				FoundTarget = foundTarget;
				DistanceFromTarget = distance;
				TargetCenter = center;
				TargetNPC = npc;
			}
		}

		/// <summary>
		/// 搜索目标
		/// </summary>
		/// <param name="owner">召唤物主人</param>
		/// <param name="projectile">召唤物</param>
		/// <param name="searchRange">搜索范围</param>
		/// <param name="maxDistance">最大目标距离</param>
		/// <returns>搜索结果</returns>
		public static TargetSearchResult SearchForTargets(Player owner, Projectile minion, 
			float searchRange = DEFAULT_TARGET_SEARCH_RANGE, 
			bool checkCanHit = true,
			Func<NPC, bool> otherCondition = null,
			bool checkAttackTarget = true)
		{
			float distanceFromTarget = searchRange;
			Vector2 targetCenter = minion.position;
			bool foundTarget = false;
			NPC targetNPC = null;

			// 检查玩家指定的目标
			if (checkAttackTarget && owner.HasMinionAttackTargetNPC)
			{
				NPC npc = Main.npc[owner.MinionAttackTargetNPC];
				float distance = Vector2.Distance(npc.Center, minion.Center);
				bool canBeChased = npc.CanBeChasedBy(minion);
				bool canHit = checkCanHit ? Collision.CanHitLine(minion.position, /* minion.width, minion.height,  */ 6, 6, 
																  npc.position, npc.width, npc.height) : true;

				if (distance < searchRange && canHit && canBeChased && (otherCondition == null || otherCondition(npc)))
				{
					distanceFromTarget = distance;
					targetCenter = npc.Center;
					foundTarget = true;
					targetNPC = npc;
				}
			}

			// 搜索附近的目标
			if (!foundTarget)
			{
				foreach (var npc in Main.ActiveNPCs)
				{
					bool canBeChased = npc.CanBeChasedBy(minion);
					bool canHit = checkCanHit ? Collision.CanHitLine(minion.position, minion.width, minion.height, 
																  npc.position, npc.width, npc.height) : true;
					if (canBeChased && canHit && (otherCondition == null || otherCondition(npc)))
					{
						float distance = Vector2.Distance(npc.Center, minion.Center);
						// bool hasSentryTargetTag = npc.HasBuff(ModBuffID.SentryTarget);
						bool hasSentryTargetTag = false;	// temporarily disabled
						if (distance < distanceFromTarget || hasSentryTargetTag)
						{
							distanceFromTarget = distance;
							targetCenter = npc.Center;
							foundTarget = true;
							targetNPC = npc;
						}

						if (hasSentryTargetTag) break;
					}
				}
			}

			return new TargetSearchResult(foundTarget, distanceFromTarget, targetCenter, targetNPC);
		}

		public static List<int> SearchForProjectiles(int type, Vector2 center, float radius)
		{
			List<int> projectileIDs = new List<int>();
			for(int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile projectile = Main.projectile[i];
				if(projectile.type == type && projectile.active)
				{
					if(projectile.Center.Distance(center) < radius)
					{
						projectileIDs.Add(i);
					}
				}
			}
			return projectileIDs;
		}

		public static Vector2 SearchForValidPosition(Vector2 center, int width, int height, int searchRadius)
		{
			Point centerTile = center.ToTileCoordinates();
			if (!Collision.SolidCollision(center, width, height))
			{
				return center;
			}
			for (int r = 0; r <= searchRadius; r++)
			{
				for (int x = -r; x <= r; x++)
				{
					for (int y = -r; y <= r; y++)
					{
						// only search edge
						if (Math.Abs(x) != r && Math.Abs(y) != r) continue;
						int checkX = centerTile.X + x;
						int checkY = centerTile.Y + y;
						if (checkX < 0 || checkX >= Main.maxTilesX || checkY < 0 || checkY >= Main.maxTilesY) continue;
						Vector2 worldPos = new Vector2(checkX * 16, checkY * 16);
						if (!Collision.SolidCollision(worldPos, width, height))
						{
							return worldPos;
						}
					}
				}
			}
			return center;
		}
		
		public static Vector2 SearchForGround(Vector2 center, int searchRadius, int width, int height)
		{
			bool debug = false;
			Point centerTile = center.ToTileCoordinates();
			for (int r = 0; r <= searchRadius; r++)
			{
				for (int x = -r; x <= r; x++)
				{
					for (int y = -r; y <= r; y++)
					{
						// 只检查正方形的边缘，减少无效搜索
						if (Math.Abs(x) != r && Math.Abs(y) != r)
							continue;

						int checkX = centerTile.X + x;
						int checkY = centerTile.Y + y;

						if (!WorldGen.InWorld(checkX, checkY, 10))
							continue;

						Tile tile = Main.tile[checkX, checkY];
						Tile tileBelow = Main.tile[checkX, checkY + 1];

						// 必须是“能站立的地面”：当前为空气，下面有实心或平台
						if (!tile.HasTile &&
							tileBelow.HasTile &&
							(Main.tileSolid[tileBelow.TileType] || Main.tileSolidTop[tileBelow.TileType]))
						{
							Vector2 worldPos = new Vector2(checkX * 16, checkY * 16)/*  + new Vector2(0, 16f) */;

							// 检查实体是否能在此处存在（无方块阻挡）
							if (!Collision.SolidCollision(worldPos + new Vector2(-width / 2f, -height), width, height))
							{
								// // 确保上方有头部空间
								// bool hasHeadroom = true;
								// for (int i = 1; i <= height / 16; i++)
								// {
								// 	if (Main.tile[checkX, checkY - i].HasTile)
								// 	{
								// 		hasHeadroom = false;
								// 		break;
								// 	}
								// }

								// if (!hasHeadroom)
								// 	continue;

								if (debug)
									Dust.QuickDust(worldPos, Color.Green);

								// ✅ 找到最近的合格表面
								return worldPos + new Vector2(0, 16f);
							}
							else if (debug)
                            {
								Main.NewText("Collision invalid.");
                            }
						}
						else if (debug)
                        {
							if (tile.HasTile) Dust.QuickDust(new Point(checkX, checkY), Color.Red);
							if (!tile.HasTile && !tileBelow.HasTile) Dust.QuickDust(new Point(checkX, checkY), Color.Yellow);
							if (!tile.HasTile && tileBelow.HasTile && !(Main.tileSolid[tileBelow.TileType] || Main.tileSolidTop[tileBelow.TileType])) Dust.QuickDust(new Point(checkX, checkY), Color.Blue);
                        }
					}
				}
			}
			return center;
        }

		public static Vector2? SearchSpawnPoint(Vector2 start, int projectileWidth, int projectileHeight)
		{
			// 使用 GenSearch 向下搜索地面
			Point? found = Searches.Chain(new Searches.Down(100), new Conditions.NotNull(), new Conditions.IsSolid()).Find(start.ToTileCoordinates());
			return found.HasValue ? found.Value.ToWorldCoordinates(0f, 0f) - new Vector2(0f, projectileHeight / 2f) : null;
		}

		public static List<NPC> SearchTargetsInRadius(Vector2 center, float radius)
		{
			List<NPC> targets = new List<NPC>();
			foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.Distance(center) < radius && !npc.dontTakeDamage && !npc.immortal)
                {
                    targets.Add(npc);
                }
            }
			return targets;
		}

		/// <summary>
		/// 更新召唤物的友好状态
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="hasTarget">是否有目标</param>
		public static void UpdateFriendlyState(Projectile projectile, bool hasTarget)
		{
			projectile.friendly = hasTarget;
		}
		#endregion

		#region Movement Helpers
		/// <summary>
		/// 应用重力
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="gravity">重力加速度</param>
		/// <param name="maxFallSpeed">最大下落速度</param>
		public static void ApplyGravity(Projectile projectile, float gravity = 0.4f, float maxFallSpeed = 10f)
		{
			projectile.velocity.Y += gravity;
			if (projectile.velocity.Y > maxFallSpeed)
			{
				projectile.velocity.Y = maxFallSpeed;
			}
		}

		/// <summary>
		/// 应用平滑移动
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="targetVelocity">目标速度</param>
		/// <param name="inertia">惯性系数</param>
		public static void ApplySmoothMovement(Projectile projectile, Vector2 targetVelocity, float inertia)
		{
			projectile.velocity = (projectile.velocity * (inertia - 1) + targetVelocity) / inertia;
		}

		/// <summary>
		/// 应用水平平滑移动
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="targetSpeedX">目标水平速度</param>
		/// <param name="inertia">惯性系数</param>
		public static void ApplySmoothHorizontalMovement(Projectile projectile, float targetSpeedX, float inertia)
		{
			float currentSpeed = projectile.velocity.X;
			projectile.velocity.X = (currentSpeed * (inertia - 1) + targetSpeedX) / inertia;
		}

		/// <summary>
		/// 检查是否卡在固体中
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <returns>是否卡在固体中</returns>
		public static bool IsStuckInSolid(Projectile projectile)
		{
			return Collision.SolidCollision(projectile.position, projectile.width, projectile.height);
		}
		#endregion

		#region Animation Helpers
		/// <summary>
		/// 更新精灵方向
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="threshold">速度阈值</param>
		public static void UpdateSpriteDirection(Projectile projectile, float threshold = 0.01f)
		{
			projectile.spriteDirection = projectile.velocity.X > threshold ? -1 :
										projectile.velocity.X < -threshold ? 1 :
										projectile.spriteDirection;
		}

		/// <summary>
		/// 更新动画帧
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="frameSpeed">帧率</param>
		/// <param name="maxFrames">最大帧数</param>
		/// <param name="resetToZero">是否重置到第0帧</param>
		public static void UpdateAnimationFrame(Projectile projectile, int frameSpeed, int maxFrames, bool resetToZero = true)
		{
			projectile.frameCounter++;

			if (projectile.frameCounter >= frameSpeed)
			{
				projectile.frameCounter = 0;
				projectile.frame++;

				if (projectile.frame >= maxFrames)
				{
					projectile.frame = resetToZero ? 0 : maxFrames - 1;
				}
			}
		}
		#endregion

		#region Prediction Methods
		/// <summary>
		/// 预测目标位置
		/// </summary>
		/// <param name="projectile">发射物</param>
		/// <param name="target">目标</param>
		/// <param name="bulletSpeed">子弹速度</param>
		/// <param name="accelerationFactor">加速度因子</param>
		/// <param name="maxPredictionTicks">最大预测帧数</param>
		/// <param name="tickStep">预测步长</param>
		/// <returns>预测位置</returns>
		public static Vector2 PredictTargetPosition(Projectile projectile, NPC target, float bulletSpeed, int maxPredictionTicks = 60, int tickStep = 3)
		{
			return PredictTargetPosition(projectile.Center, target.Center, target.velocity, bulletSpeed, maxPredictionTicks, tickStep);
		}

		public static Vector2 PredictTargetPosition(Vector2 projectileCenter, Vector2 targetCenter, Vector2 targetVelocity, float bulletSpeed, int maxPredictionTicks = 60, int tickStep = 3)
		{
			Vector2 predictedPos = targetCenter;
			
			for (int tick = 0; tick < maxPredictionTicks; tick += tickStep)
			{
				Vector2 targetPredictedPos = targetCenter + targetVelocity * tick;
				predictedPos = targetPredictedPos;

				if(Collision.SolidCollision(targetPredictedPos, 1, 1)) continue;

				float bulletFlyTime = Vector2.Distance(projectileCenter, targetPredictedPos) / bulletSpeed;

				if (bulletFlyTime < tick)
				{
					break;
				}
			}

			return predictedPos;
		}

		public static Vector2 PredictVelocityWithGravity(Vector2 projectileCenter, Vector2 targetCenter, Vector2 targetVelocity, float bulletSpeedY, float gravity = 0.4f, int maxPredictionTicks = 60, int tickStep = 3)
		{
			Vector2 predictedVel = new Vector2(0, -bulletSpeedY);
			for (int tick = 0; tick < maxPredictionTicks; tick += tickStep)
			{
				Vector2 targetPredictedPos = targetCenter + targetVelocity * tick;

				Vector2 direction = targetPredictedPos - projectileCenter;

				if(Collision.SolidCollision(targetPredictedPos, 1, 1)) continue;

				float vy = bulletSpeedY;
				float bullet_gravity = gravity;
				float delta = Math.Max(0, vy * vy + 2 * bullet_gravity * direction.Y);
				float pred_t1 = (vy + (float)Math.Sqrt(delta)) / bullet_gravity;
				float pred_t2 = (vy - (float)Math.Sqrt(delta)) / bullet_gravity;
				float bulletFlyTime = Math.Max(pred_t1, pred_t2);

				// Main.NewText("Tick:" + tick.ToString() + " bulletFlyTime:" + bulletFlyTime.ToString());

				float vx = direction.X / bulletFlyTime;
				predictedVel = new Vector2(vx, -vy);

				if (bulletFlyTime < tick)
				{
					break;
				}
			}

			return predictedVel;
		}

		public static float PredictParabolaAngle(Projectile projectile, NPC target,float gravity, float bulletSpeed, int maxPredictionTicks = 60, int tickStep = 3)
		{
			float predictedAngle = (target.Center - projectile.Center).ToRotation();

			for(int tick = 0; tick < maxPredictionTicks; tick += tickStep)
			{
				Vector2 targetPredictedPos = target.Center + target.velocity * tick;

				ParabolaSolution bulletSolution = SolveParabola(bulletSpeed, gravity, targetPredictedPos);

				if(bulletSolution.valid)
				{
					predictedAngle = bulletSolution.angle;
					if(bulletSolution.time < tick)
					{
						Main.NewText("Find solution:" + bulletSolution.time + " " + bulletSolution.angle);
						break;
					}
				}
			}

			return predictedAngle;
		}

		public struct ParabolaSolution
		{
			public float time;
			public float angle;
			public bool valid;

			public ParabolaSolution(float time, float angle, bool valid)
			{
				this.time = time;
				this.angle = angle;
				this.valid = valid;
			}
		}

		public static ParabolaSolution SolveParabola(float speed, float gravity, Vector2 goal)
		{
			float v = speed;
			float g = gravity;
			float x = goal.X;
			float y = goal.Y;

			float A = (g*g) / 4f;
			float B = y * g - v * v;
			float C = x * x + y * y;
			float D = B * B - 4 * A * C;
			if(D < 0)
			{
				return new ParabolaSolution(-1f, -1f, false);
			}
			
			float sqrtD = (float)Math.Sqrt(D);
			float[] t2Candidates = new float[]
			{
				(-B + sqrtD) / (2 * A),
				(-B - sqrtD) / (2 * A)
			};
			float minTime = float.MaxValue;
			float bestAngle = -1;

			foreach(float t2 in t2Candidates)
			{
				if(t2 > 0)
				{
					float t = (float)Math.Sqrt(t2);
					float cosTheta = x / (t * v);
					float sinTheta = (y + g * t2 / 2) / (t * v);
					float angle = (float)Math.Atan2(sinTheta, cosTheta);
					if(cosTheta >= 0f && cosTheta <= 1f && (float)Math.Abs(sinTheta) <= 1f)
					{
						if(t < minTime)
						{
							minTime = t;
							bestAngle = angle;
						}
					}
				}
			}

			if(minTime == float.MaxValue)
			{
				return new ParabolaSolution(-1f, -1f, false);
			}

			return new ParabolaSolution(minTime, bestAngle, true);
		}

		/// <summary>
		/// 预测目标位置（带加速度计算）
		/// </summary>
		/// <param name="projectile">发射物</param>
		/// <param name="target">目标</param>
		/// <param name="bulletSpeed">子弹速度</param>
		/// <param name="lastVelocity">上一帧速度</param>
		/// <param name="accelerationFactor">加速度因子</param>
		/// <param name="maxPredictionTicks">最大预测帧数</param>
		/// <param name="tickStep">预测步长</param>
		/// <returns>预测位置和当前速度</returns>
		public static (Vector2 PredictedPosition, Vector2 CurrentVelocity) PredictTargetPositionWithAcceleration(
			Projectile projectile, NPC target, float bulletSpeed, Vector2 lastVelocity, 
			float accelerationFactor = 0.05f, int maxPredictionTicks = 60, int tickStep = 3)
		{
			Vector2 predictedPos = target.Center;
			Vector2 acceleration = (target.velocity - lastVelocity) * accelerationFactor;
			Vector2 currentVelocity = target.velocity;

			for (int tick = 0; tick < maxPredictionTicks; tick += tickStep)
			{
				Vector2 targetPredictedPos = target.Center + target.velocity * tick + 
					0.5f * acceleration * tick * tick;
				predictedPos = targetPredictedPos;

				float bulletFlyTime = Vector2.Distance(projectile.Center, targetPredictedPos) / bulletSpeed;

				if (bulletFlyTime < tick)
				{
					break;
				}
			}

			return (predictedPos, currentVelocity);
		}
		#endregion

		#region Utility Methods

		/// <summary>
		/// 计算两点之间的距离
		/// </summary>
		/// <param name="point1">点1</param>
		/// <param name="point2">点2</param>
		/// <returns>距离</returns>
		public static float GetDistance(Vector2 point1, Vector2 point2)
		{
			return Vector2.Distance(point1, point2);
		}

		/// <summary>
		/// 计算水平距离
		/// </summary>
		/// <param name="point1">点1</param>
		/// <param name="point2">点2</param>
		/// <returns>水平距离</returns>
		public static float GetHorizontalDistance(Vector2 point1, Vector2 point2)
		{
			return Math.Abs(point1.X - point2.X);
		}


		public static float NormalizeAngle(float angle)
		{
			return (angle + ModGlobal.PI_FLOAT) % ModGlobal.TWO_PI_FLOAT - ModGlobal.PI_FLOAT;
		}
		

		public static class PolarCurveFitter
		{
			public struct Polar {
				public double r, theta;
				public Polar(double r, double theta) { this.r = r; this.theta = theta; }
			}

			public struct Vec2 {
				public double x, y;
				public Vec2(double x, double y) { this.x = x; this.y = y; }
				public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
				public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
				public static Vec2 operator *(Vec2 a, double f) => new Vec2(a.x * f, a.y * f);
			}

			// Catmull-Rom Spline (uniform)
			static Vec2 CatmullRom(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, double t)
			{
				double t2 = t * t;
				double t3 = t2 * t;

				double x =
					0.5 * ((2 * p1.x) +
					(-p0.x + p2.x) * t +
					(2*p0.x - 5*p1.x + 4*p2.x - p3.x) * t2 +
					(-p0.x + 3*p1.x - 3*p2.x + p3.x) * t3);

				double y =
					0.5 * ((2 * p1.y) +
					(-p0.y + p2.y) * t +
					(2*p0.y - 5*p1.y + 4*p2.y - p3.y) * t2 +
					(-p0.y + 3*p1.y - 3*p2.y + p3.y) * t3);

				return new Vec2(x, y);
			}

			public static List<Polar> FitAndInsert(List<Polar> pts, int nInsert)
			{
				// Step 1: Convert to Cartesian
				List<Vec2> xy = pts.Select(p =>
					new Vec2(p.r * Math.Cos(p.theta), p.r * Math.Sin(p.theta))
				).ToList();

				int N = xy.Count;
				List<Polar> result = new List<Polar>();

				for (int i = 0; i < N - 1; i++)
				{
					// control points
					Vec2 p0 = xy[Math.Max(0, i - 1)];
					Vec2 p1 = xy[i];
					Vec2 p2 = xy[i + 1];
					Vec2 p3 = xy[Math.Min(N - 1, i + 2)];

					// add original point
					result.Add(ToPolar(p1));

					// insert n points between p1 and p2
					for (int k = 1; k <= nInsert; k++)
					{
						double t = (double)k / (nInsert + 1);
						Vec2 c = CatmullRom(p0, p1, p2, p3, t);
						result.Add(ToPolar(c));
					}
				}

				// add last point
				// result.Add(ToPolar(xy.Last()));

				return result;
			}

			static Polar ToPolar(Vec2 v)
			{
				double r = Math.Sqrt(v.x * v.x + v.y * v.y);
				double th = Math.Atan2(v.y, v.x);
				if (th < 0) th += 2 * Math.PI;
				return new Polar(r, th);
			}
		}

		public static float TaianglePeekFunc(float h, float w, float m, float v)
		{
			if(v > m - w/2 && v <= m)
				return (v-m+w/2) / (w/2) * h;
			else if(v > m && v < m + w/2)
				return (m+w/2-v) / (w/2) * h;
			else
				return 0;
		}
		#endregion

		#region Texture Methods
		/// <summary>
		/// 绘制纹理
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="texture">纹理</param>
		/// <param name="worldPos">世界坐标（屏幕坐标，屏幕左上角为原点），由本地坐标使用ConvertToWorldPos转换得到</param>
		/// <param name="rect">矩形（贴图坐标，贴图左上角为原点）</param>
		/// <param name="color">颜色</param>
		/// <param name="origin">原点（碰撞箱坐标，碰撞箱中心为原点）</param>
		/// <param name="rotation">旋转角度</param>
		public static void DrawPart(Projectile projectile, Texture2D texture, Vector2 worldPos, Rectangle rect, Color color,float rotation, Vector2 origin)
        {
            Main.spriteBatch.Draw(
                texture,
                worldPos,
                rect,
                color,
                rotation,
                origin,
                projectile.scale,
                projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                0f
            );
        }

		/// <summary>
		/// 转换为世界坐标
		/// </summary>
		/// <param name="projectile">召唤物</param>
		/// <param name="localPos">本地坐标（碰撞箱坐标，碰撞箱左上角为原点）</param>
		/// <returns>世界坐标（屏幕坐标，屏幕左上角为原点）</returns>
		public static Vector2 ConvertToWorldPos(Projectile projectile, Vector2 localPos)
        {
            return projectile.Center + localPos.RotatedBy(projectile.rotation) - Main.screenPosition;
        }
		public static Vector2 ConvertToWorldPos(Vector2 center, float rotation, Vector2 localPos)
		{
			return center + localPos.RotatedBy(rotation) - Main.screenPosition;
		}

		public static Vector2 CenterMapping(Vector2 center, Vector2 offset, float rotation)
		{
			return center + offset.RotatedBy(rotation);
		}

		/// <summary>
		/// 计算裁剪矩形
		/// </summary>
		/// <param name="rect">矩形</param>
		/// <param name="pos">位置</param>
		/// <param name="origin">原点</param>
		/// <param name="threshold_y">阈值，映射后的Y坐标低于这个阈值的矩形部分需要裁剪</param>
		public static Rectangle CalculateClipRect(Rectangle rect, Vector2 pos, Vector2 origin,float threshold_x = -1f, float threshold_y = -1f)
		{
			// float Vertex_Y1 = pos.Y - origin.Y;
			// float Vertex_Y2 = pos.Y + rect.Height - origin.Y;
			Vector2 Vertex_1 = pos - origin;
			Vector2 Vertex_2 = pos - origin + new Vector2(rect.Width, rect.Height);
			int clip_width = rect.Width;
			int clip_height = rect.Height;
			if(threshold_x > -1f && Vertex_2.X > threshold_x)
			{
				clip_width = (int)(threshold_x - Vertex_1.X);
				if(clip_width > rect.Width)
				{
					clip_width = rect.Width;
				}
				else if (clip_width < 0)
				{
					clip_width = 0;
				}
			}
			if(threshold_y > -1f && Vertex_2.Y > threshold_y)
			{
				clip_height = (int)(threshold_y - Vertex_1.Y);
				if(clip_height > rect.Height)
				{
					clip_height = rect.Height;
				}
				else if (clip_height < 0)
				{
					clip_height = 0;
				}
			}
			Rectangle clip_rect = new Rectangle(rect.X, rect.Y, clip_width, clip_height);
			return clip_rect;
		}
		#endregion

		#region Random Methods
		public static float RandomFloat(float min, float max)
		{
			return (float)Main.rand.NextDouble() * (max - min) + min;
		}
		public static float RandomFloat(double min, double max)
		{
			return (float)(Main.rand.NextDouble() * (max - min) + min);
		}
		public static float RandomFloat(float min, double max)
		{
			return (float)Main.rand.NextDouble() * ((float)max - min) + min;
		}
		public static float RandomFloat(double min, float max)
		{
			return (float)Main.rand.NextDouble() * (max - (float)min) + (float)min;
		}
		public static bool RandomBool()
		{
			return Main.rand.Next(2) == 1;
		}
		public static float RandomSign()
		{
			return Main.rand.Next(2) == 1 ? 1 : -1;
		}
		public static int RandomInt(int min, int max)
		{
			return Main.rand.Next(min, max);
		}
		#endregion

		#region Dust Methods
		public static void GenerateLightning(Vector2 start, Vector2 end, float displacement, float minDisplacement, List<Vector2> points)
		{
			if (displacement < minDisplacement)
			{
				points.Add(start);
				points.Add(end);
			}
			else
			{
				Vector2 mid = (start + end) / 2;
				// 垂直方向
				Vector2 dir = new Vector2(end.Y - start.Y, start.X - end.X);
				dir.Normalize();
				// 随机偏移
				mid += dir * (RandomFloat(-displacement, displacement));

				// 递归生成
				GenerateLightning(start, mid, displacement / 2, minDisplacement, points);
				GenerateLightning(mid, end, displacement / 2, minDisplacement, points);
			}
		}
		#endregion

		#region Homein Methods
		public static void HomeinToTarget(Projectile projectile, Vector2 target, float speed, float inertia)
		{
			Vector2 direction = target - projectile.Center;
			direction.Normalize();
			direction *= speed;
			projectile.velocity = (projectile.velocity * (inertia - 1) + direction) / inertia;

			// Main.NewText("Homein velocity: " + projectile.velocity + " direction: " + direction + " speed: " + speed + " inertia: " + inertia);
		}
		#endregion

		#region Trail Drawing Methods
		/// <summary>
		/// 绘制顶点拖尾效果
		/// </summary>
		/// <param name="projectile">弹幕对象</param>
		/// <param name="trailTexturePath">拖尾纹理路径</param>
		/// <param name="startColor">起始颜色（拖尾头部）</param>
		/// <param name="endColor">结束颜色（拖尾尾部）</param>
		/// <param name="startWidth">起始宽度（拖尾头部）</param>
		/// <param name="endWidth">结束宽度（拖尾尾部）</param>
		/// <param name="blendState">混合模式，默认为加法混合</param>
		public static void DrawVertexTrail(
			Projectile projectile, 
			string trailTexturePath, 
			Color startColor, 
			Color endColor, 
			float startWidth, 
			float endWidth,
			BlendState blendState = null)
		{
			if (blendState == null)
				blendState = BlendState.Additive;

			SpriteBatch sb = Main.spriteBatch;
			GraphicsDevice gd = Main.graphics.GraphicsDevice;
			
			// 结束当前批次，开始顶点绘制批次
			sb.End();
			sb.Begin(SpriteSortMode.Immediate, blendState, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			
			List<Vertex> vertices = new List<Vertex>();
			int trailLength = ProjectileID.Sets.TrailCacheLength[projectile.type];
			
			// 遍历所有缓存的旧位置，创建拖尾效果
			for (int i = 0; i < trailLength; i++)
			{
				// 如果旧位置为零向量，说明还没有足够的历史记录，跳过
				if (projectile.oldPos[i] == Vector2.Zero)
					break;
				
				float ratio = i / (float)trailLength;
				// 颜色从起始颜色渐变到结束颜色
				Color trailColor = Color.Lerp(startColor, endColor, ratio);
				
				// 计算拖尾的宽度，从粗到细
				float width = MathHelper.Lerp(startWidth, endWidth, ratio);
				
				// 获取当前位置的中心点
				Vector2 center = projectile.oldPos[i] + projectile.Size / 2f - Main.screenPosition;
				
				// 根据旧的旋转角度计算垂直方向
				float rotation = projectile.oldRot[i];
				Vector2 perpendicular = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
				
				// 创建拖尾带的两个边缘顶点
				vertices.Add(new Vertex(
					center + perpendicular * width,
					new Vector3(ratio, 0, 1),
					trailColor
				));
				
				vertices.Add(new Vertex(
					center - perpendicular * width,
					new Vector3(ratio, 1, 1),
					trailColor
				));
			}
			
			// 只有至少3个顶点才能绘制三角形带
			if (vertices.Count >= 3)
			{
				Texture2D trailTexture = ModContent.Request<Texture2D>(trailTexturePath).Value;
				gd.Textures[0] = trailTexture;
				gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);
			}
			
			// 结束顶点绘制，恢复默认的SpriteBatch设置
			sb.End();
			sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
		}
		#endregion

		#region Damage Methods
		public static int AccumulateWhipDamage(NPC target)
		{
			int addDamage = 0;
			foreach (var item in ModGlobal.WhipAddDamageDict)
			{
				if (target.HasBuff(item.Key))
				{
					addDamage += item.Value;
				}
			}
			return addDamage;
		}
		#endregion

		#region Defense Methods
		public static int DefenseCompensate(int def_orig, int def_max, float max_rate=0.5f, float k=20, float B=20)
		{
			if(def_orig >= def_max)
			{
				return 0;
			}
			else
			{
				int bonus = (int)(B * (1 - (float)Math.Exp(-(def_max - def_orig) / k)));
				if(max_rate >= 0 && bonus > def_orig * max_rate)
				{
					bonus = (int)(def_orig * max_rate);
				}
				else if(bonus < 0)
				{
					bonus = 0;
				}
				return bonus;
			}
		}
		#endregion

		#region Imbue Methods
		public static Dictionary<byte, int> ImbueDeBuffDict = new()
		{
			{0, -1},
			{1, BuffID.Venom},
			{2, BuffID.CursedInferno},
			{3, BuffID.OnFire},
			{4, BuffID.Midas},
			{5, BuffID.Ichor},
			{6, BuffID.Confused},
			{7, -1},
			{8, BuffID.Poisoned}
		};

		public static Dictionary<byte, int> ImbueDustDict = new()
		{
			{0, -1},
			{1, 171},
			{2, 74},
			{3, DustID.FlameBurst},
			{4, DustID.GoldFlame},
			{5, 169},
			{6, DustID.IceTorch},
			{7, -1},
			{8, BuffID.Poisoned}
		};

		public static int GetImbueDebuff(Player player)
		{
			return ImbueDeBuffDict[player.meleeEnchant];
		}

		public static int GetImbueDust(Player player)
		{
			return ImbueDustDict[player.meleeEnchant];
		}
		
		public static bool IsPartyImbue(Player player)
        {
			return player.meleeEnchant == 7;
        }

		#endregion

		#region SelfHarm Methods
		/// <summary>
		/// 对玩家自身造成伤害（召唤物误伤）。返回是否成功造成伤害（canHarm）。
		/// </summary>
		public static bool DoHarmToSelf(Player player, Projectile projectile, int damage, float knockback, int armorPenetration=9999, bool requireIntersection=true)
		{
			// get accessory level
			float reduce_level = 0;
			if(player.GetModPlayer<HD2SentryDmgReductionPlayer>().hasLv1Accessory) reduce_level = 1f;
			if(player.GetModPlayer<HD2SentryDmgReductionPlayer>().hasLv2Accessory) reduce_level = 2f;

			// calculate difficulty factor
			float DifficultyFactor = 1f;
			if(Main.expertMode && !Main.masterMode)
            {
                DifficultyFactor = 0.5f;
            }
            else if (Main.masterMode)
            {
                DifficultyFactor = 1f/3f;
            }

			// calculate reduce factor
			float ReduceFactor = 1f;
			if(reduce_level == 1f) ReduceFactor = 0.5f;
			else if (reduce_level == 2f) ReduceFactor = 0.2f;

			bool canHarm = requireIntersection ? (projectile.Hitbox.Intersects(player.Hitbox) && !player.immune) : !player.immune;
			if(canHarm)
			{
				int hitDir = projectile.Center.X > player.Center.X ? 1 : -1;
				player.Hurt(
					PlayerDeathReason.ByProjectile(
						player.whoAmI,
						projectile.whoAmI),
						(int) (damage * DifficultyFactor * ReduceFactor),
						hitDir,
						knockback: reduce_level == 0f ? knockback : 0f,
						armorPenetration: armorPenetration
				);

				// apply additional ef
				if(ReduceFactor == 2f) player.immuneTime += 30;
				return true;
			}
			return false;
		}
		#endregion

		#region Server Methods
		public static void SetProjectileNetUpdate(Projectile projectile)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				projectile.netUpdate = true;
		}

		public static bool IsServer()
		{
			return Main.netMode != NetmodeID.MultiplayerClient;
		}
		#endregion
	}
} 
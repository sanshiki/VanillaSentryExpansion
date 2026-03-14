// using Microsoft.Xna.Framework;
// using System;
// using System.Collections.Generic;
// using Terraria;
// using Terraria.DataStructures;
// using Terraria.ID;
// using Terraria.ModLoader;

// using SummonerExpansionMod.Content.Buffs.Summon;
// using SummonerExpansionMod.ModUtils;

// namespace SummonerExpansionMod.Content.Projectiles.Summon
// {
// 	/// <summary>
// 	/// 重制的史莱姆宝宝召唤物，具有更智能的AI行为
// 	/// </summary>
// 	public class BabySlimeOverride : ModProjectile
// 	{
// 		#region Constants
// 		// 动画相关常量
// 		private const int TOTAL_FRAMES = 6;
		
// 		// 移动相关常量
// 		private const float BASE_MOVE_SPEED = 6f;
// 		private const float FLYING_SPEED = 10f;
// 		private const float JUMP_SPEED = 8f;
// 		private const float BASE_INERTIA = 20f;
// 		private const float GRAVITY = 0.4f;
// 		private const float MAX_FALL_SPEED = 10f;
// 		private const int STUCK_TIME_THRESHOLD = 10;
		
// 		// 距离相关常量
// 		private const float FLYING_TRIGGER_DISTANCE = 360f;
// 		private const float FLYING_TRIGGER_DISTANCE_WHEN_HAS_TARGET = 1700f;
// 		private const float FLYING_RETURN_DISTANCE = 200f;
// 		private const float STABLE_TRIGGER_DISTANCE = 0.5f;
// 		private const float STABLE_SPEED_THRESHOLD = 2f;
// 		private const float MOVEMENT_TRIGGER_DISTANCE = 1f;
		
// 		// 调试相关常量
// 		private const bool DEBUG_MODE = true; // 调试开关
// 		private const bool DEBUG_VERBOSE = true; // 详细调试信息开关
// 		private const int DEBUG_UPDATE_INTERVAL = 60; // 调试信息更新间隔（帧数）
// 		#endregion

// 		#region Fields
// 		private bool _isOnGround = false;
// 		private float _lastHorizontalDistance = 0f;
// 		private StateMachine<BabySlimeState, BabySlimeContext> _stateMachine;
// 		private BabySlimeAnimationManager _animationManager;
// 		private int _debugCounter = 0;
// 		private BabySlimeState _lastDebugState = BabySlimeState.Moving;
// 		#endregion

// 		#region Override Methods
// 		public override void SetStaticDefaults()
// 		{
// 			// 设置精灵表帧数
// 			Main.projFrames[Projectile.type] = TOTAL_FRAMES;
			
// 			// 启用右键目标选择功能
// 			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
			
// 			// 标记为宠物/召唤物
// 			Main.projPet[Projectile.type] = true;
			
// 			// 允许召唤物被牺牲（当召唤其他召唤物时）
// 			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
			
// 			// 使邪教徒对此召唤物有抗性
// 			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
// 		}

// 		public sealed override void SetDefaults()
// 		{
// 			// 克隆原版史莱姆宝宝的默认设置
// 			Projectile.CloneDefaults(ProjectileID.BabySlime);

// 			Projectile.width = 24; // 35
// 			Projectile.height = 26;
			
// 			// 使用自定义AI
// 			Projectile.aiStyle = -1;
// 		}

// 		public override bool? CanCutTiles()
// 		{
// 			return false;
// 		}

// 		public override bool MinionContactDamage()
// 		{
// 			return true;
// 		}

// 		public override bool OnTileCollide(Vector2 oldVelocity)
// 		{
// 			_isOnGround = true;
// 			return true;
// 		}

// 		private bool CheckActive(Player owner, int buffType)
// 		{
// 			if (owner.dead || !owner.active)
// 			{
// 				owner.ClearBuff(buffType);

// 				return false;
// 			}

// 			if (owner.HasBuff(buffType))
// 			{
// 				Projectile.timeLeft = 2;
// 			}

// 			return true;
// 		}

// 		public override void AI()
// 		{
// 			// 延迟初始化状态机和动画管理器
// 			if (_stateMachine == null)
// 			{
// 				_stateMachine = new BabySlimeStateMachine();
// 			}
// 			if (_animationManager == null)
// 			{
// 				_animationManager = new BabySlimeAnimationManager();
// 			}

// 			Player owner = Main.player[Projectile.owner];

// 			if (!CheckActive(owner, BuffID.BabySlime))
// 			// if (!CheckActive(owner, ModContent.BuffType<BabySlimeOverrideBuff>()))
// 			{
// 				return;
// 			}

// 			// 执行AI逻辑
// 			Vector2 idlePosition = MinionAIHelper.CalculateIdlePosition(owner, Projectile);
// 			MinionAIHelper.HandleTeleportation(owner, Projectile, idlePosition);
// 			MinionAIHelper.HandleMinionOverlap(Projectile);
			
// 			Vector2 vectorToIdle = idlePosition - Projectile.Center;
// 			float distanceToIdle = vectorToIdle.Length();
			
// 			var targetResult = MinionAIHelper.SearchForTargets(owner, Projectile);
// 			Vector2 movementDirection = targetResult.FoundTarget ? targetResult.TargetCenter - Projectile.Center : vectorToIdle;
			
// 			// 更新状态机上下文
// 			var context = new BabySlimeContext
// 			{
// 				Owner = owner,
// 				Projectile = Projectile,
// 				MovementDirection = movementDirection,
// 				DistanceToIdle = distanceToIdle,
// 				HorizontalDistance = Math.Abs(movementDirection.X),
// 				IsOnGround = _isOnGround,
// 				LastHorizontalDistance = _lastHorizontalDistance,
// 				HasTarget = targetResult.FoundTarget,
// 				TargetCenter = targetResult.TargetCenter,
// 			};

// 			// 更新状态机
// 			_stateMachine.Update(context);
			
// 			// 同步状态机对IsOnGround的修改
// 			_isOnGround = context.IsOnGround;

			
// 			// 更新动画
// 			_animationManager.SetStateFromBabySlimeState(_stateMachine.CurrentState);
// 			_animationManager.UpdateAnimation(Projectile);
			
// 			// 更新视觉效果
// 			UpdateVisuals();
			
// 			// 更新友好状态
// 			MinionAIHelper.UpdateFriendlyState(Projectile, targetResult.FoundTarget);
			
// 			// 保存状态
// 			_lastHorizontalDistance = context.HorizontalDistance;

// 			// 调试信息
// 			if (DEBUG_MODE && _debugCounter % DEBUG_UPDATE_INTERVAL == 0)
// 			{
// 				BabySlimeState currentState = _stateMachine.CurrentState;
// 				if (currentState != _lastDebugState)
// 				{
// 					Main.NewText($"State changed: {_lastDebugState} -> {currentState}", Color.White);
// 					_lastDebugState = currentState;
// 				}
				
// 				// 显示详细调试信息
// 				if (DEBUG_VERBOSE)
// 				{
// 					Main.NewText($"Current State: {currentState}", Color.White);
// 					Main.NewText($"Distance to idle: {distanceToIdle:F1}, Horizontal distance: {context.HorizontalDistance:F1}", Color.Gray);
// 					Main.NewText($"Has target: {targetResult.FoundTarget}, On ground: {_isOnGround}", Color.Gray);
// 					Main.NewText($"Velocity: ({Projectile.velocity.X:F1}, {Projectile.velocity.Y:F1})", Color.Gray);
// 				}
				
// 				_debugCounter = 0;
// 			}
// 			_debugCounter++;
// 		}
// 		#endregion

// 		#region Visual Methods
// 		/// <summary>
// 		/// 更新视觉效果
// 		/// </summary>
// 		private void UpdateVisuals()
// 		{
// 			// 更新精灵方向
// 			MinionAIHelper.UpdateSpriteDirection(Projectile);
			
// 			// 更新光照效果
// 			Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
// 		}
// 		#endregion

// 		#region Internal Classes

// 		#region BabySlimeContext
// 		/// <summary>
// 		/// 史莱姆宝宝状态机上下文
// 		/// </summary>
// 		public class BabySlimeContext
// 		{
// 			public Player Owner { get; set; }
// 			public Projectile Projectile { get; set; }
// 			public Vector2 MovementDirection { get; set; }
// 			public float DistanceToIdle { get; set; }
// 			public float HorizontalDistance { get; set; }
// 			public bool IsOnGround { get; set; }
// 			public float LastHorizontalDistance { get; set; }
// 			public bool HasTarget { get; set; }
// 			public Vector2 TargetCenter { get; set; }
// 		}
// 		#endregion

// 		#region BabySlimeState
// 		/// <summary>
// 		/// 史莱姆宝宝状态枚举
// 		/// </summary>
// 		public enum BabySlimeState
// 		{
// 			Stable,   // 稳定状态：静止不动
// 			Moving,   // 移动状态：在地面上移动
// 			Flying    // 飞行状态：在空中飞行
// 		}
// 		#endregion

// 		#region BabySlimeStateMachine
// 		/// <summary>
// 		/// 史莱姆宝宝状态机
// 		/// </summary>
// 		public class BabySlimeStateMachine : StateMachine<BabySlimeState, BabySlimeContext>
// 		{
// 			private int _stuckTime = 0;

// 			#region Constructor
// 			public BabySlimeStateMachine() : base(BabySlimeState.Moving)
// 			{
// 				InitializeStates();
// 				InitializeTransitions();
// 			}
// 			#endregion

// 			#region Initialization
// 			/// <summary>
// 			/// 初始化状态
// 			/// </summary>
// 			private void InitializeStates()
// 			{
// 				AddState(BabySlimeState.Stable, 
// 					onEnter: OnEnterStable,
// 					onUpdate: OnUpdateStable,
// 					onExit: OnExitStable);

// 				AddState(BabySlimeState.Moving,
// 					onEnter: OnEnterMoving,
// 					onUpdate: OnUpdateMoving,
// 					onExit: OnExitMoving);

// 				AddState(BabySlimeState.Flying,
// 					onEnter: OnEnterFlying,
// 					onUpdate: OnUpdateFlying,
// 					onExit: OnExitFlying);
// 			}

// 			/// <summary>
// 			/// 初始化状态转换
// 			/// </summary>
// 			private void InitializeTransitions()
// 			{
// 				// 稳定 -> 移动：当水平距离变化超过阈值时
// 				AddTransition(BabySlimeState.Stable, BabySlimeState.Moving, 
// 					context => 
// 					{
// 						bool shouldTransition = Math.Abs(context.HorizontalDistance - context.LastHorizontalDistance) > MOVEMENT_TRIGGER_DISTANCE;
// 						if (DEBUG_MODE && shouldTransition)
// 						{
// 							Main.NewText($"Transition: Stable -> Moving (Distance change: {Math.Abs(context.HorizontalDistance - context.LastHorizontalDistance):F2} > {MOVEMENT_TRIGGER_DISTANCE})", Color.Yellow);
// 						}
// 						return shouldTransition;
// 					});

// 				// 移动 -> 飞行：当距离玩家过远时;如果当前有攻击目标，则该距离适当增加
// 				AddTransition(BabySlimeState.Moving, BabySlimeState.Flying,
// 					context => 
// 					{
// 						bool shouldTransitionNormal = context.DistanceToIdle > FLYING_TRIGGER_DISTANCE && !context.HasTarget;
// 						bool shouldTransitionHasTarget = context.DistanceToIdle > FLYING_TRIGGER_DISTANCE_WHEN_HAS_TARGET && context.HasTarget;
// 						bool shouldTransitionStuck = _stuckTime > STUCK_TIME_THRESHOLD;
// 						bool shouldTransition = shouldTransitionNormal || shouldTransitionHasTarget || shouldTransitionStuck;
// 						if (DEBUG_MODE && shouldTransition)
// 						{
// 							Main.NewText($"Transition: Moving -> Flying (Distance to idle: {context.DistanceToIdle:F2} > {FLYING_TRIGGER_DISTANCE})", Color.Orange);
// 						}
// 						return shouldTransition;
// 					});

// 				// 移动 -> 稳定：当速度很低且距离很近时
// 				AddTransition(BabySlimeState.Moving, BabySlimeState.Stable,
// 					context => 
// 					{
// 						bool speedLow = Math.Abs(context.Projectile.velocity.X) <= STABLE_SPEED_THRESHOLD;
// 						bool distanceClose = context.HorizontalDistance < STABLE_TRIGGER_DISTANCE;
// 						bool shouldTransition = speedLow && distanceClose;
// 						if (DEBUG_MODE && shouldTransition)
// 						{
// 							Main.NewText($"Transition: Moving -> Stable (Speed: {Math.Abs(context.Projectile.velocity.X):F2} <= {STABLE_SPEED_THRESHOLD}, Distance: {context.HorizontalDistance:F2} < {STABLE_TRIGGER_DISTANCE})", Color.Green);
// 						}
// 						return shouldTransition;
// 					});

// 				// 飞行 -> 移动：当距离玩家较近且不卡在固体中时
// 				AddTransition(BabySlimeState.Flying, BabySlimeState.Moving,
// 					context => 
// 					{
// 						bool distanceClose = context.DistanceToIdle < FLYING_RETURN_DISTANCE;
// 						bool notStuck = !MinionAIHelper.IsStuckInSolid(context.Projectile);
// 						bool shouldTransition = distanceClose && notStuck;
// 						if (DEBUG_MODE && shouldTransition)
// 						{
// 							Main.NewText($"Transition: Flying -> Moving (Distance to idle: {context.DistanceToIdle:F2} < {FLYING_RETURN_DISTANCE}, Not stuck: {notStuck})", Color.Cyan);
// 						}
// 						return shouldTransition;
// 					});
// 			}
// 			#endregion

// 			#region State Handlers - Stable
// 			private void OnEnterStable(BabySlimeContext context)
// 			{
// 				if (DEBUG_MODE && DEBUG_VERBOSE)
// 				{
// 					Main.NewText("Entering Stable state", Color.LightGreen);
// 				}
// 			}

// 			private void OnUpdateStable(BabySlimeContext context)
// 			{
// 				// 停止水平移动
// 				context.Projectile.velocity.X = 0f;
				
// 				// 应用重力
// 				MinionAIHelper.ApplyGravity(context.Projectile, GRAVITY, MAX_FALL_SPEED);
// 			}

// 			private void OnExitStable(BabySlimeContext context)
// 			{
// 				if (DEBUG_MODE && DEBUG_VERBOSE)
// 				{
// 					Main.NewText("Exiting Stable state", Color.LightGreen);
// 				}
// 			}
// 			#endregion

// 			#region State Handlers - Moving
// 			private void OnEnterMoving(BabySlimeContext context)
// 			{
// 				if (DEBUG_MODE && DEBUG_VERBOSE)
// 				{
// 					Main.NewText("Entering Moving state", Color.LightBlue);
// 				}
// 				context.Projectile.tileCollide = true;
// 			}

// 			private void OnUpdateMoving(BabySlimeContext context)
// 			{
// 				Vector2 direction = context.MovementDirection;
// 				float dir_sign = Math.Sign(direction.X);

// 				// 根据距离调整移动参数
// 				float moveSpeed = BASE_MOVE_SPEED;
// 				float inertia = BASE_INERTIA;

// 				if (context.HorizontalDistance > 20f)
// 				{
// 					inertia = BASE_INERTIA;
// 				}
// 				else if (context.HorizontalDistance <= 80f && context.HorizontalDistance > 50f)
// 				{
// 					moveSpeed = 4.0f;
// 					inertia = 30f;
// 				}
// 				else if (context.HorizontalDistance <= 50f)
// 				{
// 					float control_speed = context.HorizontalDistance / 15f;
// 					moveSpeed = Math.Max(Math.Min(control_speed, 4.0f), 0.5f);
// 					inertia = 6f;
// 				}

// 				// 应用水平移动
// 				float targetSpeedX = moveSpeed * dir_sign;
// 				MinionAIHelper.ApplySmoothHorizontalMovement(context.Projectile, targetSpeedX, inertia);

// 				// 处理跳跃
// 				if (context.IsOnGround)
// 				{
// 					context.Projectile.velocity.Y = -JUMP_SPEED;
// 					context.IsOnGround = false;
// 					if (DEBUG_MODE && DEBUG_VERBOSE)
// 					{
// 						Main.NewText("Jump triggered", Color.Yellow);
// 					}
// 					_stuckTime++;
// 				}
// 				else
// 				{
// 					_stuckTime = 0;
// 				}

// 				// 应用重力
// 				MinionAIHelper.ApplyGravity(context.Projectile, GRAVITY, MAX_FALL_SPEED);
// 			}

// 			private void OnExitMoving(BabySlimeContext context)
// 			{
// 				if (DEBUG_MODE && DEBUG_VERBOSE)
// 				{
// 					Main.NewText("Exiting Moving state", Color.LightBlue);
// 				}
// 			}
// 			#endregion

// 			#region State Handlers - Flying
// 			private void OnEnterFlying(BabySlimeContext context)
// 			{
// 				if (DEBUG_MODE && DEBUG_VERBOSE)
// 				{
// 					Main.NewText("Entering Flying state", Color.LightPink);
// 				}
// 				context.Projectile.tileCollide = false;
// 			}

// 			private void OnUpdateFlying(BabySlimeContext context)
// 			{
// 				Vector2 direction = context.Owner.Center - context.Projectile.Center;
// 				direction.Normalize();
// 				direction *= FLYING_SPEED;
				
// 				MinionAIHelper.ApplySmoothMovement(context.Projectile, direction, BASE_INERTIA);
// 			}

// 			private void OnExitFlying(BabySlimeContext context)
// 			{
// 				if (DEBUG_MODE && DEBUG_VERBOSE)
// 				{
// 					Main.NewText("Exiting Flying state", Color.LightPink);
// 				}
// 				context.Projectile.tileCollide = true;
// 			}
// 			#endregion

// 			#region Abstract Implementation
// 			protected override BabySlimeContext GetContext()
// 			{
// 				// 这个方法不会被调用，因为我们使用Update(context)方法
// 				throw new NotImplementedException("This method should not be called when using Update(context)");
// 			}
// 			#endregion
// 		}
// 		#endregion

// 		#endregion
// 	}
// }

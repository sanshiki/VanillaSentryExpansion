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
using SummonerExpansionMod.Content.Items.Accessories;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    // sentry info storage
    public class SentryRecallInfo
    {
        public int ID;
        public bool TileCollide;
        public bool IsRecalled;
        public Vector2 TargetPos;
        public float Seed;
        public int Anchor_ID;
        public bool AnchorInited;

        public void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ID);
            writer.Write(TileCollide);
            writer.Write(IsRecalled);
            writer.Write(TargetPos.X);
            writer.Write(TargetPos.Y);
            writer.Write(Seed);
            writer.Write(Anchor_ID);
            writer.Write(AnchorInited);
        }

        public void ReceiveExtraAI(BinaryReader reader)
        {
            ID = reader.ReadInt32();
            TileCollide = reader.ReadBoolean();
            IsRecalled = reader.ReadBoolean();
            float TargetPosX = reader.ReadSingle();
            float TargetPosY = reader.ReadSingle();
            TargetPos = new Vector2(TargetPosX, TargetPosY);
            Seed = reader.ReadSingle();
            Anchor_ID = reader.ReadInt32();
            AnchorInited = reader.ReadBoolean();
        }
    }

    public class FlagProjectile : ModProjectile
    {
        /* ------------------------- Predraw Constants ------------------------- */
        // basic predraw
        protected const int BASE_HEIGHT = 9;  // height of the pole base
        protected const int MIN_POLE_PENGTH = 64; // minimum length of the pole
        protected const int TIP_HEIGHT = 30;  // height of the pole tip
        protected const int REPEAT_SLICE_HEIGHT = 2; // height of each repeat slice
        protected virtual string FLAGPOLE_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/FlagProjectile";
        public override string Texture => FLAGPOLE_TEXTURE_PATH;

        // waving
        protected virtual float ROT_ANGLE => 240f * ModGlobal.PI_FLOAT / 180f;   // waving range angle (150 degrees)
        protected virtual int TIME_LEFT_WAVE => 24; // waving duration
        protected virtual int HANDHELD_POINT_OFFSET => 60;   // offset of the point on the pole when held
        protected virtual bool TILE_CUT_RANGE_DEBUG => false;

        // raising
        protected float RAISE_MAX_HEIGHT => 16f * 4f + HANDHELD_POINT_OFFSET / 2f; // max height of the pole when raised
        // protected int TIME_LEFT_RAISE = 60; // raising duration
        // protected float RAISE_MAX_SPEED => 2f * RAISE_MAX_HEIGHT / (float)TIME_LEFT_RAISE;   // max speed of the pole when raised
        // protected float RAISE_ACC => RAISE_MAX_SPEED / (float)TIME_LEFT_RAISE * 2f; // acceleration of the pole when raised
        protected virtual int FULLY_CHARGED_DUST => DustID.CrimsonSpray;
        protected virtual float RAISE_BUFF_TIME_COEFF => 0.42f;

        // planting and recalling sentries
        protected virtual int PLANT_EXIST_DURATION => 60*60*10; // 10 min
        protected virtual int ONGROUND_CNT_THRESHOLD => 10;
        protected virtual int ONGROUND_PLAYER_RECALL_THRESHOLD => 45;
        protected virtual float GRAVITY => 0.8f;
        protected virtual float MAX_FALL_SPEED => 16f;
        protected virtual float SENTRY_RECALL_SPEED => 50f;
        protected virtual float SENTRY_RECALL_THRESHOLD => 40f;
        protected virtual float SENTRY_RECALL_DECAY_DIST => 1000f;
        protected virtual float SENTRY_RECALL_MAX_DIST => 4000f;
        protected virtual float SENTRY_RECALL_TARGET_OFFSET => 70f;
        protected virtual float SENTRY_RANDOM_OFFSET => 20f;
        protected virtual bool DEBUG_RECALL_SYNC => false;
        protected virtual bool USE_CUSTOM_SENTRY_RECALL => false;
        protected virtual int SENTRY_RECALL_ANCHOR_PROJECTILE_TYPE => -1;
        protected virtual bool AUTO_READD_BUFF_ON_PLANT => false;
        protected virtual bool USE_CURSOR_ASSISTED_PLANT => false;
        protected virtual float CURSOR_ASSISTED_PLANT_DISTANCE => 100f;
        protected virtual float CURSOR_ASSIST_P_FACTOR => 28f;

        // recalling flag
        protected virtual int TIME_LEFT_RECALL => 60*60; // 1 min
        protected virtual float RECALL_SPEED => 30f;
        protected virtual float RECALL_ROTATE_SPEED => 0.3f;
        protected virtual int RECALL_SOUND_INTERVAL => 10;

        /* ------------------------- State Constants ------------------------- */
        public const int WAVE_STATE = 0; // left-click: wave
        public const int RAISE_STATE = 1; // right-short-press: raise
        public const int PLANT_STATE = 2; // right-long-press: plant
        public const int RECALL_STATE = 3; // right-click after plant: recall


        /* ------------------------- Flag Cloth Constants ------------------------- */
        protected virtual int FLAG_WIDTH => 128;
        protected virtual int FLAG_HEIGHT => 80;
        protected virtual float ROT_DISPLACEMENT => (2.05f + 2.05f);  //  1.275f + 2.05f
        protected virtual float SLOW_WAVE_AMPLITUDE => 10f;
        protected virtual float SLOW_WAVE_SPEED_FACTOR => 7f;

        protected virtual float FAST_WAVE_AMPLITUDE => 10f;
        protected virtual float FAST_WAVE_SPEED_FACTOR => 1.5f;
        protected virtual int WAVE_SLICE_WIDTH => 4;
        protected virtual string FLAG_CLOTH_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Projectiles/TestFlag";

        /* ------------------------- Tail Constants ------------------------- */
        public bool TailEnabled = true;
        protected virtual bool TAIL_ENABLE_GLOBAL => true;
        protected virtual int TAIL_LENGTH => 6;
        protected virtual int FLAG_CLOTH_LENGTH => 4;
        protected virtual int TAIL_OVERLAP_SIZE => 1;    // 2
        protected virtual int TAIL_FIT_INSERT_SIZE => 2;   // 1
        protected virtual float TAIL_OFFSET_X_1 => -100f;  // -123
        protected virtual float TAIL_OFFSET_Y_1 => -135f;  // -213
        protected virtual float TAIL_OFFSET_X_2 => -100f;  // -123
        protected virtual float TAIL_OFFSET_Y_2 => -85f;   // -72
        protected virtual float TAIL_OFFSET_ROT_1 => 0f;
        protected virtual float TAIL_OFFSET_ROT_2 => 0f;
        protected virtual Vector2 SPIN_CENTER_OFFSET => new Vector2(65f, 225f);
        protected virtual float SPIN_CENTER_OFFSET_ROT => 0f;
        protected virtual Color TAIL_COLOR => new Color(98, 0, 0, 95);
        protected virtual bool TAIL_DYNAMIC_DEBUG => false;
        protected virtual string FLAG_TAIL_TEXTURE_PATH => ModGlobal.MOD_TEXTURE_PATH + "Vertexes/SwordTail4";
        protected virtual bool ENABLE_VERTEX_FLAG => true;
        protected virtual bool VERTEX_DEBUG => false;
        protected const int TAIL_BLEND_STATE_ALPHABLEND = 0;
        protected const int TAIL_BLEND_STATE_ADDITIVE = 1;
        protected const int TAIL_BLEND_STATE_NONPREMULTIPLIED = 2;
        protected virtual int TAIL_BLEND_STATE => 0;

        /* ------------------------- Buff Constants ------------------------- */
        protected virtual int ENHANCE_BUFF_ID => ModBuffID.SentryEnhancement;
        protected virtual int NPC_DEBUFF_ID => BuffID.SwordWhipNPCDebuff;
        protected virtual int BUFF_START_TIME => 25;
        protected virtual int ENHANCE_BUFF_DURATION => 60*3; // 3s
        protected virtual int ENHANCE_BUFF_DURATION_PLANTED => -1;
        protected virtual int NPC_DEBUFF_DURATION => 60*7; // 7s
        protected virtual float DAMAGE_DECAY_FACTOR => 0.8f;

        /* ------------------------- Public Attributes ------------------------- */
        // public int State = WAVE_STATE;
        public bool SwitchFlag = false;
        public int PoleLength = MIN_POLE_PENGTH;    // real pole length, can be modified in realtime (low update freq)
        public int TimeLeftRaise = 60*10;  // (low update freq)
        public float AttackSpeed = 1f;  // aborted

        /* ------------------------- Private Variables ------------------------- */
        // protected int FixedDirection = 1;   // record the fixed direction of the pole when raised (low update freq)
        protected int State = WAVE_STATE;
        protected bool Initialized = false;     // init flag (low update freq)

        protected float AimAngle = 0f;   // record the aim angle of the pole when waving (low update freq)
        protected float ItemRot = -2.05f; // (high update freq local)
        protected float RotAcc = 0f; // (high update freq local)
        protected float RotSpd = 0f; // (high update freq local)
        protected float FlagClothAmplitude = 0f; // (high update freq local)
        protected float FlagClothWaveSpeed = 0f; // (high update freq local)

        protected bool HasPlayedOnGroundSound = false;      // played onground sound flag(low update freq)
        protected bool HasPlayedBuffSound = false;      // played buff-adding sound flag(low update freq)
        protected bool UseFastAnimation = false;    // if to use fast animation(higher freq)(low update freq)

        protected Vector2 STICK_OFFSET = new Vector2(0f, -MIN_POLE_PENGTH / 2f + 20f);
        protected List<float> StickOffsetList = new List<float>();
        protected Vector2 CursorAssistedPlantPos = Vector2.Zero;

        /* ------------------------- Data Packer ------------------------- */

        protected NonUniformFloatIntPacker timerPacker = new NonUniformFloatIntPacker(
            127, // OnGroundCnt, 7bit
            127, // RaiseTime, 7bit
            15,  // RecallTime, 4bit
            1, // WaveDirection, 1bit
            7, // State, 3bit
            255, // HitCount, 8bit
            1 // FixedDirection, 1bit
        );

        protected const int OnGroundCntBit = 0;
        protected const int RaiseTimeBit = 1;
        protected const int RecallTimeBit = 2;
        protected const int WaveDirectionBit = 3;
        protected const int StateBit = 4;
        protected const int HitCountBit = 5;
        protected const int FixedDirectionBit = 6;

        protected NonUniformFloatIntPacker flagPacker = new NonUniformFloatIntPacker(
            1,   // InitializeFlag
            1,  // SentryRecallInitializeFlag
            1,   // HasSentryLockInSlotFlag
            1,  // CursorAssistingFlag
            1, // SwitchFlag
            1, // ControlUseTileFlag
            1  // ControlUseItemFlag
        );

        protected const int InitializeFlagBit = 0;
        protected const int SentryRecallInitializeFlagBit = 1;
        protected const int HasSentryLockInSlotFlagBit = 2;
        protected const int CursorAssistingFlagBit = 3;
        protected const int SwitchFlagBit = 4;
        protected const int ControlUseTileFlagBit = 5;
        protected const int ControlUseItemFlagBit = 6;
        
        protected virtual void IssueSentryRecallCommands()
        {
            List<Projectile> candidateSentries = new List<Projectile>();
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.owner == Projectile.owner && proj.sentry && (proj.Center - Projectile.Center).Length() <= SENTRY_RECALL_MAX_DIST)
                {
                    candidateSentries.Add(proj);
                }
            }

            int sentryCount = candidateSentries.Count;
            if (sentryCount == 0)
            {
                return;
            }

            float totalLength = sentryCount == 1 ? 0f : (float)Math.Sqrt(sentryCount - 1) * 200f;
            int ranDir = ((Projectile.identity + Projectile.owner) & 1) == 0 ? 1 : -1;

            for (int i = 0; i < sentryCount; i++)
            {
                Projectile sentry = candidateSentries[i];
                float localX = sentryCount == 1 ? 0f : (float)i / (float)(sentryCount - 1) * totalLength - totalLength / 2f;
                float preciseX = Projectile.Center.X + localX * ranDir;
                float preciseY = Projectile.Center.Y - SENTRY_RECALL_TARGET_OFFSET;
                float randomSeed = Main.rand.NextFloat(-SENTRY_RANDOM_OFFSET, SENTRY_RANDOM_OFFSET);
                float x = preciseX + randomSeed;
                float y = preciseY + randomSeed;
                Vector2 targetPos = MinionAIHelper.SearchForValidPosition(new Vector2(x, y), (int)(sentry.width * 1.5f), (int)(sentry.height * 1.5f), 10);
                if (USE_CUSTOM_SENTRY_RECALL && sentry.tileCollide)
                {
                    // Anchor recalls need a ground-resolved destination for sentries that collide with tiles.
                    targetPos = MinionAIHelper.SearchForGround(targetPos + new Vector2(0, 100f), 10, 16, (int)(sentry.height * 0.5f));
                }

                SentryRecallCommand command = new SentryRecallCommand
                {
                    TargetPos = targetPos,
                    RecallSpeed = SENTRY_RECALL_SPEED,
                    RecallThreshold = SENTRY_RECALL_THRESHOLD,
                    RecallDecayDist = SENTRY_RECALL_DECAY_DIST,
                    UseAnchorRecall = USE_CUSTOM_SENTRY_RECALL,
                    AnchorProjectileType = SENTRY_RECALL_ANCHOR_PROJECTILE_TYPE,
                    DisableTileCollideWhileRecalling = !USE_CUSTOM_SENTRY_RECALL
                };

                RecallSentryGlobal.IssueRecallCommand(sentry, command);
                if (DEBUG_RECALL_SYNC)
                {
                    Mod.Logger.Info(
                        $"[FlagProjectile] IssueRecall flagWho={Projectile.whoAmI} flagIdentity={Projectile.identity} owner={Projectile.owner} mode={Main.netMode} " +
                        $"sentryWho={sentry.whoAmI} sentryIdentity={sentry.identity} useAnchor={USE_CUSTOM_SENTRY_RECALL} target={targetPos} tile={sentry.tileCollide}");
                }
            }
        }



        /* -------------------------- Setting Defaults -------------------------- */

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;//这一项赋值2可以记录运动轨迹和方向（用于制作拖尾）
            ProjectileID.Sets.TrailCacheLength[Type] = 50;//这一项代表记录的轨迹最多能追溯到多少帧以前
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = PoleLength;
            Projectile.friendly = true;
            Projectile.ownerHitCheck = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)(TIME_LEFT_WAVE / AttackSpeed);
            Projectile.localNPCHitCooldown = Projectile.timeLeft;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.netImportant = true;

            if (TAIL_DYNAMIC_DEBUG)
            {
                DynamicParamManager.Register("PoleLength", 280, 80, 1000);
                DynamicParamManager.Register("TailLength", (float)TAIL_LENGTH, 3, 50);
                // DynamicParamManager.Register("Offset_X.1", TAIL_OFFSET_X_1, -300, 300);
                // DynamicParamManager.Register("Offset_Y.1", TAIL_OFFSET_Y_1, -300, 300);
                // DynamicParamManager.Register("Offset_X.2", TAIL_OFFSET_X_2, -300, 300);
                // DynamicParamManager.Register("Offset_Y.2", TAIL_OFFSET_Y_2, -300, 300);
                // DynamicParamManager.Register("Offset_rot.1", TAIL_OFFSET_ROT_1, -3.14f, 3.14f);
                // DynamicParamManager.Register("Offset_rot.2", TAIL_OFFSET_ROT_2, -3.14f, 3.14f);
                // DynamicParamManager.Register("SpinCenterOffset.X", SPIN_CENTER_OFFSET.X, -300, 300);
                // DynamicParamManager.Register("SpinCenterOffset.Y", SPIN_CENTER_OFFSET.Y, -300, 300);
                // DynamicParamManager.Register("SpinCenterOffset.Rot", SPIN_CENTER_OFFSET_ROT, -3.14f, 3.14f);
                DynamicParamManager.Register("TailColor.R", TAIL_COLOR.R, 0, 255);
                DynamicParamManager.Register("TailColor.G", TAIL_COLOR.G, 0, 255);
                DynamicParamManager.Register("TailColor.B", TAIL_COLOR.B, 0, 255);
                DynamicParamManager.Register("TailColor.A", TAIL_COLOR.A, 0, 255);
            }
            
            // DynamicParamManager.Register("StickOffsetList.extra", 0, -30, 30);
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            AttackSpeed = player.GetAttackSpeed(DamageClass.SummonMeleeSpeed);  

            if(MinionAIHelper.IsServer())
            {
                foreach(var proj in Main.projectile)
                {
                    if(proj.active && proj.owner == Projectile.owner && proj.type == Projectile.type && proj.whoAmI != Projectile.whoAmI) proj.Kill();
                }
            }

            // Main.NewText("itemAnimationMax: " + player.itemAnimationMax +
            //              " AttackSpeed: " + AttackSpeed +
            //              "time left:" + TIME_LEFT_WAVE / AttackSpeed);

            
        }


        /* -------------------------- AI -------------------------- */
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if(TAIL_DYNAMIC_DEBUG)
                PoleLength = (int)DynamicParamManager.Get("PoleLength").value;
            STICK_OFFSET = new Vector2(0f, -PoleLength/2f);     // player handheld point: avoid holding the tip
            Projectile.height = PoleLength;
            AttackSpeed = player.GetAttackSpeed(DamageClass.Melee);            

            // update state
            State = GetCurrentState();
            Initialized = flagPacker.Get(Projectile.ai[1], InitializeFlagBit)!=0;
            SwitchFlag = flagPacker.Get(Projectile.ai[1], SwitchFlagBit)!=0;

            // sync player mouse click
            if(Projectile.owner == Main.myPlayer)
            {
                Projectile.ai[1] = flagPacker.Set(Projectile.ai[1], ControlUseTileFlagBit, player.controlUseTile?1:0);
                Projectile.ai[1] = flagPacker.Set(Projectile.ai[1], ControlUseItemFlagBit, player.controlUseItem?1:0);
            }

            // basic flag state machine
            switch (State)
            {
                case WAVE_STATE:
                {
                    WaveAI(player, ref State, ref Initialized);
                } break;
                case RAISE_STATE:
                {
                    RaiseAI(player, ref State, ref Initialized);
                } break;
                case PLANT_STATE:
                {
                    PlantAI(player, ref State, ref Initialized);
                } break;
                case RECALL_STATE:
                {
                    RecallAI(player, ref State, ref Initialized);
                } break;
            }

            // long timestamp = DateTime.UtcNow.Ticks;
            // Main.NewText($"[{timestamp}][Client {Main.myPlayer}] State={State} timeLeft={Projectile.timeLeft} itemTime={player.itemTime} itemAnimation={player.itemAnimation}");
            // if (Main.netMode == NetmodeID.Server)
            // {
            //     Console.WriteLine($"[{timestamp}][SERVER] active={Projectile.active} timeLeft={Projectile.timeLeft} itemTime={player.itemTime} itemAnimation={player.itemAnimation}");
            // }

            // DamageGrassAlongBlade(player);

            CreateDustEffect(player);

            Projectile.ai[0] = timerPacker.Set(Projectile.ai[0],StateBit,State);
            Projectile.ai[1] = flagPacker.Set(Projectile.ai[1],InitializeFlagBit,Initialized?1:0);
            Projectile.ai[1] = flagPacker.Set(Projectile.ai[1],SwitchFlagBit,SwitchFlag?1:0);
        }

        protected void WaveAI(Player player, ref int State, ref bool Initialized)
        {
            if (!Initialized)
            {
                // initialize
                int FixedDirection = player.direction == 1 ? 1 : 0;
                Projectile.ai[0] = timerPacker.Set(Projectile.ai[0],FixedDirectionBit,FixedDirection);
                // get mouse dir
                Vector2 aimDir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX);
                AimAngle = aimDir.ToRotation() + ModGlobal.PI_FLOAT / 2f;
                // Main.NewText("WaveAI:"+Projectile.identity);
                // SoundEngine.PlaySound(SoundID.Item32, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item102, Projectile.Center);
                Initialized = true;

                RotSpd = 0f;

                // Projectile.timeLeft = (int)(TIME_LEFT_WAVE / AttackSpeed);
                Projectile.timeLeft = (int)player.itemAnimationMax;

                Projectile.netUpdate = true;
            }

            int WaveDirection = timerPacker.Get(Projectile.ai[0],WaveDirectionBit);
            WaveDirection = WaveDirection == 1 ? 1 : -1;
            float dir = (float)WaveDirection/*  * FixedDirection */;

            float RotDisplacement = ROT_DISPLACEMENT * 1.12f/* DynamicParamManager.QuickGet("RotDisplacement", 100f, 0f, 200f).value / 100f */;
            
            // Parameterize swing with normalized time u to avoid frame-rate dependent integration drift.
            float WaveUseTime = Math.Max(1f, player.itemAnimationMax);
            float waveSteps = Math.Max(1f, WaveUseTime - 1f);
            float elapsed = MathHelper.Clamp(WaveUseTime - Projectile.timeLeft, 0f, waveSteps);
            float u = elapsed / waveSteps; // 0 to 1 over visible frames

            float accSplitRatio = 0.333f;// DynamicParamManager.QuickGet("AccSplitRatio", 0.333f, 0f, 1f).value;
            float split = MathHelper.Clamp(accSplitRatio, 0.001f, 0.999f);

            float RotRate;
            if (u < split)
            {
                RotRate = (u * u) / split;
                RotAcc = 2f * RotDisplacement / (WaveUseTime * WaveUseTime * split);
                RotSpd = 2f * RotDisplacement * u / (split * WaveUseTime);
            }
            else
            {
                float du = u - split;
                RotRate = split + 2f * du - (du * du) / (1f - split);
                RotAcc = -2f * RotDisplacement / (WaveUseTime * WaveUseTime * (1f - split));
                RotSpd = 2f * RotDisplacement * (1f - u) / ((1f - split) * WaveUseTime);
            }
            RotRate = MathHelper.Clamp(RotRate, 0f, 1f);
            ItemRot = (RotRate - 0.5f) * RotDisplacement;

            float offset_delta = 0f;
            float offsetSplitRatio = 0.5f;// DynamicParamManager.QuickGet("OffsetSplitRatio", 0.5f, 0f, 1f).value;
            float offset_max = 0.5f;
            float offset_min = offset_max - 0.6f * player.whipRangeMultiplier;
            // Main.NewText("offset_min: "+offset_min);
            if (RotRate < offsetSplitRatio)
            {
                offset_delta = MathHelper.Lerp(offset_max, offset_min, RotRate / offsetSplitRatio) * PoleLength;
            }
            else
            {
                offset_delta = MathHelper.Lerp(offset_min, offset_max, (RotRate - offsetSplitRatio) / (1f - offsetSplitRatio)) * PoleLength;
            }
            STICK_OFFSET = new Vector2(0f, -PoleLength / 2f + offset_delta);
            Vector2 StickOffset = new Vector2(STICK_OFFSET.X * dir, STICK_OFFSET.Y);
            StickOffsetList.Add(-PoleLength / 2f + offset_delta);

            // Main.NewText($"Rotrate:{RotRate,10:0.00} RotAcc:{RotAcc,10:0.00} RotSpd:{RotSpd,10:0.00} ItemRot:{ItemRot,10:0.00}");

            // string stickOffsetString = "";
            // foreach(var offset in StickOffsetList)
            // {
            //     stickOffsetString += $"{offset,10:0.00} ";
            // }
            // Main.NewText($"{ "StickOffsetList:",-20}\t{stickOffsetString}");
            // string oldrotstring = "";
            // List<float> oldrotlist = new List<float>(Projectile.oldRot);
            // oldrotlist.RemoveAll(rot => rot == 0f);
            // oldrotlist.Reverse();
            // foreach(var rot in oldrotlist)
            // {
            //     oldrotstring += $"{rot,10:0.00} ";
            // }
            // Main.NewText($"{ "Projectile.oldRot:",-20}\t{oldrotstring}");

            // string stickOffsetString = "";
            // foreach(var offset in StickOffsetList)
            // {
            //     stickOffsetString += Math.Round(offset, 2) + " ";
            // }
            // Main.NewText("StickOffsetList: "+stickOffsetString);
            // string TailString = "";
            // foreach(var pos in Projectile.oldPos)
            // {
            //     TailString += Math.Round(pos.Y, 2) + " ";
            // }
            // Main.NewText("Projectile.oldPos: "+TailString);
            // Main.NewText("RotRate: "+RotRate);
            
            // Main.NewText("RotRate: "+RotRate + " RotAcc: "+RotAcc + " RotSpd: "+RotSpd + " ItemRot: "+ItemRot);

            float Rot = AimAngle + (ROT_ANGLE * RotRate - ROT_ANGLE / 2f) * dir;

            Projectile.Center = CenterMapping(player.Center, StickOffset, Rot);
            Projectile.rotation = Rot;
            Projectile.spriteDirection = (int)dir;

            // Main.NewText(Projectile.Center);

        }

        protected void RaiseAI(Player player, ref int State, ref bool Initialized)
        {
            bool controlUseTile = flagPacker.Get(Projectile.ai[1],ControlUseTileFlagBit)!=0;
            long timestamp = DateTime.UtcNow.Ticks;
            // Main.NewText("["+timestamp+"]"+" player.altFunctionUse:"+player.altFunctionUse+" player.ControlUseItem:"+player.controlUseItem+" player.ControlUseTile:"+controlUseTile);
            if(!controlUseTile)
            {
                Projectile.Kill();
                Projectile.netUpdate = true;
                return;
            }
            int RaiseTime = timerPacker.Get(Projectile.ai[0],RaiseTimeBit);
            if (!Initialized)
            {
                // initialize
                Projectile.timeLeft = 60*10;
                int FixedDirection = player.direction == 1 ? 1 : 0;
                Projectile.ai[0] = timerPacker.Set(Projectile.ai[0],FixedDirectionBit,FixedDirection);
                Projectile.friendly = false;
                // Main.NewText("RaiseAI:"+Projectile.identity);
                Initialized = true;
                Projectile.netUpdate = true;
            }
            Projectile.timeLeft = 60*10;
            float RaiseMaxHeight = 16f * 4f + HANDHELD_POINT_OFFSET / 2f;
            float RaiseMaxSpeed = 2f * RaiseMaxHeight / (float)TimeLeftRaise;
            float RaiseAcc = RaiseMaxSpeed / (float)TimeLeftRaise * 2f;

            float RaiseHeight = 0f;
            if(RaiseTime < TimeLeftRaise / 2)
            {
                RaiseHeight = RaiseAcc * (float)RaiseTime * (float)RaiseTime / 2f;
            }
            else
            {
                RaiseHeight = RaiseMaxHeight - RaiseAcc * (float)(TimeLeftRaise - RaiseTime) * (float)(TimeLeftRaise - RaiseTime) / 2f;
            }
            RaiseTime = RaiseTime >= 127 ? 127 : RaiseTime+1;
 
            if(RaiseTime >= TimeLeftRaise * RAISE_BUFF_TIME_COEFF)
            {
                player.AddBuff(ENHANCE_BUFF_ID, ENHANCE_BUFF_DURATION);
                if(!HasPlayedBuffSound)
                {
                    float DustSpd = 3f;
                    for (float ang = -ModGlobal.PI_FLOAT; ang <= ModGlobal.PI_FLOAT; ang += ModGlobal.PI_FLOAT / 8f)
                    {
                        Dust dust;
                        Vector2 position = player.Center;
                        Vector2 DustVel = new Vector2(1, 0).RotatedBy(ang) * DustSpd;
                        dust = Terraria.Dust.NewDustPerfect(position, FULLY_CHARGED_DUST, DustVel, 0, new Color(255,255,255), 1.3f);
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                    }
                    SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
                    HasPlayedBuffSound = true;
                }

            }

            // Vector2 StickOffset = new Vector2(STICK_OFFSET.X * FixedDirection, STICK_OFFSET.Y);
            Vector2 StickOffset = new Vector2(0f, -PoleLength/2f+40f);
            Projectile.Center = CenterMapping(player.MountedCenter, StickOffset, 0) + new Vector2(0, RaiseMaxHeight/2f-RaiseHeight);
            Projectile.spriteDirection = timerPacker.Get(Projectile.ai[0],FixedDirectionBit) == 1 ? 1 : -1;
            // Projectile.velocity = player.velocity;

            if(RaiseTime >= TimeLeftRaise * 0.88f)
            {
                SwitchFlag = true;
            }
            
            if(SwitchFlag)
            {
                State = PLANT_STATE;
                SwitchFlag = false;
                Initialized = false;
                Projectile.netUpdate = true;
                // Main.NewText("switch from raise to plant,timeleft:"+Projectile.timeLeft);
            }

            Projectile.ai[0] = timerPacker.Set(Projectile.ai[0], RaiseTimeBit, RaiseTime);
        }

        protected void PlantAI(Player player, ref int State, ref bool Initialized)
        {
            int BuffTimePlanted = ENHANCE_BUFF_DURATION_PLANTED == -1 ? ENHANCE_BUFF_DURATION : ENHANCE_BUFF_DURATION_PLANTED;
            int OnGroundCnt = timerPacker.Get(Projectile.ai[0], OnGroundCntBit);
            bool CursorAssisting = flagPacker.Get(Projectile.ai[1], CursorAssistingFlagBit)!=0;
            if (!Initialized)
            {
                // initialize
                Projectile.timeLeft = PLANT_EXIST_DURATION;
                int FixedDirection = player.direction == 1 ? 1 : 0;
                Projectile.ai[0] = timerPacker.Set(Projectile.ai[0],FixedDirectionBit,FixedDirection);
                Projectile.tileCollide = true;
                Projectile.friendly = false;
                Projectile.rotation = 0f;

                // reset buff
                player.AddBuff(ENHANCE_BUFF_ID, BuffTimePlanted);

                // calculate cursor assisted plant pos
                if(Projectile.owner == Main.myPlayer)
                {
                    float dist = (float)Math.Min((Main.MouseWorld - player.Center).Length(), CURSOR_ASSISTED_PLANT_DISTANCE);
                    CursorAssistedPlantPos = player.Center + (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX) * dist + new Vector2(0, -PoleLength/2f);
                }

                // Main.NewText("PlantAI:"+Projectile.identity);
                Initialized = true;

                SentryAnchorPlayer anchorPlayer = player.GetModPlayer<SentryAnchorPlayer>();
                if(anchorPlayer.HasLockedSentryAnchor)
                {
                    // HasSentryLockInSlot = true;
                    Projectile.ai[1] = flagPacker.Set(Projectile.ai[1],HasSentryLockInSlotFlagBit,1);
                }

                if (USE_CURSOR_ASSISTED_PLANT)
                {
                    CursorAssisting = true;
                    Projectile.ai[1] = flagPacker.Set(Projectile.ai[1], CursorAssistingFlagBit, CursorAssisting?1:0);
                }

                HasPlayedOnGroundSound = false;

                MinionAIHelper.SetProjectileNetUpdate(Projectile);

                // Main.NewText("Plant init");
            }

            // apply cursor assist
            if(CursorAssisting)
            {
                Projectile.tileCollide = false;
                Vector2 dist = CursorAssistedPlantPos - Projectile.Center;
                float process = dist.Length() / CURSOR_ASSISTED_PLANT_DISTANCE;
                float factor = CURSOR_ASSIST_P_FACTOR * process;
                Projectile.Center += dist.SafeNormalize(Vector2.UnitX) * factor;
                // Dust.QuickDust(CursorAssistedPlantPos, Color.Red);
                if(dist.Length() <= 5f)
                {
                    CursorAssisting = false;
                    Projectile.ai[1] = flagPacker.Set(Projectile.ai[1], CursorAssistingFlagBit, CursorAssisting?1:0);
                }
                else return;
            }
            else Projectile.tileCollide = true;

            // apply gravity
            // long timestamp = Main.GameUpdateCount;
            // Main.NewText("[+"+timestamp+"]"+"Add gravity");
            MinionAIHelper.ApplyGravity(Projectile, GRAVITY, MAX_FALL_SPEED);

            if (OnGroundCnt == 1 && !HasPlayedOnGroundSound)
            {
                SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                HasPlayedOnGroundSound = true;
            }

            // Main.NewText("rotation:"+Projectile.rotation);

            if (OnGroundCnt >= ONGROUND_CNT_THRESHOLD)
            {
                bool HasSentryLockInSlot = flagPacker.Get(Projectile.ai[1],HasSentryLockInSlotFlagBit)!=0;
                if(!HasSentryLockInSlot)
                {
                    bool SentryRecallInitialized = flagPacker.Get(Projectile.ai[1],SentryRecallInitializeFlagBit)!=0;
                    if (!SentryRecallInitialized)
                    {
                        if (Projectile.owner == Main.myPlayer)
                        {
                            IssueSentryRecallCommands();
                        }

                        // reset buff
                        player.AddBuff(ENHANCE_BUFF_ID, BuffTimePlanted);

                        SentryRecallInitialized = true;
                    }
                    Projectile.ai[1] = flagPacker.Set(Projectile.ai[1],SentryRecallInitializeFlagBit,SentryRecallInitialized?1:0);
                }
            }

            // sync player mouse click
            bool useItem = flagPacker.Get(Projectile.ai[1], ControlUseItemFlagBit)!=0;
            bool useTile = flagPacker.Get(Projectile.ai[1], ControlUseTileFlagBit)!=0;
            if(!(useItem && player.controlUseItem) || !(useTile && player.controlUseTile)) Projectile.netUpdate = true;

            if(OnGroundCnt >= ONGROUND_PLAYER_RECALL_THRESHOLD)
            {
                // click right or left to recall
                SwitchFlag = useItem || useTile;
                // Main.NewText("useItem:"+useItem+"useTile:"+useTile);
            }

            // if the planted flag is too far away from player, kill self
            Vector2 ToOwnerDist = player.Center - Projectile.Center;
            if (ToOwnerDist.Length() >= 4000f)
            {
                Projectile.Kill();
            }

            // auto re-add buff
            if (AUTO_READD_BUFF_ON_PLANT/*  && !player.HasBuff(ENHANCE_BUFF_ID) */)
            {
                player.AddBuff(ENHANCE_BUFF_ID, BuffTimePlanted);
            }

            if (SwitchFlag)
            {
                State = RECALL_STATE;
                SwitchFlag = false;
                Initialized = false;
                HasPlayedOnGroundSound = false;

                Projectile.netUpdate = true;
            }
        }
        
        protected virtual void CustomSentryRecall(SentryRecallInfo info)
        {
            // raise error if not implemented
            throw new NotImplementedException("CustomSentryRecall is not implemented");
        }

        protected void RecallAI(Player player, ref int State, ref bool Initialized)
        {
            int RecallTime = timerPacker.Get(Projectile.ai[0],RecallTimeBit);
            if (!Initialized)
            {
                // initialize
                Projectile.friendly = true;
                Projectile.tileCollide = false;
                Projectile.timeLeft = TIME_LEFT_RECALL;
                Projectile.localNPCHitCooldown = Projectile.timeLeft;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.ownerHitCheck = false;
                Projectile.netUpdate = true;

                Initialized = true;
            }

            Vector2 RecallDist = player.Center - Projectile.Center;
            Vector2 RecallDirection = RecallDist.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = RecallDirection * RECALL_SPEED;
            // Projectile.Center += RecallDirection * RECALL_SPEED;
            Projectile.rotation += RECALL_ROTATE_SPEED * Projectile.spriteDirection;

            RecallTime++;
            if(RecallTime >= RECALL_SOUND_INTERVAL)
            {
                RecallTime = 0;
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
            }

            Projectile.ai[0] = timerPacker.Set(Projectile.ai[0],RecallTimeBit,RecallTime);

            if(RecallDist.Length() <= 100f)
            {
                Projectile.Kill();
            }
        }

        /* -------------------------- Drawing -------------------------- */

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, 
            List<int> behindNPCs, List<int> behindProjectiles, 
            List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Texture2D flagTexture = ModContent.Request<Texture2D>(FLAGPOLE_TEXTURE_PATH).Value;
            int width = flagTexture.Width;
            int height = Projectile.height;
            int TextureHeight = flagTexture.Height;
            Vector2 origin = new Vector2(width / 2, height / 2);

            // draw tip part
            Rectangle tipRect = new Rectangle(0, 0, width, TIP_HEIGHT);
            Vector2 tipLocalPos = new Vector2(0, 0);
            Vector2 tipWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, tipLocalPos);
            DrawPart(flagTexture, tipWorldPos, tipRect, lightColor, origin);

            // draw repeat part
            for (int i = 0; i < (PoleLength - TIP_HEIGHT - BASE_HEIGHT) / REPEAT_SLICE_HEIGHT; i++)
            {
                int repeatY = i * REPEAT_SLICE_HEIGHT + TIP_HEIGHT;
                Rectangle repeatRect = new Rectangle(0, TIP_HEIGHT, width, REPEAT_SLICE_HEIGHT);
                Vector2 repeatLocalPos = new Vector2(0, repeatY);
                Vector2 repeatWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, repeatLocalPos);
                DrawPart(flagTexture, repeatWorldPos, repeatRect, lightColor, origin);
            }

            // draw base part
            Rectangle baseRect = new Rectangle(0, TextureHeight - BASE_HEIGHT, width, BASE_HEIGHT);
            Vector2 baseLocalPos = new Vector2(0, PoleLength - BASE_HEIGHT);
            Vector2 baseWorldPos = MinionAIHelper.ConvertToWorldPos(Projectile, baseLocalPos);
            DrawPart(flagTexture, baseWorldPos, baseRect, lightColor, origin);

            // draw flag cloth
            Vector2 FlagOffset = new Vector2(-FLAG_WIDTH / 2f * Projectile.spriteDirection, (FLAG_HEIGHT - Projectile.height) / 2f);
            Vector2 FlagOffsetEx = new Vector2(-2f + 1f * Projectile.spriteDirection, 0f);
            FlagOffset += FlagOffsetEx;
            Vector2 ClothCenter = Projectile.Center + FlagOffset.RotatedBy(Projectile.rotation);
            if(ENABLE_VERTEX_FLAG)
            {
                if (State == WAVE_STATE)
                    PredrawFlagClothDynamicVertices(ref lightColor, ClothCenter);   // draw dynamic vertices
                else
                    // PreDrawFlagClothVertices(ref lightColor, ClothCenter);   // draw static vertices
                    PreDrawFlagCloth(ref lightColor, ClothCenter);
            }
            else
            {
                PreDrawFlagCloth(ref lightColor, ClothCenter);   // legacy draw flag cloth (now abandoned)
            }

            return false;
        }

        protected void PreDrawFlagCloth(ref Color lightColor, Vector2 ClothCenter)
        {
            // decode
            Player player = Main.player[Projectile.owner];
            if(State == WAVE_STATE || State == RECALL_STATE)
            {
                UseFastAnimation = true;
            }
            else
            {
                UseFastAnimation = false;
            }

            if(State == RAISE_STATE || State == PLANT_STATE)
            {
                // do not draw tail when raise or plant
                TailEnabled = false;
            }
            else
            {
                TailEnabled = true;
            }
            if(TailEnabled && TAIL_ENABLE_GLOBAL)
            {
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
                int tailLength;
                float offset_X_1, offset_Y_1, offset_X_2, offset_Y_2, offset_rot_1, offset_rot_2;
                Color tailColor;
                Vector2 SpinCenter;
                if(TAIL_DYNAMIC_DEBUG)
                {
                    tailLength = (int)DynamicParamManager.Get("TailLength").value;
                    // offset_X_1 = (float)DynamicParamManager.Get("Offset_X.1").value;
                    // offset_Y_1 = (float)DynamicParamManager.Get("Offset_Y.1").value;
                    // offset_X_2 = (float)DynamicParamManager.Get("Offset_X.2").value;
                    // offset_Y_2 = (float)DynamicParamManager.Get("Offset_Y.2").value;
                    // offset_rot_1 = (float)DynamicParamManager.Get("Offset_rot.1").value;
                    // offset_rot_2 = (float)DynamicParamManager.Get("Offset_rot.2").value;
                    offset_X_1 = TAIL_OFFSET_X_1;
                    offset_Y_1 = TAIL_OFFSET_Y_1;
                    offset_X_2 = TAIL_OFFSET_X_2;
                    offset_Y_2 = TAIL_OFFSET_Y_2;
                    offset_rot_1 = TAIL_OFFSET_ROT_1;
                    offset_rot_2 = TAIL_OFFSET_ROT_2;
                    tailColor = TAIL_COLOR;
                    tailColor = new Color((int)DynamicParamManager.Get("TailColor.R").value, (int)DynamicParamManager.Get("TailColor.G").value, (int)DynamicParamManager.Get("TailColor.B").value, (int)DynamicParamManager.Get("TailColor.A").value);
                    // SpinCenter = ClothCenter + new Vector2(DynamicParamManager.Get("SpinCenterOffset.X").value*Projectile.spriteDirection, DynamicParamManager.Get("SpinCenterOffset.Y").value).RotatedBy(Projectile.rotation + DynamicParamManager.Get("SpinCenterOffset.Rot").value);
                }
                else
                {
                    tailLength = TAIL_LENGTH;
                    offset_X_1 = TAIL_OFFSET_X_1;
                    offset_Y_1 = TAIL_OFFSET_Y_1;
                    offset_X_2 = TAIL_OFFSET_X_2;
                    offset_Y_2 = TAIL_OFFSET_Y_2;
                    offset_rot_1 = TAIL_OFFSET_ROT_1;
                    offset_rot_2 = TAIL_OFFSET_ROT_2;
                    tailColor = TAIL_COLOR;
                    SpinCenter = ClothCenter + SPIN_CENTER_OFFSET.RotatedBy(Projectile.rotation + SPIN_CENTER_OFFSET_ROT);
                }

                offset_X_1 *= Projectile.spriteDirection;
                offset_X_2 *= Projectile.spriteDirection;

                SpinCenter = player.Center;


                // int CurrentTime = (int)(TIME_LEFT_WAVE / AttackSpeed) - Projectile.timeLeft;
                int CurrentTime = (int)player.itemAnimationMax - Projectile.timeLeft;
                if(State == RECALL_STATE) CurrentTime = TIME_LEFT_RECALL - Projectile.timeLeft;
                int OldPosSize = (int)Math.Min(CurrentTime, tailLength);
                for(int i = 0; i < OldPosSize;i++)
                {
                    float ratio = i / (float)tailLength;
                    float color_rate = MathHelper.Clamp(ratio*3, 0, 1);
                    Color b = tailColor;

                    // SpinCenter = Projectile.oldPos[i] + new Vector2(0, PoleLength/2);
                    Vector2 UpperVertexPffset, LowerVertexPffset;
                    if (State == WAVE_STATE)
                    {
                        Vector2 StickOffset = new Vector2(0, StickOffsetList[Math.Max(0,StickOffsetList.Count - i - 5)]);
                        SpinCenter = CenterMapping(Projectile.Center, StickOffset, Projectile.rotation + ModGlobal.PI_FLOAT);
                        UpperVertexPffset = new Vector2(-FLAG_WIDTH * 0.9f * Projectile.spriteDirection, -(PoleLength / 2f) * 0.95f) + StickOffset;
                        LowerVertexPffset = new Vector2(-FLAG_WIDTH * 0.9f * Projectile.spriteDirection, (-PoleLength / 2f + FLAG_HEIGHT) * 0.95f) + StickOffset;
                    }
                    else
                    {
                        SpinCenter = Projectile.Center;
                        UpperVertexPffset = new Vector2(-FLAG_WIDTH * 0.9f * Projectile.spriteDirection, -PoleLength / 2f * 0.95f);
                        LowerVertexPffset = new Vector2(-FLAG_WIDTH * 0.9f * Projectile.spriteDirection, (-PoleLength / 2f + FLAG_HEIGHT) * 0.95f);
                    }

                    ve.Add(new Vertex(SpinCenter - Main.screenPosition + UpperVertexPffset.RotatedBy(Projectile.oldRot[i]+offset_rot_1),
                            new Vector3(ratio, 1, 1),
                            b));
                    ve.Add(new Vertex(SpinCenter - Main.screenPosition + LowerVertexPffset.RotatedBy(Projectile.oldRot[i]+offset_rot_2),
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

            // draw flag
            Texture2D flagTexture = ModContent.Request<Texture2D>(FLAG_CLOTH_TEXTURE_PATH).Value;
            int width = flagTexture.Width;
            int height = flagTexture.Height;
            Vector2 origin = new Vector2(width / 2 * Projectile.spriteDirection, height / 2);
            Vector2 drawPos = ClothCenter - Main.screenPosition;
            FlagClothAmplitude = UseFastAnimation ? FAST_WAVE_AMPLITUDE : SLOW_WAVE_AMPLITUDE;
            FlagClothWaveSpeed = UseFastAnimation ? FAST_WAVE_SPEED_FACTOR : SLOW_WAVE_SPEED_FACTOR;

            float time = Main.GameUpdateCount / FlagClothWaveSpeed;
            if (State == WAVE_STATE || State == RECALL_STATE)
                time = 1.85f;
            int sliceWidth = WAVE_SLICE_WIDTH;
            int sliceCount = width / sliceWidth;

            for (int i = 0; i < sliceCount; i++)
            {
                int sliceX = i * sliceWidth;
                Rectangle sliceRect = new Rectangle(sliceX, 0, sliceWidth, height);
                float RealAmplitude = FlagClothAmplitude * ((float)sliceCount - (float)(i)) / (float)sliceCount;
                float wave = sliceX < width ? (float)Math.Sin((i / 6f) + time) * RealAmplitude : 0;

                Vector2 LocalPos = new Vector2(sliceX * Projectile.spriteDirection, wave);
                Vector2 WorldPos = ClothCenter + LocalPos.RotatedBy(Projectile.rotation) - Main.screenPosition;

                Main.spriteBatch.Draw(
                    flagTexture,
                    WorldPos,
                    sliceRect,
                    lightColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                    0f
                );
            }
        }


        protected Vector2 ConvertToWorldPos(Vector2 localPos)
        {
            return Projectile.Center + localPos.RotatedBy(Projectile.rotation) - Main.screenPosition;
        }

        protected Vector2 CenterMapping(Vector2 center, Vector2 offset, float rotation)
        {
            return center + offset.RotatedBy(rotation);
        }

        protected void PredrawFlagClothDynamicVertices(ref Color lightColor, Vector2 ClothCenter)
        {
            Player player = Main.player[Projectile.owner];
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;

            List<Vertex> FlagClothVerteces = new List<Vertex>();
            List<Vertex> FlagTailVerteces = new List<Vertex>();
            Vector2 SpinCenter = player.Center;
            // int CurrentTime = (int)(TIME_LEFT_WAVE / AttackSpeed) - Projectile.timeLeft;
            int CurrentTime = (int)player.itemAnimationMax - Projectile.timeLeft;
            int OverlapSize = TAIL_OVERLAP_SIZE;
            if(State == RECALL_STATE) CurrentTime = TIME_LEFT_RECALL - Projectile.timeLeft;
            int OldPosSize = (int)Math.Min(CurrentTime, TAIL_LENGTH+FLAG_CLOTH_LENGTH-OverlapSize);
            bool VertexDebug = VERTEX_DEBUG || DynamicParamManager.QuickGet("VertexDebug", 0f, 0f, 1f).value > 0.5f;
            List<Vector2> debugSpinCenters = new List<Vector2>();
            List<Vector2> debugUpperWorldPoints = new List<Vector2>();
            List<Vector2> debugLowerWorldPoints = new List<Vector2>();
            List<float> debugStickOffsets = new List<float>();
            List<float> debugOldRots = new List<float>();

            // construct polar points（循环上界必须同时不超过 StickOffsetList 与 Projectile.oldRot 的长度，否则会越界）
            List<float> StickOffsetListInvert = new List<float>(StickOffsetList);
            StickOffsetListInvert.RemoveAt(StickOffsetListInvert.Count - 1);
            StickOffsetListInvert.Reverse();
            int polarCount = Math.Min(OldPosSize, Math.Min(StickOffsetListInvert.Count, Projectile.oldRot.Length));
            List<MinionAIHelper.PolarCurveFitter.Polar> PolarPoints = new List<MinionAIHelper.PolarCurveFitter.Polar>();
            for(int i = 0; i < polarCount; i++)
            {
                PolarPoints.Add(new MinionAIHelper.PolarCurveFitter.Polar(StickOffsetListInvert[i] + PoleLength, Projectile.oldRot[i]));
            }

            PolarPoints = MinionAIHelper.PolarCurveFitter.FitAndInsert(PolarPoints, TAIL_FIT_INSERT_SIZE);

            float FlagClothLength = FLAG_CLOTH_LENGTH * (TAIL_FIT_INSERT_SIZE+1) - 1;
            float TailLength = TAIL_LENGTH * (TAIL_FIT_INSERT_SIZE+1) - 1;

            // Main.NewText("FlagClothLength: "+FlagClothLength + " TailLength: "+TailLength + " Count: "+PolarPoints.Count);
            for(int i = 0; i < PolarPoints.Count;i++)
            {
                // float ratio = (i) / (float)(OldPosSize-1);
                float FlagClothRatio = (i) / (float)(FlagClothLength-1);
                float FlagTailRatio = (i - FlagClothLength + OverlapSize - 1) / (float)(TailLength-OverlapSize);
                // float color_rate = MathHelper.Clamp(ratio*3, 0, 1);
                // 根据ratio插值alpha值，越靠后的点越透明
                // byte alpha = (byte)MathHelper.Clamp(MathHelper.Lerp(500, 0, ratio), 0, 255);

                if(PolarPoints.Count <= 1) break;

                // Main.NewText("FlagClothRatio: "+FlagClothRatio + " FlagTailRatio: "+FlagTailRatio);

                Vector2 UpperVertexPffset, LowerVertexPffset;
                if (State == WAVE_STATE)
                {
                    // int extra = (int)DynamicParamManager.Get("StickOffsetList.extra").value;
                    int extra = 0;
                    // Vector2 StickOffset = new Vector2(0, StickOffsetList[(int)MathHelper.Clamp(StickOffsetList.Count - (i + extra), 0, StickOffsetList.Count - 1)]);
                    Vector2 StickOffset = new Vector2(0, (float)PolarPoints[i].r - PoleLength);
                    SpinCenter = CenterMapping(Projectile.Center, StickOffset, Projectile.rotation + ModGlobal.PI_FLOAT);
                    UpperVertexPffset = new Vector2(-4f * Projectile.spriteDirection, -(PoleLength / 2f)) + StickOffset;
                    LowerVertexPffset = new Vector2(-4f * Projectile.spriteDirection, (-PoleLength / 2f + FLAG_HEIGHT)) + StickOffset;
                }
                else
                {
                    SpinCenter = Projectile.Center;
                    UpperVertexPffset = new Vector2(-4f * Projectile.spriteDirection, -PoleLength / 2f);
                    LowerVertexPffset = new Vector2(-4f * Projectile.spriteDirection, (-PoleLength / 2f + FLAG_HEIGHT));
                }

                float OldRot = (float)PolarPoints[i].theta;
                float debugStickOffset = (float)PolarPoints[i].r - PoleLength;
                Vector2 upperWorldPos = SpinCenter + UpperVertexPffset.RotatedBy(OldRot);
                Vector2 lowerWorldPos = SpinCenter + LowerVertexPffset.RotatedBy(OldRot);

                if (i < FlagClothLength)  // add flag cloth verteces
                {
                    // Color b = new Color(255, 255, 255, 225);
                    Color b = lightColor;
                    FlagClothVerteces.Add(new Vertex(upperWorldPos - Main.screenPosition,
                            new Vector3(1-FlagClothRatio, 0, 1),
                            b));
                    FlagClothVerteces.Add(new Vertex(lowerWorldPos - Main.screenPosition,
                            new Vector3(1-FlagClothRatio, 1, 1),
                            b));
                }
                if (i >= FlagClothLength - OverlapSize && i < FlagClothLength + TailLength - OverlapSize)  // add flag tail verteces
                {
                    byte alpha = (byte)(MathHelper.Clamp(FlagTailRatio * 255, 0, 255));
                    Color tailColor = new Color(TAIL_COLOR.R, TAIL_COLOR.G, TAIL_COLOR.B, TAIL_COLOR.A);
                    if(TAIL_DYNAMIC_DEBUG)
                        tailColor = new Color((int)DynamicParamManager.Get("TailColor.R").value, (int)DynamicParamManager.Get("TailColor.G").value, (int)DynamicParamManager.Get("TailColor.B").value, (int)DynamicParamManager.Get("TailColor.A").value);
                    Color b = new Color(Math.Min(lightColor.R, tailColor.R), Math.Min(lightColor.G, tailColor.G), Math.Min(lightColor.B, tailColor.B), Math.Min(alpha, tailColor.A));
                    FlagTailVerteces.Add(new Vertex(upperWorldPos - Main.screenPosition,
                            new Vector3(FlagTailRatio, 1, 1),
                            b));
                    FlagTailVerteces.Add(new Vertex(lowerWorldPos - Main.screenPosition,
                            new Vector3(FlagTailRatio, 0, 1),
                            b));
                }

                if(VertexDebug)
                {
                    debugSpinCenters.Add(SpinCenter);
                    debugUpperWorldPoints.Add(upperWorldPos);
                    debugLowerWorldPoints.Add(lowerWorldPos);
                    debugStickOffsets.Add(debugStickOffset);
                    debugOldRots.Add(OldRot);
                }

            }


            // draw flag cloth
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, 
                                    BlendState.AlphaBlend, //NonPremultiplied 
                                    SamplerState.AnisotropicClamp, 
                                    DepthStencilState.None, 
                                    RasterizerState.CullNone, 
                                    null, 
                                    Main.GameViewMatrix.
                                    TransformationMatrix);
            if(FlagClothVerteces.Count >= 3) // verteces should be at least 3 to form a triangle
            {
                gd.Textures[0] = ModContent.Request<Texture2D>(FLAG_CLOTH_TEXTURE_PATH).Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, FlagClothVerteces.ToArray(), 0, FlagClothVerteces.Count - 2);
            }

            BlendState TailBlendState = BlendState.AlphaBlend;
            if(TAIL_BLEND_STATE == TAIL_BLEND_STATE_ADDITIVE) TailBlendState = BlendState.Additive;
            if(TAIL_BLEND_STATE == TAIL_BLEND_STATE_NONPREMULTIPLIED) TailBlendState = BlendState.NonPremultiplied;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, TailBlendState, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if(FlagTailVerteces.Count >= 3) // verteces should be at least 3 to form a triangle
            {
                gd.Textures[0] = ModContent.Request<Texture2D>(FLAG_TAIL_TEXTURE_PATH).Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, FlagTailVerteces.ToArray(), 0, FlagTailVerteces.Count - 2);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if(VertexDebug)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                int debugCount = Math.Min(debugUpperWorldPoints.Count, 20);
                for(int i = 0; i < debugCount; i++)
                {
                    // DrawDebugWorldLine(sb, pixel, debugSpinCenters[i], debugUpperWorldPoints[i], Color.Red, 2f);
                    // DrawDebugWorldLine(sb, pixel, debugUpperWorldPoints[i], debugLowerWorldPoints[i], Color.OrangeRed, 1f);

                    Vector2 textPos = debugUpperWorldPoints[i] - Main.screenPosition + new Vector2(8f, (i % 2 == 0) ? -16f : 2f);
                    string debugText = $"S:{debugStickOffsets[i],7:0.00} R:{debugOldRots[i],7:0.00}";
                    Utils.DrawBorderString(sb, debugText, textPos, Color.Red * 0.9f, 0.65f);
                }
            }

        }

        protected void PreDrawFlagClothVertices(ref Color lightColor, Vector2 ClothCenter)
        {
            Player player = Main.player[Projectile.owner];
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;

            List<Vertex> FlagClothVerteces = new List<Vertex>();

            float VertexStartX = Projectile.Center.X - 4 * Projectile.spriteDirection;
            float FlagTipY = Projectile.Center.Y - PoleLength/2f;
            Vector2 VertexStart = new Vector2(VertexStartX, FlagTipY);

            int resolution = 10;
            for(int i = 0; i <= FLAG_WIDTH + FLAG_WIDTH % resolution; i += resolution)
            {
                if(i > FLAG_WIDTH) i = FLAG_WIDTH;
                float ratio = (i) / (float)(FLAG_WIDTH);
                Vector2 UpperVerTexPos = VertexStart + new Vector2(-i * Projectile.spriteDirection, 0);
                Vector2 LowerVerTexPos = VertexStart + new Vector2(-i * Projectile.spriteDirection, FLAG_HEIGHT);
                FlagClothVerteces.Add(new Vertex(UpperVerTexPos - Main.screenPosition, new Vector3(1-ratio, 0, 1), lightColor));
                FlagClothVerteces.Add(new Vertex(LowerVerTexPos - Main.screenPosition, new Vector3(1-ratio, 1, 1), lightColor));
            }

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.Textures[0] = ModContent.Request<Texture2D>(FLAG_CLOTH_TEXTURE_PATH).Value;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, FlagClothVerteces.ToArray(), 0, FlagClothVerteces.Count - 2);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        protected void DrawPart(Texture2D texture, Vector2 worldPos, Rectangle rect, Color color, Vector2 origin)
        {
            Main.spriteBatch.Draw(
                texture,
                worldPos,
                rect,
                color,
                Projectile.rotation,
                origin,
                Projectile.scale,
                Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                0f
            );
        }

        protected void DrawDebugWorldLine(SpriteBatch sb, Texture2D pixel, Vector2 fromWorld, Vector2 toWorld, Color color, float thickness)
        {
            Vector2 fromScreen = fromWorld - Main.screenPosition;
            Vector2 toScreen = toWorld - Main.screenPosition;
            Vector2 edge = toScreen - fromScreen;
            float rotation = edge.ToRotation();
            float length = edge.Length();
            if(length <= 0.001f)
            {
                return;
            }

            // float angle = (float)Math.Atan2(edge.Y, edge.X);
            // sb.Draw(pixel, fromScreen, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
            sb.Draw(
                pixel,
                fromScreen,
                null,
                color,
                rotation,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f
            );
        }

        /* -------------------------- Other -------------------------- */

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int OnGroundCnt = timerPacker.Get(Projectile.ai[0], OnGroundCntBit);
            OnGroundCnt = OnGroundCnt >= 127 ? 127 : OnGroundCnt+1;
            Projectile.ai[0] = timerPacker.Set(Projectile.ai[0], OnGroundCntBit, OnGroundCnt);
            Projectile.velocity.X = 0f;
            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            height = PoleLength/3;
            fallThrough = false;
            // hitboxCenterFrac = new Vector2(0.5f, -(PoleLength / height-1) / 2f);
            hitboxCenterFrac = new Vector2(0.5f, (float)((height - PoleLength/2f) / height));
            return true;
        }

        public override void Kill(int timeLeft)
        {
            // Main.NewText("projectile kill triggered, timeLeft:"+timeLeft);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            if(State == RAISE_STATE || State == PLANT_STATE) return false;
            Vector2 PoleStart = Projectile.Center + new Vector2(0, PoleLength/2f).RotatedBy(Projectile.rotation);
            Vector2 PoleEnd = Projectile.Center + new Vector2(0, PoleLength/2f).RotatedBy(Projectile.rotation+Math.PI);
            // int CurrentTime = (int)(TIME_LEFT_WAVE / AttackSpeed) - Projectile.timeLeft;
            int CurrentTime = (int)player.itemAnimationMax - Projectile.timeLeft;
            if(State == RECALL_STATE) CurrentTime = TIME_LEFT_RECALL - Projectile.timeLeft;
            Vector2 PoleOldStart = PoleStart;
            Vector2 PoleOldEnd = PoleEnd;
            int buffer = 0;
            if(CurrentTime > buffer)
            {
                PoleOldStart = Projectile.oldPos[buffer] + new Vector2(0, PoleLength/2f).RotatedBy(Projectile.oldRot[buffer]) + new Vector2(0, PoleLength / 2f);
                PoleOldEnd = Projectile.oldPos[buffer] + new Vector2(0, PoleLength/2f).RotatedBy(Projectile.oldRot[buffer]+Math.PI) + new Vector2(0, PoleLength / 2f);
            }
            float collisionPoint = 0f;
            if(Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), PoleStart, PoleEnd, 10f, ref collisionPoint))
            {
                return true;
            }
            float factor1 = 0.75f;
            Vector2 PoleMidStart1 = factor1 * PoleStart + (1-factor1) * PoleOldStart;
            Vector2 PoleMidEnd1 = factor1 * PoleEnd + (1-factor1) * PoleOldEnd;
            if(Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), PoleMidStart1, PoleMidEnd1, 10f, ref collisionPoint))
            {
                return true;
            }
            float factor2 = 0.25f;
            Vector2 PoleMidStart2 = factor2 * PoleStart + (1-factor2) * PoleOldStart;
            Vector2 PoleMidEnd2 = factor2 * PoleEnd + (1-factor2) * PoleOldEnd;
            if(Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), PoleMidStart2, PoleMidEnd2, 10f, ref collisionPoint))
            {
                return true;
            }
            //  Dust.QuickDustLine(PoleStart, PoleEnd, 10f, Color.Red);
            //  Dust.QuickDustLine(PoleMidStart1, PoleMidEnd1, 10f, Color.Blue);
            //  Dust.QuickDustLine(PoleMidStart2, PoleMidEnd2, 10f, Color.Green);
            return false;
        }

        protected void DamageGrassAlongBlade(Player player)
        {
            if ((State == RAISE_STATE || State == PLANT_STATE) ||
                (State == WAVE_STATE && Projectile.timeLeft >= (int)(/* TIME_LEFT_WAVE / AttackSpeed */ player.itemAnimationMax) - 1) ||
                (State == RECALL_STATE && Projectile.timeLeft >= TIME_LEFT_RECALL - 1)) return;
            Vector2 CurrentPoleTip = Projectile.Center + new Vector2(0, PoleLength / 2f).RotatedBy(Projectile.rotation + Math.PI);
            Vector2 OldPoleTip = Projectile.oldPos[1] + new Vector2(0, PoleLength / 2f).RotatedBy(Projectile.oldRot[0] + Math.PI) + new Vector2(0, PoleLength / 2f);
            float x1 = CurrentPoleTip.X, y1 = CurrentPoleTip.Y;
            float x2 = OldPoleTip.X, y2 = OldPoleTip.Y;
            float x3 = player.Center.X, y3 = player.Center.Y;
            float SearchMaxX = (x1 > x2 && x3 > x2) ? x1 : (x2 > x3) ? x2 : x3;
            float SearchMinX = (x1 < x2 && x3 < x2) ? x1 : (x2 < x3) ? x2 : x3;
            float SearchMaxY = (y1 > y2 && y3 > y2) ? y1 : (y2 > y3) ? y2 : y3;
            float SearchMinY = (y1 < y2 && y3 < y2) ? y1 : (y2 < y3) ? y2 : y3;

            if (TILE_CUT_RANGE_DEBUG)
            {
                Dust.QuickDustLine(CurrentPoleTip, OldPoleTip, 10f, Color.Red);
                Dust.QuickDustLine(player.Center, CurrentPoleTip, 10f, Color.Green);
                Dust.QuickDustLine(player.Center, OldPoleTip, 10f, Color.Blue);
                Dust.QuickDust(Projectile.oldPos[1] + new Vector2(0, PoleLength / 2f), Color.Yellow);

                // Main.NewText("SearchMinX: " + SearchMinX + " SearchMaxX: " + SearchMaxX + " SearchMinY: " + SearchMinY + " SearchMaxY: " + SearchMaxY + " CurrentPoleTip: " + CurrentPoleTip + " OldPoleTip: " + OldPoleTip);
            }

            int cnt = 0;
            for (float x = SearchMinX; x <= SearchMaxX; x += 16f)
            {
                for (float y = SearchMinY; y <= SearchMaxY; y += 16f)
                {
                    int tileX = (int)(x / 16f);
                    int tileY = (int)(y / 16f);

                    // 检查 tile 是否存在且可被破坏
                    if (WorldGen.InWorld(tileX, tileY))
                    {
                        Tile tile = Framing.GetTileSafely(tileX, tileY);

                        cnt++;

                        Vector2 TileInWorld = new Vector2(x,y);

                        // 只破坏草、瓶子、罐子之类（frame重要）
                        if (tile.HasTile && Main.tileCut[tile.TileType])
                        {
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                WorldGen.KillTile(tileX, tileY, false, false, true);
                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, tileX, tileY);
                            }
                        }
                    }
                }
            }
            // Main.NewText("DamageGrassAlongBlade: " + cnt);
        }

        public override void CutTiles()
        {
            Player player = Main.player[Projectile.owner];
            if ((State == RAISE_STATE || State == PLANT_STATE) ||
                (State == WAVE_STATE && Projectile.timeLeft >= (int)(/* TIME_LEFT_WAVE / AttackSpeed */ player.itemAnimationMax) - 1) ||
                (State == RECALL_STATE && Projectile.timeLeft >= TIME_LEFT_RECALL - 1)) return;

            Vector2 CurrentPoleTip = Projectile.Center + new Vector2(0, PoleLength / 2f).RotatedBy(Projectile.rotation + Math.PI);
            Vector2 OldPoleTip = Projectile.oldPos[0] + new Vector2(0, PoleLength / 2f).RotatedBy(Projectile.oldRot[0] + Math.PI) + new Vector2(0, PoleLength / 2f);
            Vector2 MidPoleTip = (CurrentPoleTip + OldPoleTip) / 2f;
            Utils.PlotTileLine(player.Center, CurrentPoleTip, 10f, DelegateMethods.CutTiles);
            Utils.PlotTileLine(player.Center, MidPoleTip, 10f, DelegateMethods.CutTiles);

            if (TILE_CUT_RANGE_DEBUG)
            {
                Dust.QuickDustLine(CurrentPoleTip, OldPoleTip, 10f, Color.Red);
                Dust.QuickDustLine(player.Center, CurrentPoleTip, 10f, Color.Green);
                Dust.QuickDustLine(player.Center, MidPoleTip, 10f, Color.Blue);
                Dust.QuickDust(Projectile.oldPos[1] + new Vector2(0, PoleLength / 2f), Color.Yellow);

                // Main.NewText("SearchMinX: " + SearchMinX + " SearchMaxX: " + SearchMaxX + " SearchMinY: " + SearchMinY + " SearchMaxY: " + SearchMaxY + " CurrentPoleTip: " + CurrentPoleTip + " OldPoleTip: " + OldPoleTip);
            }
        }

        protected virtual void CreateDustEffect(Player player)
        {
            // create dust when using imbue flasks
            if (State == WAVE_STATE || State == RECALL_STATE)
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
        }
        
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(NPC_DEBUFF_ID, NPC_DEBUFF_DURATION);

            Player player = Main.player[Projectile.owner];
            player.MinionAttackTargetNPC = target.whoAmI;
            // player.HasMinionAttackTargetNPC = true;

            int ImbueDeBuffID = MinionAIHelper.GetImbueDebuff(player);
            if (ImbueDeBuffID != -1)
            {
                target.AddBuff(ImbueDeBuffID, 3 * 60);
            }
            if(MinionAIHelper.IsPartyImbue(player))
            {
                Projectile.NewProjectile(new EntitySource_Misc("WeaponEnchantment_Confetti"), target.Center.X, target.Center.Y, target.velocity.X, target.velocity.Y, 289, 0, 0f, player.whoAmI);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            modifiers.HitDirectionOverride = (target.Center - player.Center).X > 0 ? 1 : -1;

            int hitCount = timerPacker.Get(Projectile.ai[0],HitCountBit);

            float multiplier = (float)Math.Pow(DAMAGE_DECAY_FACTOR, hitCount);

            modifiers.FinalDamage *= multiplier;

            hitCount++;
            Projectile.ai[0] = timerPacker.Set(Projectile.ai[0],HitCountBit,hitCount);
        }

        public int GetCurrentState()
        {
            return timerPacker.Get(Projectile.ai[0], StateBit);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(PoleLength);
            writer.Write(TimeLeftRaise);
            writer.Write(AimAngle);

            writer.Write(CursorAssistedPlantPos.X);
            writer.Write(CursorAssistedPlantPos.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PoleLength = reader.ReadInt32();
            TimeLeftRaise = reader.ReadInt32();
            AimAngle = reader.ReadSingle();

            float CursorAssistedPlantPosX = reader.ReadSingle();
            float CursorAssistedPlantPosY = reader.ReadSingle();
            CursorAssistedPlantPos = new Vector2(CursorAssistedPlantPosX, CursorAssistedPlantPosY);
        }

    }
}
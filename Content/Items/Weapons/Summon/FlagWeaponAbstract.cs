using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria.DataStructures;
using SummonerExpansionMod.Content.Projectiles.Summon;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;
namespace SummonerExpansionMod.Content.Items.Weapons.Summon
{
    
    public abstract class FlagWeapon<T> : ModItem where T : FlagProjectile
    {
        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Items/FlagWeapon";
        protected int Direction = 1;
        protected uint LastShootTime = 0;
        protected bool IsRightPressed = false;
        protected Projectile FlagProjectile = null;
        protected int FlagProjectileId = -1;

        // state constants
        public const int IDLE_STATE = -1; // idle
        public const int WAVE_STATE = 0; // left-click: wave
        public const int RAISE_STATE = 1; // right-short-press: raise
        public const int PLANT_STATE = 2; // right-long-press: plant
        public const int RECALL_STATE = 3; // right-click after plant: recall

        protected int State = IDLE_STATE;

        public const int LEFT_KEY = 0;
        public const int RIGHT_KEY = 2;
        protected virtual float DIR_RESET_INTERVAL_FACTOR => 1.3f;
        protected virtual int WAVE_USE_TIME => 25+2;
        protected virtual int RAISE_USE_TIME => 60+2;
        protected virtual int MAX_RAISE_TIME => 55;
        protected virtual int ONGROUND_CNT_THRESHOLD => 45;
        protected virtual int MOD_PROJECTILE_ID => ModProjectileID.FlagProjectile;
        protected virtual int POLE_LENGTH => 280;
        protected virtual bool STATE_DEBUG => false;

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
            1 // SwitchFlag
        );

        //  protected NonUniformFloatIntPacker infoPacker = new NonUniformFloatIntPacker(
        // );

        protected const int InitializeFlagBit = 0;
        protected const int SentryRecallInitializeFlagBit = 1;
        protected const int HasSentryLockInSlotFlagBit = 2;
        protected const int CursorAssistingFlagBit = 3;
        protected const int SwitchFlagBit = 4;


        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = WAVE_USE_TIME;
            Item.damage = 40;
            Item.knockBack = 4f;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.useAnimation = WAVE_USE_TIME;
            Item.noMelee = true;
            Item.noUseGraphic = true; // 不绘制物品本体
            Item.autoReuse = true;
            Item.shoot = MOD_PROJECTILE_ID;
            Item.shootSpeed = 0f;

            // DynamicParamManager.Register("State Debug", 0f, 0f, 1f);
        }

        public override bool MeleePrefix() {
			return true;
		}

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            FlagProjectile = GetFlagProjectile();
            if (player.altFunctionUse == LEFT_KEY)
            {
                // if(FlagProjectile != null && FlagProjectile.active)
                // {
                //     FlagProjectile.Kill();
                //     FlagProjectile = null;
                //     FlagProjectileId = -1;
                // }
                uint CurTime = Main.GameUpdateCount;
                if(CurTime - LastShootTime < DIR_RESET_INTERVAL_FACTOR * Item.useAnimation)
                {
                    Direction = -Direction;
                }
                else
                {
                    Direction = 1;
                }
                LastShootTime = CurTime;
                KillExistingFlagProjectiles(player);
                FlagProjectile = GenerateFlagProjectile(player, source, position, velocity, type, damage, knockback);
                if (TryGet(FlagProjectile, out T flagPole))
                {
                    // flagPole.WaveDirection = Direction;
                    // flagPole.State = WAVE_STATE;
                    flagPole.PoleLength = POLE_LENGTH;
                }
                int DirectionEncode = Direction == 1 ? 1 : 0;
                FlagProjectile.ai[0] = timerPacker.Set(FlagProjectile.ai[0],WaveDirectionBit,DirectionEncode);
                FlagProjectile.ai[0] = timerPacker.Set(FlagProjectile.ai[0],StateBit,WAVE_STATE);
            }

            return false;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true; // 表示该物品支持右键使用
        }

        public static bool TryGet(Projectile proj, out T result)
        {
            if (proj.ModProjectile is T t && proj.active)
            {
                result = t;                
                return true;
            }
            result = null;
            return false;
        }

        protected Projectile GenerateFlagProjectile(Player player, IEntitySource source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI);

            FlagProjectileId = projectile.whoAmI;
            
            return projectile;
        }

        protected Projectile GetFlagProjectile()
        {
            Projectile FlagProjectile = null;
            if(FlagProjectileId != -1 && Main.projectile[FlagProjectileId].type == MOD_PROJECTILE_ID)
            {
                FlagProjectile = Main.projectile[FlagProjectileId];
            }
            return FlagProjectile;
        }

        protected void ResetFlagProjectile()
        {
            Projectile FlagProjectile = GetFlagProjectile();
            if(FlagProjectile != null && FlagProjectile.active && FlagProjectile.type == MOD_PROJECTILE_ID)
            {
                Item.NetStateChanged();
                FlagProjectile.Kill();
                FlagProjectile.netUpdate = true;
                FlagProjectileId = -1;
            }
        }

        protected void KillExistingFlagProjectiles(Player player)
        {
            FlagProjectile = GetFlagProjectile();
            foreach(Projectile proj in Main.projectile)
            {
                if (proj.active && 
                    proj.owner == player.whoAmI &&
                    proj.ModProjectile is FlagProjectile)
                {
                    proj.Kill();
                    proj.netUpdate = true;
                }
            }
        }

        public override void HoldItem(Player player)
        {
            FlagProjectile = GetFlagProjectile();
            // bool state_debug = DynamicParamManager.Get("State Debug").value > 0.5f;
            int State = IDLE_STATE;
            if(FlagProjectile != null && TryGet(FlagProjectile, out T flag))
            {
                State = flag.GetCurrentState();
            }
            bool state_debug = false;
            if(state_debug)
            {
                string StateStr = "";
                switch(State)
                {
                    case IDLE_STATE:
                        StateStr = "IDLE";
                        break;
                    case WAVE_STATE:
                        StateStr = "WAVE";
                        break;
                    case RAISE_STATE:
                        StateStr = "RAISE";
                        break;
                    case PLANT_STATE:
                        StateStr = "PLANT";
                        break;
                    case RECALL_STATE:
                        StateStr = "RECALL";
                        break;
                }
                float distance = FlagProjectile != null && FlagProjectile.active ? Vector2.Distance(player.Center, FlagProjectile.Center) : 0f;
                Main.NewText("State: " + StateStr + " Distance: " + distance);
                if(FlagProjectile != null && FlagProjectile.active)
                {
                    Dust.QuickDustLine(player.Center, FlagProjectile.Center, 10f, Color.White);
                }
            }
            if (player.controlUseTile) // right-click
            {
                switch(State)
                {
                    case IDLE_STATE:    // switch from idle state to raise state when right key pressed
                    {
                        Item.useStyle = ItemUseStyleID.Thrust;
                        Item.useTime = RAISE_USE_TIME;
                        Item.autoReuse = false;
                        Item.shoot = ProjectileID.None;
                        if(!IsRightPressed)
                        {
                            // Main.NewText("Raising");
                            // ResetFlagProjectile();

                            // kill existing flag projectiles
                            KillExistingFlagProjectiles(player);
                            
                            FlagProjectile = GenerateFlagProjectile(player, player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, MOD_PROJECTILE_ID, Item.damage, Item.knockBack);
                            // if (FlagProjectile.ModProjectile is FlagProjectile flagPole)
                            if(TryGet(FlagProjectile, out T flagPole))
                            {
                                // flagPole.State = RAISE_STATE;
                                // State = RAISE_STATE;
                                flagPole.PoleLength = POLE_LENGTH;
                                flagPole.TimeLeftRaise = (int)(RAISE_USE_TIME);
                            }
                            FlagProjectile.ai[0] = timerPacker.Set(FlagProjectile.ai[0],StateBit,RAISE_STATE);
                            IsRightPressed = true;
                            Item.NetStateChanged();
                        }
                    } break;
                    case WAVE_STATE:    // kill flag projectile in waving state when right key pressed
                    {
                        // ResetFlagProjectile();

                        goto case IDLE_STATE;
                    }
                    case RAISE_STATE:   // raise state maintains to MAX_RAISE_TIME
                    {
                    } break;
                    case PLANT_STATE:   // switch from plant state to recall state when right key pressed
                    {
                        // if(FlagProjectile.ModProjectile is FlagProjectile flagPole)
                        if (TryGet(FlagProjectile, out T flagPole))
                        {
                            int OnGroundCnt = timerPacker.Get(FlagProjectile.ai[0], OnGroundCntBit);
                            if (OnGroundCnt > ONGROUND_CNT_THRESHOLD)
                            {
                                // flagPole.SwitchFlag = true;
                                FlagProjectile.ai[1] = flagPacker.Set(FlagProjectile.ai[1], SwitchFlagBit, 1);
                                Item.NetStateChanged();
                            }
                        }
                    } break;
                    case RECALL_STATE:  // reset params to idle state
                    {
                        Item.useStyle = ItemUseStyleID.Swing;
                        Item.useTime = Item.useAnimation;
                        Item.autoReuse = true;
                        IsRightPressed = false;
                        if (FlagProjectile == null || (FlagProjectile != null && !FlagProjectile.active) || FlagProjectile.type != MOD_PROJECTILE_ID)
                        {
                            FlagProjectile = null;
                            FlagProjectileId = -1;
                        }
                    } break;
                    default:
                    {

                    } break;
                }
            }
            else
            {
                // left-click
                if(player.controlUseItem)
                {
                    if(State == IDLE_STATE)
                    {
                        Item.shoot = MOD_PROJECTILE_ID;
                    }
                    else if(State == PLANT_STATE)
                    {
                        // if(FlagProjectile.ModProjectile is FlagProjectile flagPole)
                    //     if (TryGet(FlagProjectile, out T flagPole))
                    //     {
                    //         int OnGroundCnt = timerPacker.Get(FlagProjectile.ai[0], OnGroundCntBit);
                    //         if (OnGroundCnt > ONGROUND_CNT_THRESHOLD)
                    //         {
                    //             // flagPole.SwitchFlag = true;
                    //             FlagProjectile.ai[1] = flagPacker.Set(FlagProjectile.ai[1], SwitchFlagBit, 1);
                    //             Item.NetStateChanged();
                    //         }
                    //     }
                    }
                }
                // no key pressed
                else
                {
                    if (State != PLANT_STATE && State != RECALL_STATE && State != WAVE_STATE)
                    {
                        // ResetFlagProjectile();
                    }
                }
                
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = Item.useAnimation;
                Item.autoReuse = true;
                IsRightPressed = false;
                if (FlagProjectile == null || (FlagProjectile != null && !FlagProjectile.active) || FlagProjectile.type != MOD_PROJECTILE_ID)
                {
                    FlagProjectile = null;
                    FlagProjectileId = -1;
                }
            }
        }
        
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Direction);
            writer.Write(LastShootTime);
            writer.Write(IsRightPressed);
            writer.Write(FlagProjectileId);
        }

        public override void NetReceive(BinaryReader reader)
        {
            Direction = reader.ReadInt32();
            LastShootTime = reader.ReadUInt32();
            IsRightPressed = reader.ReadBoolean();
            FlagProjectileId = reader.ReadInt32();
        }
    }
}

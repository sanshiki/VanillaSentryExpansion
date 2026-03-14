using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using SummonerExpansionMod.Content.Projectiles.Summon;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Items.Weapons.Summon
{
    public class RocketSentrySummon : ModItem
    {

        private const float THROW_SPEED =10f;

        public override string Texture => ModGlobal.MOD_TEXTURE_PATH + "Items/RocketSentrySummonV2";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;

            ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f; // The default value is 1, but other values are supported. See the docs for more guidance. 
        }

        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.knockBack = 8f;
            // Item.mana = 10; // mana cost
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Swing; // how the player's arm moves when using the item
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Lime;
            Item.UseSound = SoundID.Item1; // What sound should play when using the item

            // These below are needed for a minion weapon
            Item.noMelee = true; // this item doesn't do any melee damage
            Item.DamageType = DamageClass.Summon; 
            Item.shoot = ModContent.ProjectileType<HellpodSummonBall>();
            Item.shootSpeed = THROW_SPEED;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = player.MountedCenter;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
            projectile.originalDamage = Item.damage;

            if(projectile.ModProjectile is HellpodSummonBall summonBall)
            {
                summonBall.SetSummonTarget(ModProjectileID.RocketSentry);
            }

            // player.UpdateMaxTurrets();

            // Since we spawned the projectile manually already, we do not need the game to spawn it for ourselves anymore, so return false
            return false;
        }

        public override void AddRecipes() {
        	Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<AutocannonSentrySummon>(), 1);
            recipe.AddIngredient(ItemID.RocketLauncher, 1);
            recipe.AddIngredient(ItemID.EyeoftheGolem, 1);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.Register();
        }
    }
}
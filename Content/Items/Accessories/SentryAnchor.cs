using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ModLoader.IO;
using System.IO;
using Terraria.Localization;

using Microsoft.Xna.Framework.Graphics;
using SummonerExpansionMod.ModUtils;
using SummonerExpansionMod.Initialization;

namespace SummonerExpansionMod.Content.Items.Accessories
{
    public class SentryAnchor : ModItem
    {
        public bool Locked; // 是否锁定

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanRightClick() => true;

        public override bool ConsumeItem(Player player) => false;

        private const string LOCKED_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Items/SentryAnchorLocked";
        private const string RELEASED_TEXTURE_PATH = ModGlobal.MOD_TEXTURE_PATH + "Items/SentryAnchorReleased";

        public static LocalizedText TooltipLocked;
        public static LocalizedText TooltipUnlocked;

        public override string Texture => RELEASED_TEXTURE_PATH;

        public override void SetStaticDefaults()
        {
            TooltipLocked = Mod.GetLocalization("Items.SentryAnchor.Tooltip_Locked");
            TooltipUnlocked = Mod.GetLocalization("Items.SentryAnchor.Tooltip_Unlocked");
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.value = Item.sellPrice(silver: 30);
            Item.rare = ItemRarityID.Blue;
            Item.useStyle = ItemUseStyleID.HoldUp; // 关键！
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useTurn = false;
            Item.autoReuse = false;
        }
        

        public override bool CanUseItem(Player player)
        {
            // Main.NewText("altFunctionUse: " + player.altFunctionUse);
            if (player.altFunctionUse == 2)
            {
                Locked = !Locked;

                if (Main.myPlayer == player.whoAmI)
                {
                    // Main.NewText(Locked ? "哨兵锚已锁定" : "哨兵锚已解锁");

                    SoundEngine.PlaySound(
                        Locked ? SoundID.Unlock : SoundID.MenuTick,
                        player.Center
                    );
                }

                return false; // 不消耗、不触发其他行为
            }

            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            // 左键使用：清理该玩家召唤的所有哨兵
            if (player.altFunctionUse != 2)
            {
                if(Main.myPlayer == player.whoAmI)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Console.WriteLine("Projectile index: "+i);
                        Projectile projectile = Main.projectile[i];
                        Console.WriteLine("Projectile: "+projectile.whoAmI);
                        if (projectile.active && projectile.owner == player.whoAmI && projectile.sentry)
                        {
                            Console.WriteLine("Killing projectile: "+projectile.whoAmI);
                            projectile.Kill();
                            Console.WriteLine("Killed projectile: "+projectile.whoAmI);
                        }
                    }
                }

                SoundEngine.PlaySound(SoundID.Item4, player.Center);
            }

            return true;
        }

        public override void RightClick(Player player)
        {   
            Locked = !Locked;

            if (Main.myPlayer == player.whoAmI)
            {
                // Main.NewText(Locked ? "哨兵锚已锁定" : "哨兵锚已解锁");

                SoundEngine.PlaySound(
                    Locked ? SoundID.Unlock : SoundID.MenuTick,
                    player.Center
                );
            }
        }

        public override bool PreDrawInInventory(
            SpriteBatch spriteBatch,
            Vector2 position,
            Rectangle frame,
            Color drawColor,
            Color itemColor,
            Vector2 origin,
            float scale)
        {
            Texture2D LockedTexture = ModContent.Request<Texture2D>(LOCKED_TEXTURE_PATH).Value;
            Texture2D ReleasedTexture = ModContent.Request<Texture2D>(RELEASED_TEXTURE_PATH).Value;
            Texture2D tex = Locked
                ? LockedTexture
                : ReleasedTexture;

            spriteBatch.Draw(
                tex,
                position,
                null,
                drawColor,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );

            return false; // 阻止原版绘制
        }

        public override bool PreDrawInWorld(
            SpriteBatch spriteBatch,
            Color lightColor,
            Color alphaColor,
            ref float rotation,
            ref float scale,
            int whoAmI)
        {
            Texture2D LockedTexture = ModContent.Request<Texture2D>(LOCKED_TEXTURE_PATH).Value;
            Texture2D ReleasedTexture = ModContent.Request<Texture2D>(RELEASED_TEXTURE_PATH).Value;
            Texture2D tex = Locked
                ? LockedTexture
                : ReleasedTexture;

            Vector2 position = Item.position - Main.screenPosition +
                            Item.Size / 2f;

            spriteBatch.Draw(
                tex,
                position,
                null,
                lightColor,
                rotation,
                tex.Size() / 2f,
                scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }

        // —— 存档 & 同步（非常重要）——
        public override void SaveData(TagCompound tag)
            => tag["Locked"] = Locked;

        public override void LoadData(TagCompound tag)
            => Locked = tag.GetBool("Locked");

        public override void NetSend(BinaryWriter writer)
            => writer.Write(Locked);

        public override void NetReceive(BinaryReader reader)
            => Locked = reader.ReadBoolean();

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronBar, 12);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }

        public override void ModifyTooltips (List< TooltipLine > tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Tooltip",
            Locked ? TooltipLocked.Value : TooltipUnlocked.Value));
        }
    }

    public class SentryAnchorPlayer : ModPlayer
    {
        public bool HasLockedSentryAnchor;

        public override void ResetEffects()
        {
            HasLockedSentryAnchor = false;
        }

        public override void PostUpdate()
        {
            // 扫描背包
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                Item item = Player.inventory[i];

                if (item?.ModItem is SentryAnchor anchor && anchor.Locked)
                {
                    HasLockedSentryAnchor = true;
                    // Main.NewText("HasLockedSentryAnchor: " + HasLockedSentryAnchor);
                    break;
                }
            }
        }
    }
}
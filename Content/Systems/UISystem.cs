// using Terraria;
// using Terraria.ModLoader;
// using Terraria.UI;
// using System.Collections.Generic;
// using Microsoft.Xna.Framework;
// using SummonerExpansionMod.Content.UI;
// using System.IO;
// using Terraria.GameContent.UI.States;
// using Terraria.GameContent.UI.Elements;
// using Terraria.GameContent.UI.Chat;
// using SummonerExpansionMod;

// namespace SummonerExpansionMod.Content.Systems
// {
//     public class UISystem : ModSystem
//     {
//         internal static UserInterface dynamicParamManagerInterface;
//         internal static DynamicParamManagerUI dynamicParamManagerUI;

//         public override void Load()
//         {
//             if(!Main.dedServ)
//             {
//                 dynamicParamManagerUI = new DynamicParamManagerUI();
//                 dynamicParamManagerUI.Activate();
//                 dynamicParamManagerInterface = new UserInterface();
//             }
//         }

//         public override void UpdateUI(GameTime gameTime)
//         {
//             if(dynamicParamManagerUI.Visible)
//             {
//                 dynamicParamManagerInterface?.Update(gameTime);
//             }
//         }

//         public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
//         {
//             int inventoryLayerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
//             if (inventoryLayerIndex != -1) {
//                 layers.Insert(inventoryLayerIndex, new LegacyGameInterfaceLayer(
//                     "SummonerExpansionMod: Dynamic Param Manager UI",
//                     delegate {
//                         if (dynamicParamManagerUI?.Visible == true) {
//                             dynamicParamManagerInterface?.Draw(Main.spriteBatch, new GameTime());
//                         }
//                         return true;
//                     },
//                     InterfaceScaleType.UI)
//                 );
//             }
//         }

//         public override void PostUpdateInput() {
//             if (SummonerExpansionMod.ToggleDynamicParamManagerUI.JustPressed) {
//                 dynamicParamManagerUI.Visible = !dynamicParamManagerUI.Visible;
//                 if (dynamicParamManagerUI.Visible) {
//                     dynamicParamManagerUI.BuildPanel();
//                     dynamicParamManagerInterface?.SetState(dynamicParamManagerUI);
//                 } else {
//                     dynamicParamManagerInterface?.SetState(null);
//                 }
//             }
//         }
//     }
// }
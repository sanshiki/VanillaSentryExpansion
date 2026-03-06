using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;


using SummonerExpansionMod.Content.Projectiles.Summon;
using SummonerExpansionMod.Content.Buffs.Summon;

namespace SummonerExpansionMod.Content.Items.Weapons.Summon
{
    public class VanillaSummonStaffOverride : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.DD2BallistraTowerT1Popper)
            {
                item.damage = 35; 
            }

            if (item.type == ItemID.DD2BallistraTowerT2Popper)
            {
                item.damage = 50; 
            }

            if (item.type == ItemID.DD2BallistraTowerT3Popper)
            {
                item.damage = 65; 
            }

            if (item.type == ItemID.StaffoftheFrostHydra)
            {
                item.damage = 60;
            }
            if (item.type == ItemID.DD2FlameburstTowerT2Popper)
            {
                item.damage = 50;
            }

        }
    }


}
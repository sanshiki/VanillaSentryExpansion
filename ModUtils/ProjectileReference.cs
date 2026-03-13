using Terraria;
using System.IO;

namespace SummonerExpansionMod.ModUtils
{
    public struct ProjectileReference
    {
        public int Identity;
        public int WhoAmI;
        public int Owner;

        public void Clear()
        {
            Identity = -1;
            WhoAmI = -1;
            Owner = -1;
        }

        public ProjectileReference(Projectile projectile)
        {
            Set(projectile);
        }

        public void Set(Projectile projectile)
        {
            Identity = projectile.identity;
            WhoAmI = projectile.whoAmI;
            Owner = projectile.owner;
        }

        public bool IsValid()
        {
            return Identity >= 0 && WhoAmI >= 0;
        }

        public Projectile Get()
        {
            // 1️⃣ 先尝试缓存命中
            if (Main.projectile.IndexInRange(WhoAmI))
            {
                Projectile cached = Main.projectile[WhoAmI];

                if (cached.active && cached.identity == Identity && cached.owner == Owner)
                {
                    // Main.NewText("Projectile cached hit: "+Identity+" "+WhoAmI+" "+cached.type);
                    return cached;
                }
            }

            // 2️⃣ fallback 查找
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];

                if (proj.active && proj.identity == Identity && proj.owner == Owner)
                {
                    WhoAmI = i; // 更新缓存
                    // Main.NewText("Projectile fallback hit: "+Identity+" "+WhoAmI+" "+proj.type);
                    return proj;
                }
            }

            // Main.NewText("Projectile not found: "+Identity);
            return null;
        }

        public void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Identity);
            writer.Write(WhoAmI);
            writer.Write(Owner);
        }

        public void ReceiveExtraAI(BinaryReader reader)
        {
            Identity = reader.ReadInt32();
            WhoAmI = reader.ReadInt32();
            Owner = reader.ReadInt32();
        }
    }
}
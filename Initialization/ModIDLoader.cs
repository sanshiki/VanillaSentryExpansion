using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;
using Terraria;
using SummonerExpansionMod.Content.Projectiles.Summon;
using SummonerExpansionMod.Content.Buffs.Summon;

namespace SummonerExpansionMod.Initialization
{
    public class ModIDLoader
    {
        public static void Load()
        {
			/* ------------------------------- projectiles ------------------------------- */
            // sentries
			ModProjectileID.DarkMagicTower = ModContent.ProjectileType<DarkMagicTower>();
			ModProjectileID.BunnySentry = ModContent.ProjectileType<BunnySentry>();
			ModProjectileID.HoneyCombSentry = ModContent.ProjectileType<HoneyCombSentry>();
			ModProjectileID.TowerOfDryadsBlessing = ModContent.ProjectileType<TowerOfDryadsBlessing>();
			ModProjectileID.IchorPressorSentry = ModContent.ProjectileType<IchorPressorSentry>();
			ModProjectileID.CursedFireExtractorSentry = ModContent.ProjectileType<CursedFireExtractorSentry>();
			ModProjectileID.SandustSentry = ModContent.ProjectileType<SandustSentry>();
			ModProjectileID.MachineGunSentry = ModContent.ProjectileType<MachineGunSentry>();
			ModProjectileID.RocketSentry = ModContent.ProjectileType<RocketSentry>();
			ModProjectileID.GatlingSentry = ModContent.ProjectileType<GatlingSentry>();
			ModProjectileID.AutocannonSentry = ModContent.ProjectileType<AutocannonSentry>();
			ModProjectileID.TempleSentry = ModContent.ProjectileType<TempleSentry>();
			ModProjectileID.MothronQueenTurret = ModContent.ProjectileType<MothronQueenTurret>();
			// sentry projectiles
			ModProjectileID.BunnySentryBullet = ModContent.ProjectileType<BunnySentryBullet>();
			ModProjectileID.AntlionSentryBullet = ModContent.ProjectileType<AntlionSentryBullet>();
			ModProjectileID.TowerOfDryadsBlessingProjectile = ModContent.ProjectileType<TowerOfDryadsBlessingProjectile>();
			ModProjectileID.MachineGunSentryBullet = ModContent.ProjectileType<MachineGunSentryBullet>();
			ModProjectileID.HoneyCombSentryBullet = ModContent.ProjectileType<HoneyCombSentryBullet>();
			ModProjectileID.HoneyCombSentryGiantBullet = ModContent.ProjectileType<HoneyCombSentryGiantBullet>();
			ModProjectileID.DarkMagicTowerBullet = ModContent.ProjectileType<DarkMagicTowerBullet>();
			ModProjectileID.IchorPressorSentryBullet = ModContent.ProjectileType<IchorPressorSentryBullet>();
			ModProjectileID.CursedFireExtractorSentryBullet = ModContent.ProjectileType<CursedFireExtractorSentryBullet>();
			ModProjectileID.SandustSentryBullet = ModContent.ProjectileType<SandustSentryBullet>();
			ModProjectileID.SandustSentrySandnadoFriendly = ModContent.ProjectileType<SandustSentrySandnadoFriendly>();
			ModProjectileID.CursedMagicTowerBulletSmall = ModContent.ProjectileType<CursedMagicTowerBulletSmall>();
			ModProjectileID.CursedMagicTowerBulletSphere = ModContent.ProjectileType<CursedMagicTowerBulletSphere>();
			ModProjectileID.RocketSentryBullet = ModContent.ProjectileType<RocketSentryBullet>();
			ModProjectileID.AutocannonSentryBullet = ModContent.ProjectileType<AutocannonSentryBullet>();
			ModProjectileID.MechEyeballTurretEyeFire = ModContent.ProjectileType<MechEyeballTurretEyeFire>();
			ModProjectileID.TempleSentryHeatRay = ModContent.ProjectileType<TempleSentryHeatRay>();
			ModProjectileID.StardustSentrySignal = ModContent.ProjectileType<StardustSentrySignal>();
			ModProjectileID.StardustSentryBullet = ModContent.ProjectileType<StardustSentryBullet>();
			ModProjectileID.TempleSentryEyeBeamBullet = ModContent.ProjectileType<TempleSentryEyeBeamBullet>();
			ModProjectileID.MothronQueenTurretBullet = ModContent.ProjectileType<MothronQueenTurretBullet>();
			ModProjectileID.MothronBabyFriendly = ModContent.ProjectileType<MothronBabyFriendly>();
			// minion
			// ModProjectileID.SentryPlatform = ModContent.ProjectileType<SentryPlatform>();

			// flag weapon
			ModProjectileID.FlagProjectile = ModContent.ProjectileType<FlagProjectile>();
			ModProjectileID.PirateFlagProjectile = ModContent.ProjectileType<PirateFlagProjectile>();
			ModProjectileID.NormalFlagProjectile = ModContent.ProjectileType<NormalFlagProjectile>();
			ModProjectileID.GoblinFlagProjectile = ModContent.ProjectileType<GoblinFlagProjectile>();
			ModProjectileID.HellFlagProjectile = ModContent.ProjectileType<HellFlagProjectile>();
			ModProjectileID.HolyFlagProjectile = ModContent.ProjectileType<HolyFlagProjectile>();
			ModProjectileID.TikiFlagProjectile = ModContent.ProjectileType<TikiFlagProjectile>();
			ModProjectileID.TikiFlagBladeShot = ModContent.ProjectileType<TikiFlagBladeShot>();
			ModProjectileID.GiantLeavesOfPlanteraProjectile = ModContent.ProjectileType<GiantLeavesOfPlanteraProjectile>();
			ModProjectileID.GiantLeavesOfPlanteraBullet1 = ModContent.ProjectileType<GiantLeavesOfPlanteraBullet1>();
			ModProjectileID.GiantLeavesOfPlanteraBullet2 = ModContent.ProjectileType<GiantLeavesOfPlanteraBullet2>();
			ModProjectileID.SantaFlagProjectile = ModContent.ProjectileType<SantaFlagProjectile>();
			ModProjectileID.SantaFlagBladeShot = ModContent.ProjectileType<SantaFlagBladeShot>();
			ModProjectileID.OneTrueFlagProjectile = ModContent.ProjectileType<OneTrueFlagProjectile>();
			ModProjectileID.OneTrueFlagBladeShot = ModContent.ProjectileType<OneTrueFlagBladeShot>();
			ModProjectileID.OneTrueFlagAnchor = ModContent.ProjectileType<OneTrueFlagAnchor>();
			ModProjectileID.HellFlagAnchor = ModContent.ProjectileType<HellFlagAnchor>();
			ModProjectileID.GiantLeavesOfPlanteraAnchor = ModContent.ProjectileType<GiantLeavesOfPlanteraAnchor>();
			/* ------------------------------- buffs ------------------------------- */
			ModBuffID.MothronDustDebuff = ModContent.BuffType<MothronDustDebuff>();
			ModBuffID.NormalFlagBuff = ModContent.BuffType<NormalFlagBuff>();
			ModBuffID.GoblinFlagBuff = ModContent.BuffType<GoblinFlagBuff>();
			ModBuffID.HellFlagBuff = ModContent.BuffType<HellFlagBuff>();
			ModBuffID.PirateFlagBuff = ModContent.BuffType<PirateFlagBuff>();
			ModBuffID.PirateFlagDebuff = ModContent.BuffType<PirateFlagDebuff>();
			ModBuffID.HolyFlagBuff = ModContent.BuffType<HolyFlagBuff>();
			ModBuffID.TikiFlagBuff = ModContent.BuffType<TikiFlagBuff>();
			ModBuffID.TikiFlagDebuff = ModContent.BuffType<TikiFlagDebuff>();
			ModBuffID.SantaFlagBuff = ModContent.BuffType<SantaFlagBuff>();
			ModBuffID.OneTrueFlagBuff = ModContent.BuffType<OneTrueFlagBuff>();
			ModBuffID.OneTrueFlagDebuff = ModContent.BuffType<OneTrueFlagDebuff>();
        }

		
    }
}
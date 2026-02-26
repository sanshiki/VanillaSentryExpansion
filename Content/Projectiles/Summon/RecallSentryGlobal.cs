using Microsoft.Xna.Framework;
using SummonerExpansionMod.Initialization;
using SummonerExpansionMod.ModUtils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.IO;

namespace SummonerExpansionMod.Content.Projectiles.Summon
{
    public interface IRecallSentryAnchor
    {
        void Configure(ProjectileReference sentryRef, Vector2 targetPos, bool originalTileCollide);
    }

    public struct SentryRecallCommand
    {
        public Vector2 TargetPos;
        public float RecallSpeed;
        public float RecallThreshold;
        public float RecallDecayDist;
        public bool UseAnchorRecall;
        public int AnchorProjectileType;
        public bool DisableTileCollideWhileRecalling;
    }

    public class RecallSentryGlobal : GlobalProjectile
    {
        private const bool DEBUG_RECALL_SYNC = false;

        public override bool InstancePerEntity => true;

        public bool RecallActive;
        public bool RecallCompleted;
        public bool OriginalTileCollide;
        public bool DisableTileCollideWhileRecalling;
        public bool UseAnchorRecall;
        public bool AnchorSpawned;
        public ProjectileReference AnchorReference;
        public int AnchorProjectileType = -1;
        public Vector2 TargetPos;
        public float RecallSpeed;
        public float RecallThreshold;
        public float RecallDecayDist;
        private bool LoggedIssue;
        private bool LoggedAnchorSpawn;
        private bool LoggedAnchorCompleted;
        private bool LoggedNormalCompleted;

        private void LogDebug(string message)
        {
            if (!DEBUG_RECALL_SYNC)
            {
                return;
            }
            Mod.Logger.Info($"[RecallSentryGlobal] {message}");
        }

        public static void IssueRecallCommand(Projectile sentry, SentryRecallCommand command)
        {
            if (sentry == null || !sentry.active)
            {
                return;
            }

            RecallSentryGlobal recallGlobal = sentry.GetGlobalProjectile<RecallSentryGlobal>();
            recallGlobal.RecallActive = true;
            recallGlobal.RecallCompleted = false;
            recallGlobal.AnchorSpawned = false;
            recallGlobal.AnchorReference.Clear();
            recallGlobal.OriginalTileCollide = sentry.tileCollide;
            recallGlobal.DisableTileCollideWhileRecalling = command.DisableTileCollideWhileRecalling;
            recallGlobal.UseAnchorRecall = command.UseAnchorRecall;
            recallGlobal.AnchorProjectileType = command.AnchorProjectileType;
            recallGlobal.TargetPos = command.TargetPos;
            recallGlobal.RecallSpeed = command.RecallSpeed;
            recallGlobal.RecallThreshold = command.RecallThreshold;
            recallGlobal.RecallDecayDist = command.RecallDecayDist;
            recallGlobal.LoggedIssue = false;
            recallGlobal.LoggedAnchorSpawn = false;
            recallGlobal.LoggedAnchorCompleted = false;
            recallGlobal.LoggedNormalCompleted = false;

            if (recallGlobal.DisableTileCollideWhileRecalling)
            {
                sentry.tileCollide = false;
            }

            if (!recallGlobal.LoggedIssue)
            {
                recallGlobal.LogDebug(
                    $"Issue whoAmI={sentry.whoAmI} identity={sentry.identity} owner={sentry.owner} mode={Main.netMode} " +
                    $"useAnchor={command.UseAnchorRecall} anchorType={command.AnchorProjectileType} tile={recallGlobal.OriginalTileCollide}->{sentry.tileCollide} target={command.TargetPos}");
                recallGlobal.LoggedIssue = true;
            }

            sentry.netUpdate = true;
        }

        public override void AI(Projectile projectile)
        {
            if (!projectile.active || !projectile.sentry || !RecallActive || RecallCompleted)
            {
                return;
            }

            // Server/Singleplayer authoritative recall logic, clients only receive state.
            if (projectile.owner != Main.myPlayer)
            {
                return;
            }

            if (UseAnchorRecall && AnchorProjectileType > -1)
            {
                if (!AnchorSpawned)
                {
                    AnchorSpawned = true;
                    int anchorWhoAmI = Projectile.NewProjectile(
                        projectile.GetSource_FromAI(),
                        TargetPos,
                        Vector2.Zero,
                        AnchorProjectileType,
                        projectile.damage,
                        projectile.knockBack,
                        projectile.owner
                    );

                    if (Main.projectile.IndexInRange(anchorWhoAmI))
                    {
                        Projectile anchor = Main.projectile[anchorWhoAmI];
                        AnchorReference.Set(anchor);
                        if (anchor.ModProjectile is IRecallSentryAnchor recallAnchor)
                        {
                            recallAnchor.Configure(new ProjectileReference(projectile), TargetPos, OriginalTileCollide);
                            anchor.netUpdate = true;
                            if (!LoggedAnchorSpawn)
                            {
                                LogDebug(
                                    $"SpawnAnchor sentryWho={projectile.whoAmI} sentryId={projectile.identity} anchorWho={anchor.whoAmI} anchorType={AnchorProjectileType} " +
                                    $"owner={projectile.owner} mode={Main.netMode} target={TargetPos} tile={OriginalTileCollide}");
                                LoggedAnchorSpawn = true;
                            }
                        }
                    }
                    projectile.netUpdate = true;
                }
                else
                {
                    Projectile anchor = AnchorReference.Get();
                    if (anchor == null || !anchor.active)
                    {
                        projectile.Center = TargetPos + new Vector2(0, -projectile.height * 0.5f);
                        projectile.velocity = OriginalTileCollide ? new Vector2(0, 20f) : Vector2.Zero;
                        projectile.tileCollide = OriginalTileCollide;
                        RecallCompleted = true;
                        RecallActive = false;
                        if (!LoggedAnchorCompleted)
                        {
                            LogDebug(
                                $"CompleteAnchor whoAmI={projectile.whoAmI} identity={projectile.identity} owner={projectile.owner} mode={Main.netMode} " +
                                $"target={TargetPos} tile={OriginalTileCollide}");
                            LoggedAnchorCompleted = true;
                        }
                        projectile.netUpdate = true;
                    }
                }
                return;
            }

            Vector2 toTarget = TargetPos - projectile.Center;
            if (toTarget.Length() >= RecallThreshold)
            {
                Vector2 toTargetDir = toTarget.SafeNormalize(Vector2.UnitX);
                float decayFactor = MathHelper.Clamp(toTarget.Length() / RecallDecayDist, 0.1f, 1f);
                projectile.velocity = toTargetDir * RecallSpeed * decayFactor;
                projectile.netUpdate = true;
                return;
            }

            if (DisableTileCollideWhileRecalling)
            {
                projectile.tileCollide = OriginalTileCollide;
            }

            RecallCompleted = true;
            RecallActive = false;
            if (!LoggedNormalCompleted)
            {
                LogDebug(
                    $"CompleteNormal whoAmI={projectile.whoAmI} identity={projectile.identity} owner={projectile.owner} mode={Main.netMode} target={TargetPos}");
                LoggedNormalCompleted = true;
            }
            projectile.netUpdate = true;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(RecallActive);
            bitWriter.WriteBit(RecallCompleted);
            bitWriter.WriteBit(OriginalTileCollide);
            bitWriter.WriteBit(DisableTileCollideWhileRecalling);
            bitWriter.WriteBit(UseAnchorRecall);
            bitWriter.WriteBit(AnchorSpawned);
            AnchorReference.SendExtraAI(binaryWriter);
            binaryWriter.Write(AnchorProjectileType);
            binaryWriter.Write(TargetPos.X);
            binaryWriter.Write(TargetPos.Y);
            binaryWriter.Write(RecallSpeed);
            binaryWriter.Write(RecallThreshold);
            binaryWriter.Write(RecallDecayDist);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            RecallActive = bitReader.ReadBit();
            RecallCompleted = bitReader.ReadBit();
            OriginalTileCollide = bitReader.ReadBit();
            DisableTileCollideWhileRecalling = bitReader.ReadBit();
            UseAnchorRecall = bitReader.ReadBit();
            AnchorSpawned = bitReader.ReadBit();
            AnchorReference.ReceiveExtraAI(binaryReader);
            AnchorProjectileType = binaryReader.ReadInt32();
            float targetX = binaryReader.ReadSingle();
            float targetY = binaryReader.ReadSingle();
            TargetPos = new Vector2(targetX, targetY);
            RecallSpeed = binaryReader.ReadSingle();
            RecallThreshold = binaryReader.ReadSingle();
            RecallDecayDist = binaryReader.ReadSingle();
        }
    }
}

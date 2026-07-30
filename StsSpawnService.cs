using System;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace STSFifth
{
    public sealed class StsSpawnService
    {
        private const string LogPrefix = "[STSFifth]";
        private const float DefaultRotation = 0f;
        private const float MaximumCoordinateAbs = 10000f;
        private const float ZeroPointToleranceSqr = 0.01f;
        private const float GroundProbeUpDistance = 4f;
        private const float GroundProbeDownDistance = 24f;
        private const float GroundNormalMinY = 0.65f;
        private const float GroundOffset = 0.08f;
        private const float CapsuleRadius = 0.35f;
        private const float CapsuleHeight = 1.8f;
        private const float MinimumPlayerSpacing = 1.1f;
        private const float GoldenAngleRadians = 2.39996323f;

        private readonly StsConfig config;

        public StsSpawnService(StsConfig config)
        {
            this.config = config ?? StsConfig.CreateDefault();
        }

        internal List<StsSpawnPoint> CreateSpawnPlans(int requestedCount)
        {
            List<StsSpawnPoint> plans = new List<StsSpawnPoint>();
            if (requestedCount <= 0)
            {
                return plans;
            }

            if (config.Spawn != null && config.Spawn.UseConfiguredPosition)
            {
                Vector3 configuredPosition = ToVector3(config.Spawn.Position);
                TryAppendSafePlans(configuredPosition, DefaultRotation, "配置坐标", requestedCount, plans);

                if (plans.Count >= requestedCount)
                {
                    return plans;
                }

                Logger.Warn(
                    $"{LogPrefix} 配置生成点只解析出 {plans.Count}/{requestedCount} 个安全落点，将继续尝试默认地表 MTF 生成点。");
            }

            if (TryGetDefaultMtfSpawnpoint(out Vector3 defaultPosition, out float defaultRotation))
            {
                TryAppendSafePlans(defaultPosition, defaultRotation, "默认地表 MTF 生成点", requestedCount, plans);
            }
            else
            {
                Logger.Warn($"{LogPrefix} 无法从 RoleSpawnpointManager 获取默认地表 MTF 生成点。");
            }

            if (plans.Count <= 0)
            {
                Logger.Error($"{LogPrefix} 未解析到任何安全第五特别行动组生成点，本次成员将保留原生承载角色出生位置。");
            }
            else if (plans.Count < requestedCount)
            {
                Logger.Warn(
                    $"{LogPrefix} 安全第五特别行动组生成点不足：安全点={plans.Count}，需要={requestedCount}。剩余成员将保留原生承载角色出生位置。");
            }

            return plans;
        }

        internal List<StsSpawnPoint> GetEscapeZoneSpawnPlans(int requestedCount)
        {
            List<StsSpawnPoint> plans = new List<StsSpawnPoint>();
            if (requestedCount <= 0)
            {
                return plans;
            }

            try
            {
                Bounds escapeZone = Escape.DefaultEscapeZone;
                Vector3 centerPosition = escapeZone.center;

                TryAppendSafePlans(centerPosition, DefaultRotation, "逃生区中心", requestedCount, plans);

                if (plans.Count <= 0)
                {
                    Logger.Error($"{LogPrefix} 未能从逃生区解析到任何安全落点，爆炸传送将使用逃生区中心原点。");
                    plans.Add(new StsSpawnPoint(centerPosition, DefaultRotation, "逃生区中心（未校验）"));
                }
                else if (plans.Count < requestedCount)
                {
                    Logger.Warn($"{LogPrefix} 逃生区安全落点不足：安全点={plans.Count}，需要={requestedCount}。");
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"{LogPrefix} 逃生区传送点解析失败，将使用原点回退。错误：{exception.Message}");
                plans.Add(new StsSpawnPoint(Vector3.zero, DefaultRotation, "Vector3.zero 回退点"));
            }

            return plans;
        }

        private void TryAppendSafePlans(
            Vector3 basePosition,
            float horizontalRotation,
            string source,
            int requestedCount,
            List<StsSpawnPoint> plans)
        {
            int beforeCount = plans.Count;
            if (!IsReasonableWorldPosition(basePosition, out string invalidReason))
            {
                Logger.Warn($"{LogPrefix} {source} 不可用：{invalidReason}。坐标={FormatVector(basePosition)}");
                return;
            }

            try
            {
                foreach (Vector3 offset in BuildOffsetCandidates(requestedCount))
                {
                    if (plans.Count >= requestedCount)
                    {
                        break;
                    }

                    Vector3 candidate = basePosition + offset;
                    if (!TryResolveSafeLanding(candidate, out Vector3 landing, out _))
                    {
                        continue;
                    }

                    if (!IsFarEnoughFromExistingPlans(landing, plans))
                    {
                        continue;
                    }

                    plans.Add(new StsSpawnPoint(landing, horizontalRotation, source));
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} {source} 安全落点解析发生异常，将跳过该来源。错误：{exception.Message}");
            }

            int addedCount = plans.Count - beforeCount;
            if (addedCount > 0)
            {
                Logger.Info(
                    $"{LogPrefix} {source} 已解析出 {addedCount} 个安全第五特别行动组生成点，累计={plans.Count}/{requestedCount}。");
            }
            else
            {
                Logger.Warn($"{LogPrefix} {source} 未能解析出安全第五特别行动组生成点。基础坐标={FormatVector(basePosition)}");
            }
        }

        private bool TryResolveSafeLanding(Vector3 candidate, out Vector3 landing, out string reason)
        {
            landing = Vector3.zero;
            if (!IsReasonableWorldPosition(candidate, out reason))
            {
                return false;
            }

            Vector3 rayStart = candidate + Vector3.up * GroundProbeUpDistance;
            float rayDistance = GroundProbeUpDistance + GroundProbeDownDistance;
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                reason = "向下射线未找到地面";
                return false;
            }

            if (hit.normal.y < GroundNormalMinY)
            {
                reason = $"地面坡度过大，法线Y={hit.normal.y:0.00}";
                return false;
            }

            landing = hit.point + Vector3.up * GroundOffset;
            if (!IsReasonableWorldPosition(landing, out reason))
            {
                return false;
            }

            if (!HasBodyClearance(landing, out reason))
            {
                return false;
            }

            return true;
        }

        private static bool HasBodyClearance(Vector3 landing, out string reason)
        {
            Vector3 capsuleBottom = landing + Vector3.up * (CapsuleRadius + GroundOffset);
            Vector3 capsuleTop = landing + Vector3.up * (CapsuleHeight - CapsuleRadius);

            if (Physics.CheckCapsule(capsuleBottom, capsuleTop, CapsuleRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                reason = "玩家身体胶囊空间被碰撞体占用";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private IEnumerable<Vector3> BuildOffsetCandidates(int requestedCount)
        {
            yield return Vector3.zero;

            float spreadRadius = config.Spawn == null ? new StsSpawnConfig().SpreadRadius : config.Spawn.SpreadRadius;
            spreadRadius = Math.Max(MinimumPlayerSpacing, spreadRadius);
            int sampleCount = Math.Max(24, requestedCount * 12);

            for (int ring = 1; ring <= 3; ring++)
            {
                float radius = Math.Max(MinimumPlayerSpacing, spreadRadius * ring / 3f);
                for (int i = 0; i < sampleCount; i++)
                {
                    float angle = (i * GoldenAngleRadians) + (ring * 0.41f);
                    yield return new Vector3((float)Math.Cos(angle) * radius, 0f, (float)Math.Sin(angle) * radius);
                }
            }
        }

        private static bool TryGetDefaultMtfSpawnpoint(out Vector3 position, out float horizontalRotation)
        {
            position = Vector3.zero;
            horizontalRotation = DefaultRotation;

            try
            {
                if (!RoleSpawnpointManager.TryGetSpawnpointForRole(RoleTypeId.NtfPrivate, out ISpawnpointHandler spawnpointHandler) ||
                    spawnpointHandler == null)
                {
                    return false;
                }

                return spawnpointHandler.TryGetSpawnpoint(out position, out horizontalRotation);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 获取默认地表 MTF 生成点时发生异常：{exception.Message}");
                return false;
            }
        }

        private static bool IsFarEnoughFromExistingPlans(Vector3 position, IEnumerable<StsSpawnPoint> plans)
        {
            float minimumDistanceSqr = MinimumPlayerSpacing * MinimumPlayerSpacing;
            foreach (StsSpawnPoint plan in plans)
            {
                Vector3 delta = position - plan.Position;
                delta.y = 0f;
                if (delta.sqrMagnitude < minimumDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsReasonableWorldPosition(Vector3 position, out string reason)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                reason = "坐标包含 NaN 或 Infinity";
                return false;
            }

            if (Math.Abs(position.x) > MaximumCoordinateAbs ||
                Math.Abs(position.y) > MaximumCoordinateAbs ||
                Math.Abs(position.z) > MaximumCoordinateAbs)
            {
                reason = $"坐标绝对值超过 {MaximumCoordinateAbs}";
                return false;
            }

            if (position.sqrMagnitude <= ZeroPointToleranceSqr)
            {
                reason = "坐标接近 Vector3.zero，疑似非法回退点";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static Vector3 ToVector3(StsVector3Config position)
        {
            return position == null ? Vector3.zero : new Vector3(position.X, position.Y, position.Z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string FormatVector(Vector3 position)
        {
            return $"({position.x:0.00}, {position.y:0.00}, {position.z:0.00})";
        }
    }

    internal sealed class StsSpawnPoint
    {
        public StsSpawnPoint(Vector3 position, float horizontalRotation, string source)
        {
            Position = position;
            HorizontalRotation = horizontalRotation;
            Source = source ?? string.Empty;
        }

        public Vector3 Position { get; }

        public float HorizontalRotation { get; }

        public string Source { get; }
    }
}

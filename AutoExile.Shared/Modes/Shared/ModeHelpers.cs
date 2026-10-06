using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using AutoExile.Systems;
using System.Numerics;

namespace AutoExile.Modes.Shared
{
    /// <summary>
    /// 파밍 모드 전반에서 공유하는 정적 유틸리티 모음입니다.
    /// </summary>
    public static class ModeHelpers
    {
        /// <summary>
        /// 가장 적합한(타겟 가능한) 귀환 포탈 엔티티를 찾습니다.
        /// 그리드 Y값이 가장 낮은(아이소메트릭 화면상 남쪽, 지도 장치를 가리지 않는) 포탈을 우선합니다.
        /// 지도 장치를 시각적으로 가리는 포탈을 피하기 위함입니다.
        /// </summary>
        public static Entity? FindNearestPortal(GameController gc)
        {
            Entity? best = null;
            float bestY = float.MaxValue;
            foreach (var entity in gc.EntityListWrapper.OnlyValidEntities)
            {
                if (!entity.IsTargetable) continue;
                // 일반 귀환 포탈(TownPortal) 또는 Effect 타입 MTX 포탈(예: Black Barya's SandHourglass)
                var isTownPortal = entity.Type == EntityType.TownPortal;
                var isMtxPortal = entity.Path.Contains("Town_Portals", StringComparison.OrdinalIgnoreCase);
                if (!isTownPortal && !isMtxPortal) continue;
                if (entity.GridPosNum.Y < bestY)
                {
                    bestY = entity.GridPosNum.Y;
                    best = entity;
                }
            }
            return best;
        }

        /// <summary>
        /// WorldToScreen → 창 오프셋 적용 → BotInput.Click. 성공 시 lastActionTime을 갱신합니다.
        /// </summary>
        public static bool ClickEntity(GameController gc, Entity entity, ref DateTime lastActionTime)
        {
            if (!BotInput.CanAct) return false;
            if (!BotInput.ClickEntity(gc, entity)) return false;
            lastActionTime = DateTime.Now;
            return true;
        }

        /// <summary>
        /// BotInput 게이트 + 쿨다운 체크.
        /// </summary>
        public static bool CanAct(DateTime lastActionTime, float cooldownMs)
        {
            return BotInput.CanAct &&
                   (DateTime.Now - lastActionTime).TotalMilliseconds >= cooldownMs;
        }

        /// <summary>
        /// DefaultPositioning 설정값을 파싱해 해당 프로필로 전투를 활성화합니다.
        /// </summary>
        public static void EnableDefaultCombat(BotContext ctx)
        {
            var positioning = Enum.TryParse<CombatPositioning>(ctx.Settings.Build.DefaultPositioning.Value, out var pos)
                ? pos : CombatPositioning.Aggressive;
            ctx.Combat.SetProfile(new CombatProfile
            {
                Enabled = true,
                Positioning = positioning,
            });
        }

        /// <summary>
        /// StashSystem.HasInventoryItems에 대한 래퍼입니다.
        /// </summary>
        public static bool HasInventoryItems(GameController gc) => StashSystem.HasInventoryItems(gc);

        /// <summary>
        /// MapDevice + Stash + Interaction 시스템을 취소하고 눌려있는 키를 모두 해제합니다.
        /// 지역 변경 및 모드 전환 시 호출됩니다.
        /// </summary>
        public static void CancelAllSystems(BotContext ctx)
        {
            var gc = ctx.Game;
            ctx.MapDevice.Cancel(gc, ctx.Navigation);
            if (ctx.Stash.IsBusy)
                ctx.Stash.Cancel(gc, ctx.Navigation);
            ctx.Interaction.Cancel(gc);
            BotInput.StopMovement();
            BotInput.ReleaseAllKeys();
        }
    }
}

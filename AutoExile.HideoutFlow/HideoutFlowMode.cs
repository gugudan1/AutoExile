using ExileCore;
using ExileCore.PoEMemory;
using AutoExile.Modes.Shared;
using System;
// 프로젝트 자체의 루트 네임스페이스가 "AutoExile.HideoutFlow"라서 공유 라이브러리의
// HideoutFlow 클래스(AutoExile.Modes.Shared.HideoutFlow)와 이름이 겹칩니다.
// 별칭을 사용해 모호함을 없앱니다.
using SharedHideoutFlow = AutoExile.Modes.Shared.HideoutFlow;

namespace AutoExile.Modes
{
    /// <summary>
    /// 하이드아웃 자동화 전용 독립 모드입니다. 공유 <see cref="HideoutFlow"/> 상태머신을
    /// 그대로 재사용하여: 정착 → 창고 정리(덤프 탭) → 공급 탭에서 지도 인출 →
    /// 지도 장치에 투입 → 포탈 자동 진입까지 완전 자동으로 반복합니다.
    /// 지도 안에서는 기본 전투와 주변 전리품 습득만 수행하며, 길찾기/탐험은
    /// 수동(플레이어 조작)입니다 — 전체 맵 자동 공략이 필요하면 WaveFarm 등
    /// 다른 전용 모드를 사용하세요.
    /// </summary>
    public class HideoutFlowMode : IBotMode
    {
        public string Name => "HideoutFlow";

        public string Status { get; private set; } = "";

        private readonly SharedHideoutFlow _hideoutFlow = new();
        private readonly LootPickupTracker _lootTracker = new();
        private string _lastAreaName = "";

        public void OnEnter(BotContext ctx)
        {
            ModeHelpers.CancelAllSystems(ctx);
            ModeHelpers.EnableDefaultCombat(ctx);
            _hideoutFlow.Cancel();
            _lastAreaName = "";
            Status = "하이드아웃 흐름 모드 진입";
            ctx.Log("[HideoutFlow] 모드 진입 — 자동 하이드아웃 흐름 시작");
        }

        public void OnExit()
        {
            _hideoutFlow.Cancel();
        }

        public void Tick(BotContext ctx)
        {
            var gc = ctx.Game;

            if (gc.IsLoading)
            {
                Status = "로딩 중...";
                return;
            }

            var currentArea = gc.Area?.CurrentArea?.Name ?? "";
            var isHideout = gc.Area?.CurrentArea?.IsHideout == true;

            // 지역이 바뀌면 흐름 상태를 재평가합니다.
            if (!string.IsNullOrEmpty(currentArea) && currentArea != _lastAreaName)
            {
                _lastAreaName = currentArea;
                if (!isHideout)
                    _hideoutFlow.Cancel();
            }

            if (isHideout)
            {
                if (!_hideoutFlow.IsActive)
                {
                    StartHideoutFlow(ctx);
                    return;
                }

                var signal = _hideoutFlow.Tick(ctx);
                Status = _hideoutFlow.Status;

                if (signal == HideoutSignal.PortalTimeout)
                {
                    ctx.Log("[HideoutFlow] 포탈 타임아웃 — 흐름 재시작");
                    StartHideoutFlow(ctx);
                }
                else if (signal == HideoutSignal.NoFragments)
                {
                    Status = "인벤토리에 지도가 없습니다 — 공급 탭(맵핑 보급 탭) 설정을 확인하세요";
                }
                return;
            }

            // ── 지도 안: 기본 전투 + 주변 전리품 습득만 수행 (이동/탐험은 수동) ──
            ctx.Combat.Tick(ctx);

            var interactionResult = ctx.Interaction.Tick(gc);
            _lootTracker.HandleResult(interactionResult, ctx);

            if (!ctx.Interaction.IsBusy)
            {
                ctx.Loot.Scan(gc);
                if (ctx.Loot.HasLootNearby)
                {
                    var (_, candidate) = ctx.Loot.PickupNext(ctx.Interaction, ctx.Navigation);
                    if (candidate != null)
                    {
                        _lootTracker.SetPending(candidate.Entity.Id, candidate.ItemName, candidate.ChaosValue);
                        Status = $"지도 안 — 습득 중: {candidate.ItemName}";
                        return;
                    }
                }
            }

            Status = $"지도 안 — 자동 전투/루팅 중 (누적 습득 {_lootTracker.PickupCount}개)";
        }

        private void StartHideoutFlow(BotContext ctx)
        {
            var stash = ctx.Settings.Stash;
            var mapRolling = ctx.Settings.MapRolling;
            var run = ctx.Settings.Run;

            // 인벤토리의 아무 식별된 지도나 지도 장치에 사용합니다 — 특정 지도 종류를
            // 가리지 않는 범용 필터입니다.
            Func<Element, bool> anyMapFilter = el =>
                el.Entity?.Path?.Contains("Maps/MapKey", StringComparison.OrdinalIgnoreCase) == true;

            _hideoutFlow.Start(
                mapFilter: anyMapFilter,
                stashItemFilter: null, // 전리품은 모두 덤프 탭으로 보냅니다
                targetMapName: null,
                minMapTier: mapRolling.MinMapTier.Value,
                inventoryFragmentPath: "Maps/MapKey",
                stashItemThreshold: run.StashItemThreshold.Value,
                dumpTabName: string.IsNullOrWhiteSpace(stash.DumpTabName.Value) ? null : stash.DumpTabName.Value,
                resourceTabName: string.IsNullOrWhiteSpace(stash.MappingSuppliesTabName.Value) ? null : stash.MappingSuppliesTabName.Value,
                withdrawFragmentPath: "Maps/MapKey",
                fragmentStock: 1,
                minFragments: 1);

            Status = "하이드아웃 — 정착 중";
            ctx.Log("[HideoutFlow] 하이드아웃 흐름 시작");
        }
    }
}

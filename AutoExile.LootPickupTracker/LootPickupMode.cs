using ExileCore;
using AutoExile.Modes.Shared;
using System;
// 프로젝트 자체의 루트 네임스페이스가 "AutoExile.LootPickupTracker"라서 공유 라이브러리의
// LootPickupTracker 클래스(AutoExile.Modes.Shared.LootPickupTracker)와 이름이 겹칩니다.
// 별칭을 사용해 모호함을 없앱니다.
using SharedLootPickupTracker = AutoExile.Modes.Shared.LootPickupTracker;

namespace AutoExile.Modes
{
    /// <summary>
    /// 전리품 자동 습득 전용 독립 모드입니다. 현재 지역에 머무르며 주변의
    /// 전리품을 공유 <see cref="LootPickupTracker"/>로 추적해 자동으로 습득하고,
    /// 공격받으면 기본 전투 프로필로 반격합니다. 길찾기/맵 탐험/하이드아웃 자동화는
    /// 수행하지 않습니다 — 플레이어가 직접 이동하면서 "자동 줍기" 보조로
    /// 사용하는 용도입니다.
    /// </summary>
    public class LootPickupMode : IBotMode
    {
        public string Name => "LootPickupTracker";

        public string Status { get; private set; } = "";

        private readonly SharedLootPickupTracker _tracker = new();
        private DateTime _lastScan = DateTime.MinValue;
        private const int ScanIntervalMs = 300;

        public void OnEnter(BotContext ctx)
        {
            ModeHelpers.CancelAllSystems(ctx);
            ModeHelpers.EnableDefaultCombat(ctx);
            _tracker.Reset();
            Status = "자동 루팁 모드 진입";
            ctx.Log("[LootPickupTracker] 모드 진입 — 자동 전리품 습득 시작");
        }

        public void OnExit() { }

        public void Tick(BotContext ctx)
        {
            var gc = ctx.Game;

            if (gc.IsLoading)
            {
                Status = "로딩 중...";
                return;
            }

            // 진행 중인 습득/상호작용 결과를 먼저 처리합니다.
            if (ctx.Interaction.IsBusy)
            {
                var result = ctx.Interaction.Tick(gc);
                _tracker.HandleResult(result, ctx);
                Status = $"습득 처리 중: {ctx.Interaction.Status}";
                return;
            }

            // 공격받고 있으면 전투가 우선입니다.
            ctx.Combat.Tick(ctx);
            if (ctx.Combat.InCombat)
            {
                Status = $"전투 중 (근처 몬스터 {ctx.Combat.NearbyMonsterCount}마리) — 누적 습득 {_tracker.PickupCount}개";
                return;
            }

            if ((DateTime.Now - _lastScan).TotalMilliseconds >= ScanIntervalMs)
            {
                ctx.Loot.Scan(gc);
                _lastScan = DateTime.Now;
            }

            if (ctx.Loot.HasLootNearby)
            {
                var (_, candidate) = ctx.Loot.PickupNext(ctx.Interaction, ctx.Navigation);
                if (candidate != null)
                {
                    _tracker.SetPending(candidate.Entity.Id, candidate.ItemName, candidate.ChaosValue);
                    Status = $"습득 이동/클릭: {candidate.ItemName} (누적 {_tracker.PickupCount}개)";
                    return;
                }
            }

            Status = $"대기 중 — 주변에 전리품 없음 (누적 {_tracker.PickupCount}개)";
        }
    }
}

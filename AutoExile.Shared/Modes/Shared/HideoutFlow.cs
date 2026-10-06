using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using AutoExile.Systems;
using System.Numerics;

namespace AutoExile.Modes.Shared
{
    /// <summary>
    /// 공유 하이드아웃 흐름: 정착 → 창고 정리 → 지도 장치로 지도 열기 → 포탈 진입.
    /// BlightMode와 SimulacrumMode에서 중복되던 5개의 하이드아웃 메서드를 대체하기 위해 사용합니다.
    /// </summary>
    public class HideoutFlow
    {
        private HideoutPhase _phase = HideoutPhase.Idle;
        private DateTime _phaseStartTime = DateTime.Now;
        private DateTime _lastActionTime = DateTime.MinValue;

        // Start()를 통해 설정되는 구성값
        private Func<Element, bool>? _mapFilter;
        private Func<ServerInventory.InventSlotItem, bool>? _stashItemFilter;
        private string? _targetMapName;
        private string? _inventoryFragmentPath;
        private int _minMapTier;
        private int _stashItemThreshold; // 이 개수 이상일 때만 창고에 정리 (0 = 항상 정리)
        private string? _dumpTabName;
        private string? _resourceTabName;
        private string? _withdrawFragmentPath;
        private int _fragmentStock; // 인벤토리에 유지할 파편 목표 수량
        private int _minFragments; // 지도를 열기 위해 필요한 최소 파편 수 (0 = 아무 수량이나 가능)

        // 다중 아이템 인출(웨이브 파밍이 스캐럽 + 포탈 스크롤 + 지도를 한 번의 창고 이동으로
        // 인출할 때 사용). 단일 항목용 _withdrawFragmentPath와는 상호 배타적이며,
        // 둘 다 설정된 경우 목록(List) 쪽이 우선합니다.
        private IReadOnlyList<(string PathSubstring, int Count)>? _withdrawList;

        // 지도 로드 후 지도 장치에 삽입할 스캐럽 경로 하위 문자열.
        // 웨이브 파밍에서 설정하며, 보스/시뮬라크룸은 스캐럽을 쓰지 않으므로 null/빈 값입니다.
        private IReadOnlyList<string>? _scarabPaths;

        private const float BasePortalTimeoutSeconds = 15f;
        private const float MapDeviceRetrySeconds = 10f;
        private const float ActionCooldownMs = 500f;

        public string Status { get; private set; } = "";
        public bool IsActive => _phase != HideoutPhase.Idle;

        /// <summary>
        /// 전체 하이드아웃 흐름을 시작합니다: 정착 → 창고 정리 → 지도 열기 → 포탈 진입.
        /// </summary>
        public void Start(Func<Element, bool> mapFilter,
            Func<ServerInventory.InventSlotItem, bool>? stashItemFilter = null,
            string? targetMapName = null, int minMapTier = 0,
            string? inventoryFragmentPath = null,
            int stashItemThreshold = 0,
            string? dumpTabName = null,
            string? resourceTabName = null,
            string? withdrawFragmentPath = null,
            int fragmentStock = 0,
            int minFragments = 1,
            IReadOnlyList<(string PathSubstring, int Count)>? withdrawList = null,
            IReadOnlyList<string>? scarabPaths = null)
        {
            _mapFilter = mapFilter;
            _stashItemFilter = stashItemFilter;
            _targetMapName = targetMapName;
            _inventoryFragmentPath = inventoryFragmentPath;
            _minMapTier = minMapTier;
            _stashItemThreshold = stashItemThreshold;
            _dumpTabName = dumpTabName;
            _resourceTabName = resourceTabName;
            _withdrawFragmentPath = withdrawFragmentPath;
            _fragmentStock = fragmentStock;
            _minFragments = minFragments;
            _withdrawList = withdrawList != null && withdrawList.Count > 0 ? withdrawList : null;
            _scarabPaths = scarabPaths != null && scarabPaths.Count > 0 ? scarabPaths : null;
            _phase = HideoutPhase.Settle;
            _phaseStartTime = DateTime.Now;
            Status = "하이드아웃 — 정착 중";
        }

        /// <summary>
        /// 포탈 재진입 흐름을 시작합니다(사망 후): 포탈 찾기 → 이동 → 클릭.
        /// </summary>
        public void StartPortalReentry()
        {
            _mapFilter = null;
            _phase = HideoutPhase.EnterPortal;
            _phaseStartTime = DateTime.Now;
            Status = "포탈로 지도 재진입 중";
        }

        /// <summary>
        /// 하이드아웃 흐름을 1틱 진행합니다. 모드가 처리할 신호를 반환합니다.
        /// </summary>
        public HideoutSignal Tick(BotContext ctx)
        {
            switch (_phase)
            {
                case HideoutPhase.Settle:
                    return TickSettle(ctx);
                case HideoutPhase.Stash:
                    return TickStash(ctx);
                case HideoutPhase.OpenMap:
                    return TickOpenMap(ctx);
                case HideoutPhase.EnterPortal:
                    return TickEnterPortal(ctx);
                default:
                    return HideoutSignal.InProgress;
            }
        }

        public void Cancel()
        {
            _phase = HideoutPhase.Idle;
            _mapFilter = null;
            _stashItemFilter = null;
            _targetMapName = null;
            _inventoryFragmentPath = null;
            _minMapTier = 0;
            _stashItemThreshold = 0;
            _dumpTabName = null;
            _resourceTabName = null;
            _withdrawFragmentPath = null;
            _fragmentStock = 0;
            _minFragments = 1;
            _withdrawList = null;
            _scarabPaths = null;
            Status = "";
        }

        // ── 단계(Phase) ──

        private HideoutSignal TickSettle(BotContext ctx)
        {
            var elapsed = (DateTime.Now - _phaseStartTime).TotalSeconds;
            if (elapsed < ctx.Settings.AreaSettleSeconds.Value)
            {
                Status = $"하이드아웃 — 게임 상태 대기 중 ({elapsed:F1}초)";
                return HideoutSignal.InProgress;
            }

            // ── 다중 아이템 경로 (웨이브 파밍) ────────────────────────────
            // _withdrawList가 설정된 경우, 아직 재고가 부족한 항목을 계산해
            // 그에 맞게 StashSystem 인출 목록을 다시 구성합니다. 이것이
            // "매 런마다 스캐럽 + 포탈 + 지도를 인출" 하는 경로입니다.
            List<(string PathSubstring, int Count)>? activeWithdrawList = null;
            int totalNeededFromList = 0;
            if (_withdrawList != null && !string.IsNullOrWhiteSpace(_resourceTabName))
            {
                activeWithdrawList = new List<(string, int)>();
                foreach (var (path, target) in _withdrawList)
                {
                    var have = StashSystem.CountInventoryItems(ctx.Game, path);
                    var need = target - have;
                    if (need > 0)
                    {
                        activeWithdrawList.Add((path, need));
                        totalNeededFromList += need;
                    }
                }
                if (activeWithdrawList.Count == 0) activeWithdrawList = null; // 재고 충분
            }

            // 인벤토리의 파편과 파편이 아닌 전리품 개수를 셉니다 (단일 항목 경로)
            int fragmentsInInventory = StashSystem.CountInventoryItems(ctx.Game, _withdrawFragmentPath);
            int lootItems = StashSystem.CountNonMatchingItems(ctx.Game, _withdrawFragmentPath);

            // 최소 필요 수량 미만일 때만 인출 — 매 런마다 채우지 않음
            bool usesFragments = !string.IsNullOrEmpty(_withdrawFragmentPath);
            int minNeeded = _minFragments > 0 ? _minFragments : 1;
            bool canWithdraw = usesFragments
                && !string.IsNullOrEmpty(_resourceTabName)
                && _fragmentStock > 0;
            bool needSingleWithdraw = canWithdraw && fragmentsInInventory < minNeeded;
            int withdrawNeeded = needSingleWithdraw ? _fragmentStock : 0;
            bool needMultiWithdraw = activeWithdrawList != null;
            bool needWithdraw = needSingleWithdraw || needMultiWithdraw;

            // 파편이 부족하고 더 얻을 방법도 없으면 — 중지 신호 (파편을 쓰는 모드에만 해당)
            if (usesFragments && fragmentsInInventory < minNeeded && !canWithdraw)
            {
                Status = "인벤토리에 파편이 없습니다";
                _phase = HideoutPhase.Idle;
                return HideoutSignal.NoFragments;
            }

            // 파편이 아닌 아이템이 임계값을 초과할 때만 전리품을 창고에 정리
            bool needStore = false;
            if (StashSystem.HasStashableItems(ctx.Game, _stashItemFilter))
                needStore = _stashItemThreshold <= 0 || lootItems >= _stashItemThreshold;

            if (needWithdraw || needStore)
            {
                _phase = HideoutPhase.Stash;
                _phaseStartTime = DateTime.Now;
                ctx.Stash.Start(
                    storeTabName:         needStore    ? _dumpTabName          : null,
                    withdrawTabName:      needWithdraw ? _resourceTabName      : null,
                    // 단일 항목 필드는 다중 항목 목록이 없을 때만 사용됩니다.
                    withdrawFragmentPath: needMultiWithdraw ? null : (needSingleWithdraw ? _withdrawFragmentPath : null),
                    withdrawCount:        needMultiWithdraw ? 0    : withdrawNeeded,
                    itemFilter:           needStore ? _stashItemFilter : (_ => false),
                    withdrawList:         activeWithdrawList);
                var parts = new List<string>();
                if (needSingleWithdraw) parts.Add($"파편 {withdrawNeeded}개 인출");
                if (needMultiWithdraw)  parts.Add($"아이템 {totalNeededFromList}개 인출 ({activeWithdrawList!.Count}종)");
                if (needStore) parts.Add($"전리품 {lootItems}개 정리");
                Status = string.Join(" & ", parts);
                return HideoutSignal.InProgress;
            }

            // 아이템 없음 — 지도 열기
            _phase = HideoutPhase.OpenMap;
            _phaseStartTime = DateTime.Now;
            StartMapDevice(ctx);
            return HideoutSignal.InProgress;
        }

        private HideoutSignal TickStash(BotContext ctx)
        {
            var result = ctx.Stash.Tick(ctx.Game, ctx.Navigation);

            switch (result)
            {
                case StashResult.Succeeded:
                case StashResult.Failed:
                {
                    // 지도 장치로 넘어가기 전에 파편이 충분한지 확인합니다
                    if (!string.IsNullOrEmpty(_withdrawFragmentPath) && !string.IsNullOrEmpty(_resourceTabName))
                    {
                        int frags = StashSystem.CountInventoryItems(ctx.Game, _withdrawFragmentPath);
                        int needed = _minFragments > 0 ? _minFragments : 1;
                        if (frags < needed)
                        {
                            Status = $"파편 부족 ({frags}/{needed}) — 중지";
                            _phase = HideoutPhase.Idle;
                            return HideoutSignal.NoFragments;
                        }
                    }

                    Status = result == StashResult.Succeeded
                        ? $"창고 정리 완료 ({ctx.Stash.ItemsStored}개 보관) — 지도 여는 중"
                        : $"창고 정리 문제: {ctx.Stash.Status} — 그래도 지도 여는 중"
                    ;
                    _phase = HideoutPhase.OpenMap;
                    _phaseStartTime = DateTime.Now;
                    StartMapDevice(ctx);
                    break;
                }
                default:
                    Status = $"창고 정리 중: {ctx.Stash.Status}";
                    break;
            }
            return HideoutSignal.InProgress;
        }

        private void StartMapDevice(BotContext ctx)
        {
            if (ctx.MapDevice.IsBusy)
                ctx.MapDevice.Cancel(ctx.Game, ctx.Navigation);

            ctx.MapDevice.TargetMapName = _targetMapName;
            ctx.MapDevice.MinMapTier = _minMapTier;

            if (_mapFilter != null && !ctx.MapDevice.Start(_mapFilter, _inventoryFragmentPath, _scarabPaths))
                Status = $"MapDevice.Start 실패 (phase={ctx.MapDevice.Phase})";
        }

        private HideoutSignal TickOpenMap(BotContext ctx)
        {
            var result = ctx.MapDevice.Tick(ctx.Game, ctx.Navigation);

            switch (result)
            {
                case MapDeviceResult.Succeeded:
                    Status = "지도 열림 — 입장 중";
                    // 플레이어가 포탈에 들어가면 지역 변경 이벤트가 발생합니다
                    break;
                case MapDeviceResult.Failed:
                    Status = $"지도 장치 실패: {ctx.MapDevice.Status}";
                    if ((DateTime.Now - _phaseStartTime).TotalSeconds > MapDeviceRetrySeconds)
                    {
                        _phaseStartTime = DateTime.Now;
                        StartMapDevice(ctx);
                    }
                    break;
                default:
                    Status = $"지도 장치: {ctx.MapDevice.Status}";
                    break;
            }
            return HideoutSignal.InProgress;
        }

        private HideoutSignal TickEnterPortal(BotContext ctx)
        {
            var gc = ctx.Game;

            if (!gc.Area.CurrentArea.IsHideout)
                return HideoutSignal.InProgress;

            if ((DateTime.Now - _phaseStartTime).TotalSeconds > BasePortalTimeoutSeconds + ctx.Settings.ExtraLatencyMs.Value / 1000f)
            {
                Status = "포탈을 찾지 못함";
                ctx.Interaction.Cancel(gc);
                _phase = HideoutPhase.Idle;
                return HideoutSignal.PortalTimeout;
            }

            // 포탈 클릭 전에 열려 있는 패널(창고/인벤토리)을 닫습니다
            if (gc.IngameState.IngameUi.StashElement?.IsVisible == true ||
                gc.IngameState.IngameUi.InventoryPanel?.IsVisible == true)
            {
                if (ModeHelpers.CanAct(_lastActionTime, ActionCooldownMs))
                {
                    BotInput.PressKey(System.Windows.Forms.Keys.Escape);
                    _lastActionTime = DateTime.Now;
                    Status = "포탈 진입 전 패널 닫는 중";
                }
                return HideoutSignal.InProgress;
            }

            // 포탈 클릭에 InteractionSystem을 사용 — 이동, 화면 경계 확인,
            // 클릭 검증, 재시도를 자동으로 처리합니다.
            // InteractionSystem은 이 코드가 실행되기 전에 모드에서 이미 틱 처리됩니다.
            if (ctx.Interaction.IsBusy)
            {
                Status = $"포탈 진입 중: {ctx.Interaction.Status}";
                return HideoutSignal.InProgress;
            }

            var portal = ModeHelpers.FindNearestPortal(gc);
            if (portal == null)
            {
                Status = "재진입할 포탈 찾는 중...";
                return HideoutSignal.InProgress;
            }

            ctx.Interaction.InteractWithEntity(portal, ctx.Navigation, requireProximity: true);
            Status = "포탈과 상호작용 중";
            return HideoutSignal.InProgress;
        }

        private enum HideoutPhase
        {
            Idle,
            Settle,
            Stash,
            OpenMap,
            EnterPortal,
        }
    }

    public enum HideoutSignal
    {
        InProgress,
        PortalTimeout,
        NoFragments,
    }
}

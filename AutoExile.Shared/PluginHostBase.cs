using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using AutoExile.Mechanics;
using AutoExile.Modes;
using AutoExile.Systems;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace AutoExile
{
    /// <summary>
    /// 공유 플러그인 호스트. 원래의 거대한 BotCore를 대체하며, 한 플러그인에 하나의
    /// 고정된 모드만 싣습니다(모드 드롭다운/다중 모드 레지스트리 없음).
    /// 각 모드 전용 플러그인(BossPlugin, FollowerPlugin 등)은 이 클래스를 상속하고
    /// <see cref="CreateMode"/>만 구현하면 됩니다. 웹 대시보드/레코더/디버그
    /// 핫키 등 독립 실행에 불필요한 기능은 의도적으로 제외되었습니다 — 설정은
    /// ExileCore의 기본 ImGui 설정 트리(DrawSettings)로 자동 표시됩니다.
    /// </summary>
    public abstract class PluginHostBase<TSettings> : BaseSettingsPlugin<TSettings>
        where TSettings : SharedSettings, new()
    {
        protected BotContext Ctx = null!;
        protected IBotMode Mode = null!;

        // --- Systems ---
        private NavigationSystem _navigation = new();
        private InteractionSystem _interaction = new();
        private TileMap _tileMap = new();
        private CombatSystem _combat = new();
        private LootSystem _loot = new();
        private MapDeviceSystem _mapDevice = new();
        private StashSystem _stash = new();
        private StashIndexer _stashIndex = new();
        private FaustusSystem _faustus = new();
        private ExplorationMap _exploration = new();
        private LootTracker _lootTracker = new();
        private MapMechanicManager _mechanics = new();
        private ThreatSystem _threat = new();
        private EldritchAltarHandler _altarHandler = new();
        private NinjaPriceService _ninjaPrice = new();
        private RuntimeTracker _runtime = new();
        private EntityCache _entityCache = new();
        private ThreatMap _threatMap = new();
        private DateTime _lastEntityPrune = DateTime.MinValue;
        private GemValuationService _gemValuation = new();
        private MapDatabase _mapDatabase = null!;
        private readonly PerformanceTracker _perf = new();

        // Gem level-up
        private DateTime _lastGemLevelAt = DateTime.MinValue;
        private const int GemLevelCooldownMs = 10000;

        // Area change tracking for tile map reload
        private string _lastAreaName = "";
        private long _lastAreaHash;
        private DateTime _areaChangedAt = DateTime.MinValue;
        private float AreaSettleSeconds => Settings.AreaSettleSeconds.Value;

        // Cross-zone state cache (e.g., Wishes portal round-trip)
        private readonly Dictionary<string, AreaStateCache> _areaStateCache = new();
        private const int MaxCachedAreas = 3;

        // Minimap icon scanner
        private readonly Dictionary<long, MinimapIconEntry> _knownMinimapIcons = new();
        private DateTime _lastMinimapIconScan = DateTime.MinValue;
        private const int MinimapIconScanIntervalMs = 2000;

        // Stash tab dropdown sync
        private List<string>? _lastStashTabNames;

        // Debug range circle
        private string _debugCircleLabel = "";
        private int _debugCircleRadius;
        private DateTime _debugCircleExpiry = DateTime.MinValue;
        private readonly Dictionary<string, int> _lastRangeValues = new();

        // Death tracking for revive
        private bool _wasDead;
        private DateTime _deathTime;
        private int _reviveDelayMs;
        private DateTime _lastReviveClickAt = DateTime.MinValue;
        private DateTime _lastDismissAt = DateTime.MinValue;
        private readonly Random _rng = new();

        // Public accessors for external tools (POEMCP /eval 등)
        public NavigationSystem Navigation => _navigation;
        public CombatSystem Combat => _combat;
        public LootSystem Loot => _loot;
        public InteractionSystem Interaction => _interaction;
        public ExplorationMap Exploration => _exploration;
        public LootTracker LootTrackerInstance => _lootTracker;
        public MapMechanicManager Mechanics => _mechanics;
        public ThreatSystem Threat => _threat;
        public NinjaPriceService NinjaPrice => _ninjaPrice;
        public IBotMode ActiveMode => Mode;
        public BotContext Context => Ctx;

        /// <summary>파생 플러그인이 구현: 이 플러그인이 구동할 단일 모드를 생성/구성합니다.</summary>
        protected abstract IBotMode CreateMode();

        /// <summary>플레이어 사망 시 호출됩니다 (모드별 사망 횟수 집계 등에 사용).</summary>
        protected virtual void OnPlayerDeath() { }

        /// <summary>엔티티 추가 시 추가 동작이 필요한 모드(Blight 등)를 위한 훅.</summary>
        protected virtual void OnEntityAddedHook(Entity entity) { }

        /// <summary>엔티티 제거 시 추가 동작이 필요한 모드(Blight 등)를 위한 훅.</summary>
        protected virtual void OnEntityRemovedHook(Entity entity, Vector2 playerGridPos) { }

        /// <summary>Initialise() 마지막에 호출되는 훅 — 모드별 드롭다운 채우기 등에 사용.</summary>
        protected virtual void OnInitialisedHook() { }

        /// <summary>매 틱, Mode.Tick(Ctx) 호출 직전에 실행되는 훅 — 설정 값을
        /// 모드 인스턴스의 공개 속성으로 동기화해야 하는 모드(Follower 등)를 위함.</summary>
        protected virtual void BeforeModeTick() { }

        public override bool Initialise()
        {
            PluginRuntimeInfo.PluginDirectory = DirectoryFullName;

            // 시작 시점에 공유 설정 파일(Plugins\Compiled\AutoExileSharedSettings.json)에서
            // 공통 섹션(빌드/루팅/위협/맵 기믹/실행 공통/창고/맵 롤링/지도 장치/파우스투스/알림)을
            // 즉시 반영합니다 — 다른 플러그인에서 먼저 바꿔둔 값을 그대로 이어받습니다.
            SharedSettingsSync.LoadIfChanged(Settings, DirectoryFullName);

            _ninjaPrice.Initialize(DirectoryFullName, msg => LogMessage($"[{Name}] NinjaPrice: {msg}"));
            _mapDatabase = new MapDatabase(msg => LogMessage($"[{Name}] {msg}"));
            _mapDatabase.Initialize(DirectoryFullName);

            Ctx = new BotContext
            {
                Game = GameController,
                Navigation = _navigation,
                Interaction = _interaction,
                TileMap = _tileMap,
                Combat = _combat,
                Loot = _loot,
                MapDevice = _mapDevice,
                Stash = _stash,
                StashIndex = _stashIndex,
                Faustus = _faustus,
                Exploration = _exploration,
                LootTracker = _lootTracker,
                Mechanics = _mechanics,
                Threat = _threat,
                AltarHandler = _altarHandler,
                NinjaPrice = _ninjaPrice,
                Runtime = _runtime,
                Entities = _entityCache,
                ThreatMap = _threatMap,
                MapDatabase = _mapDatabase,
                Settings = Settings,
                Perf = _perf,
                Log = msg => LogMessage($"[{Name}] {msg}")
            };

            // Register in-map mechanics (shared across every mode)
            _mechanics.Register(new UltimatumMechanic());
            _mechanics.Register(new HarvestMechanic());
            _mechanics.Register(new WishesMechanic());
            _mechanics.Register(new EssenceMechanic());
            _mechanics.Register(new RitualMechanic());

            // Read in-game keybindings (skill slots, flasks)
            _combat.RefreshKeybindings(GameController);

            // Single fixed mode — no dropdown/registry needed
            Mode = CreateMode();
            Mode.OnEnter(Ctx);

            // Wire loot skip events (useful for log output even without a web UI)
            _loot.OnItemSkipped = (itemName, reason, chaosValue) =>
            {
                _perf.RecordFailure("lootSkip", reason ?? "(알 수 없음)");
            };

            OnInitialisedHook();

            return base.Initialise();
        }

        public override void AreaChange(AreaInstance area)
        {
            var currentArea = area?.Name ?? "";
            var currentHash = GameController.IngameState?.Data?.CurrentAreaHash ?? 0;
            if (currentHash == 0) return; // Not loaded yet
            if (currentHash == _lastAreaHash) return;

            var previousAreaName = _lastAreaName;

            if (!string.IsNullOrEmpty(previousAreaName) && _exploration.IsInitialized)
            {
                if (!_areaStateCache.ContainsKey(previousAreaName))
                {
                    _mechanics.ForceCompleteActive();

                    _areaStateCache[previousAreaName] = new AreaStateCache
                    {
                        Exploration = _exploration.CreateSnapshot(),
                        Mechanics = _mechanics.CreateSnapshot(),
                        AreaHash = _lastAreaHash,
                        CachedAt = DateTime.Now,
                    };

                    while (_areaStateCache.Count > MaxCachedAreas)
                    {
                        string? oldest = null;
                        var oldestTime = DateTime.MaxValue;
                        foreach (var kv in _areaStateCache)
                        {
                            if (kv.Value.CachedAt < oldestTime)
                            {
                                oldestTime = kv.Value.CachedAt;
                                oldest = kv.Key;
                            }
                        }
                        if (oldest != null) _areaStateCache.Remove(oldest);
                        else break;
                    }

                    Ctx.Log($"[Cache] '{previousAreaName}' 지역 상태 저장 hash={_lastAreaHash} ({_areaStateCache.Count}개 캐시됨)");
                }
            }

            _lastAreaName = currentArea;
            _lastAreaHash = currentHash;
            _areaChangedAt = DateTime.Now;
            _tileMap.Clear();
            _tileMap.Load(GameController);
            Ctx.TileScan = _tileMap.IsLoaded
                ? TileScanner.ScanMapWide(_tileMap)
                : null;
            _loot.ClearFailed();
            _entityCache.Rebuild(GameController.EntityListWrapper.OnlyValidEntities);
            var pfGridForThreat = GameController.IngameState?.Data?.RawPathfindingData;
            if (pfGridForThreat != null)
            {
                _threatMap.Initialize(pfGridForThreat);
                _threatMap.RebuildFromEntities(_entityCache.Monsters);
            }
            _combat.ClearUnreachable();
            _combat.RefreshKeybindings(GameController);
            _altarHandler.Reset();
            _lootTracker.OnAreaChanged();
            ClearMinimapIcons();
            ScanMinimapIcons();

            if (_areaStateCache.TryGetValue(currentArea, out var cached) && cached.AreaHash == currentHash)
            {
                _exploration.RestoreSnapshot(cached.Exploration);
                _mechanics.RestoreSnapshot(cached.Mechanics);
                _areaStateCache.Remove(currentArea);
                Ctx.Log($"[Cache] '{currentArea}' 지역 상태 복원 hash={currentHash}");
            }
            else
            {
                _mechanics.Reset();

                var terrainData = GameController.IngameState?.Data?.RawPathfindingData;
                var targetingData = GameController.IngameState?.Data?.RawTerrainTargetingData;
                if (terrainData != null && GameController.Player != null)
                {
                    var playerGrid = new Vector2(
                        GameController.Player.GridPosNum.X,
                        GameController.Player.GridPosNum.Y);
                    _exploration.Initialize(terrainData, targetingData, playerGrid,
                        Settings.Build.BlinkRange.Value);
                }
            }
        }

        public override Job Tick()
        {
            // 공통 설정 동기화 — 다른 플러그인이 바꾼 값을 반영하고(Load), 이 플러그인에서
            // 방금 바꾼 값을 공유 파일에 반영합니다(Save). 둘 다 내부적으로 약 1초 간격으로만
            // 실제 디스크 I/O를 수행하므로 매 틱 호출해도 안전합니다. 플러그인이 꺼져 있거나
            // 게임 접속 전이어도 설정 메뉴는 계속 보이므로, Enable/InGame 체크보다 먼저 둡니다.
            SharedSettingsSync.LoadIfChanged(Settings, DirectoryFullName);
            SharedSettingsSync.SaveIfChanged(Settings, DirectoryFullName);

            if (!Settings.Enable || !GameController.InGame)
                return base.Tick();

            _runtime.Tick(Settings.Running.Value);

            if (Settings.Running.Value && _runtime.IsExpired(Settings.Run.MaxRuntimeMinutes.Value))
            {
                Settings.Running.Value = false;
                LogMessage($"[{Name}] 최대 실행 시간({Settings.Run.MaxRuntimeMinutes.Value}분) 도달 — 봇 정지");
            }

            if (!GameController.IsForeGroundCache)
                return base.Tick();

            Ctx.DeltaTime = (float)GameController.DeltaTime;
            Ctx.MinimapIcons = _knownMinimapIcons;

            SyncStashTabNames();

            var canAct = BotInput.CanAct;

            if (!_exploration.IsInitialized && GameController.Player != null)
            {
                var terrainData = GameController.IngameState?.Data?.RawPathfindingData;
                var targetingData = GameController.IngameState?.Data?.RawTerrainTargetingData;
                if (terrainData != null)
                {
                    var playerGrid = new Vector2(
                        GameController.Player.GridPosNum.X,
                        GameController.Player.GridPosNum.Y);
                    _exploration.Initialize(terrainData, targetingData, playerGrid,
                        Settings.Build.BlinkRange.Value);
                }
            }

            if (_exploration.IsInitialized && GameController.Player != null)
            {
                var playerGrid = new Vector2(
                    GameController.Player.GridPosNum.X,
                    GameController.Player.GridPosNum.Y);
                _exploration.Update(playerGrid);
                ScanAreaTransitions();
                ScanMinimapIcons();
            }

            // Sync settings → systems
            _navigation.BlinkRange = Settings.Build.BlinkRange.Value;
            _navigation.DashMinDistance = Settings.Build.DashMinDistance.Value;
            _navigation.PathMergeThreshold = Settings.Build.PathMergeThreshold.Value;
            BotInput.ActionCooldownMs = Settings.ActionCooldownMs.Value;
            BotInput.WindowRect = GameController.Window.GetWindowRectangleTimeCache;
            BotInput.TickHeldKeys();
            BotInput.TickMovementLayer();

            var primaryMove = Settings.Build.GetPrimaryMovement();
            _navigation.MoveKey = primaryMove?.Key.Value ?? Keys.T;

            _combat.RefreshSkillBar(GameController, Settings.Build);
            _navigation.MovementSkills = _combat.MovementSkills;

            var threatSettings = Settings.Threat;
            _threat.Enabled = threatSettings.Enabled.Value;
            _threat.ThreatRadius = threatSettings.ThreatRadius.Value;
            _threat.DodgeTriggerDistance = threatSettings.DodgeTriggerDistance.Value;
            _threat.DodgeMinProgress = threatSettings.DodgeMinProgress.Value;
            _threat.DodgeMaxProgress = threatSettings.DodgeMaxProgress.Value;
            _threat.MonitorRares = threatSettings.MonitorRares.Value;

            // Sync loot settings
            _loot.SkipLowValueUniques = Settings.Loot.SkipLowValueUniques.Value;
            _loot.MinUniqueChaosValue = Settings.Loot.MinUniqueChaosValue.Value;
            _loot.MinChaosPerSlot = Settings.Loot.MinChaosPerSlot.Value;
            _loot.IgnoreQuestItems = Settings.Loot.IgnoreQuestItems.Value;
            _loot.FilterClusterJewels = Settings.Loot.FilterClusterJewels.Value;
            _loot.MinClusterJewelChaosValue = Settings.Loot.MinClusterJewelChaosValue.Value;
            _loot.FilterSkillGems = Settings.Loot.FilterSkillGems.Value;
            _loot.MinGemChaosValue = Settings.Loot.MinGemChaosValue.Value;
            _loot.AlwaysLoot20QualityGems = Settings.Loot.AlwaysLoot20QualityGems.Value;
            _loot.FilterSynthesisedItems = Settings.Loot.FilterSynthesisedItems.Value;
            var whitelistRaw = Settings.Loot.SynthesisedWhitelist.Value ?? "";
            _loot.SynthesisedWhitelist = whitelistRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .ToList();
            var mustLootRaw = Settings.Loot.MustLootUniques.Value ?? "";
            _loot.MustLootUniques = new HashSet<string>(
                mustLootRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            _loot.LabelToggleUnstick = Settings.Loot.LabelToggleUnstick.Value;
            _loot.LabelToggleCooldownSeconds = Settings.Loot.LabelToggleCooldownSeconds.Value;
            _loot.PriceService = _ninjaPrice;
            _lootTracker.PriceService = _ninjaPrice;

            _interaction.Cache = _entityCache;
            if ((DateTime.Now - _lastEntityPrune).TotalMilliseconds > 1000)
            {
                _entityCache.Prune();
                _lastEntityPrune = DateTime.Now;
            }

            if (GameController.Player != null)
                _threatMap.Reconcile(GameController.Player.GridPosNum, _entityCache);

            _interaction.InteractRadius = Settings.InteractRadius.Value;
            _mapDevice.InteractRadius = Settings.InteractRadius.Value;
            _mapDevice.Interaction = _interaction;
            _stash.InteractRadius = Settings.InteractRadius.Value;

            var extraLatency = Settings.ExtraLatencyMs.Value;
            if (extraLatency == 0)
            {
                var serverLatency = GameController.IngameState?.ServerData?.Latency ?? 0;
                extraLatency = serverLatency > 0 ? serverLatency : 0;
            }
            float extraLatencySec = extraLatency / 1000f;
            _interaction.ExtraLatencySec = extraLatencySec;
            _interaction.MaxClickAttempts = Settings.MaxClickAttempts.Value;
            _mapDevice.ExtraLatencySec = extraLatencySec;
            _mapDevice.MaxClickAttempts = Settings.MaxClickAttempts.Value;
            _stash.ExtraLatencySec = extraLatencySec;
            _combat.ExtraLatencySec = extraLatencySec;
            var blacklistRaw = Settings.Build.BlacklistedEnemies.Value ?? "";
            _combat.BlacklistedEnemies = new HashSet<string>(
                blacklistRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            _navigation.ExtraLatencyMs = extraLatency;

            _ninjaPrice.Tick(GameController);
            _gemValuation.BuildColourMap(GameController);

            _stash.ActionCooldownMs = Settings.Loot.StashItemCooldownMs.Value;
            _stash.ApplyIncubators = Settings.AutoApplyIncubators.Value;

            if (!Settings.Running)
            {
                BotInput.StopMovement();
                return base.Tick();
            }

            if (!HandleInterrupts())
                return base.Tick();

            if ((DateTime.Now - _areaChangedAt).TotalSeconds < AreaSettleSeconds)
                return base.Tick();

            if (!BotInput.CanTick)
                return base.Tick();

            _threat.Tick(GameController);

            BeforeModeTick();
            Mode.Tick(Ctx);

            if (canAct)
                _navigation.Tick(GameController);

            TickGemLevelUp();

            return base.Tick();
        }

        public override void Render()
        {
            if (!Settings.Enable || !GameController.InGame)
                return;

            if (Settings.ToggleRunning.PressedOnce())
            {
                Settings.Running.Value = !Settings.Running.Value;
                if (Settings.Running.Value)
                {
                    if (!_lootTracker.IsActive)
                        _lootTracker.StartSession();
                }
                else if (_lootTracker.IsActive)
                    _lootTracker.StopSession();
            }

            UpdateDebugRangeCircle();

            var running = Settings.Running.Value;
            var color = running ? SharpDX.Color.LimeGreen : SharpDX.Color.Yellow;
            var status = running ? $"BOT: {Mode.Name}" : $"BOT: 일시정지됨 ({Mode.Name})";
            Graphics.DrawText(status, new Vector2(100, 80), color);

            var maxMin = Settings.Run.MaxRuntimeMinutes.Value;
            var elapsed = _runtime.ActiveDuration;
            var elapsedStr = $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}";
            string runtimeText;
            SharpDX.Color runtimeColor;
            if (maxMin <= 0)
            {
                runtimeText = $"실행 시간: {elapsedStr} (제한 없음)";
                runtimeColor = SharpDX.Color.LightGray;
            }
            else
            {
                var remaining = _runtime.Remaining(maxMin);
                var remStr = $"{(int)remaining.TotalHours}:{remaining.Minutes:D2}";
                runtimeText = $"실행 시간: {elapsedStr} / {maxMin / 60}:{(maxMin % 60):D2}  (정지까지 {remStr})";
                var pctLeft = (double)remaining.TotalMinutes / maxMin;
                runtimeColor = remaining.TotalMinutes < 5 ? SharpDX.Color.Red
                            : pctLeft < 0.10 ? SharpDX.Color.Orange
                            : SharpDX.Color.LightGray;
            }
            Graphics.DrawText(runtimeText, new Vector2(100, 96), runtimeColor);

            var winWidth = GameController.Window.GetWindowRectangle().Width;
            _lootTracker.Render(Graphics, new Vector2(winWidth - 250, 80));

            Ctx.Graphics = Graphics;
            Mode.Render(Ctx);

            RitualMechanic.RenderShopOverlay(Ctx, Graphics, GameController);

            Ctx.Graphics = null;

            if (Settings.DebugIncubatorOverlay.Value &&
                (GameController.IngameState.IngameUi.StashElement?.IsVisible == true ||
                 GameController.IngameState.IngameUi.InventoryPanel?.IsVisible == true))
            {
                _stash.RenderDebugIncubators(Graphics, GameController);
            }

            if (DateTime.Now < _debugCircleExpiry && _debugCircleRadius > 0 && GameController.Player != null)
            {
                var playerPos = GameController.Player.PosNum;
                var worldRadius = _debugCircleRadius * Pathfinding.GridToWorld;
                Graphics.DrawCircleInWorld(
                    new Vector3(playerPos.X, playerPos.Y, playerPos.Z),
                    (float)worldRadius, SharpDX.Color.Yellow, 2f);

                var camera = GameController.IngameState.Camera;
                var labelScreen = camera.WorldToScreen(playerPos);
                Graphics.DrawText(_debugCircleLabel,
                    new Vector2(labelScreen.X - 40, labelScreen.Y - 60),
                    SharpDX.Color.Yellow);
            }
        }

        private void UpdateDebugRangeCircle()
        {
            var b = Settings.Build;

            CheckRange("블링크 거리", b.BlinkRange.Value);
            CheckRange("대시 최소 거리", b.DashMinDistance.Value);
            CheckRange("전투 거리", b.FightRange.Value);
            CheckRange("교전 감지 거리", b.CombatRange.Value);
            CheckRange("상호작용 반경", Settings.InteractRadius.Value);

            int i = 1;
            foreach (var slot in b.AllSkillSlots)
            {
                CheckRange($"스킬 {i} 사거리", slot.MaxTargetRange.Value);
                i++;
            }
        }

        private void CheckRange(string label, int currentValue)
        {
            if (_lastRangeValues.TryGetValue(label, out var prev) && prev != currentValue)
            {
                _debugCircleLabel = $"{label}: {currentValue}";
                _debugCircleRadius = currentValue;
                _debugCircleExpiry = DateTime.Now.AddSeconds(5);
            }
            _lastRangeValues[label] = currentValue;
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void ScanAreaTransitions()
        {
            if (!_exploration.IsInitialized) return;

            foreach (var entity in GameController.EntityListWrapper.OnlyValidEntities)
            {
                if (entity.Type != ExileCore.Shared.Enums.EntityType.AreaTransition) continue;
                var gridPos = new Vector2(entity.GridPosNum.X, entity.GridPosNum.Y);
                _exploration.RecordTransition(gridPos, entity.RenderName ?? entity.Path ?? "");
            }
        }

        private void ScanMinimapIcons()
        {
            var gc = GameController;
            if (gc?.Player == null) return;

            if ((DateTime.Now - _lastMinimapIconScan).TotalMilliseconds < MinimapIconScanIntervalMs)
                return;
            _lastMinimapIconScan = DateTime.Now;

            var tileEntities = gc.IngameState?.Data?.TileEntities;
            if (tileEntities == null) return;

            foreach (var entity in tileEntities)
            {
                if (entity?.Path == null) continue;
                if (_knownMinimapIcons.ContainsKey(entity.Id)) continue;
                try
                {
                    var mic = entity.GetComponent<ExileCore.PoEMemory.Components.MinimapIcon>();
                    if (mic?.Name == null) continue;

                    _knownMinimapIcons[entity.Id] = new MinimapIconEntry
                    {
                        EntityId = entity.Id,
                        IconName = mic.Name,
                        Path = entity.Path,
                        GridPos = entity.GridPosNum,
                        EntityType = entity.Type.ToString(),
                    };
                }
                catch { }
            }
        }

        private void ClearMinimapIcons()
        {
            _knownMinimapIcons.Clear();
            _lastMinimapIconScan = DateTime.MinValue;
        }

        private bool HandleInterrupts()
        {
            var gc = GameController;

            if (gc.IsLoading)
                return false;

            if (!gc.Player.IsAlive)
            {
                if (!_wasDead)
                {
                    OnPlayerDeath();
                    _deathTime = DateTime.Now;
                }
                _wasDead = true;

                var reviveDelayMs = _reviveDelayMs == 0
                    ? _reviveDelayMs = 500 + _rng.Next(500)
                    : _reviveDelayMs;
                if ((DateTime.Now - _deathTime).TotalMilliseconds < reviveDelayMs)
                    return false;
                if (BotInput.CanAct && (DateTime.Now - _lastReviveClickAt).TotalMilliseconds > 1000)
                {
                    try
                    {
                        var revivePanel = gc.IngameState.IngameUi.ResurrectPanel;
                        if (revivePanel?.IsVisible == true)
                        {
                            var atCheckpoint = revivePanel.ResurrectAtCheckpoint;
                            if (atCheckpoint?.IsVisible == true)
                            {
                                var rect = atCheckpoint.GetClientRect();
                                var center = new Vector2(rect.Center.X, rect.Center.Y);
                                var windowRect = gc.Window.GetWindowRectangle();
                                BotInput.Click(new Vector2(windowRect.X + center.X, windowRect.Y + center.Y));
                                _lastReviveClickAt = DateTime.Now;
                            }
                        }
                    }
                    catch { }
                }
                return false;
            }

            _wasDead = false;
            _reviveDelayMs = 0;

            try
            {
                var ui = gc.IngameState.IngameUi;

                if (ui.RitualWindow?.IsVisible == true)
                {
                    if (BotInput.CanAct &&
                        (DateTime.Now - _lastDismissAt).TotalMilliseconds > 500)
                    {
                        var closeBtn = ui.RitualWindow.GetChildAtIndex(9);
                        if (closeBtn?.IsVisible == true)
                        {
                            var rect = closeBtn.GetClientRect();
                            if (rect.Width > 5)
                            {
                                var windowRect = gc.Window.GetWindowRectangleTimeCache;
                                var center = new Vector2(
                                    rect.X + rect.Width / 2 + windowRect.X,
                                    rect.Y + rect.Height / 2 + windowRect.Y);
                                BotInput.Click(center);
                                _lastDismissAt = DateTime.Now;
                                LogMessage($"[{Name}] 예상치 못한 의식 상점(RitualShop) 닫는 중 (X 버튼 클릭)");
                                return false;
                            }
                        }
                    }
                }

                if (ui.SellWindow?.IsVisible == true && BotInput.CanAct &&
                    (DateTime.Now - _lastDismissAt).TotalMilliseconds > 500)
                {
                    var windowRect = gc.Window.GetWindowRectangle();
                    var worldClickPos = new Vector2(
                        windowRect.X + windowRect.Width * 0.5f,
                        windowRect.Y + windowRect.Height * 0.4f);
                    BotInput.Click(worldClickPos);
                    _lastDismissAt = DateTime.Now;
                    LogMessage($"[{Name}] 예상치 못한 상인 창(VendorWindow) 닫는 중 (월드 클릭)");
                    return false;
                }
            }
            catch { }

            return true;
        }

        public override void EntityAdded(Entity entity)
        {
            _entityCache.OnEntityAdded(entity);
            _threatMap.OnEntityAdded(entity);
            OnEntityAddedHook(entity);
        }

        public override void EntityRemoved(Entity entity)
        {
            _entityCache.OnEntityRemoved(entity);
            _threatMap.OnEntityRemoved(entity);

            if (GameController?.Player != null)
                OnEntityRemovedHook(entity, GameController.Player.GridPosNum);
        }

        private static List<string> WithSavedOption(List<string> options, string? saved)
        {
            if (string.IsNullOrWhiteSpace(saved) || options.Contains(saved))
                return options;
            var copy = new List<string>(options) { saved };
            return copy;
        }

        private void SyncStashTabNames()
        {
            try
            {
                var stashEl = GameController.IngameState?.IngameUi?.StashElement;
                if (stashEl?.IsVisible != true) return;

                var names = stashEl.AllStashNames;
                if (names == null || names.Count == 0) return;

                if (_lastStashTabNames != null && _lastStashTabNames.Count == names.Count)
                {
                    bool same = true;
                    for (int i = 0; i < names.Count; i++)
                    {
                        if (names[i] != _lastStashTabNames[i]) { same = false; break; }
                    }
                    if (same) return;
                }
                _lastStashTabNames = names.ToList();

                var options = new List<string> { "" };
                options.AddRange(names);

                var savedDump = Settings.Stash.DumpTabName.Value;
                var savedFragment = Settings.Stash.FragmentTabName.Value;
                var savedSupplies = Settings.Stash.MappingSuppliesTabName.Value;

                var dumpOptions = WithSavedOption(options, savedDump);
                var fragmentOptions = WithSavedOption(options, savedFragment);
                var suppliesOptions = WithSavedOption(options, savedSupplies);

                Settings.Stash.DumpTabName.SetListValues(dumpOptions);
                Settings.Stash.FragmentTabName.SetListValues(fragmentOptions);
                Settings.Stash.MappingSuppliesTabName.SetListValues(suppliesOptions);
                Settings.Stash.DumpTabName.Value = savedDump;
                Settings.Stash.FragmentTabName.Value = savedFragment;
                Settings.Stash.MappingSuppliesTabName.Value = savedSupplies;
            }
            catch { /* stash API can throw during zone transitions */ }
        }

        private void TickGemLevelUp()
        {
            if (!Settings.AutoLevelGems.Value) return;
            if (!BotInput.CanAct) return;
            if ((DateTime.Now - _lastGemLevelAt).TotalMilliseconds < GemLevelCooldownMs) return;

            try
            {
                var panel = GameController.IngameState.IngameUi.GemLvlUpPanel;
                if (panel == null || !panel.IsVisible) return;

                var gems = panel.GemsToLvlUp;
                if (gems == null || gems.Count == 0) return;

                var windowRect = GameController.Window.GetWindowRectangle();

                try
                {
                    dynamic dynPanel = panel;
                    var levelAllBtn = (ExileCore.PoEMemory.Element)dynPanel.LevelUpAllGemsButton;
                    if (levelAllBtn?.IsVisible == true)
                    {
                        var rect = levelAllBtn.GetClientRect();
                        var absPos = new Vector2(windowRect.X + rect.Center.X, windowRect.Y + rect.Center.Y);
                        BotInput.Click(absPos);
                        _lastGemLevelAt = DateTime.Now;
                        return;
                    }
                }
                catch { /* Property not available in this ExileCore version — fall through to per-gem */ }

                foreach (var gemEl in gems)
                {
                    if (gemEl?.IsVisible != true) continue;

                    dynamic? levelButton = null;
                    int smallSquareCount = 0;
                    for (int i = 0; i < gemEl.ChildCount; i++)
                    {
                        var child = gemEl.GetChildAtIndex(i);
                        if (child?.IsVisible != true) continue;
                        var cr = child.GetClientRect();
                        if (cr.Width > 5 && cr.Width < 60 && cr.Height > 5 && cr.Height < 60)
                        {
                            smallSquareCount++;
                            if (smallSquareCount == 2)
                            {
                                levelButton = child;
                                break;
                            }
                        }
                    }

                    if (levelButton == null) continue;

                    try
                    {
                        bool enabled = levelButton.IsEnabled;
                        if (!enabled) continue;
                    }
                    catch { /* Property doesn't exist on this type — skip check */ }

                    SharpDX.RectangleF rect = levelButton.GetClientRect();
                    var absPos = new Vector2(windowRect.X + rect.Center.X, windowRect.Y + rect.Center.Y);
                    BotInput.Click(absPos);
                    _lastGemLevelAt = DateTime.Now;
                    return;
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 지역 전환 간 탐색/기믹 상태 캐시 (예: Wishes 포탈 왕복 지원).
    /// </summary>
    internal class AreaStateCache
    {
        public ExplorationSnapshot Exploration = null!;
        public MechanicsSnapshot Mechanics = null!;
        public long AreaHash;
        public DateTime CachedAt;
    }
}

using ExileCore.PoEMemory;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using AutoExile.Mechanics;
using AutoExile.Systems;
using System.Windows.Forms;

namespace AutoExile
{
    /// <summary>
    /// 모든 모드 플러그인이 공유하는 기반 설정입니다.
    /// 전투/루팅/창고/맵 기기 등 공용 섹션만 포함하며, 모드별 섹션(Boss, Blight 등)은
    /// 각 모드 플러그인이 이 클래스를 상속해 자신만의 설정 클래스에 추가합니다.
    /// </summary>
    public class SharedSettings : ISettings
    {
        [Menu("활성화", "플러그인 전체 켜기/끄기.")]
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public ToggleNode Running { get; set; } = new ToggleNode(false);
        public HotkeyNode ToggleRunning { get; set; } = new HotkeyNode(Keys.Insert);

        [Menu("맵 탐색 테스트", "맵 탐색 테스트를 시작/재시작하는 단축키 (비콘으로 이동, 전투, 70% 탐색).")]
        public HotkeyNode TestMapExplore { get; set; } = new HotkeyNode(Keys.F5);

        [Menu("게임 상태 덤프", "지형, 탐색, 경로 탐색 데이터를 이미지 + JSON 파일로 저장하는 단축키.")]
        public HotkeyNode DumpGameState { get; set; } = new HotkeyNode(Keys.F6);

        [Menu("녹화 덤프", "최근 약 10초간의 틱 단위 상태(판단, 위협, 이동, 행동)를 저장하는 단축키.")]
        public HotkeyNode DumpRecording { get; set; } = new HotkeyNode(Keys.F7);

        [Menu("타일 시그니처 스캔", "주변 타일을 스캔해 고유/희귀 맵 시그니처를 찾아 화면에 표시하는 단축키.")]
        public HotkeyNode ScanTileSignatures { get; set; } = new HotkeyNode(Keys.F8);

        [Menu("플레이 녹화", "수동 플레이 녹화 토글(F9). 오프라인 분석을 위해 매 틱 게임 상태를 기록합니다. 녹화 전 봇을 정지하세요.")]
        public HotkeyNode RecordGameplay { get; set; } = new HotkeyNode(Keys.F9);

        [Menu("행동 쿨다운 (ms)", "마우스 입력 사이의 최소 간격. 입력 과다로 인한 서버 킥을 방지합니다.")]
        public RangeNode<int> ActionCooldownMs { get; set; } = new RangeNode<int>(100, 50, 300);

        [Menu("추가 지연 시간 (ms)", "서버 응답 대기 시간(포탈 생성, 클릭 확인, 피해 확인, 페이즈 제한 등)에 더해지는 추가 시간. 0 = 게임의 ServerData.Latency 값으로 자동 감지. 수동으로 설정해 덮어쓸 수 있습니다.")]
        public RangeNode<int> ExtraLatencyMs { get; set; } = new RangeNode<int>(0, 0, 5000);

        [Menu("최대 클릭 재시도 횟수", "오브젝트 상호작용, 아틀라스 노드, 제단 등에 대한 최대 클릭 재시도 횟수. 지연이 심한 연결에서는 값을 높이면 도움이 됩니다.")]
        public RangeNode<int> MaxClickAttempts { get; set; } = new RangeNode<int>(5, 1, 20);

        [Menu("웹 UI 사용", "내장 웹 대시보드를 활성화합니다.")]
        public ToggleNode WebUiEnabled { get; set; } = new ToggleNode(true);

        [Menu("웹 UI 포트", "웹 대시보드용 포트 (변경 시 재시작 필요).")]
        public RangeNode<int> WebUiPort { get; set; } = new RangeNode<int>(9876, 1024, 65535);

        [Menu("웹 UI 네트워크 접근 허용", "같은 네트워크의 다른 기기에서 접근을 허용합니다 (관리자 권한 또는 URL 예약 필요).")]
        public ToggleNode WebUiNetworkAccess { get; set; } = new ToggleNode(false);

        [Menu("젬 자동 레벨업", "레벨업 패널이 나타나면 스킬 젬을 자동으로 레벨업합니다.")]
        public ToggleNode AutoLevelGems { get; set; } = new ToggleNode(true);

        [Menu("인큐베이터 자동 장착", "창고 단계에서 창고의 인큐베이터를 장비에 자동으로 적용합니다.")]
        public ToggleNode AutoApplyIncubators { get; set; } = new ToggleNode(true);

        [Menu("인큐베이터 디버그 오버레이", "인벤토리/창고가 열려 있을 때 장비 슬롯 번호와 창고 아이템 영역을 표시합니다.")]
        public ToggleNode DebugIncubatorOverlay { get; set; } = new ToggleNode(false);

        [Menu("성능 디버그 오버레이", "구간별 틱 소요 시간(평균/최대 ms)과 웨이브 파밍 중 루팅/상호작용/탐색 실패 횟수를 표시합니다.")]
        public ToggleNode DebugPerfOverlay { get; set; } = new ToggleNode(false);

        [Menu("상호작용 반경", "봇이 오브젝트, 아이템, 창고, 지도 장치 등을 클릭할 수 있는 그리드 거리.")]
        public RangeNode<int> InteractRadius { get; set; } = new RangeNode<int>(20, 10, 80);

        [Menu("구역 안정화 시간 (초)", "구역 전환(맵 입장, 은신처 복귀) 후 행동을 시작하기 전 대기 시간. 엔티티와 게임 상태가 안정될 시간을 줍니다.")]
        public RangeNode<float> AreaSettleSeconds { get; set; } = new RangeNode<float>(3f, 1f, 10f);

        // --- Build (player setup: movement, skills, combat, flasks) ---

        [Menu("빌드 설정", "이동, 스킬, 전투, 플라스크 등 캐릭터 빌드 관련 설정입니다.")]
        public BuildSettings Build { get; set; } = new BuildSettings();

        // --- Loot ---

        [Menu("루팅 설정", "아이템 줍기 필터와 동작 관련 설정입니다.")]
        public LootSettings Loot { get; set; } = new LootSettings();

        // --- Threat / Dodge ---

        [Menu("위협 회피 설정", "위험 회피/회피기 관련 설정입니다.")]
        public ThreatSettings Threat { get; set; } = new ThreatSettings();

        // --- In-map Mechanics ---

        [Menu("맵 기믹 설정", "하베스트, 위시, 에센스, 리추얼 등 맵 내 기믹 처리 설정입니다.")]
        public MechanicsSettings Mechanics { get; set; } = new MechanicsSettings();

        // --- Run (shared across all modes that run+exit a zone) ---

        [Menu("실행 공통 설정", "맵에 입장하고 퇴장하는 모든 모드가 공유하는 실행 설정입니다.")]
        public RunSettings Run { get; set; } = new RunSettings();

        // --- Stash (shared across all modes) ---

        [Menu("창고 설정", "모든 모드가 공유하는 창고 탭 설정입니다.")]
        public StashSettings Stash { get; set; } = new StashSettings();

        // --- Map Rolling (shared by anything that withdraws + rolls maps) ---

        [Menu("맵 롤링 설정", "창고에서 맵을 인출하고 주사위를 굴리는(롤링) 기능 관련 설정입니다.")]
        public MapRollingSettings MapRolling { get; set; } = new MapRollingSettings();

        // --- Map Device (shared slot config for atlas inserts) ---

        [Menu("지도 장치 설정", "아틀라스 투입물(지도 장치 슬롯) 공용 설정입니다.")]
        public MapDeviceSettings MapDevice { get; set; } = new MapDeviceSettings();

        // --- Faustus Currency Exchange ---

        [Menu("파우스투스 환전 설정", "파우스투스 화폐 교환 관련 설정입니다.")]
        public FaustusSettings Faustus { get; set; } = new FaustusSettings();

        // --- Notifications (Discord webhook) ---

        [Menu("알림 설정 (디스코드)", "디스코드 웹훅을 통한 알림 설정입니다.")]
        public NotificationSettings Notifications { get; set; } = new NotificationSettings();

        // =====================================================================
        // Submenu classes
        // =====================================================================

        [Submenu(CollapsedByDefault = false)]
        public class BuildSettings
        {
            // ?? Movement ??

            [Menu("점멸 거리", "이동 스킬로 틈을 건너뛸 수 있는 최대 그리드 거리. 이보다 넓은 틈은 시도하지 않습니다.")]
            public RangeNode<int> BlinkRange { get; set; } = new RangeNode<int>(40, 5, 50);

            [Menu("대시 최소 거리", "속도를 위해 대시를 사용하기 전 필요한 최소 직선 거리. 너무 낮으면 대시 모션 경직이 걷기보다 느려집니다. 0 = 속도용 대시 비활성화.")]
            public RangeNode<int> DashMinDistance { get; set; } = new RangeNode<int>(60, 0, 200);

            [Menu("경로 병합 임계값", "이 거리보다 가까운 연속 이동 경유지를 병합합니다 (그리드 단위). 계단/경사에서의 미세한 끊김을 줄입니다. 0 = 비활성화. 값이 클수록 부드럽지만 좁은 모서리를 가로지를 수 있습니다.")]
            public RangeNode<int> PathMergeThreshold { get; set; } = new RangeNode<int>(8, 0, 20);

            // ?? Skill Slots ??
            // Configure each skill on your bar: what key it's bound to, what role it plays,
            // and its priority (higher = checked first during combat).
            // One slot should be PrimaryMovement (your Move Only key).
            // Movement skills (dash/blink) use the MovementSkill role.

            public SkillSlotConfig Skill1 { get; set; } = new SkillSlotConfig(Keys.T, SkillRole.PrimaryMovement);
            public SkillSlotConfig Skill2 { get; set; } = new SkillSlotConfig(Keys.Q);
            public SkillSlotConfig Skill3 { get; set; } = new SkillSlotConfig(Keys.W);
            public SkillSlotConfig Skill4 { get; set; } = new SkillSlotConfig(Keys.E);
            public SkillSlotConfig Skill5 { get; set; } = new SkillSlotConfig(Keys.R);
            public SkillSlotConfig Skill6 { get; set; } = new SkillSlotConfig(Keys.None);
            public SkillSlotConfig Skill7 { get; set; } = new SkillSlotConfig(Keys.None);
            public SkillSlotConfig Skill8 { get; set; } = new SkillSlotConfig(Keys.None);

            /// <summary>All configured skill slots.</summary>
            public IEnumerable<SkillSlotConfig> AllSkillSlots => new[] { Skill1, Skill2, Skill3, Skill4, Skill5, Skill6, Skill7, Skill8 };

            /// <summary>Find the first skill slot with PrimaryMovement role, or null.</summary>
            public SkillSlotConfig? GetPrimaryMovement()
            {
                foreach (var slot in AllSkillSlots)
                {
                    if (slot.Key.Value != Keys.None && slot.Role.Value == SkillRole.PrimaryMovement.ToString())
                        return slot;
                }
                return null;
            }

            /// <summary>Find all movement skills (dash/blink), ordered by priority.</summary>
            public List<SkillSlotConfig> GetMovementSkills()
            {
                var result = new List<SkillSlotConfig>();
                foreach (var slot in AllSkillSlots)
                {
                    if (slot.Key.Value != Keys.None && slot.Role.Value == SkillRole.MovementSkill.ToString())
                        result.Add(slot);
                }
                result.Sort((a, b) => b.Priority.Value.CompareTo(a.Priority.Value));
                return result;
            }

            // ?? Combat Behavior ??

            [Menu("무시할 적 목록", "전역적으로 무시할 적 이름 목록(쉼표로 구분). 이 몬스터들은 모든 전투 및 색적 로직에서 제외됩니다.")]
            public TextNode BlacklistedEnemies { get; set; } = new TextNode("");

            [Menu("기본 포지셔닝", "몬스터에 대한 기본 위치 선정 방식. 각 모드가 이 값을 재정의할 수 있습니다.")]
            public ListNode DefaultPositioning { get; set; } = new ListNode();

            [Menu("전투 거리", "몬스터와 전투할 때 유지하려는 그리드 거리. 근접/원거리 포지셔닝 목표입니다. 공격적 모드에서는 무시됩니다.")]
            public RangeNode<int> FightRange { get; set; } = new RangeNode<int>(40, 5, 80);

            [Menu("전투 감지 거리", "'전투 중'으로 판단하는 그리드 거리 기준. 이 범위 안의 몬스터는 포지셔닝과 스킬 사용을 유발합니다.")]
            public RangeNode<int> CombatRange { get; set; } = new RangeNode<int>(80, 20, 200);

            // ?? Guard / Defensive Thresholds ??

            [Menu("방어 스킬 체력 기준", "체력이 이 값 이하로 떨어지면 방어 스킬을 사용합니다 (0~1).")]
            public RangeNode<float> GuardHpThreshold { get; set; } = new RangeNode<float>(0.7f, 0.1f, 1.0f);

            [Menu("방어 스킬 ES 기준", "에너지 쉴드(ES)가 이 값 이하로 떨어지면 방어 스킬을 사용합니다 (0~1). ES가 있는 캐릭터만 해당.")]
            public RangeNode<float> GuardEsThreshold { get; set; } = new RangeNode<float>(0.5f, 0.1f, 1.0f);

            // ?? Vaal ??

            [Menu("바알 스킬 최소 몬스터 수", "바알 스킬을 사용하기 위한 주변 최소 몬스터 수.")]
            public RangeNode<int> VaalMinMonsters { get; set; } = new RangeNode<int>(5, 1, 30);

            // ?? Summon ??

            [Menu("소환수 기대 수", "소환 스킬의 기대 배치 수. 이보다 적으면 재시전합니다.")]
            public RangeNode<int> SummonExpectedCount { get; set; } = new RangeNode<int>(1, 1, 20);

            // ?? Flasks ??

            [Menu("플라스크 자동 사용", "플라스크 자동 사용을 활성화합니다.")]
            public ToggleNode FlasksEnabled { get; set; } = new ToggleNode(true);

            [Menu("생명 플라스크 슬롯 (0=없음)", "생명 플라스크 슬롯 번호 (1~5, 비활성화는 0).")]
            public RangeNode<int> LifeFlaskSlot { get; set; } = new RangeNode<int>(1, 0, 5);

            [Menu("생명 플라스크 체력 기준", "체력이 이 값 이하면 생명 플라스크를 사용합니다 (0~1).")]
            public RangeNode<float> LifeFlaskHpThreshold { get; set; } = new RangeNode<float>(0.5f, 0.1f, 0.9f);

            [Menu("마나 플라스크 슬롯 (0=없음)", "마나 플라스크 슬롯 번호 (1~5, 비활성화는 0).")]
            public RangeNode<int> ManaFlaskSlot { get; set; } = new RangeNode<int>(0, 0, 5);

            [Menu("마나 플라스크 기준", "마나가 이 값 이하면 마나 플라스크를 사용합니다 (0~1).")]
            public RangeNode<float> ManaFlaskManaThreshold { get; set; } = new RangeNode<float>(0.3f, 0.1f, 0.9f);

            [Menu("유틸 플라스크 사용 간격 (ms)", "전투 중 유틸 플라스크를 사용하는 주기.")]
            public RangeNode<int> UtilityFlaskIntervalMs { get; set; } = new RangeNode<int>(5000, 1000, 30000);

            public BuildSettings()
            {
                DefaultPositioning.SetListValues(Enum.GetNames<CombatPositioning>().ToList());
                DefaultPositioning.Value = CombatPositioning.Aggressive.ToString();
            }
        }

        [Submenu(CollapsedByDefault = true)]
        public class SkillSlotConfig
        {
            public SkillSlotConfig() : this(Keys.None) { }

            public SkillSlotConfig(Keys defaultKey, SkillRole defaultRole = SkillRole.Disabled)
            {
                Key = new HotkeyNode(defaultKey);
                Role.SetListValues(Enum.GetNames<SkillRole>().ToList());
                Role.Value = defaultRole.ToString();
                TargetFilter.SetListValues(Enum.GetNames<SkillTargetFilter>().ToList());
                TargetFilter.Value = SkillTargetFilter.Any.ToString();
            }

            [Menu("키", "이 스킬 슬롯에 게임 내에서 지정된 키보드 키.")]
            public HotkeyNode Key { get; set; } = new HotkeyNode(Keys.None);

            [Menu("역할", "조준 대상: Enemy(대상), Corpse(시체), Self(커서 없음). 이동용은 PrimaryMovement/MovementSkill.")]
            public ListNode Role { get; set; } = new ListNode();

            [Menu("우선순위", "실행 우선순위 (높을수록 먼저 확인). 10이 최고.")]
            public RangeNode<int> Priority { get; set; } = new RangeNode<int>(5, 0, 10);

            [Menu("지형 통과 가능", "MovementSkill 전용: 이 스킬로 틈을 점멸/도약할 수 있는지 여부.")]
            public ToggleNode CanCrossTerrain { get; set; } = new ToggleNode(false);

            // ?? Skill Conditions ??
            // All "when to fire" logic is configured here. The Role only controls cursor targeting.

            [Menu("대상 필터", "특정 몬스터 희귀도에만 스킬을 제한합니다.")]
            public ListNode TargetFilter { get; set; } = new ListNode();

            [Menu("최소 주변 적 수", "교전 반경 내 적이 이 수 이상일 때만 사용합니다. 0=비활성화.")]
            public RangeNode<int> MinNearbyEnemies { get; set; } = new RangeNode<int>(0, 0, 30);

            [Menu("최대 대상 거리", "대상이 이 그리드 거리 이내일 때만 사용합니다. 0=비활성화(교전 반경 사용).")]
            public RangeNode<int> MaxTargetRange { get; set; } = new RangeNode<int>(0, 0, 200);

            [Menu("버프 없을 때만", "버프/디버프가 이미 적용 중이면 건너뜁니다 — 자신(Self) 버프 또는 대상(Enemy) 디버프를 확인합니다.")]
            public ToggleNode OnlyWhenBuffMissing { get; set; } = new ToggleNode(false);

            [Menu("체력 낮을 때만", "체력이 방어 임계값 이하일 때만 사용합니다.")]
            public ToggleNode OnlyOnLowLife { get; set; } = new ToggleNode(false);

            [Menu("소환 재시전", "배치 수가 소환수 기대 수보다 적으면 재시전합니다. 소환/미니언 스킬용.")]
            public ToggleNode SummonRecast { get; set; } = new ToggleNode(false);

            [Menu("최소 시전 간격 (ms)", "이 스킬의 시전 사이 최소 간격(밀리초). 0=제한 없음. 디버프 스킬이 주 공격을 가로채는 것을 방지합니다.")]
            public RangeNode<int> MinCastIntervalMs { get; set; } = new RangeNode<int>(0, 0, 10000);

            [Menu("타겟 가능할 때만", "대상이 타겟 가능한 상태(무적/페이즈 중 아님)일 때만 시전합니다. 디버프, 토템 등 면역 대상에 낭비하고 싶지 않은 스킬에 유용합니다.")]
            public ToggleNode RequireTargetable { get; set; } = new ToggleNode(false);

            [Menu("채널링 스킬", "누르고 떼는 대신 키를 계속 누르고 있습니다. 채널링 스킬(사이클론, 화염폭발, 작열 광선 등)용. 조건이 더 이상 맞지 않으면 자동으로 키를 뗍니다.")]
            public ToggleNode IsChannel { get; set; } = new ToggleNode(false);

            [Menu("버프/디버프 이름", "버프 목록에서 일치시킬 이름(부분 문자열, 대소문자 무시). '버프 없을 때만' 옵션과 함께 자신 버프 또는 대상 디버프를 확인할 때 사용합니다. 탐색(Scan) 기능으로 찾아보세요.")]
            public TextNode BuffDebuffName { get; set; } = new TextNode("");
        }

        [Submenu(CollapsedByDefault = true)]
        public class LootSettings
        {
            [Menu("저가치 유니크 건너뛰기", "최소 카오스 가치 미만의 유니크 아이템을 건너뜁니다.")]
            public ToggleNode SkipLowValueUniques { get; set; } = new ToggleNode(false);

            [Menu("최소 유니크 카오스 가치", "유니크 아이템을 주울 최소 카오스 가치.")]
            public RangeNode<int> MinUniqueChaosValue { get; set; } = new RangeNode<int>(10, 1, 100);

            [Menu("슬롯당 최소 카오스 가치 (0=끄기)", "인벤토리 슬롯당 최소 카오스 가치. 0이면 비활성화.")]
            public RangeNode<int> MinChaosPerSlot { get; set; } = new RangeNode<int>(0, 0, 10);

            [Menu("퀘스트 아이템 무시", "루팅 시 퀘스트 아이템(하이스트 계약서 등)을 건너뜁니다.")]
            public ToggleNode IgnoreQuestItems { get; set; } = new ToggleNode(true);

            // --- Cluster Jewel Filtering ---

            [Menu("클러스터 주얼 필터링", "비유니크 클러스터 주얼에 가치 기반 필터링을 적용합니다.")]
            public ToggleNode FilterClusterJewels { get; set; } = new ToggleNode(false);

            [Menu("최소 클러스터 주얼 가치", "이 카오스 가치 미만의 비유니크 클러스터 주얼은 건너뜁니다. 0 = 모두 줍기.")]
            public RangeNode<int> MinClusterJewelChaosValue { get; set; } = new RangeNode<int>(0, 0, 500);

            // --- Skill Gem Filtering ---

            [Menu("스킬 젬 필터링", "바닥의 스킬 젬에 가치 기반 필터링을 적용합니다.")]
            public ToggleNode FilterSkillGems { get; set; } = new ToggleNode(false);

            [Menu("최소 젬 카오스 가치", "이 카오스 가치 미만의 스킬 젬은 건너뜁니다.")]
            public RangeNode<int> MinGemChaosValue { get; set; } = new RangeNode<int>(5, 1, 500);

            [Menu("20% 품질 젬은 항상 줍기", "가치와 상관없이 품질 20%인 스킬 젬은 항상 줍습니다.")]
            public ToggleNode AlwaysLoot20QualityGems { get; set; } = new ToggleNode(true);

            // --- Synthesised Item Filtering ---

            [Menu("합성 아이템 필터링", "활성화하면 화이트리스트에 맞는 내재 효과를 가진 합성 아이템만 줍습니다.")]
            public ToggleNode FilterSynthesisedItems { get; set; } = new ToggleNode(false);

            [Menu("합성 내재 효과 화이트리스트", "유지할 내재 효과 문자열 목록(쉼표 구분, 대소문자 무시). 하나라도 일치하면 루팅합니다.")]
            public TextNode SynthesisedWhitelist { get; set; } = new TextNode("Onslaught,Explode,extra curse,Tailwind,Elusive,base Critical,maximum Power Charge,maximum Frenzy Charge,maximum Endurance Charge,Cooldown Recovery,additional Arrow,additional Projectile");

            // --- Must-Loot Uniques ---

            [Menu("무조건 루팅할 유니크", "가치 필터링과 상관없이 항상 주울 유니크 아이템 이름 목록(쉼표 구분). 웹 UI에서 poe.ninja 데이터로 검색/추가할 수 있습니다.")]
            public TextNode MustLootUniques { get; set; } = new TextNode("");

            // --- Label Toggle Unstick ---

            [Menu("아이템 라벨 재정렬", "아이템은 있지만 라벨이 화면 밖에 겹쳐 있을 때 Z키로 라벨을 껐다 켭니다. 대량 처치 후 재루팅에 유용합니다.")]
            public ToggleNode LabelToggleUnstick { get; set; } = new ToggleNode(true);

            [Menu("라벨 재정렬 쿨다운 (초)", "라벨 재정렬 시도 사이의 최소 간격(초).")]
            public RangeNode<float> LabelToggleCooldownSeconds { get; set; } = new RangeNode<float>(5f, 2f, 30f);

            // --- Misc ---

            [Menu("창고 쿨다운 (ms)", "아이템을 창고에 넣을 때 Ctrl+클릭 사이의 지연 시간.")]
            public RangeNode<int> StashItemCooldownMs { get; set; } = new RangeNode<int>(450, 200, 1000);
        }

        [Submenu(CollapsedByDefault = true)]
        public class MechanicsSettings
        {
            [Menu("기믹 완료 후 대기 (초)", "기믹 완료 후 다음으로 넘어가기 전 대기 시간. 아이템이 떨어지고 주울 시간을 줍니다.")]
            public RangeNode<float> PostMechanicSettleSeconds { get; set; } = new RangeNode<float>(5f, 0f, 30f);

            public UltimatumMechanicSettings Ultimatum { get; set; } = new UltimatumMechanicSettings();
            public HarvestMechanicSettings Harvest { get; set; } = new HarvestMechanicSettings();
            public WishesMechanicSettings Wishes { get; set; } = new WishesMechanicSettings();
            public EssenceMechanicSettings Essence { get; set; } = new EssenceMechanicSettings();
            public RitualMechanicSettings Ritual { get; set; } = new RitualMechanicSettings();
            public EldritchAltarSettings EldritchAltar { get; set; } = new EldritchAltarSettings();
            public InteractableSettings Interactables { get; set; } = new InteractableSettings();
        }

        [Submenu(CollapsedByDefault = false)]
        public class InteractableSettings
        {
            private static ListNode MakeInteractableMode(string defaultValue = "Optional")
            {
                var node = new ListNode();
                node.SetListValues(new List<string> { "Ignore", "Optional", "Required" });
                node.Value = defaultValue;
                return node;
            }

            [Menu("쉬라인", "Ignore=무시, Optional=지나갈 때 클릭, Required=찾아가서 처리.")]
            public ListNode Shrines { get; set; } = MakeInteractableMode();

            [Menu("보물상자", "Ignore=무시, Optional=지나갈 때 클릭, Required=찾아가서 처리.")]
            public ListNode Strongboxes { get; set; } = MakeInteractableMode();

            [Menu("진 캐시", "Ignore=무시, Optional=지나갈 때 클릭, Required=찾아가서 처리.")]
            public ListNode DjinnCaches { get; set; } = MakeInteractableMode();

            [Menu("하이스트 캐시", "Ignore=무시, Optional=지나갈 때 클릭, Required=찾아가서 처리.")]
            public ListNode HeistCaches { get; set; } = MakeInteractableMode();

            [Menu("제작 레시피", "Ignore=무시, Optional=지나갈 때 클릭, Required=찾아가서 처리.")]
            public ListNode CraftingRecipes { get; set; } = MakeInteractableMode();

            [Menu("기억의 눈물", "Ignore=무시, Optional=지나갈 때 클릭, Required=찾아가서 처리. 클릭 후 약 3초 뒤 아이템이 떨어집니다.")]
            public ListNode MemoryTears { get; set; } = MakeInteractableMode();

            /// <summary>Check if a setting is not Ignore (i.e., Optional or Required).</summary>
            public bool IsEnabled(ListNode setting) => setting.Value != "Ignore";

            /// <summary>Check if a setting is Required (actively route toward).</summary>
            public bool IsRequired(ListNode setting) => setting.Value == "Required";
        }

        [Submenu(CollapsedByDefault = false)]
        public class HarvestMechanicSettings
        {
            public HarvestMechanicSettings()
            {
                Mode.SetListValues(Enum.GetNames<MechanicMode>().ToList());
                Mode.Value = MechanicMode.Optional.ToString();

                PreferredColour.SetListValues(new List<string> { "Any", "Wild", "Vivid", "Primal" });
                PreferredColour.Value = "Any";
            }

            [Menu("모드", "Skip=무시, Optional=발견 시 수행, Required=맵 완료를 위해 반드시 수행.")]
            public ListNode Mode { get; set; } = new ListNode();

            [Menu("완료 후 퇴장", "이 기믹 완료 후 맵에서 나갑니다. 기믹 집중 파밍용.")]
            public ToggleNode ExitAfter { get; set; } = new ToggleNode(false);

            [Menu("선호 색상", "점수 계산 시 선호할 수확 타입. Any = 순수 점수만 사용.")]
            public ListNode PreferredColour { get; set; } = new ListNode();

            [Menu("색상 선호 보너스", "선호 색상 관개기에 적용되는 점수 배율 (1.0 = 보너스 없음).")]
            public RangeNode<float> ColourPreferenceBonus { get; set; } = new RangeNode<float>(1.5f, 1.0f, 3.0f);

            [Menu("일반 몬스터 가중치", "일반 등급 몬스터 1마리당 점수 가중치.")]
            public RangeNode<int> NormalWeight { get; set; } = new RangeNode<int>(1, 0, 20);

            [Menu("매직 몬스터 가중치", "매직 등급 몬스터 1마리당 점수 가중치.")]
            public RangeNode<int> MagicWeight { get; set; } = new RangeNode<int>(3, 0, 20);

            [Menu("희귀 몬스터 가중치", "희귀 등급 몬스터 1마리당 점수 가중치.")]
            public RangeNode<int> RareWeight { get; set; } = new RangeNode<int>(10, 0, 50);

            [Menu("야생형 배율", "야생(녹색) 수확 타입의 점수 배율.")]
            public RangeNode<float> WildMultiplier { get; set; } = new RangeNode<float>(1.0f, 0.0f, 5.0f);

            [Menu("생생형 배율", "생생함(노란색) 수확 타입의 점수 배율.")]
            public RangeNode<float> VividMultiplier { get; set; } = new RangeNode<float>(1.0f, 0.0f, 5.0f);

            [Menu("원시형 배율", "원시(파란색) 수확 타입의 점수 배율.")]
            public RangeNode<float> PrimalMultiplier { get; set; } = new RangeNode<float>(1.0f, 0.0f, 5.0f);

            [Menu("루팅 시간 (초)", "각 구역 전투 종료 후 루팅하는 시간.")]
            public RangeNode<float> LootSweepSeconds { get; set; } = new RangeNode<float>(3f, 1f, 10f);
        }

        [Submenu(CollapsedByDefault = false)]
        public class WishesMechanicSettings
        {
            public WishesMechanicSettings()
            {
                Mode.SetListValues(Enum.GetNames<MechanicMode>().ToList());
                Mode.Value = MechanicMode.Optional.ToString();

                PreferredWish.SetListValues(new List<string> { "Any", "Coin of Power", "Coin of Skill", "Coin of Knowledge" });
                PreferredWish.Value = "Any";
            }

            [Menu("모드", "Skip=무시, Optional=발견 시 수행, Required=맵 완료를 위해 반드시 수행.")]
            public ListNode Mode { get; set; } = new ListNode();

            [Menu("완료 후 퇴장", "이 기믹 완료 후 맵에서 나갑니다. 기믹 집중 파밍용.")]
            public ToggleNode ExitAfter { get; set; } = new ToggleNode(false);

            [Menu("선호 소원", "보상 코인 종류로 선택. Any = 첫 번째 가능한 것을 선택.")]
            public ListNode PreferredWish { get; set; } = new ListNode();

            [Menu("루팅 시간 (초)", "소원 구역에서 돌아오기 전 루팅하는 시간.")]
            public RangeNode<float> LootSweepSeconds { get; set; } = new RangeNode<float>(5f, 1f, 15f);
        }

        [Submenu(CollapsedByDefault = false)]
        public class EssenceMechanicSettings
        {
            public EssenceMechanicSettings()
            {
                Mode.SetListValues(Enum.GetNames<MechanicMode>().ToList());
                Mode.Value = MechanicMode.Optional.ToString();
                MinEssenceTier.SetListValues(new List<string> {
                    "Any", "Whispering", "Muttering", "Weeping", "Wailing",
                    "Screaming", "Shrieking", "Deafening"
                });
                MinEssenceTier.Value = "Any";
            }

            [Menu("모드", "Skip=무시, Optional=발견 시 수행, Required=맵 완료를 위해 반드시 수행.")]
            public ListNode Mode { get; set; } = new ListNode();

            [Menu("완료 후 퇴장", "이 기믹 완료 후 맵에서 나갑니다. 기믹 집중 파밍용.")]
            public ToggleNode ExitAfter { get; set; } = new ToggleNode(false);

            [Menu("최소 에센스 등급", "이 등급 미만의 조우는 건너뜁니다. 'Any' = 항상 수행.")]
            public ListNode MinEssenceTier { get; set; } = new ListNode();

            [Menu("에센스 타락", "타락 대상 에센스(고통/질투/공포/경멸 → 광기/공포/착란/광란) 조우에 바알 오브를 사용합니다.")]
            public ToggleNode CorruptEssences { get; set; } = new ToggleNode(true);

            [Menu("루팅 시간 (초)", "에센스 몬스터 처치 후 루팅하는 시간.")]
            public RangeNode<float> LootSweepSeconds { get; set; } = new RangeNode<float>(3f, 1f, 10f);
        }

        [Submenu(CollapsedByDefault = false)]
        public class RitualMechanicSettings
        {
            public RitualMechanicSettings()
            {
                Mode.SetListValues(Enum.GetNames<MechanicMode>().ToList());
                Mode.Value = MechanicMode.Skip.ToString();
            }

            [Menu("모드", "베타 — Skip=무시, Optional=발견 시 수행, Required=맵 완료를 위해 반드시 수행.")]
            public ListNode Mode { get; set; } = new ListNode();

            [Menu("완료 후 퇴장", "이 기믹 완료 후 맵에서 나갑니다. 기믹 집중 파밍용.")]
            public ToggleNode ExitAfter { get; set; } = new ToggleNode(false);

            [Menu("루팅 시간 (초)", "각 의식 조우 후 루팅하는 시간.")]
            public RangeNode<float> LootSweepSeconds { get; set; } = new RangeNode<float>(3f, 1f, 10f);
        }

        [Submenu(CollapsedByDefault = false)]
        public class EldritchAltarSettings
        {
            [Menu("활성화", "맵핑 중 선택지 점수가 임계값 이상이면 엘드리치 제단을 클릭합니다.")]
            public ToggleNode Enabled { get; set; } = new ToggleNode(true);

            [Menu("최소 점수 임계값", "최선의 선택지 순점수(긍정-부정 가중치)가 이 값 이상일 때만 제단을 선택합니다. 값이 높을수록 까다로움. 0 = 순음수만 아니면 모두 선택.")]
            public RangeNode<int> MinScoreThreshold { get; set; } = new RangeNode<int>(0, -500, 500);

            /// <summary>
            /// User overrides for mod weights. Key = NormalizeLetters(mod text), Value = weight.
            /// Positive = reward, negative = danger. Overrides the built-in defaults.
            /// Managed via web UI altar mod editor.
            /// </summary>
            public Dictionary<string, int> ModWeights { get; set; } = new();
        }

        [Submenu(CollapsedByDefault = false)]
        public class UltimatumMechanicSettings
        {
            public UltimatumMechanicSettings()
            {
                Mode.SetListValues(Enum.GetNames<MechanicMode>().ToList());
                Mode.Value = MechanicMode.Optional.ToString();
            }

            [Menu("모드", "Skip=무시, Optional=발견 시 수행, Required=맵 완료를 위해 반드시 수행.")]
            public ListNode Mode { get; set; } = new ListNode();

            [Menu("완료 후 퇴장", "이 기믹 완료 후 맵에서 나갑니다. 기믹 집중 파밍용.")]
            public ToggleNode ExitAfter { get; set; } = new ToggleNode(false);

            // ?? Encounter types ??

            [Menu("생존 수행", "생존(Survive) 조우를 완료합니다.")]
            public ToggleNode DoSurvive { get; set; } = new ToggleNode(true);

            [Menu("적 처치 수행", "적 처치(Kill Enemies) 조우를 완료합니다.")]
            public ToggleNode DoKillEnemies { get; set; } = new ToggleNode(true);

            [Menu("제단 방어 수행", "제단 방어(Defend the Altar) 조우를 완료합니다.")]
            public ToggleNode DoDefendAltar { get; set; } = new ToggleNode(true);

            [Menu("원 안에 서기 수행", "원 안에 서기(Stand in the Circles) 조우를 완료합니다 (위치 로직 필요).")]
            public ToggleNode DoStandInCircles { get; set; } = new ToggleNode(false);

            // ?? Risk management ??

            [Menu("최대 웨이브", "보상을 받기 전까지 진행할 최대 웨이브.")]
            public RangeNode<int> MaxWaves { get; set; } = new RangeNode<int>(10, 1, 10);

            [Menu("위험도 임계값", "누적 수식어 위험도가 이 값을 넘으면 보상을 받습니다. 값이 높을수록 위험 감수. 각 수식어는 위험도 1~5이며, 30은 중간 수식어 10개에 해당.")]
            public RangeNode<int> DangerThreshold { get; set; } = new RangeNode<int>(30, 5, 100);

            [Menu("최소 확보 가치 (카오스)", "누적 보상 가치(NinjaPrice 기준)가 이 카오스 임계값을 넘으면 조기에 보상을 받습니다. 0 = 비활성화(위험도/웨이브 제한만 적용).")]
            public RangeNode<int> MinSecureValue { get; set; } = new RangeNode<int>(0, 0, 500);

            // ?? Positioning ??

            [Menu("궤도 반경", "전투 중 제단으로부터 이 그리드 거리 이내를 유지합니다. 제한된 구역 수식어가 있으면 절반으로 적용.")]
            public RangeNode<float> OrbitRadius { get; set; } = new RangeNode<float>(50f, 10f, 80f);

            // ?? Modifier danger overrides ??
            public UltimatumModRanking ModRanking { get; set; } = new();

            /// <summary>
            /// Get the effective danger rating for a modifier (user override > default > 3).
            /// </summary>
            public int GetModDanger(string modId)
            {
                return UltimatumModDanger.GetDanger(modId, ModRanking.DangerOverrides);
            }
        }

        [Submenu(RenderMethod = nameof(Render))]
        public class UltimatumModRanking
        {
            // Key = modifier Id, Value = danger tier (1-5 or 99=SKIP)
            public Dictionary<string, int> DangerOverrides { get; set; } = new();
            private string _filter = "";

            private static readonly string[] TierLabels = { "매우 쉬움", "쉬움", "보통", "어려움", "매우 어려움", "건너뛰기" };
            private static readonly int[] TierValues = { 0, 1, 3, 5, 10, UltimatumModDanger.BlockedValue };

            private static int ValueToComboIndex(int value) => value switch
            {
                0 => 0,
                1 => 1,
                2 or 3 => 2,
                4 or 5 => 3,
                >= 6 and < UltimatumModDanger.BlockedValue => 4,
                >= UltimatumModDanger.BlockedValue => 5,
                _ => 2,
            };

            public void Render()
            {
                ImGui.TextWrapped("각 수식어의 위험도를 평가하세요: 매우 쉬움(사소함) → 매우 어려움(치명적) → 건너뛰기(절대 수락 안 함)");
                ImGui.Separator();
                ImGui.InputTextWithHint("##ModFilter", "수식어 검색...", ref _filter, 100);
                ImGui.Separator();

                // Try to load full mod list from game files, fall back to known defaults
                var modEntries = new List<(string Id, string DisplayName)>();
                try
                {
                    var fileMods = RemoteMemoryObject.pTheGame.Files.UltimatumModifiers.EntriesList;
                    if (fileMods != null)
                    {
                        foreach (var m in fileMods)
                        {
                            var display = string.IsNullOrWhiteSpace(m.Name) ? m.Id : m.Name;
                            modEntries.Add((m.Id, display));
                        }
                    }
                }
                catch { }

                // Fallback: use known defaults + any overrides
                if (modEntries.Count == 0)
                {
                    var allIds = new HashSet<string>(UltimatumModDanger.Defaults.Keys);
                    foreach (var k in DangerOverrides.Keys) allIds.Add(k);
                    foreach (var id in allIds.OrderBy(x => x))
                        modEntries.Add((id, id));
                }

                foreach (var (modId, displayName) in modEntries)
                {
                    if (!string.IsNullOrEmpty(_filter) &&
                        !displayName.Contains(_filter, StringComparison.OrdinalIgnoreCase) &&
                        !modId.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var currentValue = UltimatumModDanger.GetDanger(modId, DangerOverrides);
                    var comboIndex = ValueToComboIndex(currentValue);
                    var isOverridden = DangerOverrides.ContainsKey(modId);
                    var label = isOverridden ? $"{displayName} *" : displayName;

                    ImGui.PushItemWidth(120);
                    if (ImGui.Combo($"{label}###{modId}", ref comboIndex, TierLabels, TierLabels.Length))
                    {
                        var newValue = TierValues[comboIndex];
                        if (UltimatumModDanger.Defaults.TryGetValue(modId, out var def) && newValue == def)
                            DangerOverrides.Remove(modId);
                        else
                            DangerOverrides[modId] = newValue;
                    }
                    ImGui.PopItemWidth();
                }
            }
        }

        [Submenu(CollapsedByDefault = true)]
        public class ThreatSettings
        {
            [Menu("위협 감지 활성화", "유니크/희귀 몬스터의 위험한 스킬 시전을 감시합니다.")]
            public ToggleNode Enabled { get; set; } = new ToggleNode(false);

            [Menu("희귀 몬스터도 감시", "유니크뿐 아니라 희귀 몬스터도 함께 추적합니다.")]
            public ToggleNode MonitorRares { get; set; } = new ToggleNode(true);

            [Menu("자동 회피", "감지된 위협을 자동으로 회피합니다 (공격 방향에 수직으로 이동).")]
            public ToggleNode AutoDodge { get; set; } = new ToggleNode(false);

            [Menu("위협 감시 반경", "이 그리드 거리 이내의 몬스터만 감시합니다.")]
            public RangeNode<int> ThreatRadius { get; set; } = new RangeNode<int>(60, 20, 150);

            [Menu("회피 발동 거리", "공격 도착 지점이 플레이어로부터 이 그리드 거리 이내일 때 회피합니다.")]
            public RangeNode<int> DodgeTriggerDistance { get; set; } = new RangeNode<int>(15, 5, 40);

            [Menu("회피 이동 거리", "회피 시 옆으로 이동하는 거리 (그리드 단위).")]
            public RangeNode<int> DodgeDistance { get; set; } = new RangeNode<int>(20, 5, 50);

            [Menu("회피 최소 진행도", "회피를 발동할 수 있는 가장 이른 모션 진행도 (이 전에는 도착 지점이 확정되지 않음). 0.15 = 15%.")]
            public RangeNode<float> DodgeMinProgress { get; set; } = new RangeNode<float>(0.15f, 0.0f, 0.5f);

            [Menu("회피 최대 진행도", "회피를 발동할 수 있는 가장 늦은 모션 진행도 (이후에는 너무 늦음). 0.50 = 50%.")]
            public RangeNode<float> DodgeMaxProgress { get; set; } = new RangeNode<float>(0.50f, 0.2f, 0.8f);

            [Menu("회피 쿨다운 (ms)", "회피 이동 사이의 최소 시간 간격.")]
            public RangeNode<int> DodgeCooldownMs { get; set; } = new RangeNode<int>(500, 100, 2000);
        }

        [Submenu(CollapsedByDefault = false)]
        public class RunSettings
        {
            [Menu("최대 사망 횟수", "이 횟수만큼 사망하면 현재 진행을 포기합니다. 모든 모드 공통이며, 모드별로 '진행'의 의미가 다를 수 있습니다 (미궁=전체 진행, 보스=조우별, 시뮬라크럼=1회별).")]
            public RangeNode<int> MaxDeaths { get; set; } = new RangeNode<int>(3, 1, 20);

            [Menu("창고 저장 기준 개수", "인벤토리에 이 개수 이상 아이템이 있으면 진행/웨이브 사이에 창고에 저장합니다. 0 = 항상 저장.")]
            public RangeNode<int> StashItemThreshold { get; set; } = new RangeNode<int>(5, 0, 30);

            [Menu("루팅 제한 시간 (초)", "처치 후 다음으로 넘어가기 전 루팅에 소비하는 최대 시간.")]
            public RangeNode<float> LootSweepTimeoutSeconds { get; set; } = new RangeNode<float>(5f, 3f, 60f);

            [Menu("포탈 스크롤 키", "게임 내 포탈 스크롤 사용 단축키(은신처로 돌아가는 포탈 생성용). 게임 내 키 설정과 일치시키세요.")]
            public HotkeyNode PortalKey { get; set; } = new HotkeyNode(Keys.F);

            [Menu("최대 실행 시간 (분)", "이 '활성' 실행 시간(분)이 지나면 봇을 정지합니다 (일시정지 시간은 제외). 0 = 제한 없음. 기본값 300 = 5시간.")]
            public RangeNode<int> MaxRuntimeMinutes { get; set; } = new RangeNode<int>(300, 0, 1440);
        }

        [Submenu(CollapsedByDefault = true)]
        public class MapRollingSettings
        {
            [Menu("최소 맵 등급", "창고에서 선택할 최소 맵 등급. 0 = 등급 무관.")]
            public RangeNode<int> MinMapTier { get; set; } = new RangeNode<int>(0, 0, 16);

            [Menu("위험한 맵 수식어", "피해야 할 수식어 그룹 이름 목록(쉼표 구분). 이 중 하나라도 있으면 맵을 재설정합니다 (예: MapElementalReflect, MapPhysicalReflect, MapNoRegen, MapHexproof, MapCannotLeech).")]
            public TextNode DangerousMapMods { get; set; } = new TextNode("MapElementalReflect,MapPhysicalReflect,MapNoRegen,MapCannotLeech");

            [Menu("최소 아이템 수량 %", "실행 전 맵의 최소 아이템 수량 %. 0 = 확인 안 함. 이 미만이면 맵을 재설정합니다.")]
            public RangeNode<int> MinMapQuantity { get; set; } = new RangeNode<int>(0, 0, 150);
        }

        [Submenu(CollapsedByDefault = true)]
        public class MapDeviceSettings
        {
            [Menu("슬롯 1", "지도 장치 슬롯 1에 넣을 아이템 경로/이름. 비우면 없음. 전략별 기본값이 채워지며 사용자가 덮어쓸 수 있습니다.")]
            public TextNode Slot1 { get; set; } = new TextNode("");

            [Menu("슬롯 2", "슬롯 2에 넣을 아이템 경로/이름.")]
            public TextNode Slot2 { get; set; } = new TextNode("");

            [Menu("슬롯 3", "슬롯 3에 넣을 아이템 경로/이름.")]
            public TextNode Slot3 { get; set; } = new TextNode("");

            [Menu("슬롯 4", "슬롯 4에 넣을 아이템 경로/이름.")]
            public TextNode Slot4 { get; set; } = new TextNode("");

            [Menu("슬롯 5", "슬롯 5에 넣을 아이템 경로/이름. 게임 내에서 5번째 슬롯 해금 필요.")]
            public TextNode Slot5 { get; set; } = new TextNode("");

            /// <summary>Returns non-empty slot values in order.</summary>
            public List<string> ActiveSlots()
            {
                var list = new List<string>();
                foreach (var s in new[] { Slot1, Slot2, Slot3, Slot4, Slot5 })
                    if (!string.IsNullOrWhiteSpace(s.Value)) list.Add(s.Value.Trim());
                return list;
            }

            /// <summary>Replace all slot values from an ordered list (used by strategy plans for defaults).</summary>
            public void SetDefaults(IReadOnlyList<string> values)
            {
                Slot1.Value = values.Count > 0 ? values[0] : "";
                Slot2.Value = values.Count > 1 ? values[1] : "";
                Slot3.Value = values.Count > 2 ? values[2] : "";
                Slot4.Value = values.Count > 3 ? values[3] : "";
                Slot5.Value = values.Count > 4 ? values[4] : "";
            }
        }

        [Submenu(CollapsedByDefault = false)]
        public class StashSettings
        {
            [Menu("덤프 탭", "대량 전리품(희귀, 유니크, 화폐 등)을 보관할 창고 탭. 비우면 현재 탭 사용. 모든 모드 공통. 창고를 한 번 열면 실제 탭 목록으로 자동 채워집니다.")]
            public ListNode DumpTabName { get; set; } = new ListNode { Value = "" };

            [Menu("조각 탭", "보스 파편과 시뮬라크럼 조각을 넣고 뺄 창고 탭. 보스/시뮬라크럼 모드 전용. 일반 탭 타입이어야 합니다 — 프리미엄 조각 창고 하위 탭 이동은 아직 지원하지 않습니다.")]
            public ListNode FragmentTabName { get; set; } = new ListNode { Value = "" };

            [Menu("맵핑 보급 탭", "웨이브 파밍 모드가 매 진행마다 가져올 사전 설정 맵+스캐럽이 들어있는 창고 탭. 일반 탭 타입(쿼드/프리미엄)이어야 합니다 — 프리미엄 맵/조각 하위 탭 이동은 아직 지원하지 않으므로 맵과 스캐럽을 하나의 일반 탭에 모아두세요.")]
            public ListNode MappingSuppliesTabName { get; set; } = new ListNode { Value = "" };
        }

        [Submenu(CollapsedByDefault = true)]
        public class FaustusSettings
        {
            [Menu("파우스투스 재보급 사용", "은신처에서 파우스투스 NPC 화폐 교환으로 스캐럽/조각을 구매합니다.")]
            public ToggleNode EnableFaustusRestock { get; set; } = new ToggleNode(false);

            [Menu("스캐럽 재보급 기준", "인벤토리에 이 개수보다 적으면 파우스투스에게서 스캐럽을 구매합니다.")]
            public RangeNode<int> ScarabRestockThreshold { get; set; } = new RangeNode<int>(5, 0, 50);

            [Menu("시뮬라크럼 재보급 기준", "인벤토리에 이 개수보다 적으면 파우스투스에게서 완전한 시뮬라크럼을 구매합니다.")]
            public RangeNode<int> SimulacrumRestockThreshold { get; set; } = new RangeNode<int>(1, 0, 10);

            [Menu("지불 화폐", "파우스투스에게서 구매할 때 사용할 화폐 이름(예: 'Chaos Orb' 또는 'Divine Orb').")]
            public TextNode FaustusPayCurrency { get; set; } = new TextNode("Chaos Orb");
        }

        [Submenu(CollapsedByDefault = true)]
        public class NotificationSettings
        {
            [Menu("디스코드 알림 사용", "특정 아이템이 드랍되면 디스코드 웹훅으로 메시지를 전송합니다.")]
            public ToggleNode EnableDiscordNotifications { get; set; } = new ToggleNode(false);

            [Menu("디스코드 웹훅 URL", "디스코드 채널 설정에서 가져온 전체 웹훅 URL.")]
            public TextNode DiscordWebhookUrl { get; set; } = new TextNode("");

            [Menu("알림 키워드", "알림을 받을 아이템 이름/문자열 목록(쉼표 구분, 예: 'Large Cluster Jewel'). 비우면 최소 가치 이상 모든 아이템 알림.")]
            public TextNode NotificationKeywords { get; set; } = new TextNode("");

            [Menu("최소 카오스 가치", "이 카오스 가치 이상인 아이템만 알립니다. 0 = 항상 알림.")]
            public RangeNode<int> MinChaosValueNotification { get; set; } = new RangeNode<int>(100, 0, 10000);
        }

    }
}

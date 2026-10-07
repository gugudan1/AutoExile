using System.Collections.Generic;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;

namespace AutoExile.Blight
{
    /// <summary>
    /// Blight(역병) 모드 전용 설정. 공유 설정(SharedSettings)에 Blight 섹션을 추가합니다.
    /// </summary>
    public class BlightPluginSettings : SharedSettings
    {
        [Menu("역병 농사 설정", "Blight(역병) 농사 모드 전용 설정입니다.")]
        public BlightSettings Blight { get; set; } = new BlightSettings();

        [Submenu(CollapsedByDefault = false)]
        public class BlightSettings
        {
            [Menu("재화 확인 무시", "타워 건설/업그레이드 시 재화 확인을 건너뜁니다 (디버그용: 재화 UI를 읽지 못할 때 사용).")]
            public ToggleNode IgnoreCurrency { get; set; } = new ToggleNode(false);

            [Menu("타워 건설 반경", "펌프로부터 이 거리(그리드) 이내의 받침대만 건설 대상으로 고려합니다.")]
            public RangeNode<float> TowerBuildRadius { get; set; } = new RangeNode<float>(90f, 40f, 200f);

            [Menu("타워 건설 쿨다운 (ms)", "새 타워 건설을 시작하는 최소 간격입니다 (클릭 간격 아님).")]
            public RangeNode<int> TowerBuildCooldownMs { get; set; } = new RangeNode<int>(3000, 500, 10000);

            [Menu("타워 클릭 쿨다운 (ms)", "개별 클릭 동작(라벨, 메뉴 버튼) 사이의 최소 간격입니다.")]
            public RangeNode<int> TowerClickCooldownMs { get; set; } = new RangeNode<int>(200, 50, 1000);

            [Menu("타워 접근 거리", "클릭하기 전 타워에 이 거리(그리드)까지 접근합니다 — 건설/업그레이드 UI 전체가 보일 정도로 가까운 거리입니다.")]
            public RangeNode<float> TowerApproachDistance { get; set; } = new RangeNode<float>(25f, 10f, 60f);

            [Menu("타이머 종료 후 소탕 지연 (초)", "타이머 종료 후 이만큼 대기한 뒤 소탕을 시작합니다 (몬스터가 아직 스폰 중일 수 있음).")]
            public RangeNode<float> SweepDelayAfterTimerSeconds { get; set; } = new RangeNode<float>(30f, 5f, 60f);

            [Menu("소탕 제한시간 (초)", "소탕 단계에서 몬스터를 찾지 못한 채 이 시간이 지나면 포기합니다. 몬스터를 발견하거나 처치하면 초기화됩니다.")]
            public RangeNode<float> SweepTimeoutSeconds { get; set; } = new RangeNode<float>(180f, 60f, 600f);

            [Menu("소탕 중 펌프 복귀 (초)", "펌프에서 이 시간(초) 이상 떨어져 있으면 강제로 복귀합니다 — 조우 상태를 갱신하고 다른 방향의 위협을 확인합니다.")]
            public RangeNode<float> SweepPumpReturnSeconds { get; set; } = new RangeNode<float>(30f, 10f, 60f);

            [Menu("펌프 복귀 판정 반경", "펌프로부터 이 거리(그리드) 이내를 '펌프 근처'로 간주하여 복귀 타이머를 초기화합니다.")]
            public RangeNode<float> SweepPumpRadius { get; set; } = new RangeNode<float>(80f, 30f, 150f);

            [Menu("냉기 타워 (Chilling)", "냉기 타워 건설 우선순위 및 세부 설정입니다.")]
            public TowerTypeSettings Chilling { get; set; } = new TowerTypeSettings(5, canStack: false, tier3Branch: "None");

            [Menu("화염구 타워 (Fireball)", "화염구 타워 건설 우선순위 및 세부 설정입니다.")]
            public TowerTypeSettings Fireball { get; set; } = new TowerTypeSettings(4, canStack: true, tier3Branch: "Left");

            [Menu("강화 타워 (Empowering)", "강화 타워 건설 우선순위 및 세부 설정입니다.")]
            public TowerTypeSettings Empowering { get; set; } = new TowerTypeSettings(3, requiresNearbyTower: true, tier3Branch: "Left");

            [Menu("지진 타워 (Seismic)", "지진 타워 건설 우선순위 및 세부 설정입니다.")]
            public TowerTypeSettings Seismic { get; set; } = new TowerTypeSettings(2);

            [Menu("소환수 타워 (Minion)", "소환수 타워 건설 우선순위 및 세부 설정입니다.")]
            public TowerTypeSettings Minion { get; set; } = new TowerTypeSettings(0);

            [Menu("번개 충격파 타워 (ShockNova)", "번개 충격파 타워 건설 우선순위 및 세부 설정입니다.")]
            public TowerTypeSettings ShockNova { get; set; } = new TowerTypeSettings(0);

            public TowerTypeSettings GetTowerConfig(string type) => type?.ToLowerInvariant() switch
            {
                "chilling" => Chilling,
                "seismic" => Seismic,
                "empowering" => Empowering,
                "fireball" => Fireball,
                "minion" => Minion,
                "shocknova" => ShockNova,
                _ => new TowerTypeSettings(0),
            };

            public List<(string Name, TowerTypeSettings Config)> GetPriorityOrder()
            {
                var all = new List<(string Name, TowerTypeSettings Config)>
                {
                    ("Chilling", Chilling), ("Seismic", Seismic), ("Empowering", Empowering),
                    ("Fireball", Fireball), ("Minion", Minion), ("ShockNova", ShockNova),
                };
                all.RemoveAll(t => t.Config.Priority.Value <= 0);
                all.Sort((a, b) => b.Config.Priority.Value.CompareTo(a.Config.Priority.Value));
                return all;
            }
        }

        [Submenu(CollapsedByDefault = true)]
        public class TowerTypeSettings
        {
            public TowerTypeSettings() { InitBranch(); }

            public TowerTypeSettings(int defaultPriority, bool canStack = false, bool requiresNearbyTower = false, string tier3Branch = "Left")
            {
                Priority = new RangeNode<int>(defaultPriority, 0, 5);
                CanStack = new ToggleNode(canStack);
                RequiresNearbyTower = new ToggleNode(requiresNearbyTower);
                InitBranch();
                Tier3Branch.Value = tier3Branch;
            }

            private void InitBranch()
            {
                Tier3Branch.SetListValues(new List<string> { "None", "Left", "Right" });
                if (string.IsNullOrEmpty(Tier3Branch.Value))
                    Tier3Branch.Value = "Left";
            }

            [Menu("우선순위", "건설 우선순위입니다 (0=건설 안 함, 5=최우선).")]
            public RangeNode<int> Priority { get; set; } = new RangeNode<int>(3, 0, 5);

            [Menu("중첩 가능", "효과 반경 내에 같은 타워를 여러 개 지을 수 있도록 허용합니다.")]
            public ToggleNode CanStack { get; set; } = new ToggleNode(false);

            [Menu("인접 타워 필요", "효과 반경 내에 이득을 볼 다른 타워가 있을 때만 건설합니다.")]
            public ToggleNode RequiresNearbyTower { get; set; } = new ToggleNode(false);

            [Menu("3티어 분기", "최종 업그레이드 경로입니다 (None=3티어에서 멈춤, Left, Right).")]
            public ListNode Tier3Branch { get; set; } = new ListNode() { Value = "Left" };
        }
    }
}

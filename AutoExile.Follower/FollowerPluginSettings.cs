using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;

namespace AutoExile.Follower
{
    /// <summary>
    /// Follower(추종) 모드 전용 설정. 공유 설정(SharedSettings)에 추종 섹션을 추가합니다.
    /// </summary>
    public class FollowerPluginSettings : SharedSettings
    {
        [Menu("추종 설정", "파티원 추종 모드 전용 설정입니다.")]
        public FollowerSettings Follower { get; set; } = new FollowerSettings();

        [Submenu(CollapsedByDefault = true)]
        public class FollowerSettings
        {
            [Menu("리더 이름", "추종할 캐릭터 이름입니다.")]
            public TextNode LeaderName { get; set; } = new TextNode("");

            [Menu("추종 거리", "리더와 이 거리(그리드 단위)보다 멀어지면 추종을 시작합니다.")]
            public RangeNode<int> FollowDistance { get; set; } = new RangeNode<int>(28, 5, 100);

            [Menu("정지 거리", "리더와 이 거리(그리드 단위) 이내로 들어오면 이동을 멈춥니다.")]
            public RangeNode<int> StopDistance { get; set; } = new RangeNode<int>(14, 3, 50);

            [Menu("구역 전환 시 함께 이동", "리더가 구역(지역) 전환을 할 때 함께 따라갑니다.")]
            public ToggleNode FollowThroughTransitions { get; set; } = new ToggleNode(true);

            [Menu("전투 사용", "추종 중 몬스터와 전투를 벌입니다. 빌드/스킬/플라스크 설정을 사용합니다.")]
            public ToggleNode EnableCombat { get; set; } = new ToggleNode(false);

            [Menu("일반 루팅 사용", "추종 중 필터에 걸린 모든 아이템을 주웁니다. 꺼져있으면 퀘스트 아이템만 줍습니다.")]
            public ToggleNode EnableLoot { get; set; } = new ToggleNode(false);

            [Menu("리더 근처에서만 루팅", "리더의 추종 거리 이내에 있을 때만 루팅합니다 (루팅하러 멀리 벗어나지 않음).")]
            public ToggleNode LootNearLeaderOnly { get; set; } = new ToggleNode(true);
        }
    }
}

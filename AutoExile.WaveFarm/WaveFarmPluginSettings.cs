using System.Collections.Generic;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;

namespace AutoExile.WaveFarm
{
    /// <summary>
    /// WaveFarm(파밍) 모드 전용 설정. 공유 설정(SharedSettings)에 파밍 섹션을 추가합니다.
    /// </summary>
    public class WaveFarmPluginSettings : SharedSettings
    {
        [Menu("파밍 설정", "WaveFarm(파밍) 모드 전용 설정입니다.")]
        public FarmingSettings Farming { get; set; } = new FarmingSettings();

        [Submenu(CollapsedByDefault = true)]
        public class FarmingSettings
        {
            public FarmingSettings()
            {
                FarmStrategy.SetListValues(new List<string> { "Stacked Deck", "Alch & Go" });
                FarmStrategy.Value = "Stacked Deck";

                MapName.SetListValues(new List<string> { "" });
                MapName.Value = "";

                var witnessOptions = new List<string> { "None", "Searing Exarch", "Eater of Worlds", "Maven" };
                WitnessType.SetListValues(witnessOptions);
                WitnessType.Value = "None";
            }

            [Menu("파밍 전략", "사용할 파밍 전략을 선택합니다. (값은 게임 내 UI 텍스트와 일치해야 하므로 영문 그대로 유지합니다.)")]
            public ListNode FarmStrategy { get; set; } = new ListNode();

            [Menu("맵 이름", "파밍할 맵입니다.")]
            public ListNode MapName { get; set; } = new ListNode();

            [Menu("최소 몬스터 밀집도", "탐험을 멈추고 전투로 전환할 가중치 밀집도입니다. 희귀는 5, 고유는 8로 계산됩니다. 0 = 몬스터 무리를 위해 절대 우회하지 않음. 값이 낮을수록 더 적극적으로 전투에 임합니다(화염방사/근접 빌드에 적합).")]
            public RangeNode<int> MinPackDensity { get; set; } = new RangeNode<int>(5, 0, 30);

            [Menu("희귀몹 우회 추적", "밀집도가 임계값 미만이어도 범위 내 희귀/고유 몬스터가 있으면 항상 우회하여 전투합니다.")]
            public ToggleNode DetourForRares { get; set; } = new ToggleNode(true);

            [Menu("최대 우회 거리", "현재 경로에서 희귀/고유 몬스터를 쫓아갈 최대 그리드 거리입니다.")]
            public RangeNode<int> MaxDetourDistance { get; set; } = new RangeNode<int>(60, 10, 150);

            [Menu("최소 탐험 커버리지", "클리어 완료로 간주하기 위한 최소 맵 탐험 비율입니다. 값이 높을수록 더 철저하게 클리어합니다.")]
            public RangeNode<float> MinCoverage { get; set; } = new RangeNode<float>(0.85f, 0f, 1f);

            [Menu("감시자 타입", "사용할 엔드게임 감시자(Witness)입니다. 전략에 따라 기본값이 채워지며 직접 변경할 수 있습니다.")]
            public ListNode WitnessType { get; set; } = new ListNode();

            [Menu("아틀라스 트리 프리셋", "사용할 아틀라스 패시브 트리 프리셋(1~3)입니다. 0 = 전환하지 않음.")]
            public RangeNode<int> AtlasTreePreset { get; set; } = new RangeNode<int>(0, 0, 3);

            [Menu("스택 덱 전략 설정", "스택 덱(Stacked Deck) 전략 전용 세부 설정입니다.")]
            public StackedDeckStrategySettings StackedDeck { get; set; } = new StackedDeckStrategySettings();
        }

        [Submenu(CollapsedByDefault = false)]
        public class StackedDeckStrategySettings
        {
            public StackedDeckStrategySettings()
            {
                PreferredWish.SetListValues(new List<string> { "Any", "Coin of Power", "Coin of Skill", "Coin of Knowledge" });
                PreferredWish.Value = "Coin of Power";
            }

            [Menu("즉시 구매 가치 (카오스)", "리롤 전략과 무관하게, 여유 자금이 있으면 이 가치 이상의 아이템을 즉시 구매합니다. 0 = 비활성화.")]
            public RangeNode<int> AlwaysBuyValue { get; set; } = new RangeNode<int>(100, 0, 1000);

            [Menu("최소 구매 가치 (카오스)", "구매를 고려할 최소 카오스 가치입니다. 마지막 상점(리롤 불가)에서는 이 값 이상인 모든 아이템을 카오스/공물 비율이 좋은 순으로 구매합니다.")]
            public RangeNode<int> MinBuyValue { get; set; } = new RangeNode<int>(3, 0, 100);

            [Menu("리롤 구매 임계값 (카오스)", "상점 전체 가치가 이 값을 넘어야 리롤 대신 구매합니다. 이 값 미만이면 가능한 경우 리롤을 위해 공물을 아낍니다.")]
            public RangeNode<int> RerollBuyThreshold { get; set; } = new RangeNode<int>(50, 0, 500);

            [Menu("공물 보존량", "리롤이 아직 가능할 때, 구매로 인해 공물이 이 값 아래로 내려가면 구매하지 않습니다. 더 나은 리롤을 위해 공물을 아낍니다.")]
            public RangeNode<int> TributeReserve { get; set; } = new RangeNode<int>(3000, 0, 10000);

            [Menu("선호 소원 코인", "소원 선택이 동률일 때 우선할 코인 종류입니다. 최고 가치의 소원이 항상 우선 선택되며, 이 설정은 동률일 때만 사용됩니다.")]
            public ListNode PreferredWish { get; set; } = new ListNode();
        }
    }
}

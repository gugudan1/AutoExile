using System.Collections.Generic;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;

namespace AutoExile.Labyrinth
{
    /// <summary>
    /// Labyrinth(미궁) 모드 전용 설정. 공유 설정(SharedSettings)에 미궁 섹션을 추가합니다.
    /// </summary>
    public class LabyrinthPluginSettings : SharedSettings
    {
        [Menu("미궁 설정", "Labyrinth(미궁) 모드 전용 설정입니다.")]
        public LabyrinthSettings Labyrinth { get; set; } = new LabyrinthSettings();

        [Submenu(CollapsedByDefault = true)]
        public class LabyrinthSettings
        {
            [Menu("난이도", "진행할 미궁 난이도입니다. 게임 내 UI 텍스트와 정확히 일치해야 하므로 영문 그대로 유지합니다.")]
            public ListNode Difficulty { get; set; } = new ListNode
            {
                Values = new List<string>
                {
                    "The Labyrinth",
                    "The Cruel Labyrinth",
                    "The Merciless Labyrinth",
                    "The Eternal Labyrinth",
                },
                Value = "The Labyrinth",
            };

            [Menu("최대 실행 횟수", "이 횟수만큼 완료하면 중지합니다. 0 = 무제한.")]
            public RangeNode<int> MaxRuns { get; set; } = new RangeNode<int>(0, 0, 100);

            [Menu("최소 기대 수익 (카오스)", "변환을 시작하기 위한 최소 기대 수익입니다.")]
            public RangeNode<int> MinExpectedProfit { get; set; } = new RangeNode<int>(10, 0, 500);

            [Menu("보상 상자 개봉", "보상 방의 이자로(Izaro) 보물 상자를 개봉합니다.")]
            public ToggleNode OpenRewardChests { get; set; } = new ToggleNode(true);

            [Menu("구역 제한시간 (초)", "각 구역에서 이 시간이 지나면 진행을 포기합니다.")]
            public RangeNode<int> ZoneTimeoutSeconds { get; set; } = new RangeNode<int>(120, 30, 300);

            [Menu("이자로 전투 제한시간 (초)", "이자로와의 전투를 이 시간이 지나면 포기합니다.")]
            public RangeNode<int> IzaroTimeoutSeconds { get; set; } = new RangeNode<int>(60, 20, 180);

            [Menu("같은 종류 우선", "가능하면 '같은 색상'보다 '같은 종류' 변환을 우선합니다.")]
            public ToggleNode PreferSameType { get; set; } = new ToggleNode(true);

            [Menu("보존 임계값 (카오스)", "이 가치 이상의 스킬젬은 변환하지 않습니다. 가치 있는 결과물이 재활용되는 것을 방지합니다. 0 = 비활성화.")]
            public RangeNode<int> KeepThreshold { get; set; } = new RangeNode<int>(10, 0, 500);
        }
    }
}

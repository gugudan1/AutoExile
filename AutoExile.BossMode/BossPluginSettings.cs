using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;

namespace AutoExile.BossMode
{
    /// <summary>
    /// Boss 모드 전용 설정. 공유 설정(SharedSettings)에 보스 파밍 섹션을 추가합니다.
    /// </summary>
    public class BossPluginSettings : SharedSettings
    {
        [Menu("보스 파밍 설정", "보스 파밍 모드 전용 설정입니다.")]
        public BossSettings Boss { get; set; } = new BossSettings();

        [Submenu(CollapsedByDefault = false)]
        public class BossSettings
        {
            [Menu("보스 종류", "파밍할 보스 조우를 선택합니다.")]
            public ListNode BossType { get; set; } = new ListNode();

            [Menu("파편 재고", "인벤토리에 이 개수만큼 파편을 유지합니다. 재고 유지를 위해 자원 탭에서 인출합니다.")]
            public RangeNode<int> FragmentStock { get; set; } = new RangeNode<int>(20, 1, 60);

            [Menu("열쇠 드랍 가치 (카오스)", "수익 추적을 위한 열쇠 드랍 1개당 추정 카오스 가치. 대상 아이템의 평균 미감정 가치로 설정하세요.")]
            public RangeNode<int> KeyDropChaosValue { get; set; } = new RangeNode<int>(15, 0, 500);

            [Menu("공포 딜 위치", "공포의 화신(Incarnation of Fear) 사전 설치 단계에서 서 있을 그리드 위치. 형식: X,Y. '현재 위치 저장' 버튼으로 설정 가능. 비우면 기본값(206,320) 사용.")]
            public TextNode FearDpsPosition { get; set; } = new TextNode();
        }
    }
}

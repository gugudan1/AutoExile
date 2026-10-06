using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;

namespace AutoExile.Simulacrum
{
    /// <summary>
    /// Simulacrum(허상) 모드 전용 설정. 공유 설정(SharedSettings)에 Simulacrum 섹션을 추가합니다.
    /// </summary>
    public class SimulacrumPluginSettings : SharedSettings
    {
        [Menu("허상 파밍 설정", "Simulacrum(허상) 파밍 모드 전용 설정입니다.")]
        public SimulacrumSettings Simulacrum { get; set; } = new SimulacrumSettings();

        [Submenu(CollapsedByDefault = true)]
        public class SimulacrumSettings
        {
            [Menu("최소 웨이브 지연 (초)", "웨이브 종료 후 다음 웨이브 시작까지 최소 대기 시간입니다.")]
            public RangeNode<float> MinWaveDelaySeconds { get; set; } = new RangeNode<float>(5f, 1f, 30f);

            [Menu("웨이브 제한시간 (분)", "웨이브당 이 시간이 지나면 진행을 포기합니다.")]
            public RangeNode<float> WaveTimeoutMinutes { get; set; } = new RangeNode<float>(3f, 1f, 10f);

            [Menu("허상 재고", "인벤토리에 이 개수만큼 완성된 허상을 유지합니다. 재고 유지를 위해 중앙 파편 탭(창고 설정)에서 인출합니다.")]
            public RangeNode<int> SimulacrumStock { get; set; } = new RangeNode<int>(5, 1, 20);
        }
    }
}

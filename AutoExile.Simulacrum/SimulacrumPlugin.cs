using AutoExile.Modes;

namespace AutoExile.Simulacrum
{
    /// <summary>
    /// 허상(Simulacrum) 파밍 전용 독립 플러그인. SimulacrumMode 하나만 구동하며, 설정 메뉴는
    /// SimulacrumPluginSettings(공유 설정 + 허상 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class SimulacrumPlugin : PluginHostBase<SimulacrumPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.SimulacrumMode();
    }
}

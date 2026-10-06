using AutoExile.Modes;

namespace AutoExile.Heist
{
    /// <summary>
    /// Heist(강탈) 전용 독립 플러그인. HeistMode 하나만 구동하며, 설정 메뉴는
    /// HeistPluginSettings(공유 설정 + 강탈 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class HeistPlugin : PluginHostBase<HeistPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.HeistMode();
    }
}

using AutoExile.Modes;

namespace AutoExile.Blight
{
    /// <summary>
    /// Blight(역병) 농사 전용 독립 플러그인. BlightMode 하나만 구동하며, 설정 메뉴는
    /// BlightPluginSettings(공유 설정 + Blight 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class BlightPlugin : PluginHostBase<BlightPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.BlightMode();
    }
}

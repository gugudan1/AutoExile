using AutoExile.Modes;

namespace AutoExile.HideoutFlow
{
    /// <summary>
    /// 하이드아웃 자동화 전용 독립 플러그인. HideoutFlowMode 하나만 구동하며,
    /// 설정 메뉴는 공유 설정(SharedSettings)을 통해 자동 렌더링됩니다.
    /// </summary>
    public class HideoutFlowPlugin : PluginHostBase<HideoutFlowPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.HideoutFlowMode();
    }
}

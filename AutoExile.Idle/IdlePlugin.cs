using AutoExile.Modes;

namespace AutoExile.Idle
{
    /// <summary>
    /// Idle(대기) 전용 독립 플러그인. IdleMode만 구동하며 별도의 모드 전용 설정 없이
    /// 공유 설정(SharedSettings)만 사용합니다.
    /// </summary>
    public class IdlePlugin : PluginHostBase<SharedSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.IdleMode();
    }
}

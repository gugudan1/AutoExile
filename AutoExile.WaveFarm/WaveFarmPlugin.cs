using AutoExile.Modes;

namespace AutoExile.WaveFarm
{
    /// <summary>
    /// WaveFarm(파밍) 전용 독립 플러그인. WaveFarmMode 하나만 구동하며, 설정 메뉴는
    /// WaveFarmPluginSettings(공유 설정 + 파밍 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class WaveFarmPlugin : PluginHostBase<WaveFarmPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.WaveFarm.WaveFarmMode();
    }
}

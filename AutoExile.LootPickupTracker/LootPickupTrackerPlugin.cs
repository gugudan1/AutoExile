using AutoExile.Modes;

namespace AutoExile.LootPickupTracker
{
    /// <summary>
    /// 전리품 자동 습득 전용 독립 플러그인. LootPickupMode 하나만 구동하며,
    /// 설정 메뉴는 공유 설정(SharedSettings)을 통해 자동 렌더링됩니다.
    /// </summary>
    public class LootPickupTrackerPlugin : PluginHostBase<LootPickupTrackerPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.LootPickupMode();
    }
}

using AutoExile.Modes;

namespace AutoExile.Labyrinth
{
    /// <summary>
    /// 미궁(Labyrinth) 전용 독립 플러그인. LabyrinthMode 하나만 구동하며, 설정 메뉴는
    /// LabyrinthPluginSettings(공유 설정 + 미궁 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class LabyrinthPlugin : PluginHostBase<LabyrinthPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.LabyrinthMode();
    }
}

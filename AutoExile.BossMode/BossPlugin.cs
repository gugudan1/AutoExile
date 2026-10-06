using AutoExile.Modes;
using AutoExile.Modes.BossEncounters;
using System.Linq;

namespace AutoExile.BossMode
{
    /// <summary>
    /// 보스 파밍 전용 독립 플러그인. BossMode 하나만 구동하며, 설정 메뉴는
    /// BossPluginSettings(공유 설정 + 보스 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class BossPlugin : PluginHostBase<BossPluginSettings>
    {
        private AutoExile.Modes.BossMode? _bossMode;

        protected override IBotMode CreateMode()
        {
            _bossMode = new AutoExile.Modes.BossMode();
            _bossMode.Register(new KingEncounter());
            _bossMode.Register(new OshabiEncounter());
            _bossMode.Register(new FearEncounter());
            _bossMode.Register(new MavenEncounter());
            _bossMode.Register(new SareshEncounter());
            return _bossMode;
        }

        protected override void OnInitialisedHook()
        {
            // 보스 종류 드롭다운 채우기 — SetListValues 호출 시 값이 초기화되므로 저장 후 복원
            var bossSettings = ((BossPluginSettings)Settings).Boss;
            var saved = bossSettings.BossType.Value;
            bossSettings.BossType.SetListValues(_bossMode!.EncounterNames.ToList());
            if (!string.IsNullOrEmpty(saved) && _bossMode.EncounterNames.Contains(saved))
                bossSettings.BossType.Value = saved;
        }

        protected override void OnPlayerDeath()
        {
            _bossMode?.IncrementDeathCount();
        }
    }
}

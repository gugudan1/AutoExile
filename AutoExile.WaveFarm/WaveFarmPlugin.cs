using AutoExile.Modes;
using System.Collections.Generic;
using System.Linq;

namespace AutoExile.WaveFarm
{
    /// <summary>
    /// WaveFarm(파밍) 전용 독립 플러그인. WaveFarmMode 하나만 구동하며, 설정 메뉴는
    /// WaveFarmPluginSettings(공유 설정 + 파밍 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class WaveFarmPlugin : PluginHostBase<WaveFarmPluginSettings>
    {
        protected override IBotMode CreateMode() => new AutoExile.Modes.WaveFarm.WaveFarmMode();

        protected override void OnMapListReady(List<string> mapNames)
        {
            // 맵 선택 드롭다운 채우기 — SetListValues 호출 시 값이 초기화되므로 저장 후 복원
            var farming = ((WaveFarmPluginSettings)Settings).Farming;
            var saved = farming.MapName.Value;
            farming.MapName.SetListValues(mapNames);
            if (!string.IsNullOrEmpty(saved))
            {
                var match = mapNames.FirstOrDefault(m =>
                    m.TrimStart('\u2605', ' ') == saved.TrimStart('\u2605', ' '));
                if (match != null)
                    farming.MapName.Value = match;
            }
        }
    }
}

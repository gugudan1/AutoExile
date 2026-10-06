using AutoExile;

namespace AutoExile.LootPickupTracker
{
    /// <summary>
    /// 전리품 자동 습득 모드 전용 설정 클래스. 추가 설정 항목은 없지만, ExileCore의
    /// PluginManager가 플러그인 어셈블리 내부에서 ISettings 파생 타입을 찾지 못하면
    /// "Not found setting class" 오류로 로드를 포기하므로, 공유 설정을 상속하는
    /// 전용 타입을 이 어셈블리 안에 선언해 둔다.
    /// </summary>
    public class LootPickupTrackerPluginSettings : SharedSettings
    {
    }
}

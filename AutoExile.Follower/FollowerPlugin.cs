using AutoExile.Modes;

namespace AutoExile.Follower
{
    /// <summary>
    /// 파티원 추종 전용 독립 플러그인. FollowerMode 하나만 구동하며, 설정 메뉴는
    /// FollowerPluginSettings(공유 설정 + 추종 전용 섹션)를 통해 자동 렌더링됩니다.
    /// </summary>
    public class FollowerPlugin : PluginHostBase<FollowerPluginSettings>
    {
        private AutoExile.Modes.FollowerMode? _followerMode;

        protected override IBotMode CreateMode()
        {
            _followerMode = new AutoExile.Modes.FollowerMode();
            return _followerMode;
        }

        /// <summary>
        /// FollowerMode는 설정 값을 ctx.Settings에서 직접 읽지 않고 공개 속성으로 보관하므로,
        /// 매 틱마다 설정 값을 모드 인스턴스에 동기화합니다 (원본 BotCore.cs와 동일한 방식).
        /// </summary>
        protected override void BeforeModeTick()
        {
            if (_followerMode == null) return;

            var s = ((FollowerPluginSettings)Settings).Follower;
            _followerMode.LeaderName = s.LeaderName.Value;
            _followerMode.FollowDistance = s.FollowDistance.Value;
            _followerMode.StopDistance = s.StopDistance.Value;
            _followerMode.FollowThroughTransitions = s.FollowThroughTransitions.Value;
            _followerMode.EnableCombat = s.EnableCombat.Value;
            _followerMode.EnableLoot = s.EnableLoot.Value;
            _followerMode.LootNearLeaderOnly = s.LootNearLeaderOnly.Value;
        }
    }
}

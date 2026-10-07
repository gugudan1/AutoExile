using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using AutoExile;
using AutoExile.Systems;

namespace AutoExile.Heist
{
    /// <summary>
    /// Heist(강탈) 모드 전용 설정. 공유 설정(SharedSettings)에 Heist 섹션을 추가합니다.
    /// </summary>
    public class HeistPluginSettings : SharedSettings
    {
        [Menu("강탈 설정", "Heist(강탈) 모드 전용 설정입니다.")]
        public HeistSettings Heist { get; set; } = new HeistSettings();

        [Submenu(CollapsedByDefault = false)]
        public class HeistSettings
        {
            [Menu("동료 상호작용 키", "문/상자 근처에서 동료 상호작용을 위해 누를 키입니다 (기본값 V).")]
            public System.Windows.Forms.Keys CompanionInteractKey { get; set; } = System.Windows.Forms.Keys.V;

            [Menu("경보 임계치 %", "이 경보 수치를 넘으면 사이드 상자 개봉을 멈춥니다.")]
            public RangeNode<float> AlertThreshold { get; set; } = new RangeNode<float>(70f, 20f, 95f);

            [Menu("최대 상자 우회 거리 (그리드)", "보상 상자를 위해 우회할 수 있는 최대 그리드 거리입니다.")]
            public RangeNode<float> MaxChestDetour { get; set; } = new RangeNode<float>(30f, 10f, 80f);

            [Menu("보상 상자 개봉", "침투 중 가치 있는 보상 상자를 개봉합니다.")]
            public ToggleNode OpenRewardChests { get; set; } = new ToggleNode(true);

            [Menu("동료 대기 제한시간 (초)", "동료가 자물쇠를 여는 것을 기다리는 최대 시간입니다.")]
            public RangeNode<float> CompanionWaitTimeout { get; set; } = new RangeNode<float>(30f, 10f, 60f);

            [Menu("동료 재시도 지연 (초)", "이 시간 후에도 동료가 채널링을 시작하지 않으면 다시 클릭합니다.")]
            public RangeNode<float> CompanionRetryDelay { get; set; } = new RangeNode<float>(10f, 5f, 20f);

            // --- 보상 종류별 토글 ---

            [Menu("보상: 화폐", "화폐 보상 상자를 개봉합니다.")]
            public ToggleNode RewardCurrency { get; set; } = new ToggleNode(true);

            [Menu("보상: 방어구", "방어구 보상 상자를 개봉합니다.")]
            public ToggleNode RewardArmour { get; set; } = new ToggleNode(true);

            [Menu("보상: 무기", "무기 보상 상자를 개봉합니다.")]
            public ToggleNode RewardWeapons { get; set; } = new ToggleNode(true);

            [Menu("보상: 스킬젬", "스킬젬 보상 상자를 개봉합니다.")]
            public ToggleNode RewardGems { get; set; } = new ToggleNode(true);

            [Menu("보상: 예언카드", "예언카드 보상 상자를 개봉합니다.")]
            public ToggleNode RewardDivinationCards { get; set; } = new ToggleNode(true);

            [Menu("보상: 유니크", "유니크 아이템 보상 상자를 개봉합니다.")]
            public ToggleNode RewardUniques { get; set; } = new ToggleNode(true);

            [Menu("보상: 장신구", "장신구 보상 상자를 개봉합니다.")]
            public ToggleNode RewardJewellery { get; set; } = new ToggleNode(true);

            [Menu("보상: 에센스", "에센스 보상 상자를 개봉합니다.")]
            public ToggleNode RewardEssences { get; set; } = new ToggleNode(true);

            [Menu("보상: 파편", "파편 보상 상자를 개봉합니다.")]
            public ToggleNode RewardFragments { get; set; } = new ToggleNode(true);

            [Menu("보상: 지도", "지도 보상 상자를 개봉합니다.")]
            public ToggleNode RewardMaps { get; set; } = new ToggleNode(false);

            [Menu("보상: 주얼", "주얼 보상 상자를 개봉합니다.")]
            public ToggleNode RewardJewels { get; set; } = new ToggleNode(false);

            [Menu("보상: 타락", "타락한 아이템 보상 상자를 개봉합니다.")]
            public ToggleNode RewardCorrupted { get; set; } = new ToggleNode(false);

            /// <summary>설정에서 해당 보상 종류가 활성화되어 있는지 확인합니다.</summary>
            public bool IsRewardTypeEnabled(HeistRewardType type) => type switch
            {
                HeistRewardType.Currency or HeistRewardType.QualityCurrency => RewardCurrency.Value,
                HeistRewardType.Armour => RewardArmour.Value,
                HeistRewardType.Weapons => RewardWeapons.Value,
                HeistRewardType.Gems => RewardGems.Value,
                HeistRewardType.DivinationCards or HeistRewardType.StackedDecks => RewardDivinationCards.Value,
                HeistRewardType.Uniques => RewardUniques.Value,
                HeistRewardType.Jewellery => RewardJewellery.Value,
                HeistRewardType.Essences => RewardEssences.Value,
                HeistRewardType.Fragments => RewardFragments.Value,
                HeistRewardType.Maps => RewardMaps.Value,
                HeistRewardType.Jewels => RewardJewels.Value,
                HeistRewardType.Corrupted => RewardCorrupted.Value,
                HeistRewardType.Smugglers => true,
                HeistRewardType.Safe => true,
                _ => false,
            };
        }
    }
}

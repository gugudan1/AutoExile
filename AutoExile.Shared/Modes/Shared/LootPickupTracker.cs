using AutoExile.Systems;

namespace AutoExile.Modes.Shared
{
    /// <summary>
    /// 대기 중인(pending) 전리품 습득 상태를 추적하고 성공/실패 결과를 처리합니다.
    /// 각 모드마다 중복되던 _pendingLootEntityId/_pendingLootName/_pendingLootValue 필드와
    /// HandleLootResult 로직을 대체합니다.
    /// </summary>
    public class LootPickupTracker
    {
        private long _pendingEntityId;
        private string _pendingItemName = "";
        private double _pendingValue;
        private int _pickupCount;

        public bool HasPending => _pendingEntityId != 0;
        public long PendingEntityId => _pendingEntityId;
        public string PendingItemName => _pendingItemName;
        public double PendingChaosValue => _pendingValue;
        public int PickupCount => _pickupCount;


        /// <summary>
        /// InteractionSystem을 통해 습득을 시작한 직후 호출합니다.
        /// </summary>
        public void SetPending(long entityId, string itemName, double chaosValue)
        {
            _pendingEntityId = entityId;
            _pendingItemName = itemName;
            _pendingValue = chaosValue;
        }

        /// <summary>
        /// 상호작용 결과를 처리합니다. 성공(Succeeded) 시: LootTracker에 기록 + 카운트 증가.
        /// 실패(Failed) 시: LootSystem에 실패로 표시. 두 경우 모두 대기 상태를 초기화합니다.
        /// </summary>
        public void HandleResult(InteractionResult result, BotContext ctx)
        {
            if (_pendingEntityId == 0) return;

            if (result == InteractionResult.Succeeded)
            {
                ctx.LootTracker.RecordItem(_pendingItemName, _pendingValue, _pendingEntityId);
                _pickupCount++;
                // 엔티티를 블랙리스트에 올려 재습득을 방지합니다.
                // (습득 성공 후에도 엔티티가 잠깐 다시 바닥에 나타날 수 있음)
                ctx.Loot.MarkFailed(_pendingEntityId, "picked up");
            }
            else if (result == InteractionResult.Failed)
            {
                var failReason = ctx.Interaction.LastFailReason;
                ctx.Loot.MarkFailed(_pendingEntityId, failReason);
                ctx.Loot.LogSkipEvent(_pendingEntityId, _pendingItemName,
                    $"pickup failed: {failReason}", _pendingValue);
            }

            if (result == InteractionResult.Succeeded || result == InteractionResult.Failed)
            {
                _pendingEntityId = 0;
                _pendingItemName = "";
                _pendingValue = 0;
            }
        }

        /// <summary>
        /// 모든 상태를 초기화합니다 (지역 변경이나 단계 리셋 시).
        /// </summary>
        public void Reset()
        {
            _pendingEntityId = 0;
            _pendingItemName = "";
            _pendingValue = 0;
            _pickupCount = 0;
        }

        /// <summary>
        /// 습득 카운트만 초기화합니다 (맵 실행 사이에 대기 상태는 유지하면서).
        /// </summary>
        public void ResetCount() => _pickupCount = 0;
    }
}

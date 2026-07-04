using System.Collections.Generic;

namespace BlockSort.Purchase
{
    /// <summary>
    /// Interface providing game-specific purchase fulfillment logic (applying currency/items and paid status) for the PurchaseManager.
    /// Implemented by PlayerDataManager or other classes in the main game project.
    /// </summary>
    public interface IPurchaseDataProvider
    {
        void ApplyLootRewards(Dictionary<CurrencyType, int> rewards, string source);
        void MarkAsPaid();
    }
}

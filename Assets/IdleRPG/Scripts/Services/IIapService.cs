using System;

namespace IdleRPG.Services
{
    /// <summary>
    /// Store-billing abstraction (B7 S1). The game only knows SKUs from <see cref="IapCatalog"/>; a real store
    /// (Unity IAP -> App Store / Play Billing) implements this interface and nothing else in the game changes.
    ///
    /// ELI5: the game says "I want to buy product X" and the cash register answers "done" or "no". The game never
    /// cares WHO the register is - fake for testing today, Apple/Google for launch.
    /// </summary>
    public interface IIapService
    {
        /// <summary>Raised after any purchase or restore changes ownership (the shop re-renders).</summary>
        event Action PurchasesChanged;

        /// <summary>True when the store connection is ready to take a purchase.</summary>
        bool IsInitialized { get; }

        /// <summary>
        /// True when a NON-consumable product is owned (no-ads). Consumables (gem packs) are never "owned"
        /// between purchases — they are granted immediately and forget that they happened, like the real stores do.
        /// </summary>
        bool IsOwned(string sku);

        /// <summary>
        /// Begins a purchase. <paramref name="onCompleted"/> is always invoked exactly once, true only when the
        /// store confirmed the payment. The implementation logs its own failure reason; the caller just reacts.
        /// </summary>
        void Purchase(string sku, Action<bool> onCompleted);

        /// <summary>
        /// Re-checks ownership of non-consumables (iOS "Restore Purchases" button, and after a reinstall).
        /// <paramref name="onCompleted"/> receives true when the check ran without errors (not "anything owned").
        /// </summary>
        void RestorePurchases(Action<bool> onCompleted);
    }
}
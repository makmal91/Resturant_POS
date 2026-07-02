using POSSystem.Domain;

namespace POSSystem.Application.CashFlow;

public static class CashFlowEffectHelper
{
    private static readonly CashFlowTransactionType[] InflowTypes =
    [
        CashFlowTransactionType.Sale,
        CashFlowTransactionType.CashIn,
        CashFlowTransactionType.OpeningBalance,
    ];

    public static bool IsInflow(CashFlowTransactionType type, CashFlowTransactionType? reversedType = null)
    {
        if (type == CashFlowTransactionType.Reversal)
        {
            if (!reversedType.HasValue)
                return false;

            return !IsInflow(reversedType.Value);
        }

        return InflowTypes.Contains(type);
    }

    public static decimal GetSignedAmount(
        CashFlowTransactionType type,
        decimal amount,
        CashFlowTransactionType? reversedType = null)
    {
        if (amount < 0 && type != CashFlowTransactionType.Reversal)
            return amount;

        var abs = Math.Abs(amount);
        return IsInflow(type, reversedType) ? abs : -abs;
    }

    public static decimal GetDisplayAmount(decimal amount) => Math.Abs(amount);

    /// <summary>
    /// Maps cash movements to ledger debit/credit columns (cash account view).
    /// Cash in (sales, receipts) → Debit; cash out (expenses, payments) → Credit.
    /// </summary>
    public static (decimal Debit, decimal Credit) SplitDebitCredit(
        CashFlowTransactionType type,
        decimal amount,
        CashFlowTransactionType? reversedType = null)
    {
        var display = GetDisplayAmount(amount);
        var signed = GetSignedAmount(type, amount, reversedType);

        if (signed > 0)
            return (display, 0);

        if (signed < 0)
            return (0, display);

        return (0, 0);
    }
}

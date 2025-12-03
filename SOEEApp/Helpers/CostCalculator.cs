using System.Linq;
using SOEEApp.Models;
using System.Collections.Generic;

namespace SOEEApp.Helpers
{
    public static class CostCalculator
    {
        public static decimal CalculateTotal(decimal unit, decimal qty, decimal unitRate)
                => unit * qty * unitRate;

        public static decimal CalculateServiceChargeAmount(decimal total, decimal percent)
            => (total * percent) / 100;

        public static decimal CalculateTaxAmount(decimal taxable, decimal percent)
            => (taxable * percent) / 100;

        public static ComputedTotals ComputeTotalsForItems(
    IEnumerable<SOEEItem> items,
    ApplicationDbContext db,
    int projectId,
    int soeeId)
        {
            var result = new ComputedTotals();

            // work on active items only
            var activeItems = items.Where(x => !x.IsDeleted).ToList();
            if (!activeItems.Any()) return result;

            // Previous SOEE totals ignored for now
            decimal previousTotal = 0m;

            // We'll use a runningBasic to compute slab thresholds progressively.
            decimal runningBasic = 0m;
            decimal cumulativeBeforeItems = previousTotal;

            foreach (var item in activeItems)
            {
                // compute subTotal and persist it
                decimal subTotal = item.Unit * item.Quantity * item.UnitPrice;
                item.SubTotal = decimal.Round(subTotal, 2);

                // determine if we need to recalc percentage (0 means new or changed item)
                bool mustRecalc = item.ServiceChargePercent <= 0m;

                if (mustRecalc)
                {
                    // cumulativeAfter = running total for slab calculation
                    decimal cumulativeAfter = cumulativeBeforeItems + runningBasic + subTotal;

                    // lookup slab percent from mapping
                    decimal slabPercent = db.ServiceTypeSlabMaps
                        .Where(m =>
                            m.ServiceTypeID == item.ServiceTypeID &&
                            cumulativeAfter >= m.Slab.MinAmount &&
                            cumulativeAfter <= m.Slab.MaxAmount)
                        .Select(m => (decimal?)m.Percentage)
                        .FirstOrDefault() ?? 0m;

                    item.ServiceChargePercent = slabPercent;
                }

                // apply stored or recalculated percentage
                decimal pct = item.ServiceChargePercent;
                item.ServiceCharge = decimal.Round(subTotal * (pct / 100m), 2);

                // taxes on (subtotal + serviceCharge)
                decimal taxable = subTotal + item.ServiceCharge;
                item.CGST = decimal.Round(taxable * 0.09m, 2);
                item.SGST = decimal.Round(taxable * 0.09m, 2);

                // total for this item
                item.Total = decimal.Round(subTotal + item.ServiceCharge + item.CGST + item.SGST, 2);

                // update runningBasic
                runningBasic += subTotal;
            }

            // totals for the SOEE
            result.Basic = activeItems.Sum(i => i.SubTotal);
            result.ServiceCharge = activeItems.Sum(i => i.ServiceCharge);
            result.CGST = activeItems.Sum(i => i.CGST);
            result.SGST = activeItems.Sum(i => i.SGST);
            result.Total = activeItems.Sum(i => i.Total);

            return result;
        }

    }
}

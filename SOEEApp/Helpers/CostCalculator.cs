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

            // work on the active items list
            var activeItems = items.Where(x => !x.IsDeleted).ToList();
            if (!activeItems.Any()) return result;

            // BASIC so far (based on items' current unit/qty/unitprice)
            result.Basic = activeItems.Sum(i => i.Quantity * i.Unit * i.UnitPrice);

            // Previous Sums (excluding current SOEE)
            decimal previousTotal =
                db.SOEEs.Where(x => x.ProjectID == projectId && x.SOEEID != soeeId)
                        .Sum(x => (decimal?)x.GrandTotal) ?? 0;

            // We'll use a runningBasic to compute slab thresholds progressively.
            decimal runningBasic = 0m;

            // Start with previous cumulative
            decimal cumulativeBeforeItems = previousTotal;

            // For each item, determine whether to use stored percent or compute a new percent.
            // We'll compute slab percent based on cumulativeAfter = cumulativeBeforeItems + runningBasic + subTotal
            foreach (var item in activeItems)
            {
                // compute subTotal and persist it
                decimal subTotal = item.Unit * item.Quantity * item.UnitPrice;
                item.SubTotal = subTotal;

                // Determine if we must recalc percentage:
                // - If item.ServiceChargePercent is zero or negative => must recalc (new item / flagged)
                // - If unit/qty/price changed (handled by caller by setting percent=0) => recalc
                bool mustRecalc = item.ServiceChargePercent <= 0m;

                if (mustRecalc)
                {
                    // Determine cumulative value to use for slab selection.
                    // Use cumulativeAfter = cumulativeBeforeItems + runningBasic + subTotal
                    decimal cumulativeAfter = cumulativeBeforeItems + runningBasic + subTotal;

                    // lookup slab percent for this item/service type using cumulativeAfter
                    decimal slabPercent = db.ServiceTypeSlabMaps
                        .Where(m =>
                            m.ServiceTypeID == item.ServiceTypeID &&
                            cumulativeAfter >= m.Slab.MinAmount &&
                            cumulativeAfter <= m.Slab.MaxAmount)
                        .Select(m => (decimal?)m.Percentage)
                        .FirstOrDefault() ?? 0m;

                    item.ServiceChargePercent = slabPercent;
                }

                // apply the stored percentage (either pre-existing or newly set above)
                decimal pct = item.ServiceChargePercent;
                item.ServiceCharge = decimal.Round(subTotal * (pct / 100m), 2);

                // taxes on (subtotal + serviceCharge)
                decimal taxable = subTotal + item.ServiceCharge;
                item.CGST = decimal.Round(taxable * 0.09m, 2);
                item.SGST = decimal.Round(taxable * 0.09m, 2);

                // total for item
                item.Total = decimal.Round(subTotal + item.ServiceCharge + item.CGST + item.SGST, 2);

                // advance runningBasic
                runningBasic += subTotal;
            }

            // Totals (sum of mutated item fields)
            result.ServiceCharge = activeItems.Sum(i => i.ServiceCharge);
            result.CGST = activeItems.Sum(i => i.CGST);
            result.SGST = activeItems.Sum(i => i.SGST);
            result.Total = activeItems.Sum(i => i.Total);
            // Basic recomputed from saved SubTotal as robust approach
            result.Basic = activeItems.Sum(i => i.SubTotal);

            return result;
        }
    }
}

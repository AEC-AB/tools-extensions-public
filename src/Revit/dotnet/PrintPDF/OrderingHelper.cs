using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;

namespace PrintPDF;

public static class OrderingHelper
{
    // Numeric-aware comparer for sheet numbers. Splits sequences of digits and non-digits so
    // "1", "2", "10" sorts as 1,2,10 and "A1", "A2", "B1" sorts alphabetically then numeric.
    public static List<ViewSheet> GetOrderedSheets(IEnumerable<ViewSheet> sheets)
    {
        if (sheets == null)
            return new List<ViewSheet>();

        return sheets.OrderBy(s => s.SheetNumber, new AlphanumericComparer())
                     .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                     .ToList();
    }

    private class AlphanumericComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            x ??= string.Empty;
            y ??= string.Empty;
            if (x == y) return 0;

            var xi = 0;
            var yi = 0;
            while (xi < x.Length && yi < y.Length)
            {
                if (char.IsDigit(x[xi]) && char.IsDigit(y[yi]))
                {
                    // parse full number segments
                    var xStart = xi;
                    while (xi < x.Length && char.IsDigit(x[xi])) xi++;
                    var yStart = yi;
                    while (yi < y.Length && char.IsDigit(y[yi])) yi++;

                    var xNumStr = x.Substring(xStart, xi - xStart);
                    var yNumStr = y.Substring(yStart, yi - yStart);

                    if (BigIntegerCompare(xNumStr, yNumStr, out var numComp))
                        return numComp;
                    continue;
                }

                var cx = x[xi];
                var cy = y[yi];
                var cmp = char.ToUpper(cx, CultureInfo.CurrentCulture).CompareTo(char.ToUpper(cy, CultureInfo.CurrentCulture));
                if (cmp != 0) return cmp;
                xi++; yi++;
            }

            return x.Length.CompareTo(y.Length);
        }

        private bool BigIntegerCompare(string a, string b, out int result)
        {
            // Remove leading zeros for fair numeric comparison
            a = a.TrimStart('0');
            b = b.TrimStart('0');
            if (a.Length != b.Length)
            {
                result = a.Length.CompareTo(b.Length);
                return true;
            }
            var cmp = string.Compare(a, b, StringComparison.Ordinal);
            result = cmp;
            return true;
        }
    }
}

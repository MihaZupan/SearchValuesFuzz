using SharpFuzz;
using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Fuzz
{
    public class Program
    {
        private const StringComparison ComparisonType = StringComparison.OrdinalIgnoreCase;

        public static bool UseNls { get; } = (bool)typeof(object).Assembly
            .GetType("System.Globalization.GlobalizationMode")!
            .GetProperty("UseNls", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetGetMethod(true)!
            .Invoke(null, null)!;

        public static bool Invariant { get; } = (bool)typeof(object).Assembly
            .GetType("System.Globalization.GlobalizationMode")!
            .GetProperty("Invariant", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetGetMethod(true)!
            .Invoke(null, null)!;

        private delegate int ToUpperOrdinalDelegate(ReadOnlySpan<char> source, Span<char> destination);

        private static readonly ToUpperOrdinalDelegate s_toUpperOrdinal = typeof(object).Assembly
            .GetType("System.Globalization.Ordinal")!
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)!
            .Single(m => m.Name == "ToUpperOrdinal" && m.ReturnType == typeof(int))
            .CreateDelegate<ToUpperOrdinalDelegate>();

        public static void Main()
        {
            Console.WriteLine($"Invariant={Invariant}");
            Console.WriteLine($"UseNls={UseNls}");

            //Test(File.ReadAllBytes("C:\\MihaZupan\\SearchValuesFuzz\\SearchValuesFuzz\\crash-aaa"));

            Fuzzer.LibFuzzer.Run(static bytes => Test(bytes));
        }

        private static void Test(ReadOnlySpan<byte> bytes)
        {
            ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);

            int newLine = chars.IndexOf('\n');
            if (newLine < 0)
            {
                return;
            }

            ReadOnlySpan<char> haystack = chars.Slice(newLine + 1);
            string[] needles = chars.Slice(0, newLine).ToString().Split(',');

            //needles[1] = string.Create(needles[1].Length, needles[1], static (buffer, needle) =>
            //{
            //    s_toUpperOrdinal(needle, buffer);
            //});

            //Console.WriteLine(Convert.ToBase64String(MemoryMarshal.Cast<char, byte>(haystack)));
            //Console.WriteLine(Convert.ToBase64String(MemoryMarshal.Cast<char, byte>(needles[1])));

            int expected = IndexOfAnyReferenceImpl(haystack, needles);
            int actual = GetSearchValuesIndex(haystack, needles);

            if (expected != actual &&
                !HitsRuntime89591(haystack, needles, expected, actual))
            {
                throw new Exception($"Expected={expected} Actual={actual} {Convert.ToBase64String(bytes)}");
            }
        }

        private static int GetSearchValuesIndex(ReadOnlySpan<char> haystack, string[] needles)
        {
            SearchValuesExt<string> searchValues = SearchValuesExt.Create(needles, ComparisonType);

            using BoundedMemory<char> haystackWithPoisonBefore = BoundedMemory.AllocateFromExistingData(haystack, PoisonPagePlacement.Before);
            using BoundedMemory<char> haystackWithPoisonAfter = BoundedMemory.AllocateFromExistingData(haystack, PoisonPagePlacement.After);

            int resultBefore = searchValues.IndexOfAnyMultiString(haystackWithPoisonBefore.Span);
            int resultAfter = searchValues.IndexOfAnyMultiString(haystackWithPoisonAfter.Span);

            if (resultBefore != resultAfter)
            {
                throw new Exception($"Different result with poison before/after: {resultBefore}, {resultAfter}");
            }

            return resultBefore;
        }

        private static int IndexOfAnyReferenceImpl(ReadOnlySpan<char> haystack, string[] needles)
        {
            int minIndex = int.MaxValue;

            foreach (string needle in needles)
            {
                int i = haystack.IndexOf(needle, ComparisonType);
                if ((uint)i < minIndex)
                {
                    minIndex = i;
                }
            }

            return minIndex == int.MaxValue ? -1 : minIndex;
        }

        private static bool HitsRuntime89591(ReadOnlySpan<char> haystack, string[] needles, int expectedIndex, int actual)
        {
            if (!Invariant)
            {
                return false;
            }

            if (ComparisonType != StringComparison.OrdinalIgnoreCase)
            {
                return false;
            }

            if (actual >= 0 && actual < expectedIndex)
            {
                return false;
            }

            ReadOnlySpan<char> remaining = haystack.Slice(expectedIndex);

            needles = needles.Select(needle => string.Create(needle.Length, needle, static (buffer, needle) =>
            {
                s_toUpperOrdinal(needle, buffer);
            })).ToArray();

            foreach (string needle in needles)
            {
                if (remaining.StartsWith(needle, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            bool anyIndexOfIs0 = false;

            foreach (string needle in needles)
            {
                if (remaining.IndexOf(needle, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    anyIndexOfIs0 = true;
                    break;
                }
            }

            if (!anyIndexOfIs0)
            {
                throw new Exception("?");
            }

            return true;
        }
    }
}
using SharpFuzz;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Fuzz
{
    public class Program
    {
        private const StringComparison ComparisonType = StringComparison.Ordinal;

        public static void Main()
        {
            Fuzzer.LibFuzzer.Run(static bytes =>
            {
                ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);

                int newLine = chars.IndexOf('\n');
                if (newLine < 0)
                {
                    return;
                }

                ReadOnlySpan<char> haystack = chars.Slice(newLine + 1);
                string[] needles = chars.Slice(0, newLine).ToString().Split(',');

                int expected = IndexOfAnyReferenceImpl(haystack, needles);
                int actual = GetSearchValuesIndex(haystack, needles);

                if (expected != actual)
                {
                    throw new Exception($"Expected={expected} Actual={actual} {Convert.ToBase64String(bytes)}");
                }
            });
        }

        private static int GetSearchValuesIndex(ReadOnlySpan<char> haystack, string[] needles)
        {
            SearchValuesExt<string> searchValues = SearchValuesExt.Create(needles, ComparisonType);

            return searchValues.IndexOfAnyMultiString(haystack);
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
    }
}

namespace Fuzzman.Core
{
    /// <summary>
    /// Fuzzer interface, v2
    /// </summary>
    public interface IFuzzer
    {
        /// <summary>
        /// Populated array of modifications to be applied.
        /// </summary>
        Difference[] Diffs { get; }

        /// <summary>
        /// Generate the modifications.
        /// </summary>
        /// <param name="source">Source file path.</param>
        void Populate(string source);

        /// <summary>
        /// Apply the modifications.
        /// </summary>
        /// <param name="source">Source file</param>
        /// <param name="target">Target file</param>
        /// <returns></returns>
        void Apply(string source, string target);
    }
}

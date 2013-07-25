using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fuzzman.Core
{
    /// <summary>
    /// Implement a (controlled) RNG.
    /// We may need reproducibility of the test cases, so we need control over how random numbers are generated.
    /// </summary>
    public interface IRandom
    {
        uint GetNext(uint min = 0, uint max = uint.MaxValue);
    }
}

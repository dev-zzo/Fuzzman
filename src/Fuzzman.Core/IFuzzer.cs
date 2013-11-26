
namespace Fuzzman.Core
{
    public interface IFuzzer
    {
        Difference[] Process(string target);
    }
}

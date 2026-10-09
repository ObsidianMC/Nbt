using BenchmarkDotNet.Running;

namespace BenchmarkSuite1
{
    internal class Program
    {
        // Pass BenchmarkDotNet arguments through, e.g. --filter *NbtIoBenchmarks* to run one class.
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

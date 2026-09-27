// shadows MethodImplOptions so upstream's AggressiveInlining (4.5) compiles.
// values are still the real enum; AggressiveInlining is just 0 here.

using Real = System.Runtime.CompilerServices.MethodImplOptions;

namespace Ultima
{
    internal static class MethodImplOptions
    {
        public const Real AggressiveInlining = 0;
        public const Real NoInlining = Real.NoInlining;
    }
}

namespace Assistant.Core
{
    internal static class MethodImplOptions
    {
        public const Real AggressiveInlining = 0;
        public const Real NoInlining = Real.NoInlining;
    }
}

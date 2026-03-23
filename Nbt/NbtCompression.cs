using System.Diagnostics;

namespace Obsidian.Nbt;

public enum NbtCompression : byte
{
    None,

    GZip,

    ZLib,

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    Zstd,

    Brotli
}

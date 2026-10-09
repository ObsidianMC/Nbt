namespace Obsidian.Nbt;

public readonly partial struct NbtWriterStream
{
    public void WriteString(string value) => this.writer.WriteString(value);

    public void WriteString(string name, string value) => this.writer.WriteString(name, value);

    public void WriteByte(byte value) => this.writer.WriteByte(value);

    public void WriteByte(string name, byte value) => this.writer.WriteByte(name, value);

    public void WriteBool(bool value) => this.writer.WriteBool(value);

    public void WriteBool(string name, bool value) => this.writer.WriteBool(name, value);

    public void WriteShort(short value) => this.writer.WriteShort(value);

    public void WriteShort(string name, short value) => this.writer.WriteShort(name, value);

    public void WriteInt(int value) => this.writer.WriteInt(value);

    public void WriteInt(string name, int value) => this.writer.WriteInt(name, value);

    public void WriteLong(long value) => this.writer.WriteLong(value);

    public void WriteLong(string name, long value) => this.writer.WriteLong(name, value);

    public void WriteFloat(float value) => this.writer.WriteFloat(value);

    public void WriteFloat(string name, float value) => this.writer.WriteFloat(name, value);

    public void WriteDouble(double value) => this.writer.WriteDouble(value);

    public void WriteDouble(string name, double value) => this.writer.WriteDouble(name, value);
}

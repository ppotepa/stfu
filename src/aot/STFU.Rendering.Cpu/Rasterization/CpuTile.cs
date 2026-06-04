namespace STFU.Rendering.Cpu.Rasterization;

public readonly record struct CpuTile(
    int X,
    int Y,
    int Width,
    int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;
}

using System;
using System.Runtime.InteropServices;

namespace LightBulb.PlatformInterop;

/// <summary>
/// Windows Magnification API 封装（2026-09-22 定制）。
///
/// 为什么需要它：显示器 gamma 查找表是逐通道独立映射的，三条曲线再怎么趋同也无法
/// 把"红色像素"变成灰色——真正的去饱和需要通道间加权混合。Microsoft 文档与
/// Stack Overflow 均确认 SetDeviceGammaRamp 做不到灰度。
///
/// 能做的是 MagSetFullscreenColorEffect：给整个桌面应用一个 5×5 颜色变换矩阵。
/// 参考：Windows-classic-samples / FullscreenMagnifierSample、github.com/mlaily/NegativeScreen
///
/// ⚠️ 两个实测踩过的坑（都别重犯）：
/// 1. 传参必须用 Marshal.AllocHGlobal + Marshal.Copy。用
///    `ref struct { [MarshalAs(ByValArray)] float[] }` 会传错内存，屏幕变纯色。
/// 2. 矩阵的 25 个 float 是**按列排列**的（每 5 个连续值是一"列"，不是一"行"）。
///    按行写会输出整屏绿色——因为 G 通道恰好拿到最大的那个系数。
/// </summary>
public static class MagnificationInterop
{
    private const string Dll = "Magnification.dll";

    private const int MatrixFloats = 25;
    private const int MatrixBytes = MatrixFloats * sizeof(float);

    [DllImport(Dll, ExactSpelling = true, SetLastError = true)]
    private static extern bool MagInitialize();

    [DllImport(Dll, ExactSpelling = true, SetLastError = true)]
    private static extern bool MagUninitialize();

    [DllImport(Dll, ExactSpelling = true, SetLastError = true)]
    private static extern bool MagSetFullscreenColorEffect(IntPtr pEffect);

    /// <summary>单位矩阵：输出 = 输入。它的行与列相同，所以顺序无关。</summary>
    private static readonly float[] Identity =
    [
        1f,
        0f,
        0f,
        0f,
        0f,
        0f,
        1f,
        0f,
        0f,
        0f,
        0f,
        0f,
        1f,
        0f,
        0f,
        0f,
        0f,
        0f,
        1f,
        0f,
        0f,
        0f,
        0f,
        0f,
        1f,
    ];

    /// <summary>
    /// 灰度矩阵（Rec.709 亮度权重），**按列排列**：
    /// 列 0 = R 输出的 5 个系数，列 1 = G 输出，列 2 = B 输出。
    /// 三列都取同一个亮度加权和 → 三通道输出相同 = 真灰度。
    /// </summary>
    private static readonly float[] Grayscale =
    [
        0.2126f,
        0.2126f,
        0.2126f,
        0f,
        0f,
        0.7152f,
        0.7152f,
        0.7152f,
        0f,
        0f,
        0.0722f,
        0.0722f,
        0.0722f,
        0f,
        0f,
        0f,
        0f,
        0f,
        1f,
        0f,
        0f,
        0f,
        0f,
        0f,
        1f,
    ];

    private static bool _initialized;

    public static bool TryInitialize()
    {
        if (_initialized)
            return true;

        try
        {
            _initialized = MagInitialize();
        }
        catch (DllNotFoundException)
        {
            _initialized = false;
        }
        catch (EntryPointNotFoundException)
        {
            _initialized = false;
        }

        return _initialized;
    }

    public static void Uninitialize()
    {
        if (!_initialized)
            return;

        try
        {
            MagUninitialize();
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }

        _initialized = false;
    }

    /// <summary>
    /// 按饱和度设置全屏颜色矩阵。
    /// saturation = 1.0 → 原色；0.0 → 全黑白；中间值在两者之间线性插值（逐元素插值，
    /// 与行列顺序无关），所以是连续可调的真去饱和。
    /// </summary>
    public static bool SetSaturation(double saturation)
    {
        if (!TryInitialize())
            return false;

        var s = (float)Math.Clamp(saturation, 0, 1);
        var m = new float[MatrixFloats];

        // s = 1.0 → 原色（Identity）；s = 0.0 → 黑白（Grayscale）。方向别写反：
        // 这里必须是 Grayscale 乘 (1-s)、Identity 乘 s（早先写反导致"阅读彩色、其他黑白"）。
        for (var i = 0; i < MatrixFloats; i++)
            m[i] = Grayscale[i] * (1 - s) + Identity[i] * s;

        var ptr = Marshal.AllocHGlobal(MatrixBytes);
        try
        {
            Marshal.Copy(m, 0, ptr, MatrixFloats);
            return MagSetFullscreenColorEffect(ptr);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>恢复原始颜色（等价于 SetSaturation(1.0)）。</summary>
    public static bool Reset() => SetSaturation(1.0);
}

// Varianta A: ceasul
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class ClockScene : Scene
{
    private const string ToggleTheme = "ceas: zi/noapte";
    private Material _face = null!;
    private Tween _theme = null!, _second = null!;
    private bool _night;
    private double _elapsed;
    private int _lastSecond;
    public override string Name => "ceas";
    public override IEnumerable<string> ActionNames => new[] { ToggleTheme };
    public override string Status => $"{_elapsed:0.0}s | {(_night ? "noapte" : "zi")}";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0, 0, 7);
        CameraTarget = Vector3.Zero;
        input.Bind(ToggleTheme, Keys.Space);
        _face = AddMaterial("ceas", "procedural.vert", "clock.frag")
            .Param("uRimWidth", 0.025f, 0.12f, 0.065f)
            .Param("uTickWidth", 0.003f, 0.02f, 0.008f);
        _face.Transparent = true;
        _theme = Tween.Float(0, 0, 0.8f, Easing.EaseInOut);
        _second = Tween.Float(0, 0, 0.15f, Easing.EaseOut);
        _face.Uniforms.Set("uAccent", new Vector3(0.85f, 0.16f, 0.10f));
        AddObject("cadran", AddMesh(Shapes.Quad(4, 4)), _face);
        SendUniforms();
    }

    private void SendUniforms()
    {
        const float tau = 2 * MathF.PI;
        _face.Uniforms.Set("uHour", (float)((_elapsed % 43200) / 43200) * tau);
        _face.Uniforms.Set("uMinute", (float)((_elapsed % 3600) / 3600) * tau);
        _face.Uniforms.Set("uSecond", _second.Scalar);
        _face.Uniforms.Set("uNight", _theme.Scalar);
        // A full simulated lunar cycle every 60 game seconds.
        _face.Uniforms.Set("uMoonPhase", (float)((_elapsed % 60.0) / 60.0));
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);
        if (input.WasPressed(ToggleTheme))
        {
            _night = !_night;
            _theme.GoTo(_night ? 1f : 0f, 0.8f);
        }
        _elapsed += dt;
        int second = (int)_elapsed;
        if (second != _lastSecond)
        {
            _lastSecond = second;
            _second.GoTo(second * (2 * MathF.PI / 60), 0.15f);
        }
        _second.Update(dt);
        _theme.Update(dt);
        SendUniforms();
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Ceas: {Status}";
        yield return "Space: tema zi/noapte";
        yield return $"Faza lunii (simulata): {(_elapsed % 60.0) / 60.0:P0}";
    }
}

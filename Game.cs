using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicaÈ›iei È™i bucla principalÄƒ (main game loop).
/// FaÈ›Äƒ de laboratorul 5, Game este mai "subÈ›ire": nu mai conÈ›ine scena, ci doar leagÄƒ
/// componentele Ã®ntre ele. La fiecare cadru:
///   1. Clock  â€“ transformÄƒ timpul real Ã®n timp de joc (pauzÄƒ, scara timpului);
///   2. Input  â€“ citeÈ™te starea tastaturii È™i a mouse-ului; restul programului Ã®ntreabÄƒ de ACÈšIUNI;
///   3. Scene  â€“ Ã®È™i actualizeazÄƒ animaÈ›iile cu timpul de joc È™i se deseneazÄƒ.
/// </summary>
public class Game : GameWindow
{
    private readonly Clock _clock = new();
    private readonly Input _input = new();
    private readonly FixedCamera _camera = new();

    // Scenele (Tab trece la urmÄƒtoarea). Se Ã®ncarcÄƒ Ã®n OnLoad, dupÄƒ crearea contextului OpenGL.
    private readonly List<Scene> _scenes = new();
    private int _current;
    private Scene ActiveScene => _scenes[_current];

    private Shader _shader = null!;
    private bool _wireframe;

    // Titlul ferestrei: se reface de cÃ¢teva ori pe secundÄƒ, nu la fiecare cadru.
    private readonly string _baseTitle;
    private double _titleTimer, _fpsTimer;
    private int _frames, _lastFrames;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),
                   Profile = ContextProfile.Core,
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;
    }

    /// <summary>ApelatÄƒ O SINGURÄ‚ DATÄ‚, dupÄƒ crearea ferestrei È™i a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();
        ConsolePanel.Init();
        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);
        GL.Enable(EnableCap.DepthTest);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        BindCommonActions();

        _scenes.Add(new LabScene());
        _scenes.Add(new Egc.Teme.RobotArmScene());

        foreach (Scene scene in _scenes)
        {
            scene.Load(_input);
        }
        ActivateScene(0);
        PrintHelp();
    }

    /// <summary>AcÈ›iunile comune tuturor scenelor. Tastele se schimbÄƒ DOAR aici.</summary>
    private void BindCommonActions()
    {
        _input.Bind(Actions.Quit, Keys.Escape);
        _input.Bind(Actions.Help, Keys.H);
        _input.Bind(Actions.Info, Keys.I);
        _input.Bind(Actions.Pause, Keys.P);
        _input.Bind(Actions.Slower, Keys.LeftBracket, Keys.Minus, Keys.KeyPadSubtract);
        _input.Bind(Actions.Faster, Keys.RightBracket, Keys.Equal, Keys.KeyPadAdd);
        _input.Bind(Actions.NormalSpeed, Keys.N);
        _input.Bind(Actions.Freeze, Keys.B);
        _input.Bind(Actions.Wireframe, Keys.F);
        _input.Bind(Actions.VSync, Keys.L);
        _input.Bind(Actions.NextScene, Keys.Tab);
    }

    private static readonly string[] CommonActions =
    {
        Actions.Pause, Actions.Slower, Actions.Faster, Actions.NormalSpeed, Actions.Freeze,
        Actions.NextScene, Actions.Wireframe, Actions.VSync, Actions.Info, Actions.Help, Actions.Quit,
    };

    private void ActivateScene(int index)
    {
        _current = index;
        _camera.Position = ActiveScene.CameraPosition;
        _camera.Target = ActiveScene.CameraTarget;
        ConsolePanel.PrintMessage($"scena: {ActiveScene.Name}");
        _titleTimer = 1.0;   // titlul se reface imediat
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.Resize(e.Width, e.Height);
    }

    /// <summary>ApelatÄƒ la fiecare iteraÈ›ie a buclei: LOGICA aplicaÈ›iei.</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // 1. Timpul: real -> de joc.  2. Input: starea dispozitivelor pentru acest cadru.
        _input.Update(KeyboardState, MouseState);

        if (_input.WasPressed(Actions.Quit))
        {
            Close();
            return;
        }
        HandleCommonActions();
        _clock.Tick(args.Time);
        // Un pas real neobiÈ™nuit de mare (blocare, fereastra trasÄƒ cu mouse-ul) este semnalat Ã®n consolÄƒ.
        if (_clock.RealDeltaTime > Clock.MaxDelta)
        {
            ConsolePanel.PrintMessage($"pas real {_clock.RealDeltaTime * 1000:F0} ms -> pas de joc {_clock.DeltaTime * 1000:F0} ms");
        }



        // 3. Scena, cu timpul de JOC (0 Ã®n pauzÄƒ, scalat cu TimeScale).
        ActiveScene.Update(_clock.DeltaTime, _input);

        // Titlul È™i contorul de cadre folosesc timpul REAL: trebuie sÄƒ meargÄƒ È™i Ã®n pauzÄƒ.
        UpdateTitle(_clock.RealDeltaTime);
    }

    private void HandleCommonActions()
    {
        if (_input.WasPressed(Actions.Help)) PrintHelp();
        if (_input.WasPressed(Actions.Info)) PrintInfo();

        if (_input.WasPressed(Actions.Pause))
        {
            _clock.Paused = !_clock.Paused;
            ConsolePanel.PrintMessage(_clock.Paused ? "pauzÄƒ" : "continuare");
        }
        if (_input.WasPressed(Actions.Slower)) _clock.ChangeScale(0.5f);
        if (_input.WasPressed(Actions.Faster)) _clock.ChangeScale(2.0f);
        if (_input.WasPressed(Actions.NormalSpeed)) _clock.ResetScale();

        // B: simulÄƒm o "Ã®ngheÈ›are" a programului (ca la tragerea ferestrei sau la o Ã®ncÄƒrcare lungÄƒ).
        // Cadrul urmÄƒtor primeÈ™te un args.Time de aproximativ 0,5 s.
        if (_input.WasPressed(Actions.Freeze))
        {
            ConsolePanel.PrintMessage("blocare simulatÄƒ: 0,5 s");
            Thread.Sleep(500);
        }

        if (_input.WasPressed(Actions.Wireframe)) _wireframe = !_wireframe;
        if (_input.WasPressed(Actions.VSync))
        {
            VSync = VSync == VSyncMode.On ? VSyncMode.Off : VSyncMode.On;
            ConsolePanel.PrintMessage($"VSync: {(VSync == VSyncMode.On ? "pornit" : "oprit")}");
        }
        if (_input.WasPressed(Actions.NextScene))
        {
            ActivateScene((_current + 1) % _scenes.Count);
        }
    }

    /// <summary>ApelatÄƒ la fiecare iteraÈ›ie a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.PolygonMode(TriangleFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);

        _shader.Use();
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);
        ActiveScene.Draw(_shader);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        foreach (Scene scene in _scenes)
        {
            scene.Dispose();
        }
        _shader.Dispose();
        base.OnUnload();
    }

    // --- Titlul ferestrei È™i consola -------------------------------------------------------

    private void UpdateTitle(double realSeconds)
    {
        _fpsTimer += realSeconds;
        if (_fpsTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _fpsTimer -= Math.Floor(_fpsTimer);
        }

        _titleTimer += realSeconds;
        if (_titleTimer < 0.25) return;
        _titleTimer = 0.0;

        string pause = _clock.Paused ? " | PAUZÄ‚" : "";
        string status = ActiveScene.Status.Length > 0 ? $" | {ActiveScene.Status}" : "";
        Title = $"{_baseTitle} | {ActiveScene.Name} | t = {_clock.TotalTime:F1} s | Ã— {_clock.TimeScale:0.###}{pause}{status} | FPS: {_lastFrames}";
    }

    private void PrintHelp()
    {
        ConsolePanel.PrintHelp(_baseTitle, _input.Describe(CommonActions), _input.Describe(ActiveScene.ActionNames));
    }

    private void PrintInfo()
    {
        var lines = new List<string>
        {
            $"Scena          : {ActiveScene.Name}",
            $"Timp de joc    : {_clock.TotalTime:F2} s   scara: Ã— {_clock.TimeScale:0.###}   pauzÄƒ: {(_clock.Paused ? "da" : "nu")}",
            $"Î”t real / joc  : {_clock.RealDeltaTime * 1000:F1} ms / {_clock.DeltaTime * 1000:F1} ms",
            $"Cadre/secundÄƒ  : {_lastFrames}   VSync: {(VSync == VSyncMode.On ? "pornit" : "oprit")}",
        };
        lines.AddRange(ActiveScene.Describe());
        ConsolePanel.PrintInfo(lines);
    }
}

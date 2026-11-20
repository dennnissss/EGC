// Varianta A: brațul robotic.
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public sealed class RobotArmScene : Scene
{
    private const string BaseLeft = "robot: bază stânga", BaseRight = "robot: bază dreapta";
    private const string ShoulderUp = "robot: umăr sus", ShoulderDown = "robot: umăr jos";
    private const string ElbowUp = "robot: cot sus", ElbowDown = "robot: cot jos";
    private const string Grip = "robot: deschide / închide cleștele", Save = "robot: salvează poza";
    private const string Play = "robot: redare / control manual", Clear = "robot: șterge pozele";
    private const string Ease = "robot: schimbă easing-ul", Reset = "robot: poziție inițială";
    private readonly KeyframeTrack _poses = new(1.5f) { Ease = Easing.EaseInOut };
    private readonly Tween _grip = new(new Vector3(0.42f, 0, 0), new Vector3(0.42f, 0, 0), 0.35f, Easing.EaseInOut);
    private SceneObject _base = null!, _shoulder = null!, _elbow = null!;
    private SceneObject _leftFinger = null!, _rightFinger = null!;
    private Vector3 _angles = new(0, -25, -55);
    private bool _playing, _closed;
    private float _playTime;
    private Tween? _transition;
    private const float UpperLength = 2.5f, ForeLength = 2.0f, AngularSpeed = 55.0f;

    public override string Name => "Tema A - braț robotic";
    public override string Status => $"{(_playing ? "redare" : "manual")} | poze: {_poses.Count} | clește: {(_closed ? "închis" : "deschis")}";
    public override IEnumerable<string> ActionNames => new[]
    {
        BaseLeft, BaseRight, ShoulderUp, ShoulderDown, ElbowUp, ElbowDown,
        Grip, Save, Play, Ease, Clear, Reset
    };

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(9, 7, 12);
        CameraTarget = new Vector3(0, 2.4f, 0);
        input.Bind(BaseLeft, Keys.A, Keys.Left);
        input.Bind(BaseRight, Keys.D, Keys.Right);
        input.Bind(ShoulderUp, Keys.W, Keys.Up);
        input.Bind(ShoulderDown, Keys.S, Keys.Down);
        input.Bind(ElbowUp, Keys.Q);
        input.Bind(ElbowDown, Keys.Z);
        input.Bind(Grip, Keys.Space);
        input.Bind(Save, Keys.K);
        input.Bind(Play, Keys.Enter);
        input.Bind(Ease, Keys.E);
        input.Bind(Clear, Keys.Backspace);
        input.Bind(Reset, Keys.R);

        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh sphere = AddMesh(Shapes.ShadedSphere(1, 24, 12));
        AddObject("grilă", AddMesh(Shapes.Grid(8, 1, new Color4(0.27f, 0.3f, 0.36f, 1))));
        Color4 steel = new(0.35f, 0.45f, 0.58f, 1);
        Color4 orange = new(1, 0.58f, 0.12f, 1);
        Color4 cyan = new(0.2f, 0.8f, 0.95f, 1);
        Part("soclu", cube, null, new(0, 0.2f, 0), new(2.2f, 0.4f, 2.2f), steel);
        _base = AddObject("pivot bază", null);
        _base.Transform.Position = new Vector3(0, 0.4f, 0);
        Part("bază rotativă", cube, _base, new(0, 0.3f, 0), new(1.3f, 0.6f, 1.3f), cyan);
        _shoulder = AddObject("pivot umăr", null, _base);
        _shoulder.Transform.Position = new Vector3(0, 0.65f, 0);
        Part("articulație umăr", sphere, _shoulder, Vector3.Zero, new(0.38f), steel);
        Part("braț superior", cube, _shoulder, new(0, UpperLength / 2, 0), new(0.45f, UpperLength, 0.45f), orange);
        _elbow = AddObject("pivot cot", null, _shoulder);
        _elbow.Transform.Position = new Vector3(0, UpperLength, 0);
        Part("articulație cot", sphere, _elbow, Vector3.Zero, new(0.32f), steel);
        Part("antebraț", cube, _elbow, new(0, ForeLength / 2, 0), new(0.35f, ForeLength, 0.35f), cyan);
        SceneObject wrist = AddObject("pivot clește", null, _elbow);
        wrist.Transform.Position = new Vector3(0, ForeLength, 0);
        Part("palmă", cube, wrist, Vector3.Zero, new(1.05f, 0.22f, 0.4f), steel);
        _leftFinger = Part("deget stâng", cube, wrist, new(-0.42f, 0.38f, 0), new(0.16f, 0.65f, 0.35f), orange);
        _rightFinger = Part("deget drept", cube, wrist, new(0.42f, 0.38f, 0), new(0.16f, 0.65f, 0.35f), orange);
        ApplyPose();
    }

    private SceneObject Part(string name, Mesh mesh, SceneObject? parent, Vector3 position, Vector3 scale, Color4 color)
    {
        var part = AddObject(name, mesh, parent);
        part.Transform.Position = position;
        part.Transform.Scale = scale;
        part.Tint = color;
        return part;
    }

    public override void Update(float dt, Input input)
    {
        // În pauză nu schimbăm nici pozițiile, nici starea animațiilor.
        if (dt <= 0) return;
        if (input.WasPressed(Clear))
        {
            _poses.Clear();
            _playing = false;
            _transition = null;
            _playTime = 0;
        }
        if (input.WasPressed(Reset))
        {
            _playing = false;
            _transition = new Tween(_angles, new Vector3(0, -25, -55), 0.6f, Easing.EaseInOut);
            _closed = false;
            _grip.GoTo(new Vector3(0.42f, 0, 0), 0.35f);
        }
        if (input.WasPressed(Ease))
            _poses.Ease = _poses.Ease == Easing.Linear ? Easing.EaseInOut : Easing.Linear;
        if (input.WasPressed(Save))
        {
            _poses.Add(_angles);
            ConsolePanel.PrintMessage($"Poza {_poses.Count} salvată: {_angles}");
        }
        if (input.WasPressed(Play))
        {
            if (_playing) { _playing = false; _transition = null; }
            else if (_poses.Count >= 2)
            {
                _playing = true;
                _playTime = 0;
                // Ajungem lin la prima cheie, fără teleportare la începutul redării.
                _transition = new Tween(_angles, _poses.Keys[0], 0.6f, Easing.EaseInOut);
            }
            else ConsolePanel.PrintMessage("Salvează cel puțin două poze cu K înainte de redare.");
        }
        if (input.WasPressed(Grip))
        {
            _closed = !_closed;
            _grip.GoTo(new Vector3(_closed ? 0.11f : 0.42f, 0, 0), 0.35f);
        }

        if (_transition != null)
        {
            _transition.Update(dt);
            _angles = _transition.Value;
            if (_transition.IsFinished) _transition = null;
        }
        else if (_playing)
        {
            _playTime = (_playTime + dt) % _poses.Duration;
            _angles = _poses.Sample(_playTime);
        }
        else
        {
            _angles.X = Math.Clamp(_angles.X + input.Axis(BaseLeft, BaseRight) * AngularSpeed * dt, -180, 180);
            _angles.Y = Math.Clamp(_angles.Y + input.Axis(ShoulderDown, ShoulderUp) * AngularSpeed * dt, -75, 75);
            _angles.Z = Math.Clamp(_angles.Z + input.Axis(ElbowDown, ElbowUp) * AngularSpeed * dt, -135, 0);
        }
        _grip.Update(dt);
        ApplyPose();
    }

    private void ApplyPose()
    {
        _base.Transform.Rotation = new Vector3(0, _angles.X, 0);
        _shoulder.Transform.Rotation = new Vector3(0, 0, _angles.Y);
        _elbow.Transform.Rotation = new Vector3(0, 0, _angles.Z);
        _leftFinger.Transform.Position = new Vector3(-_grip.Value.X, 0.38f, 0);
        _rightFinger.Transform.Position = new Vector3(_grip.Value.X, 0.38f, 0);
    }

    public override IEnumerable<string> Describe()
    {
        yield return "Varianta A: braț robotic cu bază, umăr, cot și clește.";
        yield return $"Stare: {Status}";
        yield return $"Unghiuri: bază {_angles.X:F1}°, umăr {_angles.Y:F1}°, cot {_angles.Z:F1}°";
        yield return "Limite: bază [-180,180], umăr [-75,75], cot [-135,0] grade.";
        yield return $"Redare: 1,5 s / segment; easing: {(_poses.Ease == Easing.Linear ? "Linear" : "EaseInOut")}";
        yield return "K salvează unghiurile; Enter redă/oprește; Backspace șterge toate pozele.";
        yield return "Pozele sunt păstrate în memorie până la închiderea aplicației.";
    }
}

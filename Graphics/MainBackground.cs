using System.Numerics;
using Engine.Managers;
using Raylib_cs;

namespace Graphics;

public class MainBackground
{
    private Shader _shader;
    private int _timeLoc;
    private int _resolutionLoc;
    private float _speed = 0.2f;
    private float[] _thresholds = new float[]{
        0.25f,
        0.28f,
        0.39f,
        0.41f,
        0.43f,
        0.45f,
        0.49f,
        0.52f,
        0.55f,
        0.62f,
        0.68f,
        0.72f,
        0.75f,
        0.78f,
        0.80f,
        0.88f
    };
    private Vector3[] _colors = new Vector3[]{
        new Vector3(0.090f, 0.090f, 0.090f),
        new Vector3(0.085f, 0.085f, 0.085f),
        new Vector3(0.075f, 0.075f, 0.075f),
        new Vector3(0.085f, 0.085f, 0.085f),
        new Vector3(0.090f, 0.090f, 0.090f),
        new Vector3(0.099f, 0.099f, 0.099f),
        new Vector3(0.090f, 0.090f, 0.090f),
        new Vector3(0.085f, 0.085f, 0.085f),      
        new Vector3(0.075f, 0.075f, 0.075f),
        new Vector3(0.085f, 0.085f, 0.085f),
        new Vector3(0.090f, 0.090f, 0.090f),
        new Vector3(0.099f, 0.099f, 0.099f),
        new Vector3(0.090f, 0.090f, 0.090f),
        new Vector3(0.085f, 0.085f, 0.085f),      
        new Vector3(0.085f, 0.085f, 0.085f),
        new Vector3(0.075f, 0.075f, 0.075f),
    };

    private int _speedLoc;
    
    private Rectangle _fullscreenRect;
    
    public void SetSettings()
    {
        _shader = Resources.Instance.Shader("noise_background_shader.frag");
        _speedLoc = Raylib.GetShaderLocation(_shader, "speed");

        SetShader();
    }
    
    private float _shaderTime = 0f;
    
    
    private bool _isAnimating = false;
    private float _animStartSpeed;
    private float _animTargetSpeed;
    private float _animDuration;
    private float _animTime;
    private float _animHoldTime;

    public void Update(float deltaTime)
    {
        if (_isAnimating)
        {
            _animTime += deltaTime;

            if (_animTime <= _animDuration)
            {
                float t = _animTime / _animDuration;
                _speed = Lerp(_animStartSpeed, _animTargetSpeed, t);
            }
            else if (_animTime <= _animDuration + _animHoldTime)
            {
                _speed = _animTargetSpeed;
            }
            else if (_animTime <= _animDuration * 2 + _animHoldTime)
            {
                float t = (_animTime - _animDuration - _animHoldTime) / _animDuration;
                _speed = Lerp(_animTargetSpeed, _animStartSpeed, t);
            }
            else
            {
                _speed = _animStartSpeed;
                _isAnimating = false;
            }
            
            Console.WriteLine(_speed);

            Raylib.SetShaderValue(_shader, _speedLoc, _speed, ShaderUniformDataType.Float);
        }

        
        _shaderTime  += deltaTime * _speed;

        var resolution = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.SetShaderValue(_shader, _timeLoc, _shaderTime, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(_shader, _resolutionLoc, new float[] { resolution.X, resolution.Y }, ShaderUniformDataType.Vec2);
    }
    
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    
    public void AnimateSpeedChange(float targetSpeed, float duration, float holdTime = 0f)
    {
        _animStartSpeed = _speed;
        _animTargetSpeed = targetSpeed;
        _animDuration = duration;
        _animHoldTime = holdTime;
        _animTime = 0f;
        _isAnimating = true;
        Raylib.SetShaderValue(_shader, _speedLoc, _speed, ShaderUniformDataType.Float);
    }

    public void Draw()
    {
        Raylib.BeginShaderMode(_shader);
        Raylib.DrawRectangleRec(_fullscreenRect, Color.White);
        Raylib.EndShaderMode();
    }

    public void SetShader()
    {
        
        var scaleLoc = Raylib.GetShaderLocation(_shader, "scale");
        
        var colorsLoc = Raylib.GetShaderLocation(_shader, "colors");
        var thresholdsLoc = Raylib.GetShaderLocation(_shader, "thresholds");
        
        
        
        float[] colorsData = new float[_colors.Length * 3];
        for (int i = 0; i < _colors.Length; i++)
        {
            colorsData[i * 3] = _colors[i].X;
            colorsData[i * 3 + 1] = _colors[i].Y;
            colorsData[i * 3 + 2] = _colors[i].Z;
        }
        
        
        
        Raylib.SetShaderValue(_shader, _speedLoc, _speed, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(_shader, scaleLoc, 0.4f, ShaderUniformDataType.Float);
        
        Raylib.SetShaderValueV(_shader, colorsLoc, colorsData, ShaderUniformDataType.Vec3, _colors.Length);
        Raylib.SetShaderValueV(_shader, thresholdsLoc, _thresholds, ShaderUniformDataType.Float, _thresholds.Length);
        
        int colorCountLoc = Raylib.GetShaderLocation(_shader, "colorCount");
        Raylib.SetShaderValue(_shader, colorCountLoc, _colors.Length, ShaderUniformDataType.Int);

        _timeLoc = Raylib.GetShaderLocation(_shader, "time");
        _resolutionLoc = Raylib.GetShaderLocation(_shader, "resolution");

        _fullscreenRect = new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
    }

    public void UnloadShader() => Resources.Instance.Unload<Shader>("noise_background_shader.frag");
    static MainBackground() => Instance = new();
    public static MainBackground Instance { get; private set; }
}
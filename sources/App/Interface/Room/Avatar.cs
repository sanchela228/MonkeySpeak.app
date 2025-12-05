using Engine;
using Raylib_cs;

namespace Interface.Room;

public class Avatar : Node
{
    public float AudioLevel = 0f;
    
    public override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
        
        // InterlocutorMutedIcon = Resources.Texture("Images\\Icons\\MicrophoneMuted_White.png");
    }


    private float mainSmoothedAudioLevel = 0f;
    private float secondarySmoothedAudioLevel = 0f;

    public override void Draw()
    {
        float currentAudioLevel = AudioLevel;
    
        float mainAudioLevel = SmoothValue(
            currentAudioLevel,
            ref mainSmoothedAudioLevel,
            0.2f,
            0.98f,
            (Size.X / 2) + 5f
        );
    
        float secondaryAudioLevel = SmoothValue(
            currentAudioLevel, 
            ref secondarySmoothedAudioLevel,
            0.05f,
            0.7f,
            (Size.X / 2) + 8f, 
            1.02f
        );
    
        Raylib.DrawCircleV(Position, secondaryAudioLevel, new Color(10, 255, 10, 75));
        Raylib.DrawCircleV(Position, mainAudioLevel, new Color(10, 255, 10, 100));
        
        Raylib.DrawCircleV(Position, Size.X / 2, Color.White);
    }
    
    private float SmoothValue(float currentAudioLevel, ref float smoothedAudioLevel, float audioSmoothingFactor, 
        float decaySpeed, float maxAudioLevel, float multiplier = 1f)
    {
        if (currentAudioLevel > 0)
        {
            smoothedAudioLevel = smoothedAudioLevel * (1 - audioSmoothingFactor) +
                                 currentAudioLevel * audioSmoothingFactor;
        }
        else
        {
            smoothedAudioLevel *= decaySpeed;
    
            if (smoothedAudioLevel < 0.01f)
                smoothedAudioLevel = 0;
        }
    
        var sizeAudio = (Size.X / 2) + smoothedAudioLevel * 100;

        // if (sizeAudio > 80.07)
        // {
        //     sizeAudio += 1;
        //     sizeAudio *= multiplier;
        // }

        if (sizeAudio > maxAudioLevel)
            sizeAudio = maxAudioLevel;
        
        return sizeAudio;
    }

    
    public override void Dispose()
    {
        // throw new NotImplementedException();
    }
}
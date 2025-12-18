using System.Collections.ObjectModel;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using App.Scenes.NoAuthCall;
using App.System.Calls.Application.Facade;
using App.System.Calls.Domain;
using App.System.Calls.Media;
using App.System.Services;
using App.System.Utils;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Interface;
using Interface.Buttons;
using Interface.Room;
using Raylib_cs;
using SoundFlow.Structs;
using PointRendering = Engine.PointRendering;
using Rectangle = Raylib_cs.Rectangle;


namespace App.Scenes;

public class Room : Scene
{
    private CallFacade Facade;
    private readonly FontFamily _mainFontStartup;
    private MicrophoneSelectPopup? _micPopup;
    private PlaybackSelectPopup? _volumePopup;
    private List<IntPtr?>? _micDeviceIds;
    private List<IntPtr?>? _playbackDeviceIds;

    private int _micVolumePercent = 100;
    private int _playbackVolumePercent = 100;
    
    public Room()
    {
        Facade = Context.CallFacade;
        Facade.OnCallEnded += HandleCallEnded;
        
        Task.Run(() => { Facade.StartAudioProcess(); });
        
        _mainFontStartup = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        var volumeOnTextureIcon = Resources.Texture("Images\\Icons\\VolumeOn_Black.png");
        var volumeMuteTextureIcon = Resources.Texture("Images\\Icons\\VolumeMute_White.png");
        var texTest0 = Resources.Texture("Images\\Icons\\MicrophoneDefault_White.png");
        var texTest0_b = Resources.Texture("Images\\Icons\\MicrophoneDefault_Black.png");
        var texTest1 = Resources.Texture("Images\\Icons\\MicrophoneMuted_White.png");
        var texTest2 = Resources.Texture("Images\\Icons\\CallHangup_White.png");

        var microControl = new RoomControlIcon(texTest0_b, new Vector2(28, 28), texTest1, true)
        {
            BackgroundColor = Color.White,
            IsActive = true
        };
        
        var volumeControl = new RoomControlIcon(volumeOnTextureIcon, new Vector2(28, 28), volumeMuteTextureIcon, true)
        {
            BackgroundColor = Color.White,
            IsActive = true
        };
        
        volumeControl.OnRelease += (node) =>
        {
            // if (Facade.MicrophoneEnabled)
            // {
            //     Facade.MicrophoneEnabled = false;
            //     volumeControl.IsActive = false;
            // }
            
            Facade.ToggleVolume();
        };
        
        var hangupControl = new RoomControlIcon(texTest2, new Vector2(28, 28), texTest1)
        {
            BackgroundColor = new Color(220, 80, 80),
        };
        
        hangupControl.OnRelease += async (node) =>
        {
            Facade.Hangup();
            HandleCallEnded();
        };
        
        var testList = new List<RoomControlIcon>()
        {
            volumeControl,
            microControl,
            hangupControl
        };
        
        microControl.OnRelease += (node) =>
        {
            Facade.MicrophoneEnabled = !Facade.MicrophoneEnabled;
        };

        if (microControl.SettingsButton != null)
        {
            microControl.SettingsButton.OnClick += (_) =>
            {
                ToggleMicPopup(microControl);
            };
        }
        
        if (volumeControl.SettingsButton != null)
        {
            volumeControl.SettingsButton.OnClick += (_) =>
            {
                ToggleVolumePopup(volumeControl);
            };
        }
        
        AddNodes(testList);
        
        float size = 28 + testList[0].Padding.X + testList[0].CornerWidth;
        
        Render.PlaceInLine(testList, (int) size,
            new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() - 100),
            16
        );

        Facade.OnRemoteMuteChangedByInterlocutor += HandleRemoteMuteChangedByInterlocutor;

        InterlocutorsGrid = new InterlocutorsGrid()
        {
            Interlocutors = Facade?.CurrentSession().Interlocutors
        };
        
        InterlocutorsGrid.MuteDictionary = _muteById;
        
        AddNode(InterlocutorsGrid);

        InterlocutorsGrid.Size = new Vector2(Raylib.GetRenderWidth() - 100, Raylib.GetRenderHeight() - 200);
        InterlocutorsGrid.Position = new Vector2(50, 50);
        InterlocutorsGrid.PointRendering = PointRendering.LeftTop;
    }
    
    private InterlocutorsGrid InterlocutorsGrid;
    
    private readonly Dictionary<string, bool> _muteById = new();
    
    private void HandleRemoteMuteChangedByInterlocutor(string id, bool isMuted)
    {
        _muteById[id] = isMuted;
    }
    
    private void HandleCallEnded()
    {
        Engine.Managers.Scenes.PushScene(new StartUp(false));
    }
    
    protected override void Update(float dt)
    {

    }

    protected override void Draw()
    {
    }

    protected override void Dispose()
    {
        Logger.Write("[Dispose] Room.cs dispose");
        
        if (Facade != null)
        {
            Facade.OnCallEnded -= HandleCallEnded;
            Facade.OnRemoteMuteChangedByInterlocutor -= HandleRemoteMuteChangedByInterlocutor;
        }

        CloseMicPopup();
        CloseVolumePopup();
    }

    private void ToggleMicPopup(RoomControlIcon microControl)
    {
        if (_micPopup != null)
        {
            CloseMicPopup();
            return;
        }

        DeviceInfo[] devices = Facade.GetCaptureDevices();

        var labels = new List<string>();
        _micDeviceIds = new List<IntPtr?>();

        foreach (var d in devices)
        {
            var name = d.Name;
            if (d.IsDefault)
                name += " (default)";

            labels.Add(name);
            _micDeviceIds.Add(d.Id);
        }

        int selectedIndex = 0;
        if (Context.UserSettings?.CaptureDeviceId != null)
        {
            for (int i = 0; i < _micDeviceIds.Count; i++)
            {
                if (_micDeviceIds[i] == Context.UserSettings.CaptureDeviceId)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].IsDefault)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        _micPopup = new MicrophoneSelectPopup(labels, selectedIndex: selectedIndex, initialVolumePercent: _micVolumePercent);

        Facade.SetMicrophoneVolumePercent(_micVolumePercent);

        var parentBounds = microControl.Bounds;
        var popupPos = new Vector2(parentBounds.X - 180f, parentBounds.Y - 220f);
        _micPopup.Position = popupPos;

        _micPopup.OnCloseRequested += CloseMicPopup;
        _micPopup.OnSelected += (index) =>
        {
            if (_micDeviceIds == null || index < 0 || index >= _micDeviceIds.Count)
            {
                CloseMicPopup();
                return;
            }

            var id = _micDeviceIds[index];

            Logger.Write(Logger.Type.Info, $"[UI] Mic selected index={index} id={(id?.ToString() ?? "default")}");
            Facade.SwitchCaptureDevice(id);
            if (Context.UserSettings != null)
                Context.UserSettings.CaptureDeviceId = id;
            CloseMicPopup();
        };

        _micPopup.OnVolumeChanged += (volPercent) =>
        {
            _micVolumePercent = volPercent;
            Logger.Write(Logger.Type.Info, $"[UI] Mic volume set to {_micVolumePercent}%");
            Facade.SetMicrophoneVolumePercent(_micVolumePercent);
        };

        AddNode(_micPopup);
    }

    private void ToggleVolumePopup(RoomControlIcon volumeControl)
    {
        if (_volumePopup != null)
        {
            CloseVolumePopup();
            return;
        }

        DeviceInfo[] devices = Facade.GetPlaybackDevices();

        var labels = new List<string>();
        _playbackDeviceIds = new List<IntPtr?>();

        foreach (var d in devices)
        {
            var name = d.Name;
            if (d.IsDefault)
                name += " (default)";

            labels.Add(name);
            _playbackDeviceIds.Add(d.Id);
        }

        int selectedIndex = 0;
        if (Context.UserSettings?.PlaybackDeviceId != null)
        {
            for (int i = 0; i < _playbackDeviceIds.Count; i++)
            {
                if (_playbackDeviceIds[i] == Context.UserSettings.PlaybackDeviceId)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].IsDefault)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        _volumePopup = new PlaybackSelectPopup(labels, selectedIndex: selectedIndex, initialVolumePercent: _playbackVolumePercent);

        Facade.SetPlaybackVolumePercent(_playbackVolumePercent);

        var parentBounds = volumeControl.Bounds;
        var popupPos = new Vector2(parentBounds.X - 180f, parentBounds.Y - 220f);
        _volumePopup.Position = popupPos;

        _volumePopup.OnCloseRequested += CloseVolumePopup;
        _volumePopup.OnVolumeChanged += (volPercent) =>
        {
            _playbackVolumePercent = volPercent;
            Logger.Write(Logger.Type.Info, $"[UI] Playback volume set to {_playbackVolumePercent}%");
            Facade.SetPlaybackVolumePercent(_playbackVolumePercent);
        };
        _volumePopup.OnSelected += (index) =>
        {
            if (_playbackDeviceIds == null || index < 0 || index >= _playbackDeviceIds.Count)
            {
                CloseVolumePopup();
                return;
            }

            var id = _playbackDeviceIds[index];

            Logger.Write(Logger.Type.Info, $"[UI] Playback selected index={index} id={(id?.ToString() ?? "default")}");
            Facade.SwitchPlaybackDevice(id);
            if (Context.UserSettings != null)
                Context.UserSettings.PlaybackDeviceId = id;
            CloseVolumePopup();
        };

        AddNode(_volumePopup);
    }
    
    private void CloseMicPopup()
    {
        if (_micPopup == null)
            return;

        try
        {
            RemoveNode(_micPopup);
        }
        catch { }

        _micPopup = null;
        _micDeviceIds = null;
    }
    
    private void CloseVolumePopup()
    {
        if (_volumePopup == null)
            return;

        try
        {
            RemoveNode(_volumePopup);
        }
        catch { }

        _volumePopup = null;
        _playbackDeviceIds = null;
    }
}

# MonkeySpeak

**MonkeySpeak** is a P2P voice communication application that operates without a central server for audio transmission. All calls occur directly between participants.

## Key Features

- **P2P voice communication** — direct connection between participants without delays
- **Minimal server load** — server is used only for connection coordination
- **High-quality audio** — Opus encoding with 32 kbps bitrate
- **Simple interface** — create a room or connect by code

## Technologies

- **.NET 9.0** — core framework
- **Raylib-cs** — graphical interface
- **SoundFlow** — audio capture and playback
- **Concentus (Opus)** — audio codec for voice compression
- **WebSocket** — signaling between clients
- **UDP** — audio data transmission

## Engine

The project uses a custom game engine implementation based on **Raylib-cs**. The Engine includes a scene system, resource managers, animation, and UI components, enabling the creation of an interactive application interface.

## Server Side

The application requires a server that performs **signaling** functions (exchanging connection information via WebSocket) and **STUN server** functions (determining public IP address).

The server side is openly distributed in the repository: [monkeyspeak.backend](https://github.com/sanchela228/monkeyspeak.backend)

You can easily deploy the server locally or on your network for fully autonomous system operation.

## How Communication Works

### 1. Connection Initialization

A user can create a session or connect to an existing one by code:

```
[User] → [CreateSession/ConnectToSession] → [WebSocket Server]
```

### 2. Public IP Retrieval

The application uses a STUN server to determine the public IP address:

```
[UDP Client] → [STUN Server] → [Public IP:Port]
```

- In DEBUG mode, the local LAN address is used
- In RELEASE mode, the public IP is obtained via STUN

### 3. Information Exchange via Signaling

The WebSocket server transmits IP address information between participants:

```
[Client A] ← [WebSocket: HolePunching] → [Client B]
            IP A ←----------→ IP B
```

### 4. UDP Hole Punching

A direct UDP connection is established between participants:

```
[Client A UDP] ←--PING/PONG--→ [Client B UDP]
```

- Sending PING packets to punch through NAT
- Receiving PONG confirms connection establishment
- After successful punching, state changes to `Connected`

### 5. Audio Transmission

After connection is established, audio data exchange begins:

```
[Microphone] → [PCM Capture] → [Opus Encoding] → [UDP Packet] 
                                                      ↓
[Speaker] ← [Playback] ← [Opus Decoding] ← [UDP Reception]
```

**Audio Parameters:**
- Sample rate: 48 kHz (Broadcast quality)
- Channels: stereo
- Frame duration: 20 ms
- Bitrate: 32 kbps
- Signal type: OPUS_SIGNAL_VOICE

### 6. Connection Control

UDP transmits not only audio data but also control commands:

- **MessageType.Audio** — audio data
- **MessageType.Control** — control commands (mute, unmute, hangup)
- **MessageType.HolePunch** — service messages for maintaining connection

## Project Structure

```
MonkeySpeak/
├── App/
│   ├── Scenes/              # UI scenes (StartUp, Room, Creator, Invited)
│   ├── System/
│   │   ├── Calls/           # Call logic
│   │   │   ├── Application/ # P2PCallManager, CallFacade
│   │   │   ├── Media/       # AudioTranslator (audio processing)
│   │   │   └── Infrastructure/ # WebSocket, STUN clients
│   │   ├── Managers/        # UdpUnifiedManager
│   │   └── Modules/         # WebSocketClient, Network
│   └── Context.cs           # Global application context
└── Program.cs               # Entry point
```

## Running

1. Make sure **.NET 9.0 SDK** is installed
2. Configure `NetworkConfig.xml` with your server address:
   ```xml
   <Domain>your-server.com</Domain>
   <Port>8080</Port>
   ```
3. Build and run the project:
   ```bash
   dotnet run
   ```

## Security

- Device private keys are stored in `SecureStorage`
- Each session has a unique token
- P2P connection minimizes data transmission through the server

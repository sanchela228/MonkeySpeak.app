# MonkeySpeak

P2P voice communication app with direct connections between participants, no central server required. Works without registration or authentication.

## Features

- **Mesh P2P** — group calls with direct connections between all participants
- **No registration** — instant anonymous calls with session codes
- **NAT traversal** — UDP hole punching to work behind NAT
- **High-quality audio** — Opus codec (48kHz, 64kbps)
- **Noise suppression** — RNNoise neural network for background noise filtering

## Technologies

- **.NET 9.0** — application core
- **Raylib-cs** — graphics interface
- **SoundFlow** — audio capture and playback
- **Opus (Concentus)** — voice encoding
- **RNNoise** — neural noise suppression
- **WebSocket** — signaling
- **UDP** — audio transmission

## Architecture

```
WebSocket Server (signaling) → IP address exchange
         ↓
UDP Hole Punching → direct P2P connection
         ↓
Microphone → RNNoise → Opus → UDP → Decoding → Speaker
```

**Mesh topology**: each participant is directly connected to all others.

## Server

Signaling server: [monkeyspeak.backend](https://github.com/sanchela228/monkeyspeak.backend)

Can be deployed locally for fully autonomous operation.

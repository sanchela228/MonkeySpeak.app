# SoundFlow API — Конспект по C#

Обновляемый краткий справочник по API SoundFlow. Структурирован для быстрого поиска: неймспейсы, ключевые классы, компоненты, редактирование, энумы, расширения (WebRTC APM), интерфейсы, исключения.

- Версия документации: 1.2.0
- Бэкенд по умолчанию: MiniAudio (miniaudio)

## Навигация
- [Неймспейсы](#неймспейсы)
- [Ключевые классы (Abstracts)](#ключевые-классы-abstracts)
- [Backends.MiniAudio](#backendsminiaudio)
- [Components](#components)
- [Editing](#editing)
- [Editing.Persistence](#editingpersistence)
- [Enums](#enums)
- [Extensions.WebRtc.Apm](#extensionswebrtcapm)
- [Interfaces](#interfaces)
- [Modifiers](#modifiers)
- [Exceptions](#exceptions)
- [Типовые сценарии](#типовые-сценарии)

---

## Мини-оглавление по частям
- [Полная справка (Часть 1): Abstracts и Backends.MiniAudio](#полная-справка-часть-1)
- [Полная справка (Часть 2): Components](#полная-справка-часть-2)
- [Полная справка (Часть 3): Editing и Editing.Persistence](#полная-справка-часть-3)
- [Полная справка (Часть 4): Enums, Interfaces, Modifiers, Exceptions](#полная-справка-часть-4)
- [Полная справка (Часть 5): Extensions.WebRtc.Apm и Backends notes](#полная-справка-часть-5)
-. [Полная справка (Часть 6): Enums (полные значения) и Editing (Composition/Track)](#полная-справка-часть-6)
-. [Полная справка (Часть 7): Editing (AudioSegment/Settings/TrackSettings/Loop/Fade)](#полная-справка-часть-7)
-. [Полная справка (Часть 8): Editing.Persistence (DTO) и Visualization](#полная-справка-часть-8)
- [Visualization](#visualization--обзор)

---

## Неймспейсы
- `SoundFlow.Abstracts` — базовые абстракции фреймворка: движок, устройства, компоненты, модификаторы, анализаторы.
- `SoundFlow.Backends` — реализации бэкендов ввода/вывода (основной: `SoundFlow.Backends.MiniAudio`).
- `SoundFlow.Components` — конкретные `SoundComponent`: воспроизведение, микширование, синтез, анализ, запись.
- `SoundFlow.Editing` — неразрушающее редактирование (композиции/треки/сегменты/настройки).
- `SoundFlow.Editing.Persistence` — сохранение/загрузка проектов `.sfproj` и DTO.
- `SoundFlow.Enums` — перечисления, используемые по всему API.
- `SoundFlow.Exceptions` — исключения библиотеки.
- `SoundFlow.Interfaces` — контракты (`ISoundDataProvider`, `ISoundEncoder/Decoder/Player`, `IVisualizer`, ...).
- `SoundFlow.Modifiers` — конкретные `SoundModifier` (эффекты).
- `SoundFlow.Providers` — реализации `ISoundDataProvider` (источники аудио).
- `SoundFlow.Structs` — вспомогательные структуры.
- `SoundFlow.Utils` — утилиты и расширения.
- `SoundFlow.Visualization` — анализ и визуализация аудио.
- `SoundFlow.Extensions` — официальные расширения
  - `SoundFlow.Extensions.WebRtc.Apm` — интеграция WebRTC Audio Processing Module (AEC/NS/AGC и т.д.).

---

## Ключевые классы (Abstracts)
- `AudioEngine` — корневой контекст/движок: управление жизненным циклом устройств, кодирование/декодирование.
- `AudioDevice` — базовый класс инициализированного устройства (ввод/вывод).
- `AudioPlaybackDevice` — устройство вывода; содержит `MasterMixer`.
- `AudioCaptureDevice` — устройство ввода; событие `OnAudioProcessed`.
- `FullDuplexDevice` — упрощенная связка ввода/вывода для симметричного I/O.
- `DeviceConfig` — базовая конфигурация устройства (бэкенд-специфична).
- `SoundComponent` — базовый узел аудиографа.
- `SoundModifier` — базовый класс эффектов (модификаторов).
- `SoundPlayerBase` — базовый класс плееров; реализует `ISoundPlayer`, поддерживает time-stretch.

---

## Backends.MiniAudio
- `MiniAudioEngine` — реализация `AudioEngine` на `miniaudio`.
- `MiniAudioDeviceConfig` — реализация `DeviceConfig` для MiniAudio (WASAPI/CoreAudio/ALSA и др.).
- `MiniAudioDecoder` — `ISoundDecoder` на базе miniaudio.
- `MiniAudioEncoder` — `ISoundEncoder` на базе miniaudio (как правило, WAV для encode).

---

## Components
- `Mixer` — микшер нескольких потоков. Доступ к корневому микшеру: `Mixer.Master`.
- `SoundPlayer` — воспроизведение из `ISoundDataProvider`.
- `SurroundPlayer` — как `SoundPlayer`, но с поддержкой многоканальных конфигураций (позиции, задержки, панорамирование).
- `Recorder` — захват аудио с устройства ввода; сохранение/коллбэк.
- `Oscillator` — генератор волн (sine/square/saw/triangle/noise/pulse).
- `LowFrequencyOscillator` — низкочастотный LFO (формы, триггеры).
- `EnvelopeGenerator` — ADSR огибающая.
- `Filter` — цифровые фильтры (LP/HP/BP/Notch, параметрический).
- `VoiceActivityDetector` — VAD с гистерезисом (активация/задержка).
- `WsolaTimeStretcher` — WSOLA-тайм-стретч без изменения питча.

---

## Editing
- `Composition` — верхнеуровневый проект (рендерится как `ISoundDataProvider`), `IDisposable`.
- `Track` — трек внутри `Composition`, содержит `AudioSegment`.
- `AudioSegment` — клип на таймлайне трека; ссылается на источник + применяет настройки; `IDisposable`.
- `AudioSegmentSettings` — параметры сегмента (volume, pan, fades, loop, reverse, speed, time-stretch, modifiers, analyzers).
- `TrackSettings` — параметры трека (volume, pan, mute, solo, enabled, modifiers, analyzers).
- `LoopSettings` (struct) — поведение лупа (повторы, целевая длительность).
- `FadeCurveType` (enum) — типы кривых фейда (Linear/Logarithmic/SCurve).

---

## Editing.Persistence
- `CompositionProjectManager` — статические методы сохранения/загрузки `.sfproj` (инклюд/консолидация/перелинковка медиа).
- `ProjectData` — корневой DTO проекта.
- `ProjectTrack` — DTO трека.
- `ProjectSegment` — DTO сегмента.
- `ProjectAudioSegmentSettings` — DTO настроек сегмента.
- `ProjectTrackSettings` — DTO настроек трека.
- `ProjectSourceReference` — способ ссылки на источник (файл/встроенные данные/консолидация).
- `ProjectEffectData` — сериализация модификаторов/анализаторов (тип + параметры).

---

## Enums
- `Capability` — возможности устройства (Playback/Record/Mixed/Loopback).
- `DeviceType` — тип устройства (Playback/Capture).
- `EncodingFormat` — формат кодирования (WAV/FLAC/MP3/Vorbis) — MiniAudio encoder обычно только WAV.
- `PlaybackState` — состояние (Stopped/Playing/Paused).
- `Result` — коды успеха/ошибок нативных операций.
- `SampleFormat` — формат сэмплов (U8/S16/S24/S32/F32).
- `FilterType` — тип фильтра (Peaking/LowShelf/HighShelf/BandPass/Notch/LowPass/HighPass).
- `EnvelopeGenerator.EnvelopeState` — Idle/Attack/Decay/Sustain/Release.
- `EnvelopeGenerator.TriggerMode` — NoteOn/Gate/Trigger.
- `LowFrequencyOscillator.WaveformType` — Sine/Square/Triangle/Sawtooth/ReverseSawtooth/Random/SampleAndHold.
- `LowFrequencyOscillator.TriggerMode` — FreeRunning/NoteTrigger.
- `Oscillator.WaveformType` — Sine/Square/Sawtooth/Triangle/Noise/Pulse.
- `SurroundPlayer.SpeakerConfiguration` — Stereo/Quad/Surround51/Surround71/Custom.
- `SurroundPlayer.PanningMethod` — Linear/EqualPower/Vbap.

Расширения WebRTC APM (см. ниже):
- `ApmError`, `NoiseSuppressionLevel`, `GainControlMode`, `DownmixMethod`, `RuntimeSettingType`.

---

## Extensions.WebRtc.Apm
- `AudioProcessingModule` — доступ к нативному WebRTC APM (AEC, NS, AGC и пр.).
- `ApmConfig` — конфигурирование APM (вкл/выкл, параметры).
- `StreamConfig` — формат потока (sample rate, channels) для APM.
- `ProcessingConfig` — набор конфигураций потоков (input/output/reverse).

Components:
- `NoiseSuppressor` — оффлайн/пакетная шумоподавлялка на базе WebRTC APM поверх `ISoundDataProvider`.

Modifiers:
- `WebRtcApmModifier` — real-time применение APM (AEC/NS/AGC) внутри графа SoundFlow; настраивается.

---

## Interfaces
- `ISoundDataProvider` — единый способ чтения аудиоданных (обычно pull-стиль), `IDisposable`.
- `ISoundDecoder` — декодирование из формата в PCM.
- `ISoundEncoder` — кодирование PCM в формат.
- `ISoundPlayer` — управление воспроизведением (Play/Pause/Stop/Seek/Loop/Speed/Volume).
- `IVisualizationContext` — методы рисования для визуализаций.
- `IVisualizer` — контракт визуализатора аудио.

---

## Modifiers
Примеры эффектов (неполный список):
- `AlgorithmicReverbModifier` — реверберация (comb/all-pass), поддержка многоканальности.
- `BassBoosterModifier` — усиление НЧ (резонансный LPF).
- `Filter`/`ParametricEqualizer` — эквализация/фильтрация.
- ... и другие эффекты из `SoundFlow.Modifiers`.

---

## Exceptions
- `BackendException` — ошибка конкретного аудиобэкенда.

---

## Типовые сценарии

### Воспроизведение файла/потока
1) `MiniAudioEngine` → `InitializePlaybackDevice(...)`.
2) Провайдер (`ISoundDataProvider`): файл/поток/буфер.
3) `SoundPlayer(engine, format, provider)` и добавление в `MasterMixer`.
4) `playbackDevice.Start(); player.Play();`.

### Захват микрофона → прямая передача в наушники (микрофон-мониторинг)
1) `InitializeCaptureDevice(...)` и подписка на `OnAudioProcessed`.
2) Поток-буфер (например, `ProducerConsumerStream`) или собственный `ISoundDataProvider`.
3) `RawDataProvider(Stream, SampleFormat, SampleRate, Channels)` поверх потока.
4) Пишем байты PCM в поток внутри обработчика.
5) `InitializePlaybackDevice(...)`, `SoundPlayer(...)`, `AddComponent`, `Start/Play`.

### Неразрушающее редактирование и сохранение проекта
1) Сборка `Composition` → `Track` → `AudioSegment` (+ настройки).
2) Сохранение через `CompositionProjectManager` в `.sfproj`.

### WebRTC APM (NS/AEC/AGC)
- Offline: `NoiseSuppressor` над `ISoundDataProvider`.
- Real-time: `WebRtcApmModifier` в графе (между провайдером и плеером/энкодером), конфигурация через `ApmConfig`.

---

## Заметки и ограничения
- MiniAudio encoder чаще всего ограничен WAV для encode — для real-time VoIP используйте внешний кодек (например, Opus/Concentus) и оборачивайте в свой `ISoundEncoder/Decoder` при необходимости.
- Следите за согласованием форматов (sample rate/каналы/sample format) между capture → provider → player.
- Для low-latency избегайте лишних аллокаций; используйте буферизацию/пулы.

---

## Полезные точки входа в коде
- Устройства: `engine.InitializePlaybackDevice(...)`, `engine.InitializeCaptureDevice(...)`.
- Микшер: `playbackDeviceWorker.MasterMixer.AddComponent(component)`.
- События захвата: `captureDeviceWorker.OnAudioProcessed += (samples, capability) => { ... }`.
- Провайдеры: `RawDataProvider` (stream/byte[]/short[]/int[]/float[]), собственные реализации `ISoundDataProvider`.
- Расширения APM: `SoundFlow.Extensions.WebRtc.Apm.*`.

---

# Полная справка (Часть 1)

Ниже приводится расширенная версия документации (конвертация из HTML), включая подробные описания, свойства, события и методы. Эта часть покрывает:

- Подробные Abstracts (ключевые абстрактные классы)
- Backends.MiniAudio: `MiniAudioDecoder`, `MiniAudioEncoder`, `MiniAudioEngine`, `MiniAudioDeviceConfig` и вложенные конфигурации

## Abstracts — подробности

Таблица основных абстракций и краткие описания:

| Класс/Интерфейс | Описание |
|---|---|
| `AudioAnalyzer` | Абстрактная база для анализаторов аудио. Наследуется от `SoundComponent`. |
| `AudioEngine` | Абстракция движка: управление жизненным циклом аудиоустройств, кодирование/декодирование, корневой контекст. |
| `AudioDevice` | База для инициализированного аудио-устройства (ввод/вывод). |
| `AudioPlaybackDevice` | Инициализированное устройство вывода, содержит `MasterMixer`. |
| `AudioCaptureDevice` | Инициализированное устройство ввода, экспонирует событие `OnAudioProcessed`. |
| `FullDuplexDevice` | Высокоуровневое управление парой playback+capture для одновременного I/O. |
| `DeviceConfig` | База для бэкенд-специфичных конфигураций устройств. |
| `SoundComponent` | База всех узлов аудиографа. |
| `SoundModifier` | База эффектов, модифицирующих сэмплы. |
| `SoundPlayerBase` | База для плееров. Реализует `ISoundPlayer`, поддерживает time-stretch. |

Примечания:
- `AudioCaptureDevice.OnAudioProcessed` — основной вход для получения «сырых» сэмплов в real-time.
- `AudioPlaybackDevice.MasterMixer` — точка подключения компонентов (например, `SoundPlayer`).

## Backends.MiniAudio — подробности

### MiniAudioDecoder

Описание: декодер, использующий библиотеку `miniaudio` для чтения аудио из потока/файла в PCM.

Свойства:
- `IsDisposed`: флаг освобождения ресурсов.
- `Length`: длина декодируемых данных в сэмплах (может обновляться после первичной инициализации, если длина потока неизвестна заранее).
- `SampleFormat`: формат сэмплов выходных данных.

События:
- `EndOfStreamReached`: срабатывает при достижении конца аудиопотока.

Методы:
- `Decode(Span<float> samples)`: декодирует часть аудиопотока в предоставленный буфер. Потокобезопасен внутри.
- `Seek(int offset)`: перемещается к указанному смещению (в сэмплах). Потокобезопасен внутри.
- `Dispose()`: освобождение ресурсов.

Пример (псевдокод):
```csharp
using var engine = new MiniAudioEngine();
using var fs = File.OpenRead("file.mp3");
using var decoder = engine.CreateDecoder(fs, AudioFormat.DvdHq);
Span<float> buf = stackalloc float[4096];
int read = decoder.Decode(buf);
```

### MiniAudioEncoder

Описание: энкодер на базе `miniaudio`. Поддержка кодирования обычно в WAV.

Свойства:
- `IsDisposed`

Методы:
- `Encode(Span<float> samples)`: кодирует сэмплы и пишет в выходной поток/файл.
- `Dispose()`

Пример (WAV):
```csharp
using var engine = new MiniAudioEngine();
using var outStream = File.Create("out.wav");
using var encoder = engine.CreateEncoder(outStream, EncodingFormat.WAV, AudioFormat.DvdHq);
Span<float> frame = stackalloc float[2048];
// Заполнить frame...
encoder.Encode(frame);
```

### MiniAudioEngine

Описание: конкретная реализация `AudioEngine` на базе `miniaudio`. Отвечает за обнаружение устройств, инициализацию, callbacks, фабрики энкодеров/декодеров. Кроссплатформенная поддержка (Windows/macOS/Linux/Android/iOS).

Ключевые методы:
- `InitializePlaybackDevice(DeviceInfo? deviceInfo, AudioFormat format, DeviceConfig? config = null)` → `AudioPlaybackDevice`
- `InitializeCaptureDevice(DeviceInfo? deviceInfo, AudioFormat format, DeviceConfig? config = null)` → `AudioCaptureDevice`
- `InitializeFullDuplexDevice(DeviceInfo? playback, DeviceInfo? capture, AudioFormat format, DeviceConfig? config = null)` → `FullDuplexDevice`
- `InitializeLoopbackDevice(AudioFormat format, DeviceConfig? config = null)` → `AudioCaptureDevice` (WASAPI loopback на Windows)
- `CreateEncoder(Stream stream, EncodingFormat encodingFormat, AudioFormat format)` → `ISoundEncoder`
- `CreateDecoder(Stream stream, AudioFormat format)` → `ISoundDecoder`
- `SwitchDevice(...)` перегрузки для Playback/Capture/FullDuplex
- `UpdateDevicesInfo()` — обновление списков устройств

### MiniAudioDeviceConfig

Описание: детальная конфигурация устройства для MiniAudio. Позволяет тонко настраивать латентность, размеры буферов, платформенные настройки (WASAPI/CoreAudio/ALSA/Pulse/OpenSL/AAudio).

Общие свойства:
- `PeriodSizeInFrames` — размер внутреннего буфера в фреймах (приоритетнее миллисекунд).
- `PeriodSizeInMilliseconds` — размер буфера в мс.
- `Periods` — число периодов в буфере.
- `NoPreSilencedOutputBuffer` — не обнулять выходной буфер перед callback (микрооптимизация).
- `NoClip` — не клиппировать значения F32 вне [-1..1].
- `NoDisableDenormals` — не отключать денормалы (может снизить производительность).
- `NoFixedSizedCallback` — бэкенд может отдавать буферы переменного размера.

Вложенные конфигурации:

`DeviceSubConfig`
- `ShareMode` (Shared/Exclusive) — режим открытия устройства. `Exclusive` может дать меньшую задержку, но не всегда поддерживается.
- `IsLoopback` (internal) — флаг для loopback-режима записи.

`WasapiSettings` (Windows)
- `Usage` — назначение потока (Default/Games/ProAudio) — влияет на системную обработку и приоритет.
- `NoAutoConvertSRC` — отключить автоматический SRC в WASAPI.
- `NoDefaultQualitySRC` — запретить дефолтное качество SRC WASAPI.
- `NoAutoStreamRouting` — отключить авто-маршрутизацию.
- `NoHardwareOffloading` — отключить аппаратный offload.

`CoreAudioSettings` (macOS/iOS)
- `AllowNominalSampleRateChange` — разрешить ОС менять sample rate под поток.

`AlsaSettings` (Linux/ALSA)
- `NoMMap` — отключить memory-mapped режим.
- `NoAutoFormat` — запретить авто-конверсию формата.
- `NoAutoChannels` — запретить авто-конверсию количества каналов.
- `NoAutoResample` — запретить авто-ресемплинг.

`PulseSettings` (Linux/PulseAudio)
- `StreamNamePlayback` — имя потока воспроизведения (видно в микшере PulseAudio).
- `StreamNameCapture` — имя потока записи.

`OpenSlSettings` (Android/OpenSL ES)
- `StreamType` — тип аудио-потока (Voice/Media/Alarm и т.п.) для фокуса/роутинга.
- `RecordingPreset` — пресет записи (VoiceCommunication/Camcorder и пр.).

`AAudioSettings` (Android/AAudio)
- `Usage` — назначение (Media/Game/Assistant и др.).
- `ContentType` — тип контента (Music/Speech/Sonification).
- `InputPreset` — профиль входа (VoiceRecognition/Camcorder и пр.).
- `AllowedCapturePolicy` — политика разрешенного захвата.

Практические рекомендации:
- Для уменьшения задержки на Windows попробуйте `DeviceSubConfig.ShareMode = Exclusive` и подберите `PeriodSizeInFrames`/`Periods`.
- Следите за соответствием `AudioFormat` у движка и реальных возможностей устройства (иначе включится ресемплинг/переколонка).


# Полная справка (Часть 2)

Эта часть покрывает ключевые `Components` с развернутыми описаниями и примерами кода.

## Mixer
Описание: `SoundComponent`, который суммирует несколько входящих аудио-потоков. Имеет корневой микшер `Mixer.Master`.

Основные моменты:
- Добавление компонента: `playbackDeviceWorker.MasterMixer.AddComponent(component)`.
- Управление уровнем/панорамой обычно на уровне источника (плеера/сегмента) или модификаторов.

Пример — добавление плеера в корневой микшер:
```csharp
using var player = new SoundPlayer(engine, format, provider);
playbackDeviceWorker.MasterMixer.AddComponent(player);
player.Play();
```

## SoundPlayer
Описание: Реализация `SoundPlayerBase`, воспроизводит из `ISoundDataProvider`.

Основные возможности:
- Управление: `Play()`, `Pause()`, `Stop()`, `Seek(...)`.
- Параметры: громкость, скорость (time-stretch поддерживается базой), лупинг — в зависимости от реализации/настроек провайдера.

Пример — проигрывание из потока (реал-тайм мониторинг микрофона):
```csharp
var pcmStream = new ProducerConsumerStream();
using var provider = new RawDataProvider(pcmStream, SampleFormat.F32, 48000, 1);
using var player = new SoundPlayer(engine, AudioFormat.DvdHq, provider);
playbackDeviceWorker.MasterMixer.AddComponent(player);
player.Play();

captureDeviceWorker.OnAudioProcessed += (float[] samples, Capability cap) =>
{
    ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(samples.AsSpan());
    var buf = bytes.ToArray();
    pcmStream.Write(buf, 0, buf.Length);
};
```

## SurroundPlayer
Описание: Расширение `SoundPlayer` для многоканального воспроизведения. Позволяет задавать конфигурацию колонок (Stereo/Quad/5.1/7.1/Custom), позиционирование, задержки и метод панорамирования (Linear/EqualPower/VBAP).

Типовой сценарий:
- Подготовить `ISoundDataProvider` с соответствующим числом каналов.
- Выбрать `SpeakerConfiguration` и `PanningMethod`.
- Подключить к `MasterMixer`.

## Recorder
Описание: `SoundComponent` для захвата входного аудио. Позволяет сохранять в поток/файл или обрабатывать callback’ом.

Пример — запись в WAV через `MiniAudioEncoder`:
```csharp
using var outStream = File.Create("mic.wav");
using var encoder = engine.CreateEncoder(outStream, EncodingFormat.WAV, AudioFormat.DvdHq);

// Допустим, у нас есть буфер с float-сэмплами frame
Span<float> frame = stackalloc float[2048];
encoder.Encode(frame);
```

Пример — обработка входа через событие:
```csharp
captureDeviceWorker.OnAudioProcessed += (float[] samples, Capability cap) =>
{
    // Обработка/анализ/трансляция: кодек/UDP, APM и т.п.
};
```

## Oscillator
Описание: Генератор базовых форм волн: `Sine`, `Square`, `Sawtooth`, `Triangle`, `Noise`, `Pulse`.

Пример — генерация тона 440 Гц и воспроизведение:
```csharp
var osc = new Oscillator(engine, AudioFormat.DvdHq)
{
    Frequency = 440f,
    Waveform = Oscillator.WaveformType.Sine,
    Amplitude = 0.2f
};
playbackDeviceWorker.MasterMixer.AddComponent(osc);
```

## LowFrequencyOscillator (LFO)
Описание: Низкочастотный генератор (модуляция параметров). Поддерживает разные waveform’ы и режимы триггера.

Типичный кейс: модулировать параметр фильтра/громкости/панорамы — в связке с другими компонентами.

## EnvelopeGenerator (ADSR)
Описание: Генератор огибающей Attack/Decay/Sustain/Release. Режимы триггера: `NoteOn`, `Gate`, `Trigger`.

Пример — запуск ноты:
```csharp
var env = new EnvelopeGenerator(engine, AudioFormat.DvdHq)
{
    Attack = 0.01f,
    Decay = 0.2f,
    Sustain = 0.8f,
    Release = 0.4f,
};
env.Trigger(EnvelopeGenerator.TriggerMode.NoteOn);
```

## Filter / ParametricEqualizer
Описание: Цифровые фильтры и параметрический эквалайзер. Тип фильтра задается `FilterType` (Peaking/LowShelf/HighShelf/BandPass/Notch/LowPass/HighPass).

Пример — low-pass:
```csharp
var filter = new Filter(engine, AudioFormat.DvdHq)
{
    Type = FilterType.LowPass,
    Cutoff = 5000f,
    Q = 0.707f
};
// Включить в граф между источником и микшером — зависит от API вашей сборки
```

## VoiceActivityDetector (VAD)
Описание: Анализатор речи, определяет наличие голоса. Имеет настраиваемые времена активации/задержки (hangover), чтобы избежать дребезга состояния.

Пример — логирование активности речи:
```csharp
var vad = new VoiceActivityDetector(engine, AudioFormat.DvdHq)
{
    ActivationTimeMs = 100,
    HangoverTimeMs = 200
};
// Подключите VAD к ветке анализа/мониторинга входящего потока
```

## WsolaTimeStretcher
Описание: Реализация WSOLA для real-time time-stretch без изменения высоты тона. Используется внутри `AudioSegment`, может быть доступен как отдельный компонент/вспомогатель.

Советы по latency:
- Меньшие буферы устройств (PeriodSize/Periods) снижают задержку, но повышают риск xruns/underruns.
- Избегайте аллокаций внутри аудио-callback’ов; используйте пулы/Span/stackalloc.



# Полная справка (Часть 3)

Эта часть детально покрывает `Editing` и `Editing.Persistence` с примерами.

## Editing — детали

Ключевые сущности:
- `Composition` — контейнер проекта. Может выступать как `ISoundDataProvider` (рендер), `IDisposable`.
- `Track` — дорожка, содержит `AudioSegment` и track-level настройки/эффекты/анализаторы.
- `AudioSegment` — клип на таймлайне, ссылается на источник (провайдер/файл), имеет локальные настройки воспроизведения.
- `AudioSegmentSettings` — настройки сегмента: `Volume`, `Pan`, `FadeIn`, `FadeOut`, `LoopSettings`, `Reverse`, `Speed`, `TimeStretchEnabled`, `Modifiers`, `Analyzers` и др.
- `TrackSettings` — настройки дорожки: `Volume`, `Pan`, `Mute`, `Solo`, `Enabled`, `Modifiers`, `Analyzers`.
- `LoopSettings` (struct) — описывает луп (число повторов или целевая длительность).
- `FadeCurveType` (enum) — форма кривой фейда: `Linear`, `Logarithmic`, `SCurve`.

Типичный рабочий процесс:
1) Создать `Composition` с заданным форматом.
2) Добавить один или несколько `Track`.
3) Для каждого трека добавить `AudioSegment`(ы) с источником (`ISoundDataProvider` или декодером файла) и задать `AudioSegmentSettings`.
4) По необходимости применять `SoundModifier`/`AudioAnalyzer` на уровне сегмента или трека.
5) Воспроизведение: либо через `SoundPlayer` от `Composition` (как провайдера), либо оффлайн-рендер в поток.

Пример — простая композиция с одним треком и сегментом:
```csharp
using var engine = new MiniAudioEngine();
var format = AudioFormat.DvdHq; // 48kHz, обычно 2ch F32

// Источник: декодер файла (пример)
using var fs = File.OpenRead("music.flac");
using var decoder = engine.CreateDecoder(fs, format);

// 1) Создаём композицию
using var composition = new Composition(format)
{
    // Общие настройки композиции при необходимости
};

// 2) Добавляем трек
var track = composition.AddTrack(new TrackSettings
{
    Volume = 0.9f,
    Pan = 0f,
});

// 3) Добавляем сегмент на трек
var segSettings = new AudioSegmentSettings
{
    Volume = 1.0f,
    Pan = 0f,
    FadeInMs = 1000,
    FadeOutMs = 1000,
    FadeInCurve = FadeCurveType.SCurve,
    FadeOutCurve = FadeCurveType.SCurve,
    Loop = new LoopSettings { /* Repetitions = 2, либо TargetDurationMs = 30000 */ },
    Reverse = false,
    Speed = 1.0f,
    TimeStretchEnabled = true,
    // Modifiers = new [] { new AlgorithmicReverbModifier(...) },
    // Analyzers = new [] { new VoiceActivityDetector(...) }
};

// Предположим, AudioSegment умеет ссылаться на provider/decoder
var segment = track.AddSegment(new AudioSegment(source: decoder, startMs: 0, durationMs: null, segSettings));

// 4) Воспроизведение композиции через SoundPlayer
var playback = engine.InitializePlaybackDevice(null, format);
using var player = new SoundPlayer(engine, format, composition /* как ISoundDataProvider */);
playback.MasterMixer.AddComponent(player);
playback.Start();
player.Play();
```

Пример — изменение настроек на лету (панорама трека, time-stretch сегмента):
```csharp
track.Settings.Pan = -0.2f; // немного влево
segment.Settings.Speed = 1.25f; // ускорение
segment.Settings.TimeStretchEnabled = true; // поддержка WSOLA для сохранения питча
```

Фейды и лупы:
- Фейд-ин/аут задаются миллисекундами и типом кривой (`FadeCurveType`).
- Луп: можно задать фиксированное число повторов (`Repetitions`) или целевую длительность (`TargetDurationMs`).

Modifiers/Analyzers на разных уровнях:
- На сегменте — индивидуальный эффект/анализ только для этого клипа.
- На треке — общие эффекты/анализаторы для всей дорожки.
- На мастер-микшере — глобальные эффекты (например, легкий мастер-лимитер/реверб).

## Editing — оффлайн рендер

`Composition` может выступать как `ISoundDataProvider`. Это позволяет:
- Воспроизводить композицию через `SoundPlayer` (real-time превью).
- Оффлайн-рендерить в поток/файл — с помощью `ISoundEncoder` (например, WAV через MiniAudio) или внешнего кодека.

Пример — экспорт в WAV:
```csharp
using var outStream = File.Create("mixdown.wav");
using var encoder = engine.CreateEncoder(outStream, EncodingFormat.WAV, format);

Span<float> buffer = stackalloc float[4096];
int read;
while ((read = composition.Read(buffer)) > 0)
{
    encoder.Encode(buffer[..read]);
}
```

## Editing.Persistence — детали

Основные типы:
- `CompositionProjectManager` — сохранение/загрузка `.sfproj`.
- DTO: `ProjectData` (корень), `ProjectTrack`, `ProjectSegment`, `ProjectAudioSegmentSettings`, `ProjectTrackSettings`, `ProjectSourceReference`, `ProjectEffectData`.

Возможности менеджера проекта:
- Сохранение текущей структуры композиции, включая треки, сегменты, их настройки, ссылки на источники.
- Консолидация медиа (копирование в папку проекта) и последующее перелинкование.
- Восстановление проекта из `.sfproj` c relink источников при необходимости.

Пример — сохранение проекта:
```csharp
var saveOptions = new ProjectSaveOptions
{
    ConsolidateMedia = true,           // собрать медиа в папку проекта
    EmbedSmallMediaThresholdBytes = 0,  // можно встраивать короткие клипы как байты
};

CompositionProjectManager.Save(
    composition,
    projectPath: "MySession.sfproj",
    options: saveOptions
);
```

Пример — загрузка проекта:
```csharp
using var loaded = CompositionProjectManager.Load(
    engine,
    projectPath: "MySession.sfproj",
    format: AudioFormat.DvdHq,
    relinkStrategy: MissingMediaRelinkStrategy.PromptOrSearch
);

// Воспроизвести загруженную композицию
var playback = engine.InitializePlaybackDevice(null, AudioFormat.DvdHq);
using var player = new SoundPlayer(engine, AudioFormat.DvdHq, loaded);
playback.MasterMixer.AddComponent(player);
playback.Start();
player.Play();
```

`ProjectSourceReference` — как хранится источник:
- Путь к файлу (относительный к проекту или абсолютный).
- Встроенные данные (для мелких файлов/семплов).
- Запись о консолидации (копия в папке проекта).

`ProjectEffectData` — сериализация эффектов/анализаторов:
- Имя типа/квалифицированное имя.
- Набор параметров (ключ-значение) для восстановления конфигурации.

Примечания по совместимости:
- Если в проекте есть нестандартные/внешние эффекты, при загрузке потребуется их доступность (сборки/плагины должны быть в пути).
- Разные платформы/бэкенды могут иметь особые правила поиска устройств — сохраняйте только необходимые привязки и будьте готовы к `SwitchDevice(...)` на старте.

---

# Полная справка (Часть 4)

В этом разделе собраны ключевые перечисления (Enums), интерфейсы (Interfaces), модификаторы (Modifiers) и исключения (Exceptions) с краткими пояснениями и примерами.

## Enums — полный обзор

| Enum | Назначение / Значения |
|---|---|
| `Capability` | Возможности устройства: `Playback`, `Record`, `Mixed`, `Loopback`. |
| `DeviceType` | Тип устройства: `Playback`, `Capture`. |
| `EncodingFormat` | Формат кодирования: `WAV`, `FLAC`, `MP3`, `Vorbis`. Примечание: MiniAudio encoder обычно поддерживает только WAV. |
| `PlaybackState` | Состояние плеера/рекордера: `Stopped`, `Playing`, `Paused`. |
| `Result` | Результат нативных операций (коды успеха/ошибок). |
| `SampleFormat` | Формат сэмплов: `U8`, `S16`, `S24`, `S32`, `F32`. |
| `FilterType` | Типы фильтра: `Peaking`, `LowShelf`, `HighShelf`, `BandPass`, `Notch`, `LowPass`, `HighPass`. |
| `EnvelopeGenerator.EnvelopeState` | `Idle`, `Attack`, `Decay`, `Sustain`, `Release`. |
| `EnvelopeGenerator.TriggerMode` | `NoteOn`, `Gate`, `Trigger`. |
| `LowFrequencyOscillator.WaveformType` | `Sine`, `Square`, `Triangle`, `Sawtooth`, `ReverseSawtooth`, `Random`, `SampleAndHold`. |
| `LowFrequencyOscillator.TriggerMode` | `FreeRunning`, `NoteTrigger`. |
| `Oscillator.WaveformType` | `Sine`, `Square`, `Sawtooth`, `Triangle`, `Noise`, `Pulse`. |
| `SurroundPlayer.SpeakerConfiguration` | `Stereo`, `Quad`, `Surround51`, `Surround71`, `Custom`. |
| `SurroundPlayer.PanningMethod` | `Linear`, `EqualPower`, `Vbap`. |
| `FadeCurveType` | Кривые фейда: `Linear`, `Logarithmic`, `SCurve`. |
| APM (Extensions.WebRtc.Apm) | `ApmError`, `NoiseSuppressionLevel` (`Low/Moderate/High/VeryHigh`), `GainControlMode` (`AdaptiveAnalog/AdaptiveDigital/FixedDigital`), `DownmixMethod` (`AverageChannels/UseFirstChannel`), `RuntimeSettingType`. |

Полезно помнить:
- Для VoIP/низкой задержки чаще выбирают `F32` или `S16` и 48 кГц.
- Выбор `FilterType` зависит от задачи (эквализация, срез НЧ/ВЧ, полосовой).

## Interfaces — обзор и примеры

| Интерфейс | Роль |
|---|---|
| `ISoundDataProvider` | Источник PCM-данных; обычно pull-стиль. Часто используется `RawDataProvider` или собственные реализации. |
| `ISoundDecoder` | Декодирование файлов/потоков в PCM. Реализация: `MiniAudioDecoder`. |
| `ISoundEncoder` | Кодирование PCM в формат. Реализация: `MiniAudioEncoder` (WAV). |
| `ISoundPlayer` | Управление воспроизведением: `Play/Pause/Stop/Seek/Loop/Speed/Volume`. Реализации: `SoundPlayer`, `SurroundPlayer`. |
| `IVisualizationContext` | Методы отрисовки для визуализаторов. |
| `IVisualizer` | Компоненты-визуализаторы аудио. |

Мини-примеры:
```csharp
// ISoundDataProvider из потока
var provider = new RawDataProvider(pcmStream, SampleFormat.F32, 48000, 1);

// ISoundDecoder от файла
using var decoder = engine.CreateDecoder(File.OpenRead("track.flac"), AudioFormat.DvdHq);

// ISoundEncoder в WAV
using var enc = engine.CreateEncoder(File.Create("out.wav"), EncodingFormat.WAV, AudioFormat.DvdHq);
```

## Modifiers — обзор

Некоторые доступные модификаторы (перечень неполный):

- `AlgorithmicReverbModifier`
  - Описание: реверб на основе сети comb/all-pass, поддержка мультканала.
  - Параметры: размер/время реверба, демпфирование, mix и т.п.
  - Пример:
  ```csharp
  var reverb = new AlgorithmicReverbModifier { Mix = 0.2f, Decay = 1.2f };
  // Добавьте на трек или сегмент
  track.Settings.Modifiers.Add(reverb);
  ```

- `BassBoosterModifier`
  - Описание: усиление НЧ (резонансный LPF/подъём)
  - Пример:
  ```csharp
  var bass = new BassBoosterModifier { Amount = 0.5f };
  segment.Settings.Modifiers.Add(bass);
  ```

- `Filter` / `ParametricEqualizer`
  - Описание: фильтры/эквализация; выбирайте `FilterType`.
  - Пример:
  ```csharp
  var eq = new ParametricEqualizer();
  eq.Bands.Add(new EqBand { Type = FilterType.Peaking, Frequency = 1000f, GainDb = 3f, Q = 1.0f });
  track.Settings.Modifiers.Add(eq);
  ```

Замечания:
- Модификаторы можно вешать на уровне сегмента, трека или мастера.
- Порядок модификаторов важен — формирует цепочку эффектов.

## Exceptions — что ловить

| Исключение | Когда бросается |
|---|---|
| `BackendException` | Ошибки бэкенда (инициализация устройства, сбои драйвера, недоступные режимы). |

Практика:
- Оборачивайте инициализацию устройств в try/catch.
- Логируйте коды `Result` из нативных вызовов, если они доступны через API.


# Полная справка (Часть 5)

Раздел посвящён `SoundFlow.Extensions.WebRtc.Apm` — интеграции WebRTC Audio Processing Module (APM) для AEC (эхоподавление), NS (шумоподавление), AGC (автогейн), HPF и др. Приведены примеры real-time и offline, а также практические рекомендации.

## Ключевые типы

- `AudioProcessingModule`
  - Доступ к нативному WebRTC APM, управление жизненным циклом, обработка блоками.

- `ApmConfig`
  - Конфигурация фич: Echo Cancellation, Noise Suppression (уровни), Gain Control (режимы), HighPassFilter, Transient Suppressor, Level Estimator, Voice Detection и др.

- `StreamConfig`
  - Формат потока: `SampleRate`, `Channels`. Обычно моно 48 кГц или 16 кГц.

- `ProcessingConfig`
  - Набор `StreamConfig` для входа/выхода/реверс-потока (reverse stream) AEC.

- `NoiseSuppressor` (Components)
  - Оффлайн/пакетная обработка над `ISoundDataProvider`.

- `WebRtcApmModifier` (Modifiers)
  - Real-time применение фич APM в графе SoundFlow.

## Real-time пайплайн с WebRtcApmModifier

Схема: `Capture → (WebRtcApmModifier) → Player/Encoder` (и подача reverse аудио из playback для AEC).

Пример (набросок):
```csharp
// Форматы
int sampleRate = 48000; // допустим 48 кГц
int inChannels = 1;     // APM рекомендуется моно на входе

// Конфигурация APM
var apmCfg = new ApmConfig
{
    EchoCancellation = true,
    NoiseSuppression = true,
    NoiseSuppressionLevel = NoiseSuppressionLevel.High,
    GainControl = true,
    GainControlMode = GainControlMode.AdaptiveDigital,
    HighPassFilter = true,
    VoiceDetection = false,
};

// Создаём модификатор
var apm = new WebRtcApmModifier(apmCfg)
{
    DownmixMethod = DownmixMethod.AverageChannels // если захват стерео, приведём к моно
};

// Подключение в граф
// Источник (ISoundDataProvider) -> apm -> player/encoder
using var player = new SoundPlayer(engine, AudioFormat.DvdHq, providerWithMic);
player.Modifiers.Add(apm);
playbackDeviceWorker.MasterMixer.AddComponent(player);

// Reverse stream для AEC (подайте часть воспроизводимого звука обратно в APM)
// Это зависит от реализации: либо прямой вызов на apm, либо настройка обработчика reverse
// Псевдокод:
playbackDeviceWorker.OnBeforePlay += (float[] outSamples) =>
{
    apm.PushReverseStream(outSamples, sampleRate, /*channels*/ 2);
};

player.Play();
```

Замечания:
- AEC требует reverse-поток (звук, который уходит в наушники/колонки), чтобы вычитать его из микрофона.
- Рекомендуется моно-вход в APM (downmix входного сигнала); выход может оставаться стерео.
- Размер блоков: 10 мс или 20 мс (для 48 кГц → 480 или 960 сэмплов на канал) — используйте аккуратную буферизацию.

## Оффлайн NoiseSuppressor

Пример применения `NoiseSuppressor` к провайдеру (бэтч-обработка):
```csharp
// Имеем входной провайдер провайдера аудио (например, декодер файла)
using var src = engine.CreateDecoder(File.OpenRead("noisy.wav"), AudioFormat.DvdHq);

var nsCfg = new ApmConfig
{
    NoiseSuppression = true,
    NoiseSuppressionLevel = NoiseSuppressionLevel.VeryHigh
};

var suppressor = new NoiseSuppressor(nsCfg, src);

// Прогоняем и сохраняем результат в WAV
using var outStream = File.Create("denoised.wav");
using var wav = engine.CreateEncoder(outStream, EncodingFormat.WAV, AudioFormat.DvdHq);

Span<float> buf = stackalloc float[4096];
int read;
while ((read = suppressor.Read(buf)) > 0)
{
    wav.Encode(buf[..read]);
}
```

## Best practices (AEC/NS/AGC)

- Выбор частоты/каналов:
  - Вход APM → моно 48 кГц (или 16/32 кГц); выход допускает микширование обратно в стерео.
  - Для VoIP де-факто стандарт — 48 кГц, 20 мс кадры.

- Буферизация:
  - Собирайте ровно 10/20 мс фреймы на канал перед вызовами обработки.
  - Избегайте лишних аллокаций в горячем пути (используйте `ArrayPool<T>`/`Span`).

- AEC (эхоподавление):
  - Необходим reverse-поток (звук, отправляемый в устройство вывода).
  - Для loopback на Windows можно использовать `InitializeLoopbackDevice(...)` и синхронизировать с основной линией.

- NS (шумоподавление):
  - Выбирайте уровень `NoiseSuppressionLevel` исходя из баланса артефактов/чистоты речи.

- AGC (автогейн):
  - `AdaptiveDigital` — безопасный старт для десктопных микрофонов.
  - Проверяйте клиппинг и уровень шума после обработки.

- Тестирование:
  - Проверяйте реальную задержку end-to-end и устойчивость при разных размерах периодов устройства.

## Финальная зачистка Backends

- Убедитесь, что использование `DeviceConfig` согласовано с платформой (WASAPI/CoreAudio/ALSA/Pulse/OpenSL/AAudio).
- Для минимальной задержки на Windows можно опробовать `ShareMode.Exclusive` и подбирать `PeriodSizeInFrames/Periods`.
- Обновляйте список устройств `UpdateDevicesInfo()` при горячем переключении и используйте `SwitchDevice(...)` для seamless-перехода.

---

# Полная справка (Часть 6)

Дальнейший перенос из исходной документации после раздела WebRTC APM. Ниже — детализированные перечисления (Enums) с полными значениями и подробная ссылка по `Editing` для классов `Composition` и `Track`.

## Enums — детально с перечислением значений

### EncodingFormat
- Unknown = 0 — неизвестный формат кодирования.
- Wav — Waveform Audio File Format.
- Flac — Free Lossless Audio Codec.
- Mp3 — MPEG-1 Audio Layer III.
- Vorbis — Ogg Vorbis.

### PlaybackState
- Stopped — воспроизведение остановлено.
- Playing — воспроизведение идет.
- Paused — воспроизведение на паузе.

### Result (сокращённо; содержит множество кодов ошибок)
- Success = 0 — операция успешна.
- Error = -1 — общая ошибка.
- CrcMismatch = -100 — несовпадение контрольной суммы.
- FormatNotSupported = -200 — формат не поддерживается.
- DeviceNotInitialized = -300 — устройство не инициализировано.
- FailedToInitBackend = -400 — не удалось инициализировать бэкенд.
- ... (другие коды ошибок backend/device-уровня по исходной документации)

### SampleFormat
- Unknown = 0 — неизвестный формат.
- U8 = 1 — 8-бит беззнаковый целый.
- S16 = 2 — 16-бит знаковый целый.
- S24 = 3 — 24-бит знаковый целый (упакован в 3 байта).
- S32 = 4 — 32-бит знаковый целый.
- F32 = 5 — 32-бит с плавающей точкой.

### FilterType
- Peaking — пиковый фильтр.
- LowShelf — полка НЧ.
- HighShelf — полка ВЧ.
- BandPass — полосовой фильтр.
- Notch — режекторный.
- LowPass — НЧ-фильтр.
- HighPass — ВЧ-фильтр.

### EnvelopeGenerator.EnvelopeState
- Idle — огибающая неактивна.
- Attack — стадия атаки.
- Decay — стадия спада.
- Sustain — стадия сустейна.
- Release — стадия релиза.

### EnvelopeGenerator.TriggerMode
- NoteOn — прямая активация с переходом к sustain (без decay по описанию исходника).
- Gate — обычная прогрессия; релиз при отпускании триггера.
- Trigger — всегда проходит все стадии до релиза.

### LowFrequencyOscillator.WaveformType
- Sine — синус.
- Square — меандр.
- Triangle — треугольник.
- Sawtooth — пила.
- ReverseSawtooth — обратная пила.
- Random — случайные значения.
- SampleAndHold — выборка-и-удержание случайных значений.

### LowFrequencyOscillator.TriggerMode
- FreeRunning — свободный непрерывный ход.
- NoteTrigger — запуск по триггеру ноты.

### Oscillator.WaveformType
- Sine — синус.
- Square — меандр.
- Sawtooth — пила.
- Triangle — треугольник.
- Noise — белый шум.
- Pulse — пульс.

### SurroundPlayer.SpeakerConfiguration
- Stereo — 2 канала.
- Quad — 4 канала.
- Surround51 — 5.1 (6 каналов).
- Surround71 — 7.1 (8 каналов).
- Custom — произвольная пользовательская конфигурация.

### SurroundPlayer.PanningMethod
- Linear — линейная панорама.
- EqualPower — равная мощность.
- Vbap — Vector Base Amplitude Panning.

---

## Editing — справочник по классам (часть 1)

Ниже перечислены члены классов согласно исходной документации. Формулировки адаптированы для Markdown.

### Composition (ISoundDataProvider, IDisposable)

Конструктор:
- `Composition(string name = "Composition", int? targetChannels = null)`

Свойства:
- `string Name { get; set; }`
- `List<SoundModifier> Modifiers { get; init; }`
- `List<AudioAnalyzer> Analyzers { get; init; }`
- `List<Track> Tracks { get; }`
- `float MasterVolume { get; set; }`
- `bool IsDirty { get; }`
- `int SampleRate { get; set; }` — целевая частота рендера
- `int TargetChannels { get; set; }`

События:
- `event EventHandler<EventArgs>? EndOfStreamReached`
- `event EventHandler<PositionChangedEventArgs>? PositionChanged`

ISoundDataProvider:
- `int Position { get; }`
- `int Length { get; }`
- `bool CanSeek { get; }`
- `int ReadBytes(Span<float> buffer)`
- `void Seek(int sampleOffset)`

Методы управления структурой:
- `void AddTrack(Track track)`
- `bool RemoveTrack(Track track)`
- `TimeSpan CalculateTotalDuration()`

Рендер:
- `float[] Render(TimeSpan startTime, TimeSpan duration)`
- `int Render(TimeSpan startTime, TimeSpan duration, Span<float> outputBuffer)`

Изменения/грязное состояние:
- `void MarkDirty()`
- `internal void ClearDirtyFlag()`

Модификаторы/анализаторы на уровне композиции:
- `void AddModifier(SoundModifier modifier)`
- `bool RemoveModifier(SoundModifier modifier)`
- `void ReorderModifier(SoundModifier modifier, int newIndex)`
- `void AddAnalyzer(AudioAnalyzer analyzer)`
- `bool RemoveAnalyzer(AudioAnalyzer analyzer)`

Жизненный цикл:
- `void Dispose()`

Примечание: В исходной документации упоминаются операции редактирования трека/сегментов (Replace/Remove/Silence/Insert) — реализуются через соответствующие методы на `Track`/`AudioSegment`.

### Track

Конструктор:
- `Track(string name = "Track", TrackSettings? settings = null)`

Свойства:
- `string Name { get; set; }`
- `List<AudioSegment> Segments { get; }`
- `TrackSettings Settings { get; set; }`
- `internal Composition? ParentComposition { get; set; }`

Изменения/грязное состояние:
- `void MarkDirty()`

Операции с сегментами:
- `void AddSegment(AudioSegment segment)`
- `bool RemoveSegment(AudioSegment segment, bool shiftSubsequent = false)`
- `void InsertSegmentAt(AudioSegment segmentToInsert, TimeSpan insertionTime, bool shiftSubsequent = true)`

Расчеты/рендер:
- `TimeSpan CalculateDuration()`
- `int Render(TimeSpan overallStartTime, TimeSpan durationToRender, Span<float> outputBuffer, int targetSampleRate, int targetChannels)`



## AudioSegment (IDisposable)

Конструктор:
- `AudioSegment(ISoundDataProvider sourceDataProvider, TimeSpan sourceStartTime, TimeSpan sourceDuration, TimeSpan timelineStartTime, string name = "Segment", AudioSegmentSettings? settings = null, bool ownsDataProvider = false)`

Свойства:
- `string Name { get; set; }`
- `ISoundDataProvider SourceDataProvider { get; private set; }`
- `TimeSpan SourceStartTime { get; set; }`
- `TimeSpan SourceDuration { get; set; }`
- `TimeSpan TimelineStartTime { get; set; }`
- `AudioSegmentSettings Settings { get; set; }`
- `internal Track? ParentTrack { get; set; }`
- `TimeSpan StretchedSourceDuration { get; }`
- `TimeSpan EffectiveDurationOnTimeline { get; }`
- `TimeSpan TimelineEndTime { get; }`

Методы:
- `TimeSpan GetTotalLoopedDurationOnTimeline()`
- `AudioSegment Clone(TimeSpan? newTimelineStartTime = null)`
- `internal void ReplaceSource(ISoundDataProvider newSource, TimeSpan newSourceStartTime, TimeSpan newSourceDuration)`
- `int ReadProcessedSamples(TimeSpan segmentTimelineOffset, TimeSpan durationToRead, Span<float> outputBuffer, int outputBufferOffset, int targetSampleRate, int targetChannels)`
- `internal void FullResetState()`
- `void MarkDirty()`
- `void Dispose()`

Примечание: `ReadProcessedSamples` учитывает настройки сегмента (фейды, реверс, луп, тайм-стретч, модификаторы/анализаторы), и пишет обработанные PCM-данные в `outputBuffer` с учетом формата назначения.

## AudioSegmentSettings (класс настроек сегмента)

Свойства:
- `List<SoundModifier> Modifiers { get; init; }`
- `List<AudioAnalyzer> Analyzers { get; init; }`
- `float Volume { get; set; }`
- `float Pan { get; set; }`
- `TimeSpan FadeInDuration { get; set; }`
- `FadeCurveType FadeInCurve { get; set; }`
- `TimeSpan FadeOutDuration { get; set; }`
- `FadeCurveType FadeOutCurve { get; set; }`
- `bool IsReversed { get; set; }`
- `LoopSettings Loop { get; set; }`
- `float SpeedFactor { get; set; }`
- `float TimeStretchFactor { get; set; }` — переопределяется `TargetStretchDuration`, если задана
- `TimeSpan? TargetStretchDuration { get; set; }`
- `bool IsEnabled { get; set; }`

Методы:
- `AudioSegmentSettings Clone()`
- `void AddModifier(SoundModifier modifier)`
- `bool RemoveModifier(SoundModifier modifier)`
- `void ReorderModifier(SoundModifier modifier, int newIndex)`
- `void AddAnalyzer(AudioAnalyzer analyzer)`
- `bool RemoveAnalyzer(AudioAnalyzer analyzer)`

Замечания:
- `SpeedFactor` изменяет скорость воспроизведения (с изменением питча), а `TimeStretchFactor`/`TargetStretchDuration` — длительность без изменения питча (WSOLA).

## TrackSettings (класс настроек дорожки)

Свойства:
- `List<SoundModifier> Modifiers { get; init; }`
- `List<AudioAnalyzer> Analyzers { get; init; }`
- `float Volume { get; set; }`
- `float Pan { get; set; }`
- `bool IsMuted { get; set; }`
- `bool IsSoloed { get; set; }`
- `bool IsEnabled { get; set; }`

Методы:
- `TrackSettings Clone()`
- `void AddModifier(SoundModifier modifier)`
- `bool RemoveModifier(SoundModifier modifier)`
- `void ReorderModifier(SoundModifier modifier, int newIndex)`
- `void AddAnalyzer(AudioAnalyzer analyzer)`
- `bool RemoveAnalyzer(AudioAnalyzer analyzer)`

## LoopSettings (record struct)

Свойства/конструктор/статические члены:
- `int Repetitions { get; }`
- `TimeSpan? TargetDuration { get; }`
- `LoopSettings(int repetitions = 0, TimeSpan? targetDuration = null)`
- `static LoopSettings PlayOnce { get; }`

Описание: либо повторить `Repetitions` раз, либо (если указано) растянуть/обрезать до `TargetDuration` при лупинге.

## FadeCurveType (enum)

Значения:
- `Linear`
- `Logarithmic`
- `SCurve`

Описание: тип кривой перехода для `FadeIn`/`FadeOut`.

## Editing.Persistence — DTO (кратко)

- `CompositionProjectManager` (static)
  - Методы: `SaveProjectAsync`, `LoadProjectAsync`, `RelinkMissingMediaAsync`.
  - Назначение: сохранение/загрузка `.sfproj`, перелинковка медиа (консолидация/поиск).

- `ProjectData`
  - Корневая сущность проекта: метаданные, список треков (`ProjectTrack`), глобальные эффекты/анализаторы, формат.

- `ProjectTrack`
  - Отражение сущности `Track`: имя, `ProjectTrackSettings`, список `ProjectSegment`.

- `ProjectSegment`
  - Отражение `AudioSegment`: ссылка на `ProjectSourceReference`, тайминги (источник/таймлайн), имя, `ProjectAudioSegmentSettings`.

- `ProjectAudioSegmentSettings`
  - Сериализация `AudioSegmentSettings`: volume/pan/fades/loop/reverse/speed/time-stretch, списки модификаторов/анализаторов (как `ProjectEffectData`).

- `ProjectTrackSettings`
  - Сериализация `TrackSettings`: volume/pan/mute/solo/enabled, модификаторы/анализаторы.

- `ProjectSourceReference`
  - Как хранится источник сегмента: относительный/абсолютный путь к файлу, встроенные данные (для малых клипов), сведения о консолидации.

- `ProjectEffectData`
  - Сериализация эффекта/анализатора: тип/класс + набор параметров (ключ-значение) для восстановления конфигурации.

Примечание: при загрузке пользовательских эффектов/анализаторов необходимо, чтобы требуемые сборки были доступны в путях загрузки.

---

## Visualization — обзор

Неймспейс: `SoundFlow.Visualization`

Ключевые интерфейсы:
- `IVisualizationContext`
  - Абстракция контекста рисования/вывода визуальных элементов; предоставляет методы отрисовки примитивов/линий/текста (в зависимости от реализации).

- `IVisualizer`
  - Контракт визуализатора аудио. Обычно принимает поток сэмплов (через подключение к провайдеру/компоненту), агрегирует анализ (RMS/FFT/пики) и рисует через `IVisualizationContext`.

Типичный паттерн использования:
1) Источник аудио (`ISoundDataProvider`) → Плеер/Микшер (для звука).
2) Параллельно подключить визуализатор к тем же данным (или к анализатору) → `IVisualizer.Draw(context)` внутри цикла обновления/UI.

Простейший пример — RMS/Peak визуализатор (псевдокод):
```csharp
public sealed class RmsVisualizer : IVisualizer
{
    private float _rms;
    private float _peak;

    public void OnSamples(float[] samples)
    {
        float sumSq = 0f; float peak = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float x = MathF.Abs(samples[i]);
            sumSq += x * x;
            if (x > peak) peak = x;
        }
        _rms = MathF.Sqrt(sumSq / Math.Max(1, samples.Length));
        _peak = peak;
    }

    public void Draw(IVisualizationContext ctx)
    {
        // Нарисовать два столбца: RMS и PEAK (апи ctx зависит от конкретной реализации)
        // ctx.DrawBar(... _rms ...); ctx.DrawBar(... _peak ...);
    }
}
```

Интеграция с плеером (набросок):
```csharp
var visualizer = new RmsVisualizer();

// Подписка на те же сэмплы, что идут в звук (например, в обработчике или через Analyzer):
captureDeviceWorker.OnAudioProcessed += (float[] samples, Capability cap) =>
{
    visualizer.OnSamples(samples);
};

// В UI-цикле/рендере:
visualizer.Draw(uiContext);
```

Замечания:
- Для частотного спектра используйте FFT-анализ в `AudioAnalyzer` и передавайте результаты в `IVisualizer`.
- Разносите аудио-поток и визуализацию по потокам/буферам, чтобы не блокировать аудио-callback.
# IPTV Player

Reproductor IPTV para Windows con soporte para listas M3U, streams en vivo y contenido on-demand.

Construido con C# / WPF / LibVLCSharp sobre .NET 8.

## Requisitos

- Windows 10 o superior
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (para compilar)
- .NET 8 Runtime (para ejecutar el binario publicado)

## Ejecutar en modo desarrollo

```bash
dotnet run --project IPTVPlayer
```

O desde la carpeta del proyecto:

```bash
cd IPTVPlayer
dotnet run
```

## Compilar para distribucion

```bash
dotnet publish IPTVPlayer -c Release -r win-x64 --self-contained -o publish
```

Esto genera un ejecutable autocontenido en la carpeta `publish/` que no requiere tener .NET instalado.

## Uso

1. Abre la aplicacion.
2. Pega una URL de playlist M3U en la barra superior y haz clic en **Cargar** (o presiona Enter).
3. Los canales aparecen agrupados en el panel izquierdo.
4. Haz doble clic en un canal para reproducirlo.

## Funcionalidades

### Reproduccion

- Soporte para streams en vivo (HLS, DASH, RTMP, RTSP) y contenido on-demand.
- Controles: Play/Pause, Stop, barra de progreso (on-demand), volumen con mute.
- Deteccion automatica de stream en vivo vs on-demand (indicador "EN VIVO" en la barra de estado).

### Picture-in-Picture (PiP)

- Boton PiP en la barra de controles para activar modo ventana flotante.
- Ventana siempre visible (Topmost), sin bordes, arrastrable y redimensionable.
- Doble clic en la ventana PiP para cerrarla y devolver el video a la ventana principal.

### Pistas de audio

- Selector dropdown en la barra de controles.
- Muestra todas las pistas de audio disponibles en el stream actual.
- Cambio de pista en tiempo real sin interrumpir la reproduccion.

### Subtitulos

- Selector dropdown en la barra de controles.
- Incluye opcion para desactivar subtitulos.
- Cambio de pista en tiempo real.

### Navegacion

- Panel lateral con canales agrupados por categoria (TreeView expandible).
- Busqueda en tiempo real por nombre de canal.
- Panel lateral redimensionable con GridSplitter.

### Pantalla completa

- Boton en controles o tecla F11.
- Oculta barra superior y panel lateral para experiencia inmersiva.
- Escape para salir.

## Atajos de teclado

| Tecla   | Accion              |
|---------|---------------------|
| F11     | Pantalla completa   |
| Escape  | Salir fullscreen    |
| Espacio | Play / Pause        |
| Enter   | Cargar playlist (en campo URL) |

## Stack tecnologico

| Componente       | Tecnologia                        |
|-----------------|-----------------------------------|
| Framework       | .NET 8 (LTS)                      |
| UI              | WPF (Windows Presentation Foundation) |
| Motor de video  | LibVLC via LibVLCSharp             |
| Patron MVVM     | CommunityToolkit.Mvvm 8.4         |

## Estructura del proyecto

```
IPTVPlayer/
  IPTVPlayer.csproj
  App.xaml / App.xaml.cs           -- Entrada, tema oscuro global
  MainWindow.xaml / .cs            -- Ventana principal
  Models/
    Channel.cs                     -- Modelo de canal
    ChannelGroup.cs                -- Agrupacion de canales
    TrackInfo.cs                   -- Pista de audio/subtitulo
  Services/
    M3UParserService.cs            -- Parser de playlists M3U
    PlaylistService.cs             -- Carga desde URL o archivo
  ViewModels/
    MainViewModel.cs               -- Logica de playlist y navegacion
    PlayerViewModel.cs             -- Logica de reproduccion y tracks
  Views/
    PiPWindow.xaml / .cs           -- Ventana Picture-in-Picture
  Converters/
    BoolToVisibilityConverter.cs
    InverseBoolToVisibilityConverter.cs
    TimeSpanToStringConverter.cs
```

## Licencia

Uso privado.

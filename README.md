# IPTV Player | Chucao

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
3. Si ya ingresaste una URL antes, el dropdown muestra tus URLs recientes.
4. Los canales aparecen agrupados por categoria en el panel izquierdo.
5. Haz doble clic en un canal para reproducirlo.
6. Al reiniciar, la app recuerda tu playlist y la carga automaticamente.

## Funcionalidades

### Reproduccion

- Soporte para streams en vivo (HLS, DASH, RTMP, RTSP) y contenido on-demand.
- Controles inmersivos (flotantes sobre el video): Play/Pause, Stop, barra de progreso (on-demand), volumen con mute.
- Deteccion automatica de stream en vivo vs on-demand (indicador "EN VIVO" en la barra de estado).
- Selector de pistas de audio y subtitulos con cambio en tiempo real.
- **Reconexion automatica**: Si un stream se desconecta (error de red, fallo del servidor), el player reintenta automaticamente con backoff exponencial (hasta 10 intentos). Overlay visual muestra el progreso y permite cancelar.

### Persistencia de configuracion

- La URL de playlist y las ultimas 5 URLs usadas se guardan automaticamente.
- Volumen, mute, tamano y posicion de ventana se restauran al reabrir.
- Al iniciar, la ultima playlist se carga automaticamente.
- Configuracion almacenada en `%APPDATA%/IPTVPlayer/settings.json`.

### Picture-in-Picture (PiP)

- Boton PiP en la barra de controles para activar modo ventana flotante.
- Ventana borderless (sin decoracion), siempre visible sobre otras ventanas (Topmost).
- Arrastrable desde cualquier punto del video.
- Redimensionable: grip invisible en la esquina inferior derecha (el cursor cambia a SizeNWSE).
- Al pasar el mouse sobre la ventana PiP aparecen:
  - Boton de cierre (esquina superior derecha).
  - Control de volumen con mute (esquina inferior izquierda).
- Scroll del mouse sobre el PiP ajusta el volumen.
- Doble clic en el PiP para cerrarlo y devolver la reproduccion a la ventana principal.
- Al cerrar, retoma la posicion exacta (VOD) o reconecta al vivo (live streams).

### Pantalla completa

- Boton en controles o tecla F11 para entrar. Escape o F11 para salir.
- Oculta toda la interfaz para experiencia inmersiva.
- Overlay de controles que aparece al mover el mouse y se oculta automaticamente tras 3 segundos de inactividad.
- Cursor se oculta junto con los controles.
- Doble clic en el video para salir del fullscreen.

### Navegacion

- Panel lateral con canales agrupados por categoria (TreeView expandible) resuelto con ScrollBars consistentes.
- Tabs de categorias para filtrado rapido.
- Busqueda en tiempo real por nombre de canal con placeholders y estados responsivos.
- Vista completa interactiva (grilla in-place flotando temporalmente sobre el video) para navegar listas largas (Favoritos, Historial).
- Panel lateral redimensionable con GridSplitter.
- Logos de canales cargados asincrona desde la URL `tvg-logo` de la playlist.

## Atajos de teclado

| Tecla   | Accion                              |
|---------|-------------------------------------|
| F11     | Entrar / salir pantalla completa    |
| Escape  | Salir de pantalla completa          |
| Espacio | Play / Pause                        |
| Enter   | Cargar playlist (en campo URL)      |

## Stack tecnologico

| Componente       | Tecnologia                          |
|-----------------|-------------------------------------|
| Framework       | .NET 8 (LTS)                        |
| UI              | WPF (Windows Presentation Foundation) |
| Motor de video  | LibVLC 3 via LibVLCSharp            |
| Patron MVVM     | CommunityToolkit.Mvvm 8.4           |
| Persistencia    | System.Text.Json (JSON en AppData)  |

## Estructura del proyecto

```
IPTVPlayer/
  IPTVPlayer.csproj
  App.xaml / App.xaml.cs           -- Entrada, tema gris+naranja, estilos, icono Chucao
  MainWindow.xaml / .cs            -- Ventana principal + logica fullscreen + PiP + settings
  Assets/
    chucao.svg                     -- Icono del ave Chucao (diseño vectorial)
  Models/
    AppSettings.cs                 -- Modelo de configuracion persistente
    Channel.cs                     -- Modelo de canal
    ChannelGroup.cs                -- Agrupacion de canales por grupo
    ContentCategory.cs             -- Categoria (Live, VOD, Series, etc.)
    TrackInfo.cs                   -- Pista de audio o subtitulo
  Services/
    M3UParserService.cs            -- Parser de playlists M3U/M3U8
    PlaylistService.cs             -- Carga desde URL o archivo local
    SettingsService.cs             -- Persistencia de configuracion (JSON en AppData)
  ViewModels/
    MainViewModel.cs               -- Logica de playlist, filtrado, navegacion e historial de URLs
    PlayerViewModel.cs             -- Logica de reproduccion, tracks, reconexion automatica
  Views/
    PiPWindow.xaml / .cs           -- Ventana Picture-in-Picture borderless
  Converters/
    BoolToVisibilityConverter.cs
    InverseBoolToVisibilityConverter.cs
    TimeSpanToStringConverter.cs
    ImageLoadConverter.cs          -- Carga asincrona de logos de canales
```

## Licencia

Uso privado.

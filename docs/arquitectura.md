# Arquitectura y decisiones técnicas

## Capas

| Proyecto | Target | Qué contiene |
|---|---|---|
| `src/IPTVPlayer.Core` | `net10.0` | Modelos y servicios sin WPF ni LibVLC: listas M3U, cliente Xtream, EPG, series, historial, favoritos y configuración. |
| `src/IPTVPlayer` | `net10.0-windows` | App WPF con MVVM (CommunityToolkit.Mvvm): ViewModels, vistas, estilos y la integración con LibVLC. |
| `tests/IPTVPlayer.Tests` | `net10.0` | Tests de xUnit v3 sobre Core. No cargan la interfaz ni LibVLC. |

Como Core no depende de la interfaz, toda la lógica que se puede probar sin abrir una ventana vive ahí y tiene tests.

### ViewModels

- **`MainViewModel`** coordina las pestañas, la búsqueda, los filtros, la navegación de series, los favoritos y el historial. Delega la lógica pura a `SeriesCatalog`, `WatchHistory` y `Favorites`.
- **`PlayerViewModel`** encapsula LibVLC: reproducción, pistas, reconexión y transición a PiP.
- **`EpgViewModel`** carga la guía y expone los programas del canal en reproducción, con el actual y el siguiente.

### Flujo de carga

```
Fuente (PlaylistSource)
  ├─ M3U    → PlaylistService → M3UParserService → clasificación por grupo/URL
  └─ Xtream → XtreamApiService (player_api.php)
        ↓
  List<ContentCategory> (TV en vivo, Películas, Series)
        ↓
  filtro de contenido adulto → listas de cada pestaña → SeriesCatalog (series)
        ↓
  EPG: url-tvg / x-tvg-url de la M3U o xmltv.php del panel → EpgService
```

## Decisiones técnicas

### Airspace de WPF con LibVLC 3

`VideoView` dibuja el video en una ventana nativa (HWND) y WPF no puede pintar encima. Por eso todos los controles superpuestos (barra flotante, pantalla completa, reconexión, «Siguiente capítulo», lista completa) van dentro de `VideoView.Content`. LibVLCSharp los muestra en una ventana transparente propia (`ForegroundWindow`) sobre el video. No se usan ventanas WPF separadas como overlay, porque generan problemas de orden z y de foco.

### PiP con su propia instancia de LibVLC

En LibVLC 3, un `MediaPlayer` queda ligado al HWND donde empezó a reproducir y no se puede mover a otra ventana. El PiP crea su propia instancia de `LibVLC` y `MediaPlayer`:

- al abrirlo, la ventana principal pausa y el PiP retoma desde la posición actual (`:start-time`);
- al cerrarlo, la ventana principal vuelve a conectarse: el contenido bajo demanda sigue desde donde quedó el PiP y el en vivo se reconecta directamente, porque `Time` y `Position` no sirven en streams en vivo.

### Threading de LibVLCSharp

Llamar métodos de LibVLC desde los handlers de eventos de LibVLC produce deadlocks. Los handlers solo despachan al hilo de la UI (`Dispatcher.BeginInvoke`), y las operaciones que bloquean (`Stop`, reconexiones) corren en `Task.Run`.

### Reconexión con espera exponencial

`EncounteredError`, o `EndReached` en un stream en vivo, sin un stop pedido por el usuario, inicia la reconexión: espera 1 s, 2 s, 4 s… hasta 30 s, con un máximo configurable de intentos (10 por defecto). El contenido bajo demanda retoma desde la última posición conocida. Cambiar de canal o detener cancela la reconexión (`CancellationTokenSource`). Además, al reanudar después de más de 10 minutos en pausa se abre una conexión nueva, porque muchos servidores cortan la sesión inactiva.

### Por qué todavía LibVLC 3

LibVLC 4 resolvería el airspace (render vía Direct3D/OpenGL dentro de WPF) y permitiría un PiP sin reconectar. Cuando se evaluó (febrero de 2026), LibVLC 4 no tenía versión estable y LibVLCSharp 4 solo publicaba paquetes alpha. Conviene revisarlo de nuevo cuando haya versiones estables.

### Series a partir del nombre

Las listas M3U no traen metadatos de series. `EpisodeParser` reconoce `S01E01`, `T01E01` y `1x01` en el nombre y separa el nombre de la serie, y `SeriesCatalog` agrupa por serie y temporada y resuelve el siguiente episodio. El patrón `1x01` no acepta dígitos pegados, para no leer resoluciones como `1920x1080`.

### Xtream Codes: episodios bajo demanda

Un panel Xtream puede tener miles de series, y pedir todos los episodios al cargar sería lento. `XtreamApiService` arma el catálogo con una entrada por serie (`Channel.IsPendingSeries`). Al abrir una serie se piden sus episodios con `get_series_info`, se guardan en caché y se nombran «Serie S01E02 — título». Así, la misma navegación de series, el siguiente episodio automático, los favoritos y «Continuar viendo» funcionan sin código especial para Xtream.

Los paneles mandan números como texto o como número, y `episodes` como objeto o como arreglo; el parser acepta ambas formas.

### Contraseñas

La contraseña de una cuenta Xtream se guarda cifrada con DPAPI (`SecretProtector`): solo el mismo usuario de Windows, en el mismo equipo, puede descifrarla. Limitación conocida: las URLs de reproducción de Xtream llevan usuario y contraseña en la ruta, y esas URLs quedan en el historial y en los favoritos de `settings.json` (ver [roadmap](roadmap.md)).

### Configuración

`settings.json` vive en `%APPDATA%\IPTVPlayer`, con el mismo nombre de siempre para no perder la configuración existente. Los campos nuevos tienen valores por defecto, así que un archivo de una versión anterior se lee sin errores. Las URLs que guardaban las versiones anteriores se convierten en fuentes M3U al leerlo.

### Fechas y tests

`EpgService` y `WatchHistory` reciben un `TimeProvider`. Los tests fijan «ahora» y comparan las horas convertidas a hora local, así que dan el mismo resultado en cualquier zona horaria (CI corre en UTC).

### Publicación

El paquete `VideoLAN.LibVLC.Windows` copia LibVLC para x86, x64 y arm64. Al publicar para `win-x64` se desactivan las otras dos, y la carpeta publicada baja de 444 MB a 241 MB. El ejecutable es autocontenido: incluye el runtime de .NET.

## Qué cubren los tests y qué se prueba a mano

Los tests cubren el parser M3U, la clasificación de la lista, la detección de episodios, el parser XMLTV (plano y `.gz`), el catálogo de series, el historial, los favoritos, la configuración (incluida la migración y el cifrado) y el cliente Xtream con respuestas simuladas.

La interfaz y la reproducción se prueban a mano: TV en vivo, VOD, PiP, pantalla completa, navegación de series y siguiente episodio, y una cuenta Xtream real.

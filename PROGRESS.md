# IPTV Player | Chucao - Progreso y Roadmap

## Estado actual: Layout multi-tab con UI profesional — en desarrollo activo

La aplicacion compila y ejecuta correctamente. Todas las funcionalidades core estan implementadas y verificadas en pruebas reales.

---

## Completado

### Base
- [x] Proyecto .NET 8 WPF configurado con dependencias NuGet (LibVLCSharp, CommunityToolkit.Mvvm)
- [x] Modelos de datos: Channel, ChannelGroup, TrackInfo
- [x] Parser M3U con soporte para atributos EXTINF (tvg-name, tvg-logo, group-title, tvg-id)
- [x] Servicio de carga de playlists desde URL (HTTP/HTTPS) y archivo local
- [x] Agrupacion automatica de canales por categoria
- [x] Integracion LibVLCSharp: inicializacion, reproduccion, eventos
- [x] Controles de reproduccion: Play/Pause, Stop, volumen, mute
- [x] Barra de progreso/seek para contenido on-demand
- [x] Deteccion automatica live vs on-demand (indicador "EN VIVO")
- [x] Selector de pistas de audio multiple
- [x] Selector de subtitulos multiple
- [x] Busqueda/filtro de canales en tiempo real
- [x] Tema oscuro con estilos personalizados (sliders, botones, TreeView)
- [x] Iconografia con Segoe MDL2 Assets
- [x] Barra de estado con indicador EN VIVO
- [x] Manejo basico de errores (carga fallida, stream con error)
- [x] Atajos de teclado (F11, Escape, Space, Enter)
- [x] Panel lateral redimensionable con GridSplitter
- [x] Logos de canales cargados de forma asincrona desde tvg-logo

### Pantalla completa
- [x] Entrada/salida via boton o F11/Escape
- [x] Oculta toda la chrome (sidebar, topbar, controles)
- [x] Overlay de controles dentro del VideoView.Content (sin ventana separada — resuelve airspace problem y conflicto de z-order)
- [x] Auto-hide: controles aparecen al mover el mouse, se ocultan tras 3 segundos de inactividad con fade-out
- [x] Cursor se oculta junto con los controles
- [x] Doble clic en el video para salir del fullscreen
- [x] Atajos Escape/F11/Space funcionales desde la ventana principal

### Picture-in-Picture
- [x] Ventana borderless (sin decoracion DWM), Topmost, siempre visible
- [x] Arrastrable desde cualquier punto del video
- [x] Redimensionable via grip invisible en esquina inferior derecha (cursor SizeNWSE)
- [x] Controles de volumen (slider + mute) aparecen/desaparecen con hover (fade in/out)
- [x] Boton de cierre aparece/desaparece con hover (esquina superior derecha)
- [x] Doble clic para cerrar y volver a ventana principal
- [x] Scroll de mouse ajusta volumen
- [x] PiP usa instancia propia de LibVLC/MediaPlayer (requerido por LibVLC 3 — el MediaPlayer esta vinculado al HWND al inicio de reproduccion)
- [x] Al abrir PiP: captura posicion actual y retoma desde ahi
- [x] Al cerrar PiP: retorna a ventana principal con conexion fresca al stream
  - VOD: retoma desde la posicion en que estaba el PiP (via `:start-time`)
  - Live: reconecta al stream en vivo directamente (posicion irrelevante en live)

### Reconexion automatica al stream
- [x] Deteccion de desconexion via eventos `EncounteredError` y `EndReached` de LibVLC
- [x] Reintentos automaticos con backoff exponencial (1s, 2s, 4s, 8s... hasta 30s max)
- [x] Maximo 10 intentos de reconexion antes de reportar fallo definitivo
- [x] Distincion entre stop manual del usuario vs desconexion involuntaria
- [x] Reconexion en live streams cuando `EndReached` indica desconexion
- [x] Reconexion en VOD con reanudacion desde la ultima posicion conocida (`:start-time`)
- [x] Cancelacion de reconexion al cambiar de canal o detener manualmente
- [x] Overlay visual con progreso de reconexion (icono + barra + texto + boton cancelar)
- [x] Llamadas a LibVLC desde `Task.Run` para respetar la restriccion de threading de LibVLCSharp

### Persistencia de configuracion
- [x] Modelo `AppSettings` con todas las propiedades persistibles
- [x] `SettingsService` con load/save en `%APPDATA%/IPTVPlayer/settings.json` via System.Text.Json
- [x] Guarda: ultima URL, historial de URLs recientes (5), volumen, mute, dimensiones/posicion de ventana, ancho del sidebar
- [x] Restaura al iniciar: posicion/tamano de ventana, volumen, URL, carga automatica de playlist
- [x] Auto-load de la ultima playlist al iniciar la aplicacion

### Mejora UX del input de URL
- [x] ComboBox editable reemplaza el TextBox de URL
- [x] Dropdown muestra las ultimas 5 URLs usadas
- [x] Al cargar una playlist, la URL se agrega al historial automaticamente (duplicados eliminados)
- [x] Enter en el ComboBox cierra el dropdown y carga la playlist
- [x] Template custom oscuro con borde accent al hover/focus

### Rediseño visual
- [x] Paleta de colores actualizada: base gris neutro + acentos naranja (inspirado en el ave Chucao)
- [x] Icono de la app: Chucao como DrawingImage vectorial en App.xaml + SVG de referencia
- [x] Colores: BgBrush #1A1A1E, SurfaceBrush #262629, AccentBrush #E8752A, AccentHoverBrush #F0924A

### UI profesional y layout multi-tab
- [x] Barra de titulo oscura via DWM API (`DWMWA_USE_IMMERSIVE_DARK_MODE`)
- [x] ScrollBars oscuros custom (thin, rounded, accent on drag) aplicados globalmente
- [x] ListBox y ListBoxItem con estilos dark consistentes
- [x] Header bar: reloj/fecha, tabs de contenido (LIVE TV | MOVIES | SERIES), selector M3U, icono configuracion
- [x] Sistema de tabs con `ContentTab` enum y switching via RadioButtons estilizados
- [x] Tab LIVE TV: sidebar (favoritos/recientes placeholder + buscar + TreeView canales), video + EPG placeholder, panel info canal
- [x] Tab MOVIES: sidebar (favoritos/continuar placeholder + categorias peliculas), video + buscar + grid de posters
- [x] Tab SERIES: sidebar (favoritos/continuar placeholder + categorias series), video + buscar + grid de posters
- [x] Panel info del canal (Live TV): logo ampliado, nombre, grupo, indicador EN VIVO
- [x] Grids de peliculas/series con cards (poster + nombre), hover effect, doble-clic para reproducir
- [x] Busqueda contextual por tab (Live TV filtra canales, Movies/Series filtran grids)
- [x] Si no hay categoria seleccionada en Movies/Series, busqueda aplica a todas las categorias
- [x] Splitter horizontal entre video y contenido inferior (EPG/grid) redimensionable
- [x] Panel info derecho auto-oculta en tabs Movies/Series, visible solo en Live TV
- [x] Persistencia del tab activo en AppSettings
- [x] Fullscreen actualizado para manejar todos los paneles del nuevo layout
- [x] Estados vacios para cada seccion (sin favoritos, sin peliculas, sin series, etc.)

### Refinamiento UI/UX
- [x] Controles del reproductor inmersivos (flotantes en modo overlay sobre el video) solucionando de forma óptima el Airspace problem de WPF+LibVLC.
- [x] Vista completa interactiva (grilla in-place sobre el reproductor) para ver el listado total de Favoritos e Historial sin recortarlos a 2 elementos.
- [x] Soporte de scroll vertical pasivo (usando `DockPanel`) en los sidebars para navegar listas largas sin glitches gráficos de anidamiento.
- [x] Labels y placeholders ("Buscar canal...") flotantes sobre todos los inputs de búsqueda de la aplicación.
- [x] Lógica preventiva en ComboBox editable de URLs M3U para evitar pérdida accidental de la lista actual en reproducción (Two-Way Binding safety).

---

## Completado — Funcionalidades implementadas sobre el layout

### Favoritos
- [x] Marcar/desmarcar canales, peliculas y series como favorito (icono estrella)
- [x] Persistencia en AppSettings (FavoriteUrls para canales/peliculas, FavoriteSeriesNames para series)
- [x] Paneles FAVORITOS en cada tab (Live TV, Movies, Series) con lista interactiva
- [x] En Live TV, estrella visible en TreeViewItem al hover y cuando esta marcado
- [x] Click en favorito reproduce directamente (series navega al show)

### Vistos recientemente / Continuar viendo
- [x] Registro automatico de historial al reproducir cualquier canal/pelicula/serie
- [x] Para VOD: tracking de posicion (PositionMs) y duracion (DurationMs) en tiempo real
- [x] Persistencia en AppSettings (WatchHistory con Url, Name, LogoUrl, PositionMs, DurationMs, Timestamp, Category)
- [x] Panel VISTOS RECIENTEMENTE en Live TV (ultimos 10 canales)
- [x] Panel CONTINUAR VIENDO en Movies/Series con barra de progreso sobre la card
- [x] Click retoma desde la posicion guardada (PlayChannelFromPosition con :start-time)
- [x] Maximo 20 entradas, las mas antiguas se eliminan automaticamente

### EPG (Electronic Program Guide)
- [x] Deteccion automatica de URL EPG desde atributos url-tvg / x-tvg-url en cabecera M3U
- [x] Parser XMLTV completo con soporte GZip
- [x] Panel EPG debajo del video (Live TV) con lista horizontal de programas
- [x] Programa actual destacado, descripcion al hover
- [x] Panel info del canal (derecha) muestra programa actual y siguiente
- [x] Vinculacion canal-EPG via tvg-id / tvg-name
- [x] Cache en memoria, refresh automatico cada 60 segundos

### Info del canal completa
- [x] Logo ampliado, nombre, grupo, indicador EN VIVO
- [x] Programa actual y siguiente con titulo y horario (requiere EPG cargada)

### Episodios de series
- [x] Navegacion Shows → Temporadas → Episodios (SeriesNavLevel enum)
- [x] Deteccion automatica de episodios via regex (S01E01, T01E01, 1x01)
- [x] Breadcrumb y boton Volver para navegar entre niveles
- [x] Doble clic en episodio lo reproduce
- [x] Agrupacion por nombre de serie derivado del prefijo antes del patron de episodio
- [x] Auto-play del siguiente episodio al terminar el actual (preserva pista de audio y subtitulos, mantiene fullscreen)
- [x] Boton overlay "Siguiente capitulo" aparece al 93% del progreso, permite saltar manualmente al siguiente episodio

### Configuracion (Settings)
- [x] Boton gear (⚙) activo en header bar
- [x] Ventana modal de configuracion con tema oscuro y barra de titulo oscura (DWM)
- [x] Checkbox: cargar ultima playlist automaticamente al iniciar
- [x] Slider: intentos maximos de reconexion (1-20), configurable en AppSettings
- [x] Boton: limpiar historial de reproduccion (con confirmacion visual)
- [x] Boton: limpiar favoritos (con confirmacion visual)
- [x] Seccion Acerca de: icono Chucao, nombre de la app, version, descripcion
- [x] Guardar/Cancelar con persistencia en AppSettings

### Prioridad media

- [ ] **Ordenamiento de canales**: Opciones de ordenar por nombre, grupo, o posicion original
- [ ] **Multiples playlists**: Gestionar varias playlists guardadas con nombres personalizados
- [ ] **Actualizacion automatica de playlist**: Recargar periodicamente la playlist para detectar cambios
- [ ] **Selector de calidad de video**: Seleccion manual cuando el stream ofrece multiple bitrate (adaptive)
- [ ] **Grabacion de stream**: Grabar el stream actual a archivo local (LibVLC lo soporta nativamente)
- [ ] **Menu contextual en canales**: Click derecho para copiar URL, agregar a favoritos, ver info del stream
- [ ] **Icono .ico para el ejecutable**: Convertir el SVG del Chucao a formato .ico multi-resolucion

### Prioridad baja

- [ ] **Soporte multi-idioma (i18n)**: Interfaz traducible
- [ ] **Temas claros/oscuros**: Opcion para cambiar entre tema oscuro y claro
- [ ] **Instalador MSI/MSIX**: Empaquetado con acceso directo en menu inicio
- [ ] **Auto-update**: Verificar y descargar nuevas versiones automaticamente
- [ ] **Soporte para playlist con DRM**: Streams protegidos con tokens o headers de autenticacion
- [ ] **Cast/DLNA**: Enviar stream a dispositivos en la red local
- [ ] **Ecualizador de audio**: Ajustes de ecualizacion via LibVLC
- [ ] **Capturas de pantalla**: Screenshot del video actual
- [ ] **Rendimiento con playlists grandes**: Virtualizacion del TreeView para miles de canales
- [ ] **Logs y diagnostico**: Sistema de logging para depuracion
- [ ] **Tests unitarios**: Cobertura para parser M3U, servicios y ViewModels

---

## Notas tecnicas

- **Airspace problem (LibVLC 3)**: Todo overlay de controles debe estar dentro del `VideoView.Content`. El `ForegroundWindow` interno de LibVLCSharp renderiza el contenido WPF sobre el HWND nativo de VLC. No usar ventanas WPF separadas como overlays.
- **PiP y transferencia de MediaPlayer**: LibVLC 3 vincula el MediaPlayer a un HWND especifico al iniciar reproduccion. No es posible mover un player en ejecucion entre ventanas. El approach correcto es una instancia independiente de LibVLC+MediaPlayer en el PiP.
- **Seeking en live streams**: `MediaPlayer.Time` y `Position` no son funcionales en streams en vivo (HLS/RTMP). Al retornar de PiP en contenido live se hace una reconexion fresca al stream.
- **Threading en LibVLCSharp**: NUNCA llamar metodos de LibVLC directamente desde event handlers de LibVLC. Usar `ThreadPool.QueueUserWorkItem` o `Task.Run` para evitar deadlocks. La reconexion automatica usa `Task.Run` con `CancellationTokenSource` para manejar esto correctamente.
- **Migracion a LibVLC 4**: LibVLC 4 resolveria el airspace problem y permitiria PiP seamless via Direct3D/OpenGL. A Feb 2026, LibVLC 4 esta al 72% del milestone y LibVLCSharp 4 solo tiene paquetes alpha. No apto para produccion aun.

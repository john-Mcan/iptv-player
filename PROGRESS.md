# IPTV Player - Progreso y Roadmap

## Estado actual: MVP funcional

La aplicacion compila y ejecuta correctamente. Todas las funcionalidades core estan implementadas.

---

## Completado

- [x] Proyecto .NET 8 WPF configurado con dependencias NuGet (LibVLCSharp, CommunityToolkit.Mvvm)
- [x] Modelos de datos: Channel, ChannelGroup, TrackInfo
- [x] Parser M3U con soporte para atributos EXTINF (tvg-name, tvg-logo, group-title, tvg-id)
- [x] Servicio de carga de playlists desde URL (HTTP/HTTPS) y archivo local
- [x] Agrupacion automatica de canales por categoria
- [x] Integracion LibVLCSharp: inicializacion, reproduccion, eventos
- [x] Controles de reproduccion: Play/Pause, Stop, volumen, mute
- [x] Barra de progreso/seek para contenido on-demand
- [x] Deteccion automatica live vs on-demand
- [x] Selector de pistas de audio multiple
- [x] Selector de subtitulos multiple
- [x] Picture-in-Picture (ventana Topmost, sin bordes, arrastrable, redimensionable) — usa VideoView con overlay dentro de ForegroundWindow para resolver airspace problem
- [x] Transferencia de MediaPlayer entre VideoViews (main <-> PiP) — PiP usa LibVLC/MediaPlayer propio, sincroniza posicion al cerrar
- [x] Pantalla completa (F11 / boton) con ocultamiento de UI — controles overlay dentro de VideoView.Content (sin ventana separada)
- [x] Busqueda/filtro de canales en tiempo real
- [x] Tema oscuro con estilos personalizados (sliders, botones, TreeView)
- [x] Iconografia con Segoe MDL2 Assets
- [x] Barra de estado con indicador EN VIVO
- [x] Manejo basico de errores (carga fallida, stream con error)
- [x] Atajos de teclado (F11, Escape, Space, Enter)
- [x] Panel lateral redimensionable con GridSplitter

---

## Pendiente para produccion

### Prioridad alta

- [ ] **Persistencia de configuracion**: Guardar ultima playlist cargada, volumen, tamano de ventana (usar `Settings` o archivo JSON en AppData)
- [ ] **Manejo robusto de errores de red**: Reintentos automaticos en streams que se desconectan, timeout configurable, reconexion al canal actual
- [ ] **EPG (Electronic Program Guide)**: Soporte para guias de programacion XMLTV vinculadas a la playlist M3U, mostrar programa actual y siguiente
- [ ] **Favoritos**: Permitir marcar canales como favoritos con acceso rapido
- [ ] **Historial reciente**: Ultimos canales reproducidos para acceso rapido
- [ ] **Icono de aplicacion**: Disenar e incluir icono .ico para el ejecutable y la barra de tareas

### Prioridad media

- [ ] **Logos de canales**: Cargar y mostrar thumbnails/logos desde las URLs `tvg-logo` en la lista de canales
- [ ] **Ordenamiento de canales**: Opciones de ordenar por nombre, grupo, o posicion original
- [ ] **Multiples playlists**: Gestionar varias playlists guardadas con nombres personalizados
- [ ] **Actualizacion automatica de playlist**: Recargar periodicamente la playlist para detectar cambios
- [ ] **Selector de calidad de video**: Cuando el stream ofrece multiples calidades (adaptive bitrate), permitir seleccion manual
- [ ] **Grabacion de stream**: Capacidad de grabar el stream actual a un archivo local (LibVLC lo soporta nativamente)
- [x] **Controles en fullscreen**: Overlay de controles que aparece al mover el mouse y se oculta tras inactividad
- [ ] **Menu contextual en canales**: Click derecho para copiar URL, agregar a favoritos, ver informacion del stream

### Prioridad baja

- [ ] **Soporte multi-idioma (i18n)**: Interfaz traducible, actualmente todo en espanol
- [ ] **Temas claros/oscuros**: Opcion para cambiar entre tema oscuro y claro
- [ ] **Instalador MSI/MSIX**: Empaquetado como instalador Windows con acceso directo en menu inicio
- [ ] **Auto-update**: Mecanismo para verificar y descargar nuevas versiones automaticamente
- [ ] **Soporte para playlist con DRM**: Manejo de streams protegidos con tokens o headers de autenticacion
- [ ] **Cast/DLNA**: Enviar el stream a dispositivos compatibles en la red local
- [ ] **Ecualizador de audio**: Ajustes de ecualizacion usando las capacidades de LibVLC
- [ ] **Capturas de pantalla**: Boton para tomar screenshot del video actual
- [ ] **Picture-in-Picture mejorado**: Controles minimos dentro de la ventana PiP (play/pause, canal anterior/siguiente)
- [ ] **Rendimiento con playlists grandes**: Virtualizacion del TreeView para listas con miles de canales
- [ ] **Logs y diagnostico**: Sistema de logging para depuracion de problemas de conectividad/reproduccion
- [ ] **Tests unitarios**: Cobertura para el parser M3U, servicios, y ViewModels

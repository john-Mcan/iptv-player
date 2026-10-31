# Registro de cambios

Aquí se documentan los cambios relevantes de Chucao.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto usa
[versionado semántico](https://semver.org/lang/es/). Las versiones hasta la 1.0.0 se reconstruyeron
a partir del historial de commits: no tienen tag ni binario publicado.

## [Sin publicar]

### Añadido

- Cuentas Xtream Codes como fuente: TV en vivo, películas y series desde la API del panel, con
  guía XMLTV del propio panel. Los episodios de cada serie se cargan al abrirla.
- Gestor de fuentes: varias listas M3U (URL o archivo local) y cuentas Xtream guardadas con
  nombre. Las URLs que guardaban las versiones anteriores pasan a ser fuentes.
- Programa actual y siguiente en la cabecera de la guía y en pantalla completa.
- Ícono del chucao en el ejecutable, en varios tamaños.
- La ventana recuerda si quedó maximizada o en pantalla completa y vuelve a abrirse así.
- Tests automatizados (xUnit) de listas M3U, EPG, series, historial, favoritos, fuentes, cliente
  Xtream y configuración.
- Compilación y tests en GitHub Actions en cada push, release automático al subir un tag y
  revisión mensual de dependencias con Dependabot.
- Licencia PolyForm Noncommercial 1.0.0.

### Cambiado

- La app se llama Chucao en las ventanas, en «Acerca de» y en los metadatos del ejecutable.
- Migración a .NET 10 (LTS), ya que el soporte de .NET 8 termina el 10 de noviembre de 2026.
- Dependencias al día: LibVLCSharp 3.10.2, LibVLC 3.0.24 y VirtualizingWrapPanel 2.5.4.
- La publicación para `win-x64` incluye solo la LibVLC de x64: la carpeta publicada baja de 444 MB
  a 241 MB.
- El selector de fuentes reemplaza al campo de URL del encabezado.
- Código reorganizado: la lógica sin interfaz pasa a `IPTVPlayer.Core`; `App.xaml` se divide en
  diccionarios de estilos y `MainWindow.xaml` en un control por panel de cada pestaña.

### Corregido

- Algunos streams IPTV quedaban en negro porque publican la pista de video desactivada; ahora se
  activa la primera pista disponible.
- La ventana podía abrirse fuera de la pantalla al cambiar de monitores o de resolución.
- Al cerrar la app en pantalla completa, la columna izquierda quedaba oculta en el siguiente inicio.
- Un clic en la grilla de series se contaba más de una vez y no dejaba entrar a temporadas y
  episodios.
- Un contenido con la resolución en el nombre (por ejemplo «1920x1080») se agrupaba como si fuera
  un episodio.
- En la primera sesión, los cambios de la ventana de configuración no se guardaban en el disco.
- Al retomar algo antiguo de «Continuar viendo» y abrir después otro contenido, se podía perder la
  entrada en curso con su progreso.
- Las grillas de pósters avanzaban a saltos con la rueda del mouse.

### Seguridad

- Las contraseñas de las cuentas Xtream se guardan cifradas con DPAPI para el usuario de Windows.

## [1.0.0] - 2026-02-26

### Añadido

- Ventana de configuración: cargar la última lista al iniciar, intentos máximos de reconexión
  (1 a 20), limpiar historial y favoritos, y sección «Acerca de» con la versión.
- Secciones de favoritos, «Continuar viendo» y «Vistos recientemente» en cada pestaña, con
  «Ver todo» para abrir la lista completa en una grilla sobre el reproductor.
- Siguiente episodio automático al terminar un capítulo: conserva las pistas de audio y subtítulos
  y la pantalla completa.
- Botón «Siguiente capítulo» sobre el video cuando el episodio llega al 90 %.
- Filtro de contenido para adultos, configurable.
- Orden de películas y series: más recientes, A-Z y Z-A.
- Botón de pausa en la ventana PiP e indicador de carga en el reproductor y en el PiP.
- Opción para quitar entradas de «Continuar viendo».

### Cambiado

- Los controles del reproductor flotan sobre el video y reemplazan al panel lateral derecho de
  información del canal.
- Los canales 24/7 y los grupos mixtos (por ejemplo, «cine y series») se clasifican como TV en
  vivo.

### Corregido

- Reanudar después de una pausa larga fallaba: tras 10 minutos en pausa se vuelve a conectar al
  stream desde la última posición.

## [0.2.0] - 2026-02-22

### Añadido

- Interfaz en pestañas (TV en vivo, Películas y Series) con barra de título oscura, grilla de
  pósters para películas y series, y búsqueda por pestaña.
- Favoritos de canales, películas y series.
- Historial de reproducción y «Continuar viendo» con la posición guardada.
- Guía de programación (EPG): lee el XMLTV indicado en `url-tvg` o `x-tvg-url`, también
  comprimido con GZip, y lo vincula por `tvg-id` o `tvg-name`.
- Navegación de series por temporadas y episodios, a partir de nombres con `S01E01`, `T01E01` o
  `1x01`.
- Reconexión automática con espera exponencial (1 s, 2 s, 4 s… hasta 30 s) y hasta 10 intentos.
- La app recuerda la última lista, las 5 URL recientes, el volumen y el tamaño y la posición de la
  ventana.
- Paleta gris y naranja inspirada en el chucao, con el ave como ícono.

### Cambiado

- PiP: control de volumen con silencio y esquina para cambiar el tamaño de la ventana.

## [0.1.0] - 2026-02-20

### Añadido

- Picture-in-Picture: ventana sin bordes, siempre visible y arrastrable, con volumen en la rueda
  del mouse. Al cerrarla, el contenido bajo demanda sigue desde la misma posición y el en vivo se
  reconecta.
- Controles en pantalla completa que aparecen al mover el mouse y se ocultan a los 3 segundos.

### Corregido

- Errores del modo pantalla completa.

## [0.0.1] - 2026-02-20

### Añadido

- Carga de listas M3U desde una URL o un archivo local, agrupadas por categoría.
- Reproducción con LibVLC de streams en vivo y contenido bajo demanda: reproducir, pausar,
  detener, volumen y barra de progreso.
- Selección de pistas de audio y subtítulos.
- Pantalla completa básica.

[Sin publicar]: https://github.com/john-Mcan/iptv-player/commits/main

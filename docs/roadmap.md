# Próximos pasos

Lo pendiente, ordenado por prioridad dentro de cada grupo.

## Problemas conocidos

Encontrados al sacar las capturas del README (octubre de 2026).

- **Canal seleccionado ilegible sin foco.** Cuando el árbol de canales pierde el foco (por ejemplo, al hacer clic en el video), el canal seleccionado queda con fondo gris claro y texto naranja claro, y casi no se lee. El estilo de `TreeViewItem` en `Themes/Lists.xaml` solo cambia el color del texto y deja la plantilla por defecto, que usa el color de selección inactiva del sistema. Hay que darle una plantilla propia o redefinir `SystemColors.InactiveSelectionHighlightBrushKey`.
- **«Disable» en los selectores de pista.** En los canales en vivo, los selectores de audio y subtítulos muestran «Disable», el nombre en inglés que LibVLC da a la pista vacía. Debería decir «Sin audio» o «Sin subtítulos», o no aparecer en la lista.
- **La PiP no respeta el silencio.** `PiPWindow` recibe el volumen pero no el estado de silencio, así que si el reproductor principal está silenciado, la PiP se abre con sonido.

## Código

- **Controles del reproductor fuera de `MainWindow`.** La barra flotante, los controles de pantalla completa y la lógica de PiP siguen en `MainWindow.xaml` y su code-behind. Se pueden pasar a un UserControl con eventos para PiP y pantalla completa.
- **`MainViewModel` más chico.** Todavía tiene unas mil líneas: la búsqueda, los filtros y el orden de cada pestaña podrían ir a ViewModels hijos (TV en vivo, Películas, Series), como ya se hizo con la EPG.
- **Tests de ViewModels.** `MainViewModel` crea un `PlayerViewModel` que inicializa LibVLC. Con una interfaz para el reproductor se podría probar la navegación de series y el «Continuar viendo» sin LibVLC.
- **Pestañas con binding.** Las pestañas cambian con un handler `Click` y no con un binding de `IsChecked`, así que el teclado y la automatización de UI (lectores de pantalla) no las cambian.

## Xtream Codes

- **Credenciales fuera del historial.** Las URLs de reproducción llevan usuario y contraseña, y quedan en claro en el historial y los favoritos de `settings.json`. Conviene guardar una referencia a la fuente (id de fuente + id de stream) y armar la URL al reproducir.
- **Probar con más paneles.** El cliente se probó con respuestas simuladas; falta validarlo con varios proveedores reales.
- Metadatos en la interfaz: sinopsis, año y calificación de películas y series.
- Soporte de Stalker Portal.

## EPG

- `x-tvg-url` con varias URLs separadas por coma (hoy se toma el texto completo como una sola URL).
- Fechas XMLTV sin espacio antes de la zona horaria (`20260115103000+0000`): hoy se ignora la zona.

## Funciones

- Recargar la fuente activa cada cierto tiempo para ver canales nuevos.
- Ordenar canales por nombre, grupo o posición original.
- Menú contextual en canales: copiar URL, favoritos, información del stream.
- Selector de calidad en streams adaptativos.
- Grabar el stream actual (LibVLC lo permite).
- Encabezados HTTP y user-agent por fuente, para proveedores que los exigen.
- Capturas de pantalla del video.
- Ecualizador de audio.
- Registro de diagnóstico (logs).
- Interfaz en otros idiomas.
- Tema claro.
- Instalador MSIX y actualización automática.
- Enviar a dispositivos de la red (DLNA/Chromecast).

# Publicar una versión

El workflow [`release.yml`](../.github/workflows/release.yml) se ejecuta al subir un tag `v*`: corre los tests, publica el ejecutable autocontenido para `win-x64` con la versión del tag, lo comprime como `Chucao-vX.Y.Z-win-x64.zip` y crea el GitHub Release con las notas de esa versión sacadas de `CHANGELOG.md`.

## Pasos

1. En `CHANGELOG.md`, cambia `## [Sin publicar]` por `## [X.Y.Z] - AAAA-MM-DD`, agrega un `## [Sin publicar]` vacío arriba y actualiza los enlaces del final.
2. En `src/IPTVPlayer/IPTVPlayer.csproj`, actualiza `<Version>` (la del tag manda en el release, pero así coinciden las compilaciones locales).
3. Haz commit (`chore(release): versión X.Y.Z`), crea el tag y súbelo:

   ```bash
   git tag -a vX.Y.Z -m "Chucao X.Y.Z"
   git push origin main --follow-tags
   ```

4. Revisa el workflow en la pestaña Actions y el release creado.

Si el CHANGELOG no tiene la sección `## [X.Y.Z]`, el workflow falla antes de crear el release.

## Probar el paquete antes de publicar

```bash
dotnet publish src/IPTVPlayer -c Release -r win-x64 --self-contained -o publish
```

Abre `publish/IPTVPlayer.exe` en un equipo sin .NET instalado y revisa TV en vivo, VOD, PiP, pantalla completa y series.

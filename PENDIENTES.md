# Pendientes

Mejoras identificadas que quedan para más adelante. Al terminar una, se marca
con `[x]` y se anota la fecha o el commit.

## Repositorio y pipeline

- [x] **SAST con CodeQL** (Settings → Code security → CodeQL, Default setup).
- [x] Dependabot alerts + security updates, y secret scanning con push protection.
- [x] Ruleset `proteger-main`: sin borrado ni force push en `main` (sin PR obligatoria: se trabaja con push directo).
- [x] Actions fijadas por SHA (con su versión en un comentario); `dependabot.yml` ya las actualizaba. (2026-10-31)
- [x] Checksum SHA-256 de gitleaks verificado antes de ejecutarlo. (2026-10-31)
- [x] `SECURITY.md` con cómo reportar vulnerabilidades en privado. (2026-10-31)
- [x] `azure-pipelines.yml`: mismo análisis de dependencias que en GitHub Actions y `timeoutInMinutes`. (2026-10-31)
- [x] SBOM CycloneDX de la release, adjunto a los instalables y atestado. (2026-10-31)
- [x] Atestación de procedencia (SLSA) de los instalables con `actions/attest`. (2026-10-31)

## Hecho

- [x] `permissions` globales de solo lectura y `timeout-minutes` en todos los jobs.

## Cobertura de tests

- [x] **Medir la cobertura en el CI** con coverlet (como la API): informe en el resumen del
  job y un % mínimo, excluyendo el código generado (XAML, `*.g.cs`). Fijar el mínimo con la
  cobertura medida menos 5 puntos. Hecho (nov 2026): 67,0 % de líneas, mínimo 60 %.

## Publicación en Microsoft Store

- [] **Automatizar la subida a la Store desde `release.yml`** con la Microsoft Store Developer CLI (`msstore`), para que un `git tag` publique sin tocar Partner Center.
  - Notas de "Novedades" en inglés y español: reutilizar las que ya genera `release.yml` y meterlas en el `releaseNotes` de cada ficha (`en-us` y `es-es`).
  - Flujo: obtener el JSON de metadatos del envío (`msstore submission get`), rellenar los dos `releaseNotes`, `msstore submission updateMetadata` y `msstore submission publish`.
    Ref.: https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/github-actions
  - Primer paso: registrar una app en Entra ID y asociarla a Partner Center (secretos en GitHub: tenant, seller, client id y secret).
  - Coste: ninguno (registro en Entra ID, API/CLI de la Store y Actions en repo público son gratis).
  - Ojo: el secreto de la app caduca (máx. 24 meses); apuntar la fecha y renovarlo.

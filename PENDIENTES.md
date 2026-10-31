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

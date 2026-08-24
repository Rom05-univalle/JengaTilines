# Aviso de Vuforia

Este proyecto usa Vuforia desde el registry oficial, no desde un `.tgz` local.

Si haces `pull` o abres el proyecto por primera vez:

1. Deja que Unity resuelva los paquetes con `Packages/manifest.json`.
2. No vuelvas a agregar `Packages/com.ptc.vuforia.engine-11.4.4.tgz` al repositorio.
3. Si el editor muestra errores de paquetes, vuelve a abrir el proyecto para que Unity regenere `packages-lock.json`.

El archivo local grande fue eliminado para poder publicar la rama en GitHub.
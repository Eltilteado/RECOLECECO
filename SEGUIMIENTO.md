# Seguimiento de RECOLECECO

## Punto de partida

El primer commit incorpora la versión base disponible de RECOLECECO.
A partir de ahí, el equipo registra las revisiones y los cambios actuales
en ramas propias y los integra mediante pull requests.

En la revisión de la entrega se extrajo el RAR sin errores y se comprobó que
incluye las escenas `MainMenu` y `BahiaPuerto`, los archivos de configuración
y los paquetes declarados para Unity 6000.3.23f1. La revisión fue estática.

## Pruebas para registrar

- [ ] Abrir el proyecto en Unity 6000.3.23f1 y entrar a la partida desde el menú.
- [ ] Navegar, recoger residuos y comprobar el aviso de bodega llena.
- [ ] Atracar, clasificar residuos y verificar el saldo de EcoCréditos.
- [ ] Comprar el motor. La bodega inicial admite 10 de los 12 residuos del
  Sector A. Con solo seis residuos de ese sector no se llega a los 50 créditos.
- [ ] Verificar el guardado volviendo al menú y eligiendo Continuar.
- [ ] Limpiar los sectores B y C hasta cumplir la última misión.
- [ ] Verificar la compilación de Windows y registrar el resultado.

En cada actualización registra la fecha, el cambio, cómo se probó y el
resultado. Si tienes una captura o un informe de prueba, enlázalo desde la
entrada correspondiente.

| Fecha | Cambio o prueba | Resultado | Evidencia |
| --- | --- | --- | --- |
| 27/11/2026 | Integrante 02: modelos Kenney del muelle y rompeolas, fichas de las 11 tareas y guía de agua y cielo | Fichas preparadas; pruebas en Unity pendientes | [Evidencias/integrante-02](Evidencias/integrante-02/README.md) |

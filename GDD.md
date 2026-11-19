# GDD actualizado — RECOLEC(ECO): Mareas de Esperanza

Documento de diseño **con lo realmente implementado** en la versión 1.0.0 (Unity 6000.3.23f1, URP).
Las referencias § remiten al documento de requisitos v2.

## 1. Resumen

| Campo | Implementado |
|---|---|
| Género | Aventura ecológica 3D de navegación y gestión ligera |
| Plataforma | Windows x64, un jugador, teclado y mouse |
| Protagonista y vehículo | Chalana pesquera «La Esperanza», controlada en tercera persona durante todo el juego |
| Mapa | Una escena (`BahiaPuerto`): Puerto de Pisco + Sector A (muelle), B (playa rocosa), C (islote de aves) |
| Duración estimada | 25–40 min para un jugador nuevo (la prueba automatizada, sin pausas de lectura, tarda menos) |
| ODS | 12 (reciclaje, Doña Rosa), 13 (mensajes de Don Mateo y final), 14 (limpieza marina, fauna) |

**Pilares cumplidos (§9):** juego 3D real en Unity; chalana controlable como eje; mar peruano y puerto colorido;
recolección desde el barco con brazo-red animado; bodega y regreso al muelle; Don Mateo y Doña Rosa;
progresión de la misma chalana, misiones y recuperación visible; ODS 12/13/14.

## 2. Historia y personajes

Un joven pescador llega al Puerto de Pisco. **Don Mateo** (carpintero naval, sombrero de paja, barba blanca)
le presta su chalana; **Doña Rosa** (centro de reciclaje, delantal verde) clasifica los residuos.
Un **pescador local** decorativo pesca al final del muelle. **El Recolector** se ve a bordo junto a la cabina.

- Diálogos cortos en español (2–4 líneas) con retrato, nombre y efecto máquina de escribir.
- NPC con animación procedural: respiración, giran hacia la chalana, saludan con el brazo cuando se acerca,
  gesticulan al hablar y muestran un «!» flotante si dan la misión activa.

## 3. Mapa

| Zona | Contenido |
|---|---|
| Puerto | Malecón de piedra, muelle de madera con defensas, zona de atraque señalizada, arco «Puerto de Pisco», taller de Don Mateo (con chalana en reparación), centro de reciclaje con 5 contenedores de colores, 8 casas coloridas, palmeras, faroles, redes colgadas que se mecen, banderines, cajas y barriles, cartel ODS, 5 chalanas de fondo, gaviotas |
| Sector A | Zona tutorial frente al muelle, boya «A» del primer viaje |
| Sector B | Playa de arena con hilera de rocas, rocas en el agua, lobos marinos |
| Sector C | Islote con guano y matorral, anillo de rocas, piqueros, pingüinos de Humboldt, gaviotas |
| Fondo | Cerros y acantilados low-poly en tonos arena/ocre, faro rojo y blanco con luz giratoria |

Límites invisibles cierran la bahía. Distribución de residuos exacta de §3 (A 12, B 18, C 20 = 50),
colocados por un algoritmo que garantiza agua libre, distancia a rocas/costa y separación mínima.

| Sector | Botellas | Latas | Redes | Cajas cartón/madera | Cajas tecnopor | Total |
|---|---|---|---|---|---|---|
| A | 6 | 5 | 0 | 1 | 0 | 12 |
| B | 5 | 4 | 4 | 3 | 2 | 18 |
| C | 6 | 5 | 5 | 2 | 2 | 20 |

## 4. Mecánicas implementadas

### 4.1 Navegación
- Velocidad máx. 6 m/s (8 con motor mejorado), ~2 s hasta velocidad máxima, retroceso 40 %, frenado más rápido.
- Giro más lento a baja velocidad (inercia) e invertido en retroceso.
- Física simplificada: Rigidbody con altura fija; choque contra rocas/muelle = rebote suave (35 %), sin daño.
- Balanceo con las olas (pitch/roll/heave desde la misma función de olas del shader), inclinación al acelerar y girar.
- Estela (dos trails planos), espuma de popa, salpicadura de proa a alta velocidad, hélice girando, sonido de motor según velocidad.
- Cámara en tercera persona con seguimiento suave, colisión (no atraviesa objetos), órbita con clic derecho y zoom con la rueda.

### 4.2 Recolección
- Manual con **E**; alcance 3 m (5 m mejorado) medido desde la proa.
- El residuo en rango se resalta (brillo pulsante + anillo) y aparece «E — Recoger …».
- El brazo amarillo con red gira hacia el residuo, baja, lo atrapa, lo lleva a la bodega y vuelve (≈1,7 s); chispas y sonido.
- Bodega llena: «Bodega llena, vuelve al puerto» (indicador y aviso) y el marcador apunta al muelle.
- Los residuos flotan y se mecen con las olas.

### 4.3 Inventario
Capacidad 10 (18 con mejora). Panel con **I**: cantidad por tipo, usado/libre/capacidad. HUD: «Bodega: X/Y».

### 4.4 Centro de reciclaje
Mesa de clasificación con los residuos de la bodega mezclados, uno a uno; 5 contenedores (clic o teclas 1–5),
con colores inspirados en la norma peruana NTP 900.058:

| Residuo | Contenedor | EC |
|---|---|---|
| Botella | Plástico (blanco) | 5 |
| Lata | Metal (amarillo) | 8 |
| Red | Redes y textiles (turquesa) | 12 |
| Caja de cartón/madera | Otros reciclables (azul) | 6 |
| Caja de tecnopor | Disposición adecuada — NO reciclable (negro) | 2 |

Acierto: créditos + residuo sale de la bodega + mensaje educativo de Doña Rosa (la primera vez por tipo).
Error: sin créditos ni castigo; se indica el contenedor correcto (parpadea) y se reintenta.

### 4.5 Taller (3 mejoras, compra única con confirmación)
| Mejora | Costo | Efecto | Cambio visible en la chalana |
|---|---|---|---|
| Motor mejorado | 50 EC | 6 → 8 m/s | Motor fuera de borda naranja más grande |
| Herramienta mejorada | 70 EC | Alcance 3 → 5 m | Brazo 45 % más largo y red más grande |
| Bodega ampliada | 100 EC | 10 → 18 | Dos cajas azules extra en cubierta |

Economía real: los 50 residuos valen **349 EC** (el documento estimaba ≈390); las tres mejoras cuestan 220 EC.
El Sector A completo vale 76 EC, suficiente para el motor tras el primer viaje.

### 4.6 Misiones
| # | Nombre | Objetivo implementado | Da la misión |
|---|---|---|---|
| 1 | Primer viaje | Llegar a 12 m de la boya «A» | Don Mateo |
| 2 | Una bahía más limpia | Retirar 6 residuos del Sector A | Don Mateo |
| 3 | Nada se desperdicia | Atracar y clasificar correctamente 6 residuos | Doña Rosa |
| 4 | Una chalana mejor | Comprar una mejora | Don Mateo |
| 5 | Guardianes de la bahía | ≥ 80 % retirado en B (15/18) **y** en C (16/20) | Ambos |

HUD con misión activa, quién la da, progreso y barra. Flecha amarilla en pantalla (se fija al borde si el destino
está fuera de vista, con nombre y distancia) + columna de luz en el mundo. El destino se elige según el estado
(residuo más cercano del sector requerido, o el muelle si la bodega está llena o toca clasificar/comprar).

### 4.7 Recuperación ambiental
- «Limpieza de la bahía» = retirados / 50, con hitos 25/50/75/100 % (aviso, sonido y línea de diálogo).
- Por sector, al limpiarse: el agua pasa de verde-marrón con manchas aceitosas a turquesa (shader), desaparecen
  grupos de desechos pequeños, aparecen peces saltando, gaviotas, lobos marinos (B) y aves y pingüinos (C).
- Global al 75 %: delfines saltando en el centro de la bahía.
- Es un indicador del juego, no una medición científica.

### 4.8 Final
Al completar la misión 5: fanfarria, toma de cámara orbitando el muelle, diálogo de Don Mateo y Doña Rosa,
pantalla de victoria con residuos por tipo, total y % de limpieza, EcoCréditos ganados/disponibles, mejoras,
misiones y tiempo de juego. Botones **Seguir navegando** y **Volver al menú**.

## 5. Interfaz
Menú principal (Continuar si hay guardado / Jugar / Controles / Salir), pantalla de controles al iniciar y desde
menú y pausa, HUD, inventario, diálogos, panel del muelle, reciclaje, taller, confirmación, pausa
(continuar, controles, reiniciar con confirmación, menú) y victoria. Paleta azul marino / verde / turquesa.

## 6. Arte y sonido
- Estilo low-poly colorido, todo generado para el proyecto (ver `CREDITS.md`). Iluminación de tarde soleada con sombras suaves,
  cielo procedural, niebla de distancia y post-proceso suave (bloom, tonemapping neutro, saturación, viñeta).
- Agua con shader propio: olas (3 ondas compartidas con C#), reflejo de cielo por Fresnel, especular, espuma en crestas, suciedad por sector.
- Audio 100 % sintetizado: olas, gaviotas, motor, brazo, salpicadura, aciertos/errores, compra, misión, hitos, atraque, música relajante de menú y juego.

## 7. Técnica
- C#, URP (Forward+), Input System. Lógica separada de la interfaz (eventos). Valores en `GameConfig` (ScriptableObject).
- Guardado JSON en `Application.persistentDataPath` (misión, mejoras, EcoCréditos, residuos retirados, inventario, estadísticas).
- Contenido generado de forma reproducible por `RecolecBuilder` (editor).
- Prueba automatizada dentro de la build (`AutoTestRunner`).

## 8. Diferencias con el documento de requisitos
| Tema | Requisito | Implementado | Motivo |
|---|---|---|---|
| Valor total de residuos | ≈ 390 EC | 349 EC | Se respetaron exactamente los valores por tipo y la distribución de §3; la diferencia viene de separar tecnopor (2 EC) |
| Cajas | «Caja» genérica | Dos variantes: cartón/madera (reciclable) y tecnopor (no reciclable) | Decisión §0.3 sobre el tecnopor |
| Personajes | Animaciones de NPC | Animación procedural (sin Animator/esqueleto) | Modelos propios de primitivas |
| Encargo del pescador (P2) | Opcional | **No implementado** (pescador solo decorativo) | Recorte permitido §11 |
| Minimapa (P2) | Opcional | **No implementado** (flecha + columna de luz) | Recorte permitido §11 |
| Recursos externos | Permitidos con licencia | No se usaron | Todo original, sin riesgos de licencia |

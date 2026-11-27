# Guía de ajustes del agua y el cielo

Referencia de configuración según los parches 06 y 07 del paquete del integrante 02.
Los valores siguientes corresponden a esos parches; confirmar en la rama actual antes de editarlos.

## Agua

Abrir `Assets/RECOLECECO/Materials/M_Water.mat` desde Project en Unity.
El shader asociado se encuentra en `Assets/RECOLECECO/Shaders/RecolecWater.shader`.

| Parámetro | Valor de referencia | Función |
| --- | --- | --- |
| _Gloss | 110 | Concentración del brillo especular |
| _SpecStrength | 0.85 | Intensidad del reflejo especular |
| _ShallowColor | (0.16, 0.65, 0.65, 1) | Color de agua clara |
| _DeepColor | (0.055, 0.34, 0.45, 1) | Color de agua profunda |
| _SkyColor | (0.58, 0.72, 0.79, 1) | Color del reflejo del cielo |

La espuma se calcula en el shader usando crestas, ruido y distancia a cámara.
Para evaluar cambios, comparar el mismo encuadre cerca de la chalana y en el horizonte.

## Cielo

Abrir `Assets/RECOLECECO/Materials/M_Sky.mat`.
Los parámetros del material también se configuran en `Assets/RECOLECECO/Scripts/Editor/RecolecBuilder.Scenes.cs`.

| Parámetro | Valor de referencia |
| --- | --- |
| _SunSize | 0.05 |
| _AtmosphereThickness | 0.8 |
| _Exposure | 1.1 |
| _SkyTint | #9AB8D3 |
| _GroundColor | #BBC0CB |

Si se cambia el material y se vuelve a generar la escena, revisar también el constructor para evitar perder los ajustes.

## Comprobación pendiente en Unity

1. Localizar ambos materiales y confirmar los valores del Inspector.
2. Abrir la escena del puerto, observar el agua, el horizonte y los tablones del muelle.
3. Registrar versión de Unity, escena, commit, resultado y capturas propias.
4. Pedir a otro integrante que localice los ajustes siguiendo esta guía.

Esta guía no certifica que se haya realizado la prueba ni que otro integrante la haya validado.

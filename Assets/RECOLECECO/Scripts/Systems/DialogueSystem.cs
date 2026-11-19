using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecolecEco
{
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        public string text;
        public DialogueLine(string speaker, string text) { this.speaker = speaker; this.text = text; }
    }

    /// <summary>Cola de diálogos cortos. La vista (DialogueView) muestra la línea actual.</summary>
    public class DialogueSystem : MonoBehaviour
    {
        readonly Queue<DialogueLine> _queue = new Queue<DialogueLine>();
        Action _onComplete;

        public bool IsOpen { get; private set; }
        public DialogueLine Current { get; private set; }
        public static string CurrentSpeaker { get; private set; }

        public event Action LineChanged;
        public event Action Closed;

        public void Show(IEnumerable<DialogueLine> lines, Action onComplete = null)
        {
            if (IsOpen)
            {
                // Encadena sin perder el callback anterior.
                foreach (var l in lines) _queue.Enqueue(l);
                var prev = _onComplete;
                _onComplete = () => { prev?.Invoke(); onComplete?.Invoke(); };
                return;
            }
            _queue.Clear();
            foreach (var l in lines) _queue.Enqueue(l);
            _onComplete = onComplete;
            if (_queue.Count == 0) { onComplete?.Invoke(); return; }
            IsOpen = true;
            Next();
        }

        public void Next()
        {
            if (!IsOpen) return;
            if (_queue.Count == 0) { Close(); return; }
            Current = _queue.Dequeue();
            CurrentSpeaker = Current.speaker;
            AudioManager.I.PlaySfx(Sfx.Click);
            LineChanged?.Invoke();
        }

        public void SkipAll()
        {
            if (!IsOpen) return;
            _queue.Clear();
            Close();
        }

        void Close()
        {
            IsOpen = false;
            CurrentSpeaker = null;
            Closed?.Invoke();
            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }
    }

    /// <summary>Textos del juego (español, máximo 3-4 líneas por intercambio).</summary>
    public static class DialogueScripts
    {
        public const string Mateo = "Don Mateo";
        public const string Rosa = "Doña Rosa";
        public const string Player = "El Recolector";
        public const string Fisher = "Pescador";

        static DialogueLine M(string t) => new DialogueLine(Mateo, t);
        static DialogueLine R(string t) => new DialogueLine(Rosa, t);

        public static readonly DialogueLine[] Intro =
        {
            M("¡Bienvenido al Puerto de Pisco, muchacho! Soy Mateo, carpintero naval."),
            M("La bahía está llena de basura: botellas, latas, redes... los peces se van y la pesca se pierde."),
            M("Te presto mi chalana, «La Esperanza». Recoge residuos con la red de proa y tráelos al muelle."),
            M("Primero, sal del muelle y navega hasta la boya marcada del Sector A. ¡Buen viento!"),
        };

        public static DialogueLine[] MissionStart(int i)
        {
            switch (i)
            {
                case 1: return new[]
                {
                    M("¡Así se navega! Ahora acércate a la basura flotante."),
                    M("Cuando veas «E — Recoger», presiona E y la red hará el trabajo. Trae 6 residuos del Sector A."),
                };
                case 2: return new[]
                {
                    R("¡Hola! Soy Rosa, del centro de reciclaje. Ya te vi trabajando."),
                    R("Vuelve al muelle, atraca con E y ven a clasificar lo que traigas. Necesito 6 residuos clasificados."),
                };
                case 3: return new[]
                {
                    R("¡Muy bien clasificado! Cada material en su lugar vale EcoCréditos."),
                    M("Con esos EcoCréditos pasa por mi taller. Una chalana mejor limpia más rápido."),
                };
                case 4: return new[]
                {
                    M("¡Mira cómo quedó! Ahora sí estás listo para lo grande."),
                    M("La playa rocosa (Sector B) y el islote de aves (Sector C) están muy sucios."),
                    R("Retira al menos el 80 % de los residuos de cada uno. ¡Toda la caleta cuenta contigo!"),
                };
                default: return Array.Empty<DialogueLine>();
            }
        }

        public static readonly DialogueLine[] Finale =
        {
            M("¡Lo lograste, muchacho! Mira el agua: clara otra vez. Ya volvieron los peces."),
            R("Y todo lo que sacaste del mar tuvo un buen destino. Así se cuida la bahía (ODS 12 y 14)."),
            M("Un mar limpio también nos ayuda a enfrentar el cambio climático (ODS 13)."),
            R("Desde hoy eres un Guardián de la Bahía. ¡Gracias por devolverle la vida al mar!"),
        };

        public static readonly DialogueLine[] RosaGreeting =
        {
            R("¡Bienvenido al centro de reciclaje! Pon cada residuo en su contenedor."),
            R("Ojo: el tecnopor que viene del mar NO es reciclable. Va a «Disposición adecuada»."),
        };

        public static readonly DialogueLine[] MateoGreeting =
        {
            M("Bienvenido al taller. Cada mejora se instala en tu misma chalana."),
        };

        public static DialogueLine[] Milestone(int pct)
        {
            switch (pct)
            {
                case 25: return new[] { M("¡Un cuarto de la bahía limpio! Ya se ven los primeros peces saltando.") };
                case 50: return new[] { R("¡La mitad de la bahía limpia! Los lobos marinos vuelven a las rocas.") };
                case 75: return new[] { M("¡75 %! El agua se ve turquesa otra vez. Las aves regresan al islote.") };
                case 100: return new[] { R("¡La bahía está completamente limpia! Pisco te lo agradece.") };
                default: return Array.Empty<DialogueLine>();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace RecolecEco
{
    public partial class SaveData
    {
        public int bestSortingQuizScore;
    }

    public sealed partial class FieldJournal
    {
        static readonly WasteType[] QuizItems = { WasteType.Botella, WasteType.Lata, WasteType.Red,
            WasteType.CajaCarton, WasteType.CajaTecnopor };
        static readonly BinType[] QuizBins = { BinType.Plastico, BinType.Metal, BinType.RedesTextiles,
            BinType.OtrosReciclables, BinType.Disposicion };
        int _quizStep, _quizScore;
        bool _answered;

        partial void AddQuiz() => AddTab("Reto de reciclaje", ShowQuizHome);

        void ShowQuizHome()
        {
            ClearPage();
            Line(_body, "Reto de Doña Rosa", 37, UIColors.Text, 0, 24, 960, 52);
            Line(_body, "Clasifica cinco residuos de práctica. Recibirás una explicación después de cada respuesta.",
                28, UIColors.TextDim, 0, 100, 880, 100);
            Line(_body, $"Mejor resultado: {GM.State.bestSortingQuizScore}/{QuizItems.Length}", 29,
                UIColors.Yellow, 0, 225, 900, 46);
            ActionButton(_body, "Empezar reto", () =>
            {
                _quizStep = 0;
                _quizScore = 0;
                ShowQuestion();
            }, 0, 310, 280, 58);
        }

        void ShowQuestion()
        {
            ClearPage();
            _answered = false;
            if (_quizStep >= QuizItems.Length)
            {
                if (_quizScore > GM.State.bestSortingQuizScore)
                {
                    GM.State.bestSortingQuizScore = _quizScore;
                    GM.Autosave("récord de reciclaje");
                }
                Line(_body, $"Resultado: {_quizScore}/{QuizItems.Length}", 45, UIColors.Yellow, 0, 78, 950, 82);
                ActionButton(_body, "Volver al reto", ShowQuizHome, 0, 230, 280, 58);
                return;
            }

            var material = GM.Config.GetWaste(QuizItems[_quizStep]);
            Line(_body, $"Pregunta {_quizStep + 1}/{QuizItems.Length} · ¿Dónde clasificarías este residuo?", 24,
                UIColors.Turquoise, 0, 0, 955, 38);
            Line(_body, material.displayName, 39, UIColors.Text, 0, 48, 960, 62);
            var feedback = Line(_body, "", 24, UIColors.Text, 0, 281, 950, 124);
            var next = ActionButton(_body, "Siguiente", () => { _quizStep++; ShowQuestion(); }, 725, 425, 237, 57);
            next.gameObject.SetActive(false);
            for (int i = 0; i < QuizBins.Length; i++)
            {
                var choice = QuizBins[i];
                string choiceName = GM.Config.GetBin(choice).displayName;
                ActionButton(_body, choiceName, () =>
                {
                    if (_answered) return;
                    _answered = true;
                    bool correct = choice == material.correctBin;
                    if (correct) _quizScore++;
                    feedback.text = (correct ? "¡Correcto! " : $"Contenedor: {GM.Config.GetBin(material.correctBin).displayName}. ")
                        + material.tip;
                    next.gameObject.SetActive(true);
                }, (i % 3) * 332, 120 + (i / 3) * 71, 316, 62);
            }
        }
    }
}

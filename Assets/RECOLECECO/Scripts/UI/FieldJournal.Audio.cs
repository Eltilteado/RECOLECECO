using System;
using UnityEngine;
using UnityEngine.UI;

namespace RecolecEco
{
    public sealed partial class FieldJournal
    {
        partial void AddAudio() => AddTab("Audio", ShowAudio);

        void ShowAudio()
        {
            ClearPage();
            var audio = AudioManager.I;
            Line(_body, "Regula la música y los sonidos del juego por separado.", 27,
                UIColors.TextDim, 0, 7, 974, 58);
            var music = AudioRow("Música", 105, () => audio.MusicLevel,
                value => audio.SetLevels(value, audio.EffectsLevel));
            var effects = AudioRow("Efectos y ambiente", 242, () => audio.EffectsLevel,
                value => audio.SetLevels(audio.MusicLevel, value));
            _refresh = () =>
            {
                music.text = $"{Mathf.RoundToInt(audio.MusicLevel * 100)} %";
                effects.text = $"{Mathf.RoundToInt(audio.EffectsLevel * 100)} %";
            };
            ActionButton(_body, "Restaurar volúmenes", () =>
            {
                audio.SetLevels(1f, 1f);
                _refresh();
            }, 0, 410, 267, 60);
            _refresh();
        }

        Text AudioRow(string title, int y, Func<float> current, Action<float> change)
        {
            var panel = UIFactory.Panel(_body, title, UIColors.PanelLight);
            UIFactory.Place(panel.rectTransform, TopLeft, TopLeft, new Vector2(0, -y), new Vector2(984, 113));
            Line(panel.transform, title, 31, UIColors.Text, 27, 32, 495, 42);
            var amount = Line(panel.transform, "", 32, UIColors.Yellow, 682, 34, 125, 48);
            ActionButton(panel.transform, "−", () =>
            {
                change(current() - .1f);
                _refresh();
            }, 592, 27, 64, 59);
            ActionButton(panel.transform, "+", () =>
            {
                change(current() + .1f);
                _refresh();
            }, 807, 27, 64, 59);
            return amount;
        }
    }
}

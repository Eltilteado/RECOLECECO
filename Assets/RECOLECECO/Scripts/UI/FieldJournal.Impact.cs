using UnityEngine;
using UnityEngine.UI;

namespace RecolecEco
{
    public sealed partial class FieldJournal
    {
        partial void AddImpact() => AddTab("Impacto", ShowImpact);

        void ShowImpact()
        {
            ClearPage();
            Line(_body, "Separa lo que retiras del mar de lo que ya clasificaste en el muelle.",
                25, UIColors.TextDim, 0, 0, 970, 42);
            var cleanup = ImpactCard("Retirados del mar", 0);
            var sorting = ImpactCard("Clasificados", 332);
            var pending = ImpactCard("En la bodega", 664);
            Line(_body, "Material", 23, UIColors.Turquoise, 22, 205, 460, 34);
            Line(_body, "Retirados        Clasificados", 23, UIColors.Turquoise, 584, 205, 390, 34);
            var rows = new Text[5];
            var counts = new Text[5];
            for (int i = 0; i < rows.Length; i++)
            {
                int y = 252 + i * 47;
                rows[i] = Line(_body, GM.Config.GetWaste((WasteType)i).displayName, 23,
                    UIColors.Text, 22, y, 520, 35);
                counts[i] = Line(_body, "", 23, UIColors.Text, 638, y, 320, 35);
            }
            _refresh = () =>
            {
                cleanup.text = $"{GM.Environment.Retired}/{GM.Environment.TotalWaste} · {GM.Environment.CleanPercent} %";
                sorting.text = GM.State.totalSortedCorrect.ToString();
                pending.text = $"{GM.Inventory.Used}/{GM.Inventory.Capacity}";
                for (int i = 0; i < rows.Length; i++)
                    counts[i].text = $"{GM.State.collectedByType[i],2}                   {GM.State.sortedByType[i],2}";
            };
            _refresh();
        }

        Text ImpactCard(string title, float x)
        {
            var card = UIFactory.Panel(_body, title, UIColors.PanelLight);
            UIFactory.Place(card.rectTransform, TopLeft, TopLeft, new Vector2(x, -60), new Vector2(320, 122));
            Line(card.transform, title, 23, UIColors.TextDim, 18, 13, 284, 34);
            return Line(card.transform, "", 34, UIColors.Yellow, 18, 52, 284, 50);
        }
    }
}

using UnityEngine.UI;

namespace RecolecEco
{
    public partial class SaveData
    {
        public int claimedJournalContracts;
    }

    public sealed partial class FieldJournal
    {
        const int ContractCount = 3;
        static readonly string[] ContractTitles =
            { "Campaña de envases", "Redes fuera del agua", "Bahía sin pendientes" };
        static readonly string[] ContractGoals =
            { "Clasifica 2 botellas y 2 latas.", "Clasifica 3 redes de pesca.", "Retira todos los residuos del Sector A." };
        static readonly int[] ContractRewards = { 12, 18, 20 };

        partial void AddContract() => AddTab("Encargos", ShowContracts);

        void ShowContracts()
        {
            ClearPage();
            Line(_body, "Encargos opcionales de Doña Rosa. Cada premio se cobra una sola vez.", 24,
                UIColors.TextDim, 0, 0, 970, 46);
            var progress = new Text[ContractCount];
            var claim = new Button[ContractCount];
            for (int i = 0; i < ContractCount; i++)
            {
                int id = i;
                int y = 66 + i * 135;
                Line(_body, ContractTitles[i], 30, UIColors.Text, 0, y, 680, 40);
                Line(_body, ContractGoals[i], 24, UIColors.TextDim, 0, y + 42, 695, 35);
                progress[i] = Line(_body, "", 22, UIColors.Turquoise, 0, y + 78, 695, 35);
                claim[i] = ActionButton(_body, $"Cobrar {ContractRewards[i]} EC",
                    () => ClaimContract(id), 725, y + 16, 237, 58);
            }
            _refresh = () =>
            {
                for (int i = 0; i < ContractCount; i++)
                {
                    bool claimed = ContractWasClaimed(i);
                    bool complete = ContractComplete(i);
                    progress[i].text = claimed ? "Premio cobrado" : complete ? "Listo para cobrar" : ContractProgress(i);
                    claim[i].interactable = complete && !claimed;
                }
            };
            _refresh();
        }

        bool ContractWasClaimed(int id) => (GM.State.claimedJournalContracts & (1 << id)) != 0;
        int Sorted(WasteType type) => GM.State.sortedByType[(int)type];
        int SectorTotalA => GM.Quests.SectorTotal(SectorId.A);

        bool ContractComplete(int id)
        {
            switch (id)
            {
                case 0: return Sorted(WasteType.Botella) >= 2 && Sorted(WasteType.Lata) >= 2;
                case 1: return Sorted(WasteType.Red) >= 3;
                default: return SectorTotalA > 0 && GM.Quests.SectorCollected(SectorId.A) >= SectorTotalA;
            }
        }

        string ContractProgress(int id)
        {
            switch (id)
            {
                case 0: return $"Botellas {Sorted(WasteType.Botella)}/2 · Latas {Sorted(WasteType.Lata)}/2";
                case 1: return $"Redes {Sorted(WasteType.Red)}/3";
                default: return $"Sector A · {GM.Quests.SectorCollected(SectorId.A)}/{SectorTotalA}";
            }
        }

        void ClaimContract(int id)
        {
            if (ContractWasClaimed(id) || !ContractComplete(id)) return;
            GM.State.claimedJournalContracts |= 1 << id;
            GM.AddCredits(ContractRewards[id]);
            GM.Autosave("premio del encargo");
            _refresh?.Invoke();
        }
    }
}

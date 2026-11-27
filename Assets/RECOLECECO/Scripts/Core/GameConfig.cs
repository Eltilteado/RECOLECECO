using System;
using UnityEngine;

namespace RecolecEco
{
    public enum WasteType { Botella = 0, Lata = 1, Red = 2, CajaCarton = 3, CajaTecnopor = 4 }
    public enum BinType { Plastico = 0, Metal = 1, RedesTextiles = 2, OtrosReciclables = 3, Disposicion = 4 }
    public enum SectorId { A = 0, B = 1, C = 2 }
    public enum UpgradeType { Motor = 0, Herramienta = 1, Bodega = 2 }

    [Serializable]
    public class WasteInfo
    {
        public WasteType type;
        public string displayName;
        public BinType correctBin;
        public int value;
        public bool recyclable;
        public Color iconColor = Color.white;
        [TextArea] public string tip;
    }

    [Serializable]
    public class BinInfo
    {
        public BinType type;
        public string displayName;
        public string subtitle;
        public Color color = Color.white;
        public Color textColor = Color.black;
    }

    [Serializable]
    public class UpgradeInfo
    {
        public UpgradeType type;
        public string displayName;
        public int cost;
        public string effectText;
        [TextArea] public string description;
    }

    [Serializable]
    public class SectorWasteCounts
    {
        public SectorId sector;
        public string displayName;
        public int botellas, latas, redes, cajasCarton, cajasTecnopor;
        public int Total => botellas + latas + redes + cajasCarton + cajasTecnopor;
    }

    /// <summary>Todos los valores ajustables del juego (requisito §8: datos en ScriptableObjects).</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "RECOLECECO/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Navegación (§4.1)")]
        public float maxSpeed = 6f;
        public float maxSpeedUpgraded = 8f;
        public float accelTime = 2f;
        public float reverseFactor = 0.4f;
        public float turnSpeedDeg = 60f;
        [Range(0, 1)] public float minTurnFactor = 0.3f;
        [Range(0, 1)] public float bounceFactor = 0.35f;

        [Header("Recolección e inventario (§4.2, §4.3)")]
        public float reach = 3f;
        public float reachUpgraded = 5f;
        public int capacity = 10;
        public int capacityUpgraded = 18;

        [Header("Residuos y reciclaje (§4.4)")]
        public WasteInfo[] wastes;
        public BinInfo[] bins;

        [Header("Mejoras (§4.5)")]
        public UpgradeInfo[] upgrades;

        [Header("Distribución de residuos (§3)")]
        public SectorWasteCounts[] sectors;

        [Header("Misiones (§4.6)")]
        public int mission2Target = 6;
        public int mission3Target = 6;
        [Range(0, 1)] public float mission5Fraction = 0.8f;
        public float mission1Radius = 12f;

        [Header("Recuperación ambiental (§4.7)")]
        public int[] milestones = { 25, 50, 75, 100 };

        public WasteInfo GetWaste(WasteType t)
        {
            foreach (var w in wastes) if (w.type == t) return w;
            return null;
        }

        public BinInfo GetBin(BinType t)
        {
            foreach (var b in bins) if (b.type == t) return b;
            return null;
        }

        public UpgradeInfo GetUpgrade(UpgradeType t)
        {
            foreach (var u in upgrades) if (u.type == t) return u;
            return null;
        }

        public SectorWasteCounts GetSector(SectorId s)
        {
            foreach (var c in sectors) if (c.sector == s) return c;
            return null;
        }

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }

        /// <summary>Valores de partida del documento de requisitos.</summary>
        public void ResetToDefaults()
        {
            maxSpeed = 6f; maxSpeedUpgraded = 8f; accelTime = 2f; reverseFactor = 0.4f;
            turnSpeedDeg = 60f; minTurnFactor = 0.3f; bounceFactor = 0.35f;
            reach = 3f; reachUpgraded = 5f; capacity = 10; capacityUpgraded = 18;
            mission2Target = 6; mission3Target = 6; mission5Fraction = 0.8f; mission1Radius = 12f;
            milestones = new[] { 25, 50, 75, 100 };

            wastes = new[]
            {
                new WasteInfo { type = WasteType.Botella, displayName = "Botella de plástico", correctBin = BinType.Plastico, value = 5, recyclable = true, iconColor = Hex("#7FD3F0"),
                    tip = "Las botellas PET se lavan, se trituran y se vuelven fibra o nuevas botellas. ¡Una botella en el mar puede durar siglos!" },
                new WasteInfo { type = WasteType.Lata, displayName = "Lata de metal", correctBin = BinType.Metal, value = 8, recyclable = true, iconColor = Hex("#E35D5B"),
                    tip = "El aluminio se recicla una y otra vez sin perder calidad. Reciclar una lata ahorra mucha energía." },
                new WasteInfo { type = WasteType.Red, displayName = "Red de pesca", correctBin = BinType.RedesTextiles, value = 12, recyclable = true, iconColor = Hex("#3FA07A"),
                    tip = "Las redes abandonadas atrapan tortugas, aves y lobos marinos. Recicladas se convierten en hilo, alfombras o tablas." },
                new WasteInfo { type = WasteType.CajaCarton, displayName = "Caja de cartón o madera", correctBin = BinType.OtrosReciclables, value = 6, recyclable = true, iconColor = Hex("#C9955A"),
                    tip = "Va al contenedor «Otros reciclables»: el cartón y la madera limpios se aprovechan para hacer papel, compost o tableros." },
                new WasteInfo { type = WasteType.CajaTecnopor, displayName = "Caja de tecnopor", correctBin = BinType.Disposicion, value = 2, recyclable = false, iconColor = Hex("#F2F2EE"),
                    tip = "El tecnopor (poliestireno) que viene del mar no se puede reciclar aquí: va a disposición adecuada para que no vuelva al agua. ¡Mejor usar envases reutilizables!" },
            };

            bins = new[]
            {
                new BinInfo { type = BinType.Plastico, displayName = "Plástico", subtitle = "Botellas PET", color = Hex("#F4F4F4"), textColor = Hex("#123047") },
                new BinInfo { type = BinType.Metal, displayName = "Metal", subtitle = "Latas", color = Hex("#F2C230"), textColor = Hex("#123047") },
                new BinInfo { type = BinType.RedesTextiles, displayName = "Redes y textiles", subtitle = "Redes, cabos", color = Hex("#2BB3A3"), textColor = Color.white },
                new BinInfo { type = BinType.OtrosReciclables, displayName = "Otros reciclables", subtitle = "Cartón, madera", color = Hex("#2F78C4"), textColor = Color.white },
                new BinInfo { type = BinType.Disposicion, displayName = "Disposición adecuada", subtitle = "NO reciclable", color = Hex("#2B2B2B"), textColor = Color.white },
            };

            upgrades = new[]
            {
                new UpgradeInfo { type = UpgradeType.Motor, displayName = "Motor mejorado", cost = 50, effectText = "Velocidad máxima: 6 → 8 m/s",
                    description = "Un motor fuera de borda más potente para cruzar la bahía en menos tiempo." },
                new UpgradeInfo { type = UpgradeType.Herramienta, displayName = "Herramienta mejorada", cost = 70, effectText = "Alcance de la red: 3 → 5 m",
                    description = "Brazo más largo y red más grande: recoges residuos desde más lejos." },
                new UpgradeInfo { type = UpgradeType.Bodega, displayName = "Bodega ampliada", cost = 100, effectText = "Capacidad: 10 → 18",
                    description = "Una caja de bodega extra en cubierta para traer más residuos por viaje." },
            };

            sectors = new[]
            {
                new SectorWasteCounts { sector = SectorId.A, displayName = "Sector A · Cerca del muelle", botellas = 6, latas = 5, redes = 0, cajasCarton = 1, cajasTecnopor = 0 },
                new SectorWasteCounts { sector = SectorId.B, displayName = "Sector B · Playa rocosa", botellas = 5, latas = 4, redes = 4, cajasCarton = 3, cajasTecnopor = 2 },
                new SectorWasteCounts { sector = SectorId.C, displayName = "Sector C · Islote de aves", botellas = 6, latas = 5, redes = 5, cajasCarton = 2, cajasTecnopor = 2 },
            };
        }

        static GameConfig _cached;
        public static GameConfig Load()
        {
            if (_cached == null) _cached = Resources.Load<GameConfig>("GameConfig");
            if (_cached == null)
            {
                Debug.LogWarning("GameConfig no encontrado en Resources; usando valores por defecto.");
                _cached = CreateInstance<GameConfig>();
                _cached.ResetToDefaults();
            }
            return _cached;
        }
    }
}

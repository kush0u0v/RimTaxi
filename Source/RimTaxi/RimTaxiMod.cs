using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Mod entry point. Phase 0: load settings + apply Harmony patches.
    /// </summary>
    public class RimTaxiMod : Mod
    {
        public const string PackageId = "kush.rimtaxi";
        public const string HarmonyId = "kush.rimtaxi";

        public static RimTaxiMod Instance { get; private set; }
        public static TaxiSettings Settings { get; private set; }

        public RimTaxiMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<TaxiSettings>();

            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll();

            Log.Message("[RimTaxi] Loaded (world-map caravan drop forced on arrival).");
        }

        public override string SettingsCategory()
        {
            return "RimTaxi_SettingsTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}

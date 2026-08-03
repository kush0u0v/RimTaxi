using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimTaxi
{
    /// <summary>
    /// Virtual "taxi company" on the comms console contact list (same pattern as factions / passing ships).
    /// Not a real Faction — no goodwill or diplomacy.
    /// </summary>
    public class TaxiCommsContact : ICommunicable
    {
        public static readonly TaxiCommsContact Instance = new TaxiCommsContact();

        private TaxiCommsContact()
        {
        }

        public string GetCallLabel()
        {
            return "RimTaxi_CommsCallLabel".Translate();
        }

        public string GetInfoText()
        {
            return "RimTaxi_CommsInfoText".Translate(
                TaxiFareCalculator.CallFee,
                TaxiFareCalculator.FarePerKgPerTile.ToString("0.00"));
        }

        public Faction GetFaction()
        {
            // Not a real faction contact
            return null;
        }

        public void TryOpenComms(Pawn negotiator)
        {
            if (negotiator == null)
            {
                return;
            }

            DiaNode root = TaxiDialogMaker.RootNode(negotiator);
            Dialog_Negotiation dialog = new Dialog_Negotiation(negotiator, this, root, radioMode: true)
            {
                soundAmbient = SoundDefOf.RadioComms_Ambience
            };
            Find.WindowStack.Add(dialog);
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator)
        {
            if (console == null || negotiator == null)
            {
                return null;
            }

            string label = "CallOnRadio".Translate(GetCallLabel());
            string blocked = TaxiCallService.GetBlockedReason(console.Map, console);

            if (blocked != null)
            {
                return new FloatMenuOption(label + " (" + blocked + ")", null);
            }

            if (!negotiator.CanReach(console, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    label,
                    () => console.GiveUseCommsJob(negotiator, this),
                    MenuOptionPriority.Default),
                negotiator,
                console);
        }
    }
}

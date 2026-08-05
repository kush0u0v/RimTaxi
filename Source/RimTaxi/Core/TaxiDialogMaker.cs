using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Radio-style negotiation tree for RimTaxi (comms GUI).
    /// After request: pick where to send the taxi (pickup) inside the dialog.
    /// </summary>
    public static class TaxiDialogMaker
    {
        public static DiaNode RootNode(Pawn negotiator)
        {
            Map map = negotiator?.Map;
            int fee = TaxiFareCalculator.CallFee;
            string rate = TaxiFareCalculator.FarePerKgPerTile.ToString("0.00");

            string greeting = "RimTaxi_DialogGreeting".Translate(fee, rate);
            DiaNode root = new DiaNode(greeting);

            // Request taxi → stay in radio GUI to pick where to send it
            DiaOption call = new DiaOption("RimTaxi_DialogRequestTaxi".Translate(fee));
            string blocked = TaxiCallService.GetBlockedReason(map, console: FindConsole(map));
            if (blocked != null)
            {
                call.Disable(blocked);
            }
            else
            {
                call.linkLateBind = () => PickupLocationNode(negotiator, map);
            }

            root.options.Add(call);

            DiaOption status = new DiaOption("RimTaxi_DialogStatus".Translate());
            status.link = StatusNode(negotiator);
            root.options.Add(status);

            DiaOption bye = new DiaOption("RimTaxi_DialogHangUp".Translate());
            bye.resolveTree = true;
            root.options.Add(bye);

            return root;
        }

        /// <summary>
        /// Comms GUI step: choose where to send the taxi (pickup site).
        /// </summary>
        public static DiaNode PickupLocationNode(Pawn negotiator, Map callMap)
        {
            int fee = TaxiFareCalculator.CallFee;
            DiaNode node = new DiaNode("RimTaxi_DialogPickPickup".Translate(fee));

            List<TaxiPickupSite> sites = TaxiPickupSite.GetAll(callMap);
            TaxiGameComponent gc = TaxiGameComponent.Get();

            // World map pick
            DiaOption worldPick = new DiaOption("RimTaxi_PickupFromWorldMap".Translate());
            worldPick.action = () =>
            {
                TaxiCallService.BeginPickupWorldTargeting(callMap, negotiator);
            };
            worldPick.resolveTree = true;
            node.options.Add(worldPick);

            int listed = 0;
            for (int i = 0; i < sites.Count; i++)
            {
                TaxiPickupSite site = sites[i];
                string siteBlocked = TaxiCallService.GetPickupSiteBlockedReason(site, gc);
                string label = site.label;
                if (siteBlocked != null)
                {
                    DiaOption disabled = new DiaOption(label + " — " + siteBlocked);
                    disabled.Disable(siteBlocked);
                    node.options.Add(disabled);
                    listed++;
                    continue;
                }

                TaxiPickupSite captured = site;
                DiaOption opt = new DiaOption(label);
                opt.action = () =>
                {
                    TaxiCallService.BeginPickupFlow(callMap, negotiator, captured);
                };
                opt.resolveTree = true;
                node.options.Add(opt);
                listed++;
            }

            if (listed == 0)
            {
                DiaOption none = new DiaOption("RimTaxi_NoListedPickupsHint".Translate());
                none.Disable("RimTaxi_NoListedPickupsHint".Translate());
                node.options.Add(none);
            }

            DiaOption back = new DiaOption("GoBack".Translate());
            back.linkLateBind = () => RootNode(negotiator);
            node.options.Add(back);

            DiaOption hang = new DiaOption("RimTaxi_DialogHangUp".Translate());
            hang.resolveTree = true;
            node.options.Add(hang);

            return node;
        }

        private static DiaNode StatusNode(Pawn negotiator)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RimTaxi_DialogStatusHeader".Translate());

            TaxiGameComponent gc = TaxiGameComponent.Get();
            bool any = false;

            if (gc != null)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map m = maps[i];
                    TaxiPendingDispatch p = gc.GetPendingDispatch(m);
                    if (p == null)
                    {
                        continue;
                    }

                    any = true;
                    string name = m.Parent?.LabelCap ?? m.ToString();
                    sb.AppendLine(" - " + "RimTaxi_DialogStatusEnRouteMap".Translate(
                        name,
                        p.TicksRemaining.ToStringTicksToPeriod()));
                }

                List<Caravan> caravans = Find.WorldObjects?.Caravans;
                if (caravans != null)
                {
                    for (int i = 0; i < caravans.Count; i++)
                    {
                        Caravan c = caravans[i];
                        if (c == null || !c.IsPlayerControlled)
                        {
                            continue;
                        }

                        TaxiPendingDispatch p = gc.GetPendingDispatch(c);
                        if (p != null)
                        {
                            any = true;
                            sb.AppendLine(" - " + "RimTaxi_DialogStatusEnRouteCaravan".Translate(
                                c.Name ?? c.LabelCap,
                                p.TicksRemaining.ToStringTicksToPeriod()));
                        }

                        TaxiCaravanBoarding b = gc.GetBoarding(c);
                        if (b != null)
                        {
                            any = true;
                            string dest = b.HasDestination
                                ? "RimTaxi_DialogStatusBoardedDest".Translate(b.tripDistance)
                                : "RimTaxi_DialogStatusBoardedNoDest".Translate();
                            sb.AppendLine(" - " + "RimTaxi_DialogStatusReadyCaravan".Translate(
                                c.Name ?? c.LabelCap,
                                dest,
                                b.WaitTicksRemaining.ToStringTicksToPeriod()));
                        }
                    }
                }

                if (gc.OnCooldown)
                {
                    any = true;
                    sb.AppendLine(" - " + "RimTaxi_OnCooldown".Translate(gc.CooldownTicksRemaining.ToStringTicksToPeriod()));
                }
            }

            if (!any)
            {
                sb.AppendLine("RimTaxi_DialogStatusNone".Translate());
            }

            DiaNode node = new DiaNode(sb.ToString().TrimEnd());
            DiaOption back = new DiaOption("GoBack".Translate());
            back.linkLateBind = () => RootNode(negotiator);
            node.options.Add(back);

            DiaOption hang = new DiaOption("RimTaxi_DialogHangUp".Translate());
            hang.resolveTree = true;
            node.options.Add(hang);

            return node;
        }

        private static Building_CommsConsole FindConsole(Map map)
        {
            if (map == null)
            {
                return null;
            }

            List<Thing> list = map.listerThings?.ThingsOfDef(ThingDefOf.CommsConsole);
            if (list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Building_CommsConsole c && c.Spawned && c.Faction == Faction.OfPlayer)
                {
                    return c;
                }
            }

            return null;
        }
    }
}

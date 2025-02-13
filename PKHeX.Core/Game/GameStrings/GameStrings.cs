﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core
{
    public class GameStrings : IBasicStrings
    {
        // PKM Info
        public readonly string[] specieslist, movelist, itemlist, abilitylist, types, natures, forms,
            memories, genloc, trainingbags, trainingstage, characteristics,
            encountertypelist, balllist, gamelist, pokeblocks, ribbons;

        private readonly string[] g4items, g3coloitems, g3xditems, g3items, g2items, g1items;

        // Met Locations
        public readonly string[] metGSC_00000, metRSEFRLG_00000, metCXD_00000;
        public readonly string[] metHGSS_00000, metHGSS_02000, metHGSS_03000;
        public readonly string[] metBW2_00000, metBW2_30000, metBW2_40000, metBW2_60000;
        public readonly string[] metXY_00000, metXY_30000, metXY_40000, metXY_60000;
        public readonly string[] metSM_00000, metSM_30000, metSM_40000, metSM_60000;
        public readonly string[] metGG_00000, metGG_30000, metGG_40000, metGG_60000;

        // Misc
        public readonly string[] wallpapernames, puffs;
        private readonly string lang;
        private readonly int LanguageIndex;

        public string EggName { get; }
        public IReadOnlyList<string> Species => specieslist;
        public IReadOnlyList<string> Item => itemlist;
        public IReadOnlyList<string> Move => movelist;
        public IReadOnlyList<string> Ability => abilitylist;
        public IReadOnlyList<string> Types => types;
        public IReadOnlyList<string> Natures => natures;

        private string[] Get(string ident) => GameLanguage.GetStrings(ident, lang);
        private const string NPC = "NPC";
        private static readonly string[] LanguageNames = GameDataSource.LanguageList.GetArray();

        public GameStrings(string l)
        {
            lang = l;
            LanguageIndex = GameLanguage.GetLanguageIndex(l);
            ribbons = Get("ribbons");
            // Past Generation strings
            g3items = Get("ItemsG3");
            // XD and Colosseum
            {
                g3coloitems = (string[])g3items.Clone();
                string[] tmp = Get("ItemsG3Colosseum");
                Array.Resize(ref g3coloitems, 500 + tmp.Length);
                for (int i = g3items.Length; i < g3coloitems.Length; i++)
                    g3coloitems[i] = $"UNUSED {i}";
                tmp.CopyTo(g3coloitems, g3coloitems.Length - tmp.Length);

                g3xditems = (string[])g3items.Clone();
                string[] tmp2 = Get("ItemsG3XD");
                Array.Resize(ref g3xditems, 500 + tmp2.Length);
                for (int i = g3items.Length; i < g3xditems.Length; i++)
                    g3xditems[i] = $"UNUSED {i}";
                tmp2.CopyTo(g3xditems, g3xditems.Length - tmp2.Length);
            }
            g2items = Get("ItemsG2");
            g1items = Get("ItemsG1");
            metRSEFRLG_00000 = Get("rsefrlg_00000");
            metGSC_00000 = Get("gsc_00000");

            metCXD_00000 = Get("cxd_00000");
            metCXD_00000 = SanitizeMetStringsCXD(metCXD_00000);

            // Current Generation strings
            natures = Util.GetNaturesList(l);
            types = Get("types");
            abilitylist = Get("abilities");

            movelist = Get("moves");
            string[] ps = { "P", "S" }; // Distinguish Physical/Special
            for (int i = 622; i < 658; i++)
                movelist[i] += $" ({ps[i % 2]})";

            itemlist = Get("items");
            characteristics = Get("character");
            specieslist = Get("species");
            wallpapernames = Get("wallpaper");
            encountertypelist = Get("encountertype");
            gamelist = Get("games");

            balllist = new string[Legal.Items_Ball.Length];
            for (int i = 0; i < balllist.Length; i++)
                balllist[i] = itemlist[Legal.Items_Ball[i]];

            pokeblocks = Get("pokeblock");
            forms = Get("forms");
            memories = Get("memories");
            genloc = Get("genloc");
            trainingbags = Get("trainingbag");
            trainingstage = Get("supertraining");
            puffs = Get("puff");
            Array.Resize(ref puffs, puffs.Length + 1); // shift all down, 0th will be 'none' -- applied later
            Array.Copy(puffs, 0, puffs, 1, puffs.Length - 1);

            EggName = specieslist[0];
            metHGSS_00000 = Get("hgss_00000");
            metHGSS_02000 = Get("hgss_02000");
            metHGSS_03000 = Get("hgss_03000");
            metBW2_00000 = Get("bw2_00000");
            metBW2_30000 = Get("bw2_30000");
            metBW2_40000 = Get("bw2_40000");
            metBW2_60000 = Get("bw2_60000");
            metXY_00000 = Get("xy_00000");
            metXY_30000 = Get("xy_30000");
            metXY_40000 = Get("xy_40000");
            metXY_60000 = Get("xy_60000");
            metSM_00000 = Get("sm_00000");
            metSM_30000 = Get("sm_30000");
            metSM_40000 = Get("sm_40000");
            metSM_60000 = Get("sm_60000");

            metGG_00000 = Get("gg_00000");
            metGG_30000 = metSM_30000;
            metGG_40000 = Get("gg_40000");
            metGG_60000 = metSM_60000;

            Sanitize();

            g4items = (string[])itemlist.Clone();
            Get("mail4").CopyTo(g4items, 137);
        }

        private static string[] SanitizeMetStringsCXD(string[] cxd)
        {
            // Mark duplicate locations with their index
            var metSanitize = (string[])cxd.Clone();
            for (int i = 0; i < metSanitize.Length; i++)
            {
                if (cxd.Count(z => z == metSanitize[i]) > 1)
                    metSanitize[i] += $" [{i:000}]";
            }

            return metSanitize;
        }

        private void Sanitize()
        {
            SanitizeItemNames();
            SanitizeMetLocations();

            // Replace the Egg Name with ---; egg name already stored to eggname
            specieslist[0] = "---";
            // Fix (None) tags
            var none = $"({itemlist[0]})";
            abilitylist[0] = itemlist[0] = movelist[0] = metXY_00000[0] = metBW2_00000[0] = metHGSS_00000[0] = metCXD_00000[0] = puffs[0] = none;
        }

        private void SanitizeItemNames()
        {
            // Fix Item Names (Duplicate entries)
            var HM06 = itemlist[425];
            var HM0 = HM06.Substring(0, HM06.Length - 1); // language ambiguous!
            itemlist[426] = $"{HM0}7 (G4)";
            itemlist[427] = $"{HM0}8 (G4)";
            itemlist[456] += " (HG/SS)"; // S.S. Ticket
            itemlist[736] += " (OR/AS)"; // S.S. Ticket
            itemlist[463] += " (DPPt)"; // Storage Key
            itemlist[734] += " (OR/AS)"; // Storage Key
            itemlist[476] += " (HG/SS)"; // Basement Key
            itemlist[723] += " (OR/AS)"; // Basement Key
            itemlist[621] += " (M)"; // Xtransceiver
            itemlist[626] += " (F)"; // Xtransceiver
            itemlist[629] += " (2)"; // DNA Splicers
            itemlist[637] += " (2)"; // Dropped Item
            itemlist[707] += " (2)"; // Travel Trunk
            itemlist[713] += " (2)"; // Alt Bike
            itemlist[714] += " (2)"; // Holo Caster
            itemlist[729] += " (1)"; // Meteorite
            itemlist[740] += " (2)"; // Contest Costume
            itemlist[751] += " (2)"; // Meteorite
            itemlist[771] += " (3)"; // Meteorite
            itemlist[772] += " (4)"; // Meteorite
            itemlist[842] += " (SM)"; // Fishing Rod
            itemlist[945] += " (2)"; // Used Solarizer
            itemlist[946] += " (2)"; // Used Lunarizer

            itemlist[873] += " (GP/GE)"; // S.S. Ticket
            itemlist[459] += " (HG/SS)"; // Parcel
            itemlist[467] += " (Pt)"; // Secret Key
            itemlist[475] += " (HG/SS)"; // Card Key
            itemlist[894] += " (GP)"; // Leaf Letter
            itemlist[895] += " (GE)"; // Leaf Letter

            // some languages have same names for other items!
            itemlist[878] += " (GP/GE)"; // Lift Key (Elevator Key=700)
            itemlist[479] += " (HG/SS)"; // Lost Item (Dropped Item=636)

            // Append Z-Crystal flagging
            foreach (var i in Legal.Pouch_ZCrystal_USUM)
                itemlist[i] += " [Z]";

            for (int i = 12; i <= 29; i++) // Differentiate DNA Samples
                g3coloitems[500 + i] += $" ({i - 11:00})";
            // differentiate G3 Card Key from Colo
            g3coloitems[500 + 10] += " (COLO)";
        }

        private void SanitizeMetLocations()
        {
            // Fix up some of the Location strings to make them more descriptive
            SanitizeMetG4HGSS();
            SanitizeMetG5BW();
            SanitizeMetG6XY();
            SanitizeMetG7SM();

            if (lang == "es" || lang == "it")
            {
                // Campeonato Mundial duplicates
                for (int i = 27; i < 34; i++)
                    metXY_40000[i] += " (-)";

                // Evento de Videojuegos -- first as duplicate
                metXY_40000[34] += " (-)";
                metSM_40000[37] += " (-)";
                metGG_40000[26] += " (-)";
            }

            if (lang == "ko")
            {
                // Pokémon Ranger duplicate (should be Ranger Union)
                metBW2_40000[70] += " (-)";
            }
        }

        private void SanitizeMetG4HGSS()
        {
            metHGSS_00000[054] += " (DP/Pt)"; // Victory Road
            metHGSS_00000[221] += " (HG/SS)"; // Victory Road

            // German language duplicate; handle for all since it can be confused.
            metHGSS_00000[104] += " (DP/Pt)"; // Vista Lighthouse
            metHGSS_00000[212] += " (HG/SS)"; // Lighthouse

            metHGSS_02000[1] += $" ({NPC})";     // Anything from an NPC
            metHGSS_02000[2] += $" ({EggName})"; // Egg From Link Trade
        }

        private void SanitizeMetG5BW()
        {
            metBW2_00000[36] = $"{metBW2_00000[84]}/{metBW2_00000[36]}"; // Cold Storage in BW = PWT in BW2
            metBW2_00000[40] += "(B/W)"; // Victory Road in BW
            metBW2_00000[134] += "(B2/W2)"; // Victory Road in B2W2
            // BW2 Entries from 76 to 105 are for Entralink in BW
            for (int i = 76; i < 106; i++)
                metBW2_00000[i] += "●";

            // Collision between 40002 (legal) and 00002 (illegal) "Faraway place"
            if (metBW2_00000[2] == metBW2_40000[2 - 1])
                metBW2_00000[2] += " (2)";

            for (int i = 96; i < 108; i++)
                metBW2_40000[i] += $" ({i - 96})";

            // Localize the Poketransfer to the language (30001)
            metBW2_30000[1 - 1] = GameLanguage.GetTransporterName(LanguageIndex);
            metBW2_30000[2 - 1] += $" ({NPC})";             // Anything from an NPC
            metBW2_30000[3 - 1] += $" ({EggName})";         // Link Trade (Egg)

            // Zorua/Zoroark events
            metBW2_30000[10 - 1] = $"{specieslist[251]} ({specieslist[570]} 1)"; // Celebi's Zorua Event
            metBW2_30000[11 - 1] = $"{specieslist[251]} ({specieslist[570]} 2)"; // Celebi's Zorua Event
            metBW2_30000[12 - 1] = $"{specieslist[571]} (1)"; // Zoroark
            metBW2_30000[13 - 1] = $"{specieslist[571]} (2)"; // Zoroark

            metBW2_60000[3 - 1] += $" ({EggName})";  // Egg Treasure Hunter/Breeder, whatever...
        }

        private void SanitizeMetG6XY()
        {
            metXY_00000[104] += " (X/Y)";      // Victory Road
            metXY_00000[106] += " (X/Y)";      // Pokémon League
            metXY_00000[202] += " (OR/AS)";    // Pokémon League
            metXY_00000[298] += " (OR/AS)";    // Victory Road
            metXY_30000[0] += $" ({NPC})";     // Anything from an NPC
            metXY_30000[1] += $" ({EggName})"; // Egg From Link Trade

            for (int i = 62; i < 69; i++)
                metXY_40000[i] += $" ({i - 61})";
        }

        private void SanitizeMetG7SM()
        {
            // Sun/Moon duplicates -- elaborate!
            var metSM_00000_good = (string[])metSM_00000.Clone();
            for (int i = 0; i < metSM_00000.Length; i += 2)
            {
                var nextLoc = metSM_00000[i + 1];
                if (!string.IsNullOrWhiteSpace(nextLoc) && nextLoc[0] != '[')
                    metSM_00000_good[i] += $" ({nextLoc})";
                if (i > 0 && !string.IsNullOrWhiteSpace(metSM_00000_good[i]) && metSM_00000_good.Take(i - 1).Contains(metSM_00000_good[i]))
                    metSM_00000_good[i] += $" ({metSM_00000_good.Take(i - 1).Count(s => s == metSM_00000_good[i]) + 1})";
            }
            Array.Copy(metSM_00000, 194, metSM_00000_good, 194, 4); // Restore Island Names (unused)
            metSM_00000_good.CopyTo(metSM_00000, 0);

            metSM_30000[0] += $" ({NPC})";      // Anything from an NPC
            metSM_30000[1] += $" ({EggName})";  // Egg From Link Trade
            for (int i = 2; i <= 5; i++) // distinguish first set of regions (unused) from second (used)
                metSM_30000[i] += " (-)";

            for (int i = 58; i < 65; i++) // distinguish Event year duplicates
                metSM_40000[i] += " (-)";

            for (int i = 47; i < 54; i++) // distinguish Event year duplicates
                metGG_40000[i] += " (-)";
        }

        public IReadOnlyList<string> GetItemStrings(int generation, GameVersion game = GameVersion.Any)
        {
            switch (generation)
            {
                case 0: return Array.Empty<string>();
                case 1: return g1items;
                case 2: return g2items;
                case 3: return GetItemStrings3(game);
                case 4: return g4items; // mail names changed 4->5
                default: return itemlist;
            }
        }

        private string[] GetItemStrings3(GameVersion game)
        {
            switch (game)
            {
                case GameVersion.COLO:
                    return g3coloitems;
                case GameVersion.XD:
                    return g3xditems;
                default:
                    if (Legal.EReaderBerryIsEnigma)
                        return g3items;

                    var g3itemsEBerry = (string[])g3items.Clone();
                    g3itemsEBerry[175] = Legal.EReaderBerryDisplayName;
                    return g3itemsEBerry;
            }
        }

        /// <summary>
        /// Gets the location name for the specified parameters.
        /// </summary>
        /// <param name="eggmet">Location is from the <see cref="PKM.Egg_Location"/></param>
        /// <param name="locval">Location value</param>
        /// <param name="format">Current <see cref="PKM.Format"/></param>
        /// <param name="generation"><see cref="PKM.GenNumber"/> of origin</param>
        /// <param name="version">Current GameVersion (only applicable for <see cref="GameVersion.GG"/> differentiation)</param>
        /// <returns>Location name</returns>
        public string GetLocationName(bool eggmet, int locval, int format, int generation, GameVersion version)
        {
            int gen = -1;
            int bankID = 0;

            if (format == 2)
            {
                gen = 2;
            }
            else if (format == 3)
            {
                gen = 3;
            }
            else if (generation == 4 && (eggmet || format == 4)) // 4
            {
                const int size = 1000;
                bankID = locval / size;
                gen = 4;
                locval %= size;
            }
            else // 5-7+
            {
                const int size = 10000;
                bankID = locval / size;

                int g = generation;
                if (g >= 5)
                    gen = g;
                else if (format >= 5)
                    gen = format;

                locval %= size;
                if (bankID >= 3) // 30000 and onwards don't use 0th index, shift down 1
                    locval--;
            }

            var bank = GetLocationNames(gen, bankID, version);
            if (bank.Count <= locval)
                return string.Empty;
            return bank[locval];
        }

        /// <summary>
        /// Gets the location names array for a specified generation.
        /// </summary>
        /// <param name="gen">Generation to get location names for.</param>
        /// <param name="bankID">BankID used to choose the text bank.</param>
        /// <param name="version">Version of origin</param>
        /// <returns>List of location names.</returns>
        public IReadOnlyList<string> GetLocationNames(int gen, int bankID, GameVersion version)
        {
            switch (gen)
            {
                case 2: return metGSC_00000;
                case 3:
                    return version == GameVersion.CXD ? metCXD_00000 : metRSEFRLG_00000;
                case 4: return GetLocationNames4(bankID);
                case 5: return GetLocationNames5(bankID);
                case 6: return GetLocationNames6(bankID);
                case 7:
                    if (GameVersion.GG.Contains(version))
                        return GetLocationNames7GG(bankID);
                    return GetLocationNames7(bankID);
                default:
                    return Array.Empty<string>();
            }
        }

        private IReadOnlyList<string> GetLocationNames4(int bankID)
        {
            switch (bankID)
            {
                case 0: return metHGSS_00000;
                case 2: return metHGSS_02000;
                case 3: return metHGSS_03000;
                default: return Array.Empty<string>();
            }
        }

        public IReadOnlyList<string> GetLocationNames5(int bankID)
        {
            switch (bankID)
            {
                case 0: return metBW2_00000;
                case 3: return metBW2_30000;
                case 4: return metBW2_40000;
                case 6: return metBW2_60000;
                default: return Array.Empty<string>();
            }
        }

        public IReadOnlyList<string> GetLocationNames6(int bankID)
        {
            switch (bankID)
            {
                case 0: return metXY_00000;
                case 3: return metXY_30000;
                case 4: return metXY_40000;
                case 6: return metXY_60000;
                default: return Array.Empty<string>();
            }
        }

        public IReadOnlyList<string> GetLocationNames7(int bankID)
        {
            switch (bankID)
            {
                case 0: return metSM_00000;
                case 3: return metSM_30000;
                case 4: return metSM_40000;
                case 6: return metSM_60000;
                default: return Array.Empty<string>();
            }
        }

        public IReadOnlyList<string> GetLocationNames7GG(int bankID)
        {
            switch (bankID)
            {
                case 0: return metGG_00000;
                case 3: return metGG_30000;
                case 4: return metGG_40000;
                case 6: return metGG_60000;
                default: return Array.Empty<string>();
            }
        }
    }
}
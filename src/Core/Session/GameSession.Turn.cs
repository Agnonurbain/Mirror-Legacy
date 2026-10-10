using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    // The yearly turn: the phases in order, and what each one resolves.
    public sealed partial class GameSession
    {
        /// <summary>Moves to the next phase and resolves it. Does nothing once the game is over.</summary>
        public void AdvancePhase()
        {
            if (Victory.IsOver || Challenges.Current != null) return; // a battle under way is fought to its end first

            if (Clock.Advance()) BeginYear();
            else ResolvePhase(Clock.Phase);

            Events.TriggerPhaseChanged(Clock.Phase);
        }

        /// <summary>Plays the rest of the current year, up to the Management phase of the next one.</summary>
        public void AdvanceYear()
        {
            int year = Clock.Year;
            while (Clock.Year == year && !Victory.IsOver)
                AdvancePhase();
        }

        private void ResolvePhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Events: // the Management phase is over
                    Tasks.ProcessYearlyTasks();
                    Arts.ProcessYear();               // the Immortal Arts practised this year (audit §2)
                    Alchemy.ProcessYear();            // a hostile power may poison the clan's pills (audit §2.7)
                    Factions.ProcessYearlyFactionAI();
                    Secrets.ProcessYear();            // those who know may talk (L2c.4b)
                    Plots.ProcessYear();              // the powers investigate, and strike what they suspect (D7)
                    Schemes.ProcessYear();            // the powers scheme for profit: ambushes on members away (L6a)
                    Captives.ProcessYear();           // the captives are interrogated, and may be executed
                    Treaties.ProcessYear();           // the treaties: tribute, trade, the allies close by — and betrayal (D7)
                    Politics.ProcessYear();           // the powers ally, feud, subjugate, band against the clan (2026-09-27)
                    Intrigues.ProcessYear();          // blackmail, theft, spies (2026-09-27)
                    Probes.ProcessYear();             // the powers probe each other and the clan (2026-09-27)
                    Watch.ProcessYear();              // the clan's memory of offences fades a little
                    Dealings.ProcessYear();           // a power with a pierced secret of the clan may expose it
                    Patrons.ProcessYear();            // the great partners' tribute, boons, and wrath
                    Wars.ProcessYear();               // open wars: battles, surrenders, coalitions against a hegemon, the clan's war
                    Challenges.ProcessYear();         // a rival's challenge left unanswered lapses: silence is a refusal
                    RandomEvents.TriggerYearlyEvent();
                    Marriages.ProcessAnnualMarriages(); // before Inheritance, so newlyweds can have children
                    DaoHunts.ProcessYear();             // a ripe Dao is prey (LORE.md §5.3.3): learnt, then struck, maybe foiled
                    Sponsorships.ProcessYear();        // a patron may offer an ascent method, with a design (LORE.md §11.10)
                    Rivals.ProcessYear();              // those who hinder an ascent (LORE.md §11.10)
                    Paths.ProcessYear();               // disciples serve and come home, defectors come (LORE.md §11.10)
                    ShardSense.ProcessYear();          // the mirror senses a shard near, through its seeds (2026-10-01)
                    Elders.ProcessYear();              // the powers' elders age, die and rise (the living world, 2026-10-01)
                    PowerEconomy.ProcessYear();        // their income, upkeep and growth toward what their elders lead (step B)
                    Lifecycle.ProcessYear();           // powers fall, gates and sects are founded, kingdoms rise, families rise (step C)
                    WorldFruitions.ProcessYear();      // holders pass and are reborn, lineages freed are raced for (step D)
                    Ancestors.ProcessYear();           // the clan's reborn ancestors regain their realms, or are harvested (R9)
                    Imperial.ProcessYear();            // the clan's sovereign cultivates by governing (R20)
                    Phenomena.ProcessYear();           // the weathers of the dead pass (L4c)
                    PowerSchemes.ProcessYear();        // the powers scheme against each other (the world's, 2026-10-03)
                    Rebirths.ProcessYear();            // the powers' ancestors come back (the world's, 2026-10-03)
                    Enthrallment.ProcessYear();        // a reborn True Monarch bends lesser minds (audit §1.8)
                    Marks.ProcessYear();               // a power recognizes its style in a stolen art (audit §3.9)
                    Arsenal.ProcessYear();             // the powers' treasures, Designations and artifacts (the world's, 2026-10-03)
                    ArtifactTrade.ProcessYear();       // loans of artifacts end (L4f)
                    Mandate.ProcessYear();             // a reign, a seclusion feed the images they embody (§5.4.3)
                    Dharma.ProcessYear();              // treasures condensed; a masterless Designation may strike (L4e)
                    Demons.ProcessYear();              // a demon let be ravages; an unanswered one goes to the Underworld (L4e)
                    Upkeep.PayUpkeep();                 // the year's income in, every member costs its upkeep; short, a poor year
                    break;
                case GamePhase.Breakthrough:
                    Breakthroughs.ProcessBreakthroughPhase();
                    PurpleMansion.ProcessBreakthroughPhase(); // the ascent's four trials and the retreats under way
                    Abilities.ProcessBreakthroughPhase();     // divine abilities condensed from the Dao Partners
                    GoldenCore.ProcessBreakthroughPhase();    // the false Left Hands pay their patrons, or fall
                    break;
                case GamePhase.Inheritance:
                    Clan.ProcessAnnualBirths(Upkeep.BirthFactor);
                    Clan.ExamineOrifices();
                    break;
            }
        }

        private void BeginYear()
        {
            Aging.AgeOneYear();
            Buildings.ApplyPassiveBonuses();
            Events.TriggerYearStarted(Clock.Year);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;
namespace MirrorChronicles.Tests.Session
{
    public class TmpProbe
    {
        [Test, Explicit, Category("Tmp")]
        public void Probe()
        {
            foreach (int seed in new[] { 4, 6 })
            {
                int printed = 0;
                BalanceRun.Play(Fixtures.Content, seed, 500, out _, autopilot: true, observe: s =>
                {
                    s.Events.OnYearStarted += _ =>
                    {
                        int xp = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
                        var m = s.Clan.LivingMembers.FirstOrDefault(x => x.Realm == CultivationRealm.PurpleMansion && x.CultivationXP >= xp);
                        if (m == null || printed++ % 20 != 0) return;
                        string f = m.FoundationId ?? m.DivineAbilities.FirstOrDefault();
                        string lineage = FoundationRef.Parse(f).FruitionId;
                        var fr = s.Context.Content.Fruitions.First(x => x.Id == lineage);
                        var known = fr.Abilities.Select(a => $"{lineage}:{a.Id}").Where(a => s.Knowledge.Knows(FactKind.Ability, a)).ToList();
                        TestContext.Progress.WriteLine($"P seed {seed} y{s.Clock.Year} {m.FullName} f={f} partners={s.Knowledge.Knows(FactKind.DaoPartners, f)} knownAbilities={known.Count} retreat={m.Retreat} sealed={m.ProgressionSealed} pursued={m.PursuedAbility} "
                            + $"stones={s.Resources.SpiritStones} upkeep={s.Upkeep.YearlyUpkeep} herbs={s.Resources.MedicinalHerbs} ores={s.Resources.SpiritualOres} mirror={s.Mirror.MirrorPower} garden={s.Buildings.GetBuilding(BuildingType.HerbGarden).Level} "
                            + $"holders={string.Join("/", known.Select(a => s.Factions.Factions.Count(p => p.Techniques.Any(t => s.Context.Content.Techniques.FirstOrDefault(x => x.ID == t)?.RequiredQiId is { } q && s.Techniques.FindQi(q)?.Foundation == a))))} "
                            + $"rel={string.Join(",", s.Factions.Factions.Select(p => p.RelationWithPlayer).OrderByDescending(r => r).Take(5))}");
                    };
                });
            }
        }
    }
}

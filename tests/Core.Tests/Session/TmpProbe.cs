using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Session;
using MirrorChronicles.Data;
using MirrorChronicles.Characters;
namespace MirrorChronicles.Tests.Session
{
    public class TmpProbe
    {
        [Test, Explicit, Category("Tmp")]
        public void Probe()
        {
            foreach (int seed in new[] { 3, 9 })
                BalanceRun.Play(Fixtures.Content, seed, 500, out var s, autopilot: true, observe: x =>
                {
                    x.Events.OnYearStarted += y =>
                    {
                        if (y % 50 != 1) return;
                        var ascent = x.Techniques.Known.Where(t => t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t) && t.RequiredQiId != null).ToList();
                        var found = x.Clan.LivingMembers.Where(m => m.Realm == CultivationRealm.Foundation).ToList();
                        var onAscent = found.Where(m => ascent.Any(t => t.ID == m.CultivationMethodId)).ToList();
                        TestContext.Progress.WriteLine($"P seed {seed} y{y} ascent methods [{string.Join(",", ascent.Select(t => t.ID + "/" + t.RequiredQiId))}] foundation {found.Count} onAscentMethod {onAscent.Count} peak {found.Count(m => m.RealmStage >= 4)} retreat {x.Clan.LivingMembers.Count(m => m.Retreat != Retreat.None)} shards {x.Mirror.RestoredFragments} mirror {x.Mirror.MirrorPower} qi {string.Join(",", ascent.Select(t => x.Resources.QiPortions(t.RequiredQiId)))} clanQi {string.Join(",", found.Select(m => m.QiId).Distinct())}");
                    };
                });
        }
    }
}

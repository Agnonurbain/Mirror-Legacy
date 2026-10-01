using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Session;
using MirrorChronicles.World;
namespace MirrorChronicles.Tests.Session
{
    public class TmpProbe
    {
        [Test, Explicit, Category("Tmp")]
        public void Probe()
        {
            BalanceRun.Play(Fixtures.Content, 27, 66, out var s, autopilot: true, observe: x =>
            {
                x.Events.OnMemberCaptured += (m, f) => TestContext.Progress.WriteLine($"P y{x.Clock.Year} {m.FullName} ({m.Realm}) captured by {f}; stones {x.Resources.SpiritStones} upkeep {x.Upkeep.YearlyUpkeep} ransom {SchemeRules.Ransom(m.Realm, x.Context.Content.Balance.Schemes)} members {x.Clan.LivingMembers.Count}");
                x.Events.OnCharacterDied += (c, cause) => { if (cause == MirrorChronicles.Data.DeathCause.Executed) TestContext.Progress.WriteLine($"P y{x.Clock.Year} {c.FullName} executed; stones {x.Resources.SpiritStones}"); };
            });
        }
    }
}

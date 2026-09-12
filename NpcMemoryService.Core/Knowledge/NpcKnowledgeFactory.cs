// Code written by Gabriel Mailhot, 11/09/2026.
// Gabriel's correction to the first sketch, and it fixes a real defect: this is a FACTORY, not a catalogue. A
// hand-kept inventory rots - cr.help described 70 commands of 259 for exactly that reason, and the cure was not
// keeping the list better, it was making the registry ENUMERABLE and checking completeness against it.
//
// So the declaration and the producer are the same objects, and the property that saves it from the closed loop
// is that the set is enumerable: All lets a live sweep walk real heroes and ask, pack by pack, "you say he would
// know this - did anyone actually tell him?". That is the question a unit test structurally cannot ask, because
// a unit test builds the very context that was wrong.

#region

using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   Composes what a character knows. One declaration, read by three: this factory at conversation time, a
    ///   unit test asserting the composition rule, and the live sweep asserting the host actually supplied it.
    /// </summary>
    public static class NpcKnowledgeFactory
    {
        /// <summary>
        ///   Every pack that exists, in the order they are said. ENUMERABLE on purpose: it is what lets somebody
        ///   other than the producer ask whether it is all there, and what lets the audit Gabriel asked for
        ///   ("should a character know something it does not know today?") be a pass somebody runs rather than a
        ///   comment somebody writes.
        /// </summary>
        public static IReadOnlyList<KnowledgePack> All { get; } = new List<KnowledgePack> {
            // The room first: where and when they are standing frames everything said after it.
            new HereAndNowPack(),
            new HouseStandingPack(),

            // After the house, because a seat is one of the house's holdings seen close up.
            new SeatStandingPack(),
            new PersonalBondsPack()
        };

        /// <summary>What a person like this carries. Unknown bearer carries nothing, which refuses rather than guesses.</summary>
        public static IReadOnlyList<KnowledgePack> For(KnowledgeBearer bearer)
            => bearer == null
                ? new List<KnowledgePack>()
                : All.Where(p => p.CarriedBy(bearer)).ToList();
    }
}

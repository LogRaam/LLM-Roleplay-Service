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
            // What they ARE comes first, and is never budgeted away. See PackKind.
            new StationPack(),

            // The room first: where and when they are standing frames everything said after it.
            new HereAndNowPack(),

            // The body they are standing in, right after the room they are standing in. Costs nothing when
            // they are well, which is most conversations.
            new OwnBodyPack(),
            new HouseStandingPack(),

            // What the house IS, then what it can bring to bear.
            new HouseMeansPack(),

            // After the house, because a seat is one of the house's holdings seen close up.
            new SeatStandingPack(),

            // The company they ride in, which is a seat of a different kind.
            new PartyStandingPack(),

            // After the seat: a trader's market is the same place seen through what it costs.
            new MarketWordPack(),

            // The same town seen from underneath: who is wanted, and whose ground this is.
            new UnderworldPack(),

            // The wider world, last of the outward-facing packs and the first whose REACH depends on rank.
            new RealmNewsPack(),
            new PersonalBondsPack()
        };

        /// <summary>
        ///   How many characters the whole knowledge section may spend in a COMPACT prompt.
        ///   <para>
        ///     Why a collective allowance rather than five careful packs. On 2026-09-12 the budget guard was
        ///     finally handed a character who knows things - until then it built a context with no Bearer, so
        ///     it measured a prompt containing not one pack and was green and blind to all five. The real
        ///     number was 16,329 against DuskSymphony's 16,000 ceiling, and trimming coaching out of every
        ///     Lean path brought it to 15,986. Fourteen characters of headroom is not a guarantee, it is
        ///     arithmetic that happened to work, and the next pack would have broken it silently.
        ///   </para>
        ///   <para>
        ///     So Compact spends at most this, packs are offered the budget in <see cref="All" /> order, and
        ///     one that will not fit is skipped rather than truncated - a half-rendered fact is worse than an
        ///     absent one. The order is therefore a PRIORITY: the room first, then the house, then what it can
        ///     bring to bear, then the seat, and personal bonds last because they are the most droppable.
        ///   </para>
        /// </summary>
        public const int LeanBudgetChars = 560;

        // 2026-09-12, second measurement: 700 never bound, so it was a declaration rather than a budget. When
        // the knowledge boundary was made to run both ways it cost the Compact prompt 66 characters it did not
        // have, and this is where they came from - personal_bonds, last in the order, yields in Compact only.
        // That is the mechanism doing its job: a feature displaces a feature, visibly, instead of a player's
        // 8k context breaking. Full keeps every pack.

        /// <summary>
        ///   The packs that say what a character IS. They apply to EVERYBODY, including somebody the host
        ///   could not compose a bearer for: "we do not know who this is" is precisely when a crown must
        ///   still be stated, and making identity depend on a successful composition would rebuild the
        ///   king-denies-his-crown bug one level down. Found by twelve existing tests failing the moment
        ///   station became a pack (2026-09-12).
        /// </summary>
        public static IReadOnlyList<KnowledgePack> Constitutive { get; } =
            All.Where(p => p.Kind == PackKind.Constitutive).ToList();

        /// <summary>What a person like this carries. Unknown bearer carries nothing, which refuses rather than guesses.</summary>
        public static IReadOnlyList<KnowledgePack> For(KnowledgeBearer bearer)
            => bearer == null
                ? new List<KnowledgePack>()
                : All.Where(p => p.CarriedBy(bearer)).ToList();
    }
}

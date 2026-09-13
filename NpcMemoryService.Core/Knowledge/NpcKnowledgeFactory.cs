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

using System;
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

            // What earns while they are elsewhere, which is the market's opposite number.
            new OwnVenturesPack(),

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
        ///     So Compact spends at most this, and a pack that will not fit is skipped rather than truncated -
        ///     a half-rendered fact is worse than an absent one.
        ///   </para>
        ///   <para>
        ///     WHICH pack is skipped is <see cref="KnowledgePack.DropPriority" />, NOT this list. It was this
        ///     list until 2026-09-12, when a second measurement showed what that costs at thirteen packs: a
        ///     rich character in Compact kept his grudges and lost the fact that the player was wanted. The
        ///     order below is the order things are SAID, and it is no longer asked to answer a question it was
        ///     never written to answer.
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

        /// <summary>
        ///   Which of these packs a Compact prompt can afford, returned in the order they were given so the
        ///   caller still SAYS them in narrative order. The two orderings are separate, and this is the seam.
        ///   <para>
        ///     THE ORDERINGS HAD TO BE SPLIT, and the measurement that forced it is worth keeping. Until
        ///     2026-09-12 the drop was first-fit down <see cref="All" />, which is the order things are said.
        ///     With seven packs that was harmless. With thirteen, a rich character in Compact kept his
        ///     personal grudges - 60 characters, last in the registry - and lost, in this order: that the
        ///     player is badly WANTED, that a war has been declared, and that his own company is short of
        ///     food. Every one of those changes what he may say or agree to; the grudges change how it
        ///     sounds. The registry had quietly become a priority list nobody had ever written as one.
        ///   </para>
        ///   <para>
        ///     Greedy by <see cref="KnowledgePack.DropPriority" />, ties broken by the given order so the
        ///     result is deterministic. A cheap pack may still slip in behind an expensive one that would not
        ///     fit: the allowance exists to be spent, and the rule that matters is that a pack is skipped
        ///     WHOLE rather than truncated, because half a fact can say the opposite of what it meant.
        ///   </para>
        ///   <para>
        ///     It lives here rather than in the builder for the reason the registry does: a composition rule
        ///     that only the producer can run is a rule no test can check without re-implementing it, and a
        ///     re-implemented rule is the budget guard that was green and blind to all five packs.
        ///   </para>
        /// </summary>
        /// <param name="carried">The packs that rendered something, in the order they will be said.</param>
        /// <param name="costOf">What each one costs in characters, as actually rendered.</param>
        public static IReadOnlyList<KnowledgePack> AffordableInCompact(IReadOnlyList<KnowledgePack> carried,
                                                                      Func<KnowledgePack, int> costOf)
        {
            if (carried == null || costOf == null) return new List<KnowledgePack>();

            var kept = new HashSet<KnowledgePack>();
            var spent = 0;

            IEnumerable<KnowledgePack> byValue = carried.Select((pack, order) => (pack, order))
                                                        .OrderBy(x => x.pack.DropPriority)
                                                        .ThenBy(x => x.order)
                                                        .Select(x => x.pack);

            foreach (KnowledgePack pack in byValue)
            {
                // Only what is budgeted counts against the budget: a constitutive pack cannot be made to
                // crowd out the knowledge, and cannot be crowded out by it either.
                if (pack.Kind == PackKind.Constitutive)
                {
                    kept.Add(pack);

                    continue;
                }

                if (spent + costOf(pack) > LeanBudgetChars) continue;

                kept.Add(pack);
                spent += costOf(pack);
            }

            return carried.Where(kept.Contains).ToList();
        }

        /// <summary>What a person like this carries. Unknown bearer carries nothing, which refuses rather than guesses.</summary>
        public static IReadOnlyList<KnowledgePack> For(KnowledgeBearer bearer)
            => bearer == null
                ? new List<KnowledgePack>()
                : All.Where(p => p.CarriedBy(bearer)).ToList();
    }
}

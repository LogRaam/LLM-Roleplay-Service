// Code written by Gabriel Mailhot, 11/09/2026.
// Increment 1 of the knowledge-composition pillar. One declaration, three readers - and the third is the whole
// point of the shape.
//
// A pack answers three questions about one coherent body of knowledge:
//
//   CarriedBy   - would a person like this know it?      (the composition rule: pure, unit-tested)
//   IsSupplied  - did the host actually put it there?    (completeness: only a live campaign can answer)
//   Render      - how is it said to the character?
//
// Keeping all three on one object is what makes the guarantee possible. A factory that is also its own
// specification can only be checked against itself, and this project has broken that closed loop twice in a
// week: a test deriving its expectation from the implementation, and a guard measuring its own construction.
// Because CarriedBy and IsSupplied sit side by side and the set of packs is ENUMERABLE, a sweep over real
// heroes can ask the one question a unit test structurally cannot: "you say he would know this - did anyone
// actually tell him?"
//
// That is the question fkasad's governor failed on 2026-09-11. The prompt was right, the rule would have been
// right, and the host never filled the field.

#region

using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   Whether a pack says what a character IS or what a character KNOWS. Gabriel's distinction, 2026-09-12,
    ///   and it exists because the two must be treated differently by the prompt BUDGET.
    /// </summary>
    public enum PackKind
    {
        /// <summary>
        ///   What the character KNOWS. Contingent: they could be ignorant of it, it can change under them, and
        ///   dropping it from a Compact prompt leaves a character who is merely less informed. Budgeted.
        /// </summary>
        Contingent,

        /// <summary>
        ///   What the character IS. Their name, their station, their nature. Dropping it does not leave a
        ///   character who knows less; it leaves a character who is nobody - which is the king-denies-his-crown
        ///   bug rebuilt on purpose.
        ///   <para>
        ///     So a constitutive pack is rendered FIRST and OUTSIDE the Compact allowance, and it must be
        ///     carried by every bearer there is. A constitutive pack with a composition rule that can refuse
        ///     is a contradiction, and a test asserts that none of them has one.
        ///   </para>
        /// </summary>
        Constitutive
    }

    /// <summary>
    ///   Whether a pack's content survives from one encounter to the next, which decides WHERE in the prompt
    ///   it may be rendered.
    ///   <para>
    ///     Measured 2026-09-12 and it was not theoretical: on a 37,267-character prompt, changing only the
    ///     hour and the weather cost 10,018 characters of cacheable prefix - 27% - because here_and_now
    ///     rendered inside the stable prefix and everything below it was invalidated with it. PromptBuilder's
    ///     own seam comment had already named "time of day" as belonging after the marker; the pack simply
    ///     had no way to say so.
    ///   </para>
    /// </summary>
    public enum PackVolatility
    {
        /// <summary>
        ///   Holds still from one conversation to the next. Belongs in the cacheable prefix, with everything
        ///   else about who this person is.
        /// </summary>
        Stable,

        /// <summary>
        ///   Changes with the hour, the weather, the map. Belongs BELOW the encounter marker, with the day
        ///   count and the scene stage, where the prompt already keeps what cannot be cached.
        /// </summary>
        PerEncounter
    }

    /// <summary>
    ///   HOW WELL somebody knows a subject. Gabriel, 2026-09-12: "un pack pourrait avoir des niveaux requis de
    ///   connaissance pour un sujet... un habitant de village pourrait être peu connaissant du reste du monde,
    ///   mais un Lord pourrait être très connaissant."
    ///   <para>
    ///     The turn that makes it fit without bending anything: depth belongs to the PAIR of a person and a
    ///     SUBJECT, not to a person. A villager is slight on the realm and deep on his own village; a lord is
    ///     the reverse about the price of grain. A pack already IS a subject, so the question has a natural
    ///     home - and because <see cref="KnowledgePack.CarriedBy" /> is derived from it, there is one source
    ///     of truth rather than two predicates that must agree (the trap that HoldsASeat fell into earlier the
    ///     same day).
    ///   </para>
    ///   <para>
    ///     THE CONSTRAINT THAT KEEPS IT HONEST: a shallower depth means FEWER FACTS, never vaguer ones. "You
    ///     know a little about the wars" is an invitation to invent the rest, which is the exact failure the
    ///     whole pillar exists to close. Every level must render specific things, just less of them.
    ///   </para>
    /// </summary>
    public enum KnowledgeDepth
    {
        /// <summary>They do not know this at all. The pack is not carried.</summary>
        None,

        /// <summary>They have heard of it. The barest concrete fact, and nothing that invites elaboration.</summary>
        Slight,

        /// <summary>What anyone in their position would know.</summary>
        Ordinary,

        /// <summary>Their business. They follow it and could be asked about it.</summary>
        Knowing,

        /// <summary>They live in it. Nobody knows this subject better than they do.</summary>
        Deep
    }

    /// <summary>One coherent body of knowledge a character may carry, and everything askable about it.</summary>
    public abstract class KnowledgePack
    {
        /// <summary>
        ///   IS or KNOWS. Defaults to <see cref="PackKind.Contingent" />, so a new pack is budgeted unless
        ///   somebody deliberately argues it is part of who the character is.
        /// </summary>
        public virtual PackKind Kind => PackKind.Contingent;

        /// <summary>
        ///   Which packs a Compact prompt keeps when it cannot keep them all. LOWER survives longer.
        ///   <para>
        ///     Separate from the registry order on purpose, and it had to become separate the moment there
        ///     were thirteen packs. The registry order is NARRATIVE - the order things are said - and using it
        ///     to decide what to DROP produced exactly the wrong answer: measured 2026-09-12, a rich character
        ///     in Compact kept his personal grudges and lost the fact that the player was BADLY WANTED, that a
        ///     war had been declared, and that his own company was running out of food.
        ///   </para>
        ///   <para>
        ///     The ordering principle: what changes what a character can SAY OR DO outranks what colours how
        ///     he says it. A man who does not know the player is wanted plays a different scene; a man who
        ///     does not mention his cousin plays the same one slightly flatter.
        ///   </para>
        /// </summary>
        public virtual int DropPriority => 50;

        /// <summary>
        ///   Whether this survives from one encounter to the next. Defaults to <see cref="PackVolatility.Stable" />,
        ///   because the safe direction is the cheap one: a stable pack wrongly marked volatile only moves down
        ///   the prompt, while a volatile one left in the prefix silently costs every later section its cache.
        /// </summary>
        public virtual PackVolatility Volatility => PackVolatility.Stable;

        /// <summary>Short stable name, used by the completeness sweep and the generated design page.</summary>
        public abstract string Name { get; }

        /// <summary>
        ///   What this covers, in the words a reader of the design page needs. Not a code comment: this is the
        ///   sentence Gabriel reviews when asking "should a character know something it does not know today?".
        /// </summary>
        public abstract string Covers { get; }

        /// <summary>
        ///   How well a person like this knows the subject. The composition rule, and a rule about the world.
        ///   <see cref="KnowledgeDepth.None" /> means they do not know it at all.
        /// </summary>
        public abstract KnowledgeDepth DepthFor(KnowledgeBearer? bearer);

        /// <summary>
        ///   Whether they know it at all. DERIVED from <see cref="DepthFor" /> and deliberately not overridable:
        ///   two predicates that must agree are two predicates that will one day disagree, which is exactly the
        ///   fault that had "he would know this" and "somebody told him" drift apart in the first place.
        /// </summary>
        public bool CarriedBy(KnowledgeBearer? bearer) => DepthFor(bearer) != KnowledgeDepth.None;

        /// <summary>
        ///   Whether the host actually supplied it for this encounter. Separate from <see cref="CarriedBy" /> on
        ///   purpose: the gap between "he would know" and "we told him" is exactly where the bugs have lived.
        /// </summary>
        public abstract bool IsSupplied(EncounterContext context);

        /// <summary>
        ///   True when a CARRIED pack that arrives empty is a FAULT: the host was supposed to fill it and did
        ///   not. False when emptiness is a legitimate state of a life.
        ///   <para>
        ///     Found by the sweep on its very first run in a live game (2026-09-11), which is a fair
        ///     advertisement for the instrument. house_standing is the first kind: if a man has a house, that
        ///     house has a name, so an empty one means nobody read the engine. personal_bonds is the second: a
        ///     lord with no living feud and no lover simply has nothing to say about anyone, and reporting that
        ///     as a missing fact would teach everyone to ignore the report.
        ///   </para>
        ///   <para>
        ///     The cost of the distinction, stated rather than hidden: for an optional pack the sweep can no
        ///     longer prove the host wired it at all, only note how often it arrives. The unit tests cover the
        ///     wiring's shape; the sweep covers the facts it cannot see.
        ///   </para>
        /// </summary>
        public virtual bool RequiresContent => true;

        /// <summary>Says it to the character. Called only when the pack is both carried and supplied.</summary>
        public abstract void Render(StringBuilder sb, EncounterContext context, bool lean);
    }
}

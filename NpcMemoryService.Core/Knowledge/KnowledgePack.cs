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
    /// <summary>One coherent body of knowledge a character may carry, and everything askable about it.</summary>
    public abstract class KnowledgePack
    {
        /// <summary>Short stable name, used by the completeness sweep and the generated design page.</summary>
        public abstract string Name { get; }

        /// <summary>
        ///   What this covers, in the words a reader of the design page needs. Not a code comment: this is the
        ///   sentence Gabriel reviews when asking "should a character know something it does not know today?".
        /// </summary>
        public abstract string Covers { get; }

        /// <summary>Whether a person like this would know it. The composition rule, and a rule about the world.</summary>
        public abstract bool CarriedBy(KnowledgeBearer bearer);

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

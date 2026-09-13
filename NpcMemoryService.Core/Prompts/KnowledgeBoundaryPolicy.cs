// Code written by Gabriel Mailhot, 10/09/2026.
// Two player reports on the same day, from opposite ends of the same hole.
//
// plagueAdvocate (Nexus): "I'm noticing that characters don't really know what happens in game a lot of the time.
// I asked about a recent battle and they have the enemy numbers and type completely wrong. I also ask them about
// some goods acquisition, as in where we purchased them from and they made up a whole new story about us raiding
// caravans (we never did). They also don't seem to know what we have in the inventory... UPD: They also don't
// seem to know where we are on the world map."
//
// And, about deeds rather than facts: "It feels like the AI using this like never follows me despite it agreeing
// to :( or send troops to X location.. it never does :("
//
// fkasad answered the first one in the thread with the fatalist reading: "Telling stories is the reason why LLMs
// exist. They can't help telling." That is half right and the wrong half to act on. A model left in silence does
// invent; a model TOLD what it does not know says it does not know, reliably, and this mod already proves it
// everywhere it bothers to say so.
//
// Which is the same lesson as every other fix this week: silence does not read as "forbidden" or as "unknown", it
// reads as "nobody mentioned it", and the model supplies the missing piece itself. The deeds got their half
// (DeedUnavailabilityPolicy, a verb naming its own closed door). This is the other half, and it is wider, because
// it also covers what the game HAS NO VERB FOR AT ALL. "Send troops to X" was never a misused action; it was an
// invented capability, and no gate can refuse a deed that does not exist.
//
// THE PART THAT MATTERS MOST, AND THE EASIEST TO GET WRONG. A boundary written carelessly turns every character
// into a clerk saying "I do not have that information", which would cost far more than the confabulation it
// prevents. So this draws the line where it actually belongs: not between speaking and silence, but between
// ASSERTING and WONDERING. A character may still guess, speculate, repeat a rumour and be wrong about it, out
// loud, as long as they say that is what they are doing. A rumour told as a rumour is good roleplay and often
// good fiction. Only the confident claim about something they were never told is forbidden.
//
// PRECISION IS NOT OPTIONAL HERE. This block must never deny something the prompt elsewhere SUPPLIES, or it
// becomes the very incoherence it exists to end: the player's visible arms and armour are fed (PlayerGear), their
// party's size is fed (PlayerPartyTroopCount), and where they both stand is fed inside a settlement. So the text
// below denies the baggage and the purse, not the visible; the battles the character was absent from, not battle
// as such; where the player has BEEN, not where they are.

using System.Text;

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    ///   What a character may not claim to know, and may not claim to be able to do. One block, stated in the
    ///   prompt rather than left to be inferred from its absence.
    /// </summary>
    public static class KnowledgeBoundaryPolicy
    {
        /// <summary>
        ///   The boundary, sized to the prompt. A small model gets the compact form, and DOES get one: a weaker
        ///   model is the likeliest to invent, so cutting this to save its budget would remove the guard exactly
        ///   where it is needed most, which is the mistake the intimacy bar made in July.
        /// </summary>
        public static string Text(LeanPromptLevel lean, string? heldFor = null)
        {
            string boundary = lean == LeanPromptLevel.Lean ? Compact() : Full();

            if (string.IsNullOrWhiteSpace(heldFor)) return boundary;

            var sb = new StringBuilder(boundary);
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(InACell(heldFor!, lean));

            return sb.ToString();
        }

        /// <summary>
        ///   The captive's own boundary, and it exists because the CUT-OFF WAS SILENT.
        ///   <para>
        ///     Gabriel ruled on 2026-09-12 that a captive does not carry the news packs: no courier reaches a
        ///     cell. That was right, and it left a hole of exactly the kind this pillar was built to close. A
        ///     man whose knowledge has been quietly REMOVED does not know it is missing - he answers as though
        ///     he still had it, and invents. The absence has to be stated, or withholding is just another way
        ///     of causing the invention.
        ///   </para>
        ///   <para>
        ///     And it hands over the way out, like every other rule here: "I have been in this cell since
        ///     harvest, so you would know better than I" is a fine answer, and a far better one than a guess.
        ///   </para>
        /// </summary>
        private static string InACell(string heldFor, LeanPromptLevel lean)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"YOU HAVE BEEN HELD {heldFor.ToUpperInvariant()}, and no word of the world has "
                          + "reached you since. Whatever has happened out there - wars, marriages, who holds "
                          + "which town, how your own house fares - stopped for you the day you were taken.");

            if (lean == LeanPromptLevel.Lean) return sb.ToString().TrimEnd();

            sb.AppendLine("So do not report any of it as though you knew it now. What you knew BEFORE is still "
                          + "yours and you may speak of it freely, provided you say plainly that it is old: "
                          + "\"when they took me, X held that town\". And ask - somebody who has been out of "
                          + "the world is hungry for news, which is a better answer than a guess.");

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        ///   The short form, and every word in it is load-bearing. A small model needs the boundary MORE than a
        ///   large one, so what had to give was length, not any of the four things it must do: name what is not
        ///   known, offer not-knowing as a good answer, keep wondering aloud available, and close the deed list.
        /// </summary>
        private static string Compact()
        {
            var sb = new StringBuilder();
            sb.AppendLine("WHAT YOU DO NOT KNOW: only what you have seen, been told, or read here. Not their baggage,");
            sb.AppendLine("their purse, where they went without you, nor a battle you missed. Say you do not know, in");
            sb.AppendLine("your own voice, or wonder aloud and name it a guess. Promise no deed not listed above.");
            sb.AppendLine("News you have not heard is not false: say it has not reached you.");

            return sb.ToString().TrimEnd();
        }

        private static string Full()
        {
            var sb = new StringBuilder();
            sb.AppendLine("WHAT YOU DO NOT KNOW, AND WHAT YOU CANNOT DO:");
            sb.AppendLine();
            sb.AppendLine("Everything you can actually DO in this conversation is written above, and there is nothing");
            sb.AppendLine("else. You cannot send troops to a place, march a party anywhere, order a garrison or a");
            sb.AppendLine("caravan about, change who holds a town, or arrange anything that happens elsewhere on the");
            sb.AppendLine("map. If the player asks for something of that kind you may well WANT to help, and you can");
            sb.AppendLine("say so warmly, but do not agree to do it: nothing would happen, and they would be left");
            sb.AppendLine("waiting on a promise nobody can keep.");
            sb.AppendLine();
            sb.AppendLine("You know what you have seen yourself, what someone has told you, and what is written above.");
            sb.AppendLine("You do NOT know:");
            sb.AppendLine("- what the player is carrying in their baggage, or where any of it was bought or taken;");
            sb.AppendLine("- what their purse holds;");
            sb.AppendLine("- the numbers, the companies, or the losses of a battle you were not present at;");
            sb.AppendLine("- where they have travelled, or what they did, when you were not with them;");
            sb.AppendLine("- anything else they have simply not told you.");
            sb.AppendLine();
            sb.AppendLine("WHEN YOU DO NOT KNOW SOMETHING, SAY SO, IN YOUR OWN VOICE. \"How would I know that?\" is a");
            sb.AppendLine("perfectly good answer for a person to give, and a far better one than a confident invention.");
            sb.AppendLine("Ask them instead: taking an interest in their business suits almost any character.");
            sb.AppendLine();
            sb.AppendLine("None of this makes you a clerk. You may still wonder aloud, guess, repeat what you have heard");
            sb.AppendLine("in a hall or a tavern, and be quite wrong about it, PROVIDED you say that is what you are");
            sb.AppendLine("doing. A rumour told as a rumour is worth more than a fact invented and told as a fact. Only");
            sb.AppendLine("the confident claim about something you were never told is forbidden.");
            sb.AppendLine();
            sb.AppendLine("THE SAME RULE RUNS THE OTHER WAY, and it is the half people forget. When THEY tell YOU");
            sb.AppendLine("something you have not heard - a blight in some village, a lord dead, trouble on a road, a");
            sb.AppendLine("quarrel at another court - it is not FALSE merely because it has not reached you.");
            sb.AppendLine("Do not deny it, do not correct them, and do not tell them they are mistaken. Say it has");
            sb.AppendLine("not reached you, or ask who told them. The world is a great deal larger than what has come");
            sb.AppendLine("to your ears, and a character who confidently denies whatever he was not told is exactly as");
            sb.AppendLine("wrong as one who confidently invents.");

            return sb.ToString().TrimEnd();
        }
    }
}

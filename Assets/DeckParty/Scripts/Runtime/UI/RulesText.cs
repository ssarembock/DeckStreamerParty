namespace DeckParty.UI
{
    /// <summary>How-to-play copy shared by the main menu and the in-game pause menu.</summary>
    public static class RulesText
    {
        public const string Full =
@"<size=125%><b><color=#FFC94A>The goal</color></b></size>
Throw one party with <b>4 Diamond guests there at the same time</b>, and end it successfully before your final night (25 by default). Recruiting four Diamonds isn't enough: they have to show up together.

<size=125%><b><color=#FFC94A>A night has two halves</color></b></size>
<b>1. The party.</b> Press <b>Open the Door</b> to let the next guest from your Squad in. Each guest can come once per party. Guests with a purple <b>action</b> button can use it once per party. When you're happy, press <b>End Party</b> to bank what everyone inside brought in.
<b>2. Planning.</b> Guests bring in <color=#FFC94A>Subs</color>; when the party ends they're banked as <color=#FFC94A>Gifted Subs</color>, which recruit new guests into your Squad. <color=#6BE38B>Cash</color> raises your <b>Sub Cap</b> (how many guests fit at once).

<size=125%><b><color=#FF5A5A>Swatting Risk</color></b></size>
Guests with a red X add Swatting Risk. Up to 2 net Swatting Risk is fine; at <b>3 the SWAT team shows up</b>: the party ends with no payout and you give one guest a <b>Temporary Ban</b> (they sit out the next party). AutoMod, VIPS and the Wholesome Icon <b>cancel</b> 1 Swatting Risk each while present (Cheems does too when a Doge is around). Some guests add Swatting Risk with their actions; This Is Fine Dog calls off the SWAT team once.

<size=125%><b><color=#FF9A3D>Sub Cap</color></b></size>
You can't open the door once you've hit your Sub Cap, but guests who <b>bring</b> others (Parasocialite, Raid Leader) or take extra space (Big Chungus takes 3) can push you over it. If that happens the party is shut down with no payout. Leave spare room!

<size=125%><b><color=#6BE38B>Money</color></b></size>
Raising your Sub Cap costs 2 Cash, then 1 more each time (max 12 per space, up to 34). Some guests charge fees (negative Cash). Fees are paid from your Cash; any unpaid amount costs <b>7 Gifted Subs per Cash</b>.
Normal guests are limited to 4 copies each; Diamonds are unlimited. You can't remove guests from your Squad, so every recruit changes your odds.

<size=125%><b><color=#A77BFF>Actions and families</color></b></size>
Every guest belongs to a family (Community, Animal, Wojak, Rage Comic, Reaction, Brainrot, Diamond) and every action has a type (Recruit, Remove, Modify, Shuffle, Scout, Economy). Doge loves mixed families, Bongo Cat buffs Animals and Dancing Baby rewards two actions of the same type in a row. Some actions ask you to pick an option first, then one or more guests; actions that take several guests show a Confirm button. Permanent changes (Chad vs Virgin, Tung Tung, Bad Luck Brian) stay with that guest card forever. Hover any guest to see its family, action type and tonight's effects.

<size=125%><b><color=#3DDBD9>Controls</color></b></size>
<b>Space</b> open the door / confirm picks / start the next night   <b>E</b> end the party   <b>R</b> Squad   <b>Esc</b> cancel / pause menu.

<size=125%><b><color=#FF4F8B>Multiplayer</color></b></size>
Everyone gets the same character set and starts with the same Squad, and everyone can watch every party: click a player in the Players panel to see their board live. In <b>Take Turns</b> mode (the default) players throw their parties one after another each night while everyone else watches, and the season ends after the first night someone throws a 4-Diamond party. In <b>Synchronized Nights</b> mode everyone plays the same night at once and the next night starts when every player is ready; the first night someone throws a 4-Diamond party ends the season (ties go to whoever earned more Gifted Subs). In <b>Race</b> mode everyone plays at their own pace and the first 4-Diamond party wins instantly. If nobody wins, the best Diamond count at a successful party takes it.";

        public const string Quick =
@"<b>Win:</b> 4 Diamonds at one successful party.
<b>Door:</b> admits the next waiting guest.  <b>End Party:</b> banks Subs as Gifted Subs, plus Cash.
<b>3 net Swatting Risk</b> = SWAT team (no payout, one guest gets a Temporary Ban).
<b>Over your Sub Cap</b> = party shut down (no payout).
<b>Planning:</b> Gifted Subs recruit guests, Cash raises your Sub Cap.
<b>Keys:</b> Space door/confirm/next night, E end party, R Squad, Esc pause.";
    }
}

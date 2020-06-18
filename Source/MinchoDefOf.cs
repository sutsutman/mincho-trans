using System;
using RimWorld;
using Verse;
using Garam_RaceAddon;


namespace RW_Mincho
{
	[DefOf]
	public static class MinchoDefOf
	{
		public static RaceAddonPawnKindDef Mincho_Colonist = new RaceAddonPawnKindDef();
		public static RaceAddonPawnKindDef Mincho_WildMan = new RaceAddonPawnKindDef();
		public static SoundDef Pawn_Mincho_Death = new SoundDef();
		public static RaceAddonThingDef Mincho_ThingDef = new RaceAddonThingDef();
		public static HediffDef Mincho_Infection = new HediffDef();
		public static ThingDef Mincho_Filth_BloodDef = new ThingDef();
		static MinchoDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(MinchoDefOf));
		}
	}
}
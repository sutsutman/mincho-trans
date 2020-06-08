using System;
using System.Collections.Generic;
using System.Linq;
using Garam_RaceAddon;
using RimWorld;
using Verse;
using System.Text;
using Verse.Sound;
using System.Text.RegularExpressions;
using System.Collections;

namespace RW_Mincho
{
	// Token: 0x02000002 RID: 2
	public static class MinchoGenerator
	{
		static MinchoGenerator()
		{
			var forbiddenTraits = @"Nudist,
								TooSmart,
								Transhumanist,
								BodyPurist,
								Masochist,
								Pyromaniac,
								Wimp,
								FastLearner,
								GreatMemory,
								Psychopath,
								Bloodlust,
								SpeedOffset,
								PsychicSensitivity,
								Tough,
								Nimble,
								Abrasive,
								DislikesMen,
								DislikesWomen,
								Beauty,
								AnnoyingVoice,
								CreepyBreathing,
								DrugDesire,
								Immunity";
			var matches = Regex.Matches(forbiddenTraits, "[a-zA-Z]+");
			forbiddenStrings = new HashSet<string>();
			foreach (var match in matches)
				forbiddenStrings.Add(((Match)match).Value);
		}
		public static HashSet<String> forbiddenStrings;
		public static void ConvertToMincho(Pawn originalPawn, Hediff hediff)
		{
			CreateMinchoFilth(originalPawn);
			(var generatedPawn, var isWildPawn) = CreateReplacedPawn(originalPawn);
			GenSpawn.Spawn(generatedPawn, originalPawn.Position, originalPawn.Map, 0);
			originalPawn.Destroy(DestroyMode.Vanish);
			// play sound here if you want to put sound when the pawn created.
			if (isWildPawn)
				generatedPawn.SetFaction(null);
			// IDK why this exists.
			if (generatedPawn.Spawned && !generatedPawn.Downed)
				generatedPawn.jobs.StopAll(false);
		}

		private static void CreateMinchoFilth(Pawn pawn)
		{
			for (int i = 0; i < 9; i++)
			{
				IntVec3 Pos = pawn.Position + GenRadial.RadialPattern[i];
				bool canPlaceFilthToPos = GenGrid.InBounds(Pos, pawn.Map) && GridsUtility.GetRoom(pawn.Position, pawn.Map, (RegionType)6) == GridsUtility.GetRoom(Pos, pawn.Map, (RegionType)6);
				if (canPlaceFilthToPos)
					FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, MinchoDefOf.Mincho_Filth_BloodDef, GenText.LabelIndefinite(pawn), Rand.RangeInclusive(0, 4));
			}
		}

		private static (Pawn pawn, bool isWildMan) CreateReplacedPawn(in Pawn originalPawn)
		{
			bool isWildMan = false;
			PawnGenerationRequest generationRequest = new PawnGenerationRequest(
			MinchoDefOf.Mincho_Colonist, Faction.OfPlayer, (PawnGenerationContext)2, -1, false,
			false, false, false, false, false, 0f, false, false, false, false, false, false,
			false, false, 0, null, 0, null, null, null, null, null,
			originalPawn.ageTracker.AgeBiologicalYearsFloat, originalPawn.ageTracker.AgeChronologicalYearsFloat,
				null, 0, "Temp", "Temp", null);

			// set faction of pawn
			if (originalPawn.Faction == Faction.OfPlayer || originalPawn.IsPrisonerOfColony)
				generationRequest.Faction = Faction.OfPlayer;
			else if (originalPawn.Faction.AllyOrNeutralTo(Faction.OfPlayer))
				generationRequest.Faction = originalPawn.Faction;
			else
				isWildMan = true;

			(var childhood, var adult, var faction) = GetBackstoryAndFaction(originalPawn);

			// generate pawn and create 
			var generatedPawn = PawnGenerator.GeneratePawn(generationRequest);
			generatedPawn.story.childhood = childhood;
			generatedPawn.story.adulthood = adult;
			generatedPawn.SetFaction(faction, null);

			TranslatePawnRelations(originalPawn, generatedPawn);
			RemoveForbiddenTrait(ref generatedPawn);
			CopyName(originalPawn, generatedPawn);
			generatedPawn.skills = originalPawn.skills; // move skills


			return (generatedPawn, isWildMan);
		}

		private static void TranslatePawnRelations(Pawn from, Pawn to)
		{
			RemoveRelationFromOtherRelatedPawns(from);
			foreach (var relation in from.relations.DirectRelations)
				to.relations.AddDirectRelation(relation.def, relation.otherPawn);
		}

		private static void CopyName(Pawn from, Pawn to)
		{
			var name = from.Name;
			if (name is NameTriple triple)
				to.Name = new NameTriple(triple.First, triple.Nick, triple.Last);
			else if (name is NameSingle single)
				to.Name = new NameSingle(single.Name, single.Numerical);
		}

		private static void RemoveRelationFromOtherRelatedPawns(Pawn pawn)
		{ // pawn과 관계가 있는 모든 폰에게서 pawn에 관한 관계를 삭제
			var relatedPawns = pawn.relations.RelatedPawns;
			foreach (var otherPawn in relatedPawns)
			{
				var relationsList = new List<DirectPawnRelation>(otherPawn.relations.DirectRelations);
				foreach (var relation in relationsList.Where(r => r.otherPawn == pawn))
					otherPawn.relations.RemoveDirectRelation(relation);
			}
		}

		private static void RemoveForbiddenTrait(ref Pawn pawn)
		{
			var traits = pawn.story.traits.allTraits;
			foreach (var trait in traits)
				if (forbiddenStrings.Contains(trait.def.defName))
					pawn.story.traits.allTraits.Remove(trait);
		}

		private static (Backstory childhood, Backstory adult, Faction faction) GetBackstoryAndFaction(in Pawn originalPawn)
		{ // TODO : 폰의 상태에 따른 민초 백스토리 설정
			Backstory childHoodBackstory = null;
			Faction faction = null;
			string Identifier = string.Empty;
			if (originalPawn.IsColonist) // 정착민
			{
				Identifier = "Mincho_Infection_Colonist";
				faction = Faction.OfPlayer;
			}
			else if (originalPawn.IsPrisoner) // 죄수
			{
				Identifier = "Mincho_Infection_Prisoner";
				faction = Faction.OfPlayer;
			}
			else if (originalPawn.Faction == null) // 야인
				Identifier = "Mincho_Infection_Wildman";
			else if (originalPawn.Faction.AllyOrNeutralTo(Faction.OfPlayer)) // 적대
				Identifier = "Mincho_Infection_NPC";
			else if (originalPawn.Faction.HostileTo(Faction.OfPlayer))
				Identifier = "Mincho_Infection_Pirate";
			else if (originalPawn.RaceProps.intelligence == Intelligence.Humanlike)
			{ // 동물
				if (originalPawn.Faction == Faction.OfPlayer)
				{
					Identifier = "Mincho_Infection_Animal_Colony";
					faction = Faction.OfPlayer;
				}
				else if (originalPawn.Faction == null)
					Identifier = "Mincho_Infection_Animal_Wild";
				else if (originalPawn.Faction.HostileTo(Faction.OfPlayer))
					Identifier = "Mincho_Infection_Animal_Pirate";
				else if (originalPawn.Faction.AllyOrNeutralTo(Faction.OfPlayer))
				{
					Identifier = "Mincho_Infection_Animal_NPC";
					faction = originalPawn.Faction;
				}
			}
			else
				Identifier = "Mincho_Infection_Undefined";

			childHoodBackstory = BackstoryDatabase.allBackstories[Identifier];

			return (childHoodBackstory, null, faction);
		}
	}
}

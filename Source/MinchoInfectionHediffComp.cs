using System;
using Verse;

namespace RW_Mincho
{
	public class MinchoInfectionHediffComp : HediffComp
	{
		private bool finalStage = false;

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			float severity = this.parent.Severity;
			bool isFinalStage = severity >= 0.75f && !this.finalStage;
			if (isFinalStage)
			{
				this.parent.comps.RemoveAll((HediffComp x) => x is HediffComp_TendDuration);
				this.finalStage = true;
			}
			bool isPassedMinchoTrans = severity >= 1f;
			if (isPassedMinchoTrans)
			{
				MinchoGenerator.ConvertToMincho(base.Pawn, this.parent);
			}
		}
	}
}

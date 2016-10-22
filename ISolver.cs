namespace Nima
{
	public interface ISolver
	{
		int Order
		{
			get;
		}

		bool NeedsSolve
		{
			get;
		}

		bool SuppressMarkDirty
		{
			get;
			set;
		}
		
		void SolveStart();

		void Solve();
	}
}
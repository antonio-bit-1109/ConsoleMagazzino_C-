using System;

namespace ConsoleMagazzino
{
	internal class Triplet
	{
		private bool? V1 { get; set; } // errore al primo passaggio (select) 
		private bool? V2 { get; set; } // errore al secondo passaggio (update prodotto)
		private bool? V3 { get; set; } // errore al terzo passaggio (update magazzino)

		public Triplet()
		{
			V1 = null;
			V2 = null;
			V3 = null;
		}

		public void PopolateV1(bool value)
		{
			V1 = value;
		}
		public void PopolateV2(bool value)
		{
			V2 = value;
		}
		public void PopolateV3(bool value)
		{
			V3 = value;
		}

		public void ShowEsiti()
		{
			Console.WriteLine($"esito1: {V1}");
			Console.WriteLine($"esito2: {V2}");
			Console.WriteLine($"esito3: {V3}");
		}
	}
}

namespace ConsoleMagazzino
{
	internal class Program
	{

		static void Main(string[] args)
		{


			Magazzino.Welcome();

			bool exit = false;

			do
			{
				Magazzino.SelectAction();
				Magazzino.settingSceltaUtente();


			} while (!Magazzino.InputValido);

			do
			{
				switch (Magazzino.SceltaUtente)
				{
					case 1:
						Magazzino.VisualizzaStatoMagazzino();
						Magazzino.SelectAction();
						Magazzino.settingSceltaUtente();
						break;
					case 2:
						Magazzino.AggiungiProdottoMagazzino();
						Magazzino.SelectAction();
						Magazzino.settingSceltaUtente();
						break;
					case 3:
						Magazzino.RimuoviProdottoMagazzino();
						Magazzino.SelectAction();
						Magazzino.settingSceltaUtente();
						break;
					case 4:
						Magazzino.ControllaCapienzaMagazzino();
						Magazzino.SelectAction();
						Magazzino.settingSceltaUtente();
						break;
					case 5:
                        Magazzino.InviaEmailDalMagazzino();
                        Magazzino.SelectAction();
                        Magazzino.settingSceltaUtente();
						break;
					case 6:
                        exit = true;
                        break;
                }
			} while (!exit);

			Magazzino.Esci();











		}
	}
}

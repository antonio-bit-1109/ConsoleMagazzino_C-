using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleMagazzino
{
	internal static class Magazzino
	{
		private static string NomeMagazzino { get; set; } = "magazzino delle favole";

		// spazio disponibile 
		private static float SpazioDisponibile { get; set; } = 1000;
		//private static float SpazioUtilizzato { get; set; }

		// dictionary contenente nome prodotto immagazzinato e spazio occupato in magazzino
		// nome prodotto , spazio occupato in magazzino
		private static Dictionary<string, float> SpazioOccupatoProdotti { get; set; } = new Dictionary<string, float>();
		// nome prodotto , quantità di prodotto
		private static Dictionary<string, float> ProdottiImmagazzinati { get; set; } = new Dictionary<string, float>();

		public static int SceltaUtente { get; set; }
		public static string ChoiseProp { get; set; }
		public static bool InputValido { get; set; }

		private static string nomeProdotto_To_Store { get; set; }
		private static float qta_prodotto_to_store { get; set; }
		private static float dimens_prodotto_to_store { get; set; }

		//private static DatabaseConfigManager _DbConfigManager = new DatabaseConfigManager();
		private static IDatabaseConfigManager_Interface _dbConfigManager;

		private static IEmail_Interface _sendEmail;

		// inizializzo un costruttore statico per fare dependency injection, in pratica sto fornendo alla proprietà _dbConfigManager
		// l'astrazione della classe DatabaseConfigManager
		// ora, tramite la proprietà _dbConfigManager ho acesso a metodi e proprietà della classe DatabaseConfigManager
		static Magazzino()
		{
			var serviceProvider = IOC_Config.Configure();
			_dbConfigManager = serviceProvider.GetService<IDatabaseConfigManager_Interface>();
			_sendEmail = serviceProvider.GetService<IEmail_Interface>();
		}

		public static void Welcome()
		{
			// stringa di benvenuto e inizializzazine tabella magazzino sul db. 
			_dbConfigManager.InizializzaMagazzinoDb(NomeMagazzino, SpazioDisponibile);
			Console.WriteLine("--------------------------------------------------");
			Console.WriteLine($"benvenuto nel magazzino {NomeMagazzino}");
			Console.WriteLine("--------------------------------------------------");

		}

		public static void SelectAction()
		{
			Console.WriteLine("scegli cosa vuoi fare:");
			Console.WriteLine("1- Visualizza lo stato del magazzino");
			Console.WriteLine("2- Aggiungi un acquisto da un fornitore");
			Console.WriteLine("3- Scarica il magazzino di una quantità di prodotto");
			Console.WriteLine("4- Controlla capienza magazzino");
			Console.WriteLine("5- Invia una email");
			Console.WriteLine("6- Esci");
			Console.WriteLine("7- Controlla email inviate con successo.");
			Console.WriteLine("------------------------------------------------------");
			Console.WriteLine("Scegli un opzione tra 1 , 2 , 3 , 4 , 5 , 6 , 7");
			var choise = Console.ReadLine();
			ChoiseProp = choise;
		}

		public static void settingSceltaUtente()
		{
			if (int.TryParse(ChoiseProp, out int numero))
			{

				if (numero == 1 || numero == 2 || numero == 3 || numero == 4 || numero == 5 || numero == 6 || numero == 7)
				{
					SceltaUtente = numero;
					InputValido = true;
				}
			}
			else
			{
				Console.WriteLine("--------------------------------------");
				Console.WriteLine("INPUT INVALIDO. RIPROVA");
				Console.WriteLine("--------------------------------------");
				Thread.Sleep(2000);
			}
		}

		public static void VisualizzaStatoMagazzino()
		{
			Console.WriteLine("----------------STATO MAGAZZINO------------------");

			var (listaProd, esito) = _dbConfigManager.GetTuttiProdottiDalMagazzino();

			if (esito)
			{
				foreach (var prod in listaProd)
				{

					Console.WriteLine("---------------------------------------------");
					Console.WriteLine($"PRODOTTO: {prod.NomeProdotto} - QUANTITà: {prod.QtaProdotto}");
					Console.WriteLine($"DIMENSIONI:{prod.DimensSingoloProd} mq - SPAZIO TOTALE OCCUPATO: {prod.DimensTotProdotto} mq");
					Console.WriteLine("---------------------------------------------");

				}
			}
			else
			{
				Console.WriteLine("---------------------------------------------");
				Console.WriteLine("---------------------------------------------");
				Console.WriteLine("--------------MAGAZZINO VUOTO----------------");
				Console.WriteLine("---------------------------------------------");
				Console.WriteLine("---------------------------------------------");
				Thread.Sleep(2000);

			}

			Thread.Sleep(2000);
		}


		public static void AggiungiProdottoMagazzino()
		{
			try
			{

				// check sul nome del prodotto. 
				bool check0 = false;
				while (!check0)
				{
					Console.WriteLine("--------------------------------------------");
					Console.WriteLine("Inserisci il nome del prodotto da aggiungere al magazzino.");
					Console.WriteLine("--------------------------------------------");
					string nomeProdotto = Console.ReadLine().Trim().ToLower();

					if (string.IsNullOrEmpty(nomeProdotto))
					{
						Console.WriteLine("--------------------------------------------");
						Console.WriteLine("input non valido. inserisci caratteri.");
						Console.WriteLine("--------------------------------------------");
						Thread.Sleep(2000);
						continue;
					}

					bool isNumber = false;
					for (var i = 0; i < nomeProdotto.Length; i++)
					{
						char lett = nomeProdotto[i];
						if (char.IsDigit(lett))
						{
							isNumber = true;
						}
					}

					if (isNumber)
					{
						Console.WriteLine("--------------------------------------------");
						Console.WriteLine("il nome del prodotto non può contenere numeri.");
						Console.WriteLine("--------------------------------------------");
						Thread.Sleep(2000);
						continue;
					}

					check0 = true;

					if (check0)
					{
						nomeProdotto_To_Store = nomeProdotto.Trim().ToLower();
						break;
					}
				}

				// check quantità prodotto
				bool check1 = false;
				while (!check1)
				{
					Console.WriteLine("--------------------------------------------");
					Console.WriteLine("Inserisci la quantità di prodotto.");
					Console.WriteLine("--------------------------------------------");
					string quantitaProdotto = Console.ReadLine();

					if (float.TryParse(quantitaProdotto, out float qtaProd))
					{

						if (qtaProd != 0)
						{
							qta_prodotto_to_store = qtaProd;
						}
						else
						{
							Console.WriteLine("--------------------------------------------");
							Console.WriteLine("quantità inserita deve essere un numero.");
							Console.WriteLine("--------------------------------------------");
							Thread.Sleep(2000);
							continue;
						}


					}
					else
					{
						Console.WriteLine("--------------------------------------------");
						Console.WriteLine("quantità inserita deve essere un numero.");
						Console.WriteLine("--------------------------------------------");
						Thread.Sleep(2000);
						continue;
					}

					Console.WriteLine("--------------------------------------------");
					Console.WriteLine("inserisci le dimensioni del SINGOLO prodotto espressa in m2.");
					Console.WriteLine("--------------------------------------------");
					string dimensProd = Console.ReadLine();

					string convertedValue = ConvertToDecimalManual(dimensProd);

					if (float.TryParse(convertedValue, out float floatDimensProd))
					{

						if (floatDimensProd != 0)
						{
							dimens_prodotto_to_store = floatDimensProd;

						}
						else
						{
							Console.WriteLine("--------------------------------------------");
							Console.WriteLine("dimensioni del prodotto non può essere zero.");
							Console.WriteLine("--------------------------------------------");
							Thread.Sleep(2000);
							continue;
						}
					}
					else
					{
						Console.WriteLine("--------------------------------------------");
						Console.WriteLine("dimensioni del prodotto deve essere un numero, intero o decimale.");
						Console.WriteLine("--------------------------------------------");
						Thread.Sleep(2000);
						continue;
					}

					//Prodotto product = new Prodotto(nomeProdotto_To_Store , qta_prodotto_to_store , dimens_prodotto_to_store , )

					// aggiungo ad dict i valori immagazzinati
					ProdottiImmagazzinati.Add(nomeProdotto_To_Store, qta_prodotto_to_store);
					SpazioDisponibile -= dimens_prodotto_to_store * qta_prodotto_to_store;
					SpazioOccupatoProdotti.Add(nomeProdotto_To_Store, dimens_prodotto_to_store * qta_prodotto_to_store);

					//richiamo l'istanza della classe DatabaseConfigManager per fare una insert nel database.
					//_DbConfigManager.InsertRecordintoDb(ProdottiImmagazzinati);
					_dbConfigManager.InsertRecordintoDb(ProdottiImmagazzinati, dimens_prodotto_to_store, dimens_prodotto_to_store * qta_prodotto_to_store, NomeMagazzino);
					ProdottiImmagazzinati.Remove(nomeProdotto_To_Store);
					nomeProdotto_To_Store = null;
					qta_prodotto_to_store = 0;
					Thread.Sleep(2000);
					break;

				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Si è verificato un errore: {ex.Message}");
			}
		}

		public static void RimuoviProdottoMagazzino()
		{

			var (listaProdotti ,esito) =_dbConfigManager.GetTuttiProdottiDalMagazzino();

			if (listaProdotti.Count > 0 && esito)
			{
				Console.WriteLine("--------------------------------------------");
                Console.WriteLine("PRODOTTI IMMAGAZZINATI:");
				foreach (var prodotto in listaProdotti)
				{
					Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Nome Prodotto : {prodotto.NomeProdotto} -  Quantità immagazzinata: {prodotto.QtaProdotto}" +
						$" - Singolo spazio occupato: {prodotto.DimensSingoloProd}  - Totale spazio occupato: {prodotto.DimensTotProdotto}");
					Console.ResetColor();
				}
                Console.WriteLine("--------------------------------------------");
            }

            Console.WriteLine("--------------------------------------------");
			Console.WriteLine("inserisci il nome del prodotto che vuoi eliminare");
			Console.WriteLine("--------------------------------------------");
			string nomeProdotto = Console.ReadLine();

			Console.WriteLine("--------------------------------------------");
			Console.WriteLine("inserisci la quantita da eliminare.");
			Console.WriteLine("--------------------------------------------");
			string quantita = Console.ReadLine();

			if (float.TryParse(quantita, out float qta))
			{
				var esiti = _dbConfigManager.RimuoviQuantitaProdottoDalDb(nomeProdotto.Trim().ToLower(), qta, NomeMagazzino);
				esiti.ShowEsiti();
			}
			else
			{
				Console.WriteLine("--------------------------------------------");
				Console.WriteLine("nome prodotto o quantita inserita non compatibile");
				Console.WriteLine("--------------------------------------------");
			}
		}


		public static void ControllaCapienzaMagazzino()
		{

			_dbConfigManager.ControllaStatoCapienzaMagazzino(NomeMagazzino);
			Thread.Sleep(2000);
		}

		public static void Esci()
		{
			Console.WriteLine("ARRIVEDERCI...");
			Thread.Sleep(2000);
			Environment.Exit(0);
		}

		private static float calcolaTotSpazioOccupatoProds()
		{
			float spazioOccupato = 0;

			foreach (var prodotto in SpazioOccupatoProdotti)
			{
				spazioOccupato += prodotto.Value;
			}

			return spazioOccupato;
		}
		// se il valore di spazio occupato viene scritto con punto anzichè con virgola , tramuto il punto con la virgola.
		private static string ConvertToDecimalManual(string value)
		{
			string newValue = "";

			for (var i = 0; i < value.Length; i++)
			{
				char c = value[i];

				if (c == '.')
				{
					c = ',';
				}

				newValue += c;

			}

			return newValue;
		}

		public static void InviaEmailDalMagazzino()
		{
			bool check0 = false;
			bool check1 = false;
			bool check2 = false;
			string email;
			string oggetto;
            string testo;

            do
			{
				Console.WriteLine("inserisci l'email del destinatario");
				 email = Console.ReadLine();

                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("campo email non valido.");
					continue;
                }
                else
                {
                    check0 = true;
                }
            } while (!check0);


            do
            {
                Console.WriteLine("inserisci l'oggetto della mail");
                oggetto = Console.ReadLine();

                if (string.IsNullOrEmpty(oggetto))
                {
                    Console.WriteLine("campo oggetto non valido.");
                    continue;
                }
                else
                {
                    check1 = true;
                }
            } while (!check1);

            do
            {
                Console.WriteLine("inserisci i testo della mail");
                testo = Console.ReadLine();

                if (string.IsNullOrEmpty(testo))
                {
                    Console.WriteLine("campo testo non valido.");
                    continue;
                }
                else
                {
                    check2 = true;
                }
            } while (!check2);

            _sendEmail.SendEmail_classEmail(email, oggetto, testo);
		}

		public static void ControllaEMailInviate()
		{
			_sendEmail.CheckIndirizziEmailSent();
        }
	}
}

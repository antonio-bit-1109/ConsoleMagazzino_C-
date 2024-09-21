using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace ConsoleMagazzino
{
	class DatabaseConfigManager : IDatabaseConfigManager_Interface
	{
		//proprietà della classe
		private readonly string _connectionString;

		// costruttore
		public DatabaseConfigManager()
		{
			_connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
		}

		private string GetConnectionString()
		{
			return _connectionString;
		}

		// il metodo ritorna una tupla (boolean , connection)
		public (bool, OracleConnection) OpenConnectionDb()
		{
			OracleConnection connection = new OracleConnection(_connectionString);
			try
			{

				//await connection.OpenAsync();
				connection.Open();
				if (connection.State == System.Data.ConnectionState.Open)
				{
					Console.WriteLine("------------------------------------");
					Console.WriteLine("connessione con il db correttamente eseguita.");
					Console.WriteLine("------------------------------------");
					return (true, connection);
				}

				throw new Exception("connessione con il db non riuscita");

			}
			catch (Exception ex)
			{
				Console.WriteLine("------------------------------------");
				Console.WriteLine(ex.ToString());
				Console.WriteLine("------------------------------------");
				return (false, null);
			}
		}


		public bool ClosingConnection(OracleConnection connection)
		{
			try
			{

				if (connection != null && connection.State == System.Data.ConnectionState.Open)
				{
					connection.Close();
					Console.WriteLine("------------------------------------");
					Console.WriteLine("Connessione con il db chiusa correttamente.");
					Console.WriteLine("------------------------------------");
					return true;
				}

				throw new Exception("impossibile chiudere la connessione al db.");

			}
			catch (Exception ex)
			{
				Console.WriteLine("------------------------------------");
				Console.WriteLine($"Errore durante la chiusura della connessione: {ex.Message}");
				Console.WriteLine("------------------------------------");
				return false;
			}
		}

		public bool InsertRecordintoDb(Dictionary<string, float> ProdottoDictionary, float dimensioniProdotto, float dimensioniTotProdotto, string nomeMagazzino)
		{
			var (result, conn) = OpenConnectionDb();

			try
			{

				if (conn == null)
				{
					throw new Exception("impossibile stabile connessione al db. connessione null.");
				}

				if (ProdottoDictionary.Count != 0)
				{
					foreach (var prodotto in ProdottoDictionary)
					{
						var commandText = $" INSERT INTO PRODOTTI (NOME_PRODOTTO , QTA , DIMENSIONI_MQ ,DIMENSIONI_MQ_TOT ) " +
						$"VALUES (:nomeProdotto , :quantita , :dimensione_singoloProd , :dimensione_totProd)";

						using (var command = new OracleCommand(commandText, conn))
						{
							command.Parameters.Add(new OracleParameter("nomeProdotto", prodotto.Key));
							command.Parameters.Add(new OracleParameter("quantita", prodotto.Value));
							command.Parameters.Add(new OracleParameter("dimensione", dimensioniProdotto));
							command.Parameters.Add(new OracleParameter("dimensione_totProd", dimensioniTotProdotto));
							command.ExecuteNonQuery();

						}
					}
					AggiornaCapienzaMagazzino(conn, dimensioniTotProdotto, nomeMagazzino, '-');
					return true;
				}
				else
				{
					throw new Exception("impossibile eseguire l'insert. IL dictionary è vuoto.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"errore durante l'insert nel database: {ex.Message}");
				return false;
			}
			finally
			{
				ClosingConnection(conn);
			}
		}

		public bool InizializzaMagazzinoDb(string nomeMagazzino, float capienzaIniziale)
		{
			var (result, conn) = OpenConnectionDb();

			try
			{

				var recordPresent = IsRecord_Magazzino_delle_favole_AlreadyPresent(conn, nomeMagazzino);

				if (!recordPresent)
				{

					Console.WriteLine("----------------------------------------------------------");
					Console.WriteLine($"tentativo inizializzazione magazzino : {nomeMagazzino}");
					Console.WriteLine("----------------------------------------------------------");

					var commandText = $" INSERT INTO MAGAZZINO (NOME_MAGAZZINO , CAPIENZA ) " +
									$"VALUES (:nomeMagazzino , :capienza )";
					using (var command = new OracleCommand(commandText, conn))
					{
						command.Parameters.Add(new OracleParameter("nomeMagazzino", nomeMagazzino));
						command.Parameters.Add(new OracleParameter("capienza", capienzaIniziale));
						command.ExecuteNonQuery();

					}

					Console.WriteLine("----------------------------------------------------------");
					Console.WriteLine($"inizializzazione magazzino : {nomeMagazzino} avvenuta con successo.");
					Console.WriteLine("----------------------------------------------------------");
					return true;
				}
				else
				{
					return false;
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"errore durante inizializzazione magazzino: {ex.Message}");
				return false;
			}
			finally
			{
				ClosingConnection(conn);
			}

		}

		public bool IsRecord_Magazzino_delle_favole_AlreadyPresent(OracleConnection conn, string nomeMagazzino)
		{
			try
			{
				var commandText = "SELECT COUNT(*) FROM MAGAZZINO WHERE NOME_MAGAZZINO = :nomeMagazzino";
				using (var command = new OracleCommand(commandText, conn))
				{
					command.Parameters.Add(new OracleParameter("nomeMagazzino", nomeMagazzino));
					int count = Convert.ToInt32(command.ExecuteScalar());

					if (count > 0)
					{
						return true;
					}

					return false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"errore durante l'inseriemento del record: {ex.Message}");
				return true;
			}
		}


		public bool AggiornaCapienzaMagazzino(OracleConnection conn, float dimensioniTotProdotto, string nomeMagazzino, char segno)
		{
			try
			{

				var commandText = $"UPDATE MAGAZZINO SET CAPIENZA = (CAPIENZA {segno} :dimensioniTotProdotto) WHERE NOME_MAGAZZINO = :nomeMagazzino";
				using (var command = new OracleCommand(commandText, conn))
				{
					command.Parameters.Add(new OracleParameter("dimensioniTotProdotto", dimensioniTotProdotto));
					command.Parameters.Add(new OracleParameter("nomeMagazzino", nomeMagazzino));
					int rowsAffected = command.ExecuteNonQuery();

					if (rowsAffected > 0)
					{
						Console.WriteLine("Capienza del magazzino aggiornata correttamente.");
						return true;
					}

					throw new Exception("Nessun magazzino trovato con il nome specificato.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"errore durante aggiornamento capienza magazzino: {ex.Message}");
				return false;
			}
		}


		public (List<Prodotto>, bool) GetTuttiProdottiDalMagazzino()
		{
			var (result, conn) = OpenConnectionDb();
			var prodottiList = new List<Prodotto>();

			try
			{

				var commandText = "SELECT NOME_PRODOTTO, QTA, DIMENSIONI_MQ, DIMENSIONI_MQ_TOT FROM PRODOTTI";
				using (var command = new OracleCommand(commandText, conn))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{

							Prodotto product = new Prodotto()
							{
								NomeProdotto = reader.GetString(reader.GetOrdinal("NOME_PRODOTTO")),
								QtaProdotto = reader.GetFloat(reader.GetOrdinal("QTA")),
								DimensSingoloProd = reader.GetFloat(reader.GetOrdinal("DIMENSIONI_MQ")),
								DimensTotProdotto = reader.GetFloat(reader.GetOrdinal("DIMENSIONI_MQ_TOT"))
							};

							prodottiList.Add(product);
						}

						if (prodottiList.Count > 0)
						{
							return (prodottiList, true);
						}
						else
						{
							return (prodottiList, false);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Errore durante la get di tutti i prodotti dal database. Errore: {ex.Message}");
				return (prodottiList, false);
			}
			finally
			{
				ClosingConnection(conn);

			}
		}

		public void ControllaStatoCapienzaMagazzino(string nomeMagazzino_to_find)
		{
			var (result, conn) = OpenConnectionDb();

			try
			{

				var commandText = "SELECT NOME_MAGAZZINO, CAPIENZA FROM MAGAZZINO WHERE NOME_MAGAZZINO = :nomeMagazzino";
				using (var command = new OracleCommand(commandText, conn))
				{
					command.Parameters.Add(new OracleParameter("nomeMagazzino", nomeMagazzino_to_find));

					using (var reader = command.ExecuteReader())
					{

						if (reader.HasRows)
						{

							while (reader.Read())
							{

								string nomeMagazzino = reader.GetString(reader.GetOrdinal("NOME_MAGAZZINO"));
								float capienza = reader.GetFloat(reader.GetOrdinal("CAPIENZA"));


								Console.WriteLine("--------------------------------------------------------");
								Console.WriteLine("--------------------------------------------------------");
								Console.WriteLine($"NOME MAGAZZINO: {nomeMagazzino.ToUpper()} -- CAPIENZA RIMASTA: {capienza} mq");
								Console.WriteLine("--------------------------------------------------------");
								Console.WriteLine("--------------------------------------------------------");
							}

						}
						else
						{

							throw new Exception("impossibile trovati dati per il magazzino specificato.");
						}
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Errore durante il controllo della capienza del magazzino. Errore: {ex.Message}");
			}
			finally
			{
				ClosingConnection(conn);

			}
		}


		// NON VIENE SUPERATA LA SECONDA QUERY (UPDATE) PER MODIFICARE I  VALORI DEL PRODOTTO , I VALORI NUMERICI PASSATI SEMBRANO NON ESSERE VALIDI 
		public Triplet RimuoviQuantitaProdottoDalDb(string nomeProdotto, float quantitaDaEliminare, string nomeMagazzino)
		{
			// inizializzo la tripletta che mi serve per capire dove sia eventuale errore. 
			Triplet triplettaErrori = new Triplet();

			var (result, conn) = OpenConnectionDb();

			if (!result || conn == null)
			{
				throw new Exception("Impossibile stabilire una connessione al database.");
			}

			OracleTransaction transaction = null;

			float qta = 0;
			float dimensioniSingProd = 0;
			float dimensioniTotProd = 0;

			try
			{
				transaction = conn.BeginTransaction();

				// ricavo le info del prodotto 
				var commandText0 = "SELECT QTA, DIMENSIONI_MQ, DIMENSIONI_MQ_TOT FROM PRODOTTI WHERE NOME_PRODOTTO = :nomeProdotto";

				using (var command = new OracleCommand(commandText0, conn))
				{
					command.Parameters.Add(new OracleParameter("nomeProdotto", nomeProdotto));

					using (var reader = command.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read())
							{
								qta = reader.GetFloat(reader.GetOrdinal("QTA"));
								dimensioniSingProd = reader.GetFloat(reader.GetOrdinal("DIMENSIONI_MQ"));
								dimensioniTotProd = reader.GetFloat(reader.GetOrdinal("DIMENSIONI_MQ_TOT"));
							}

							Console.WriteLine("Prodotto correttamente individuato.");
						}
						else
						{
							triplettaErrori.PopolateV1(true);
							triplettaErrori.PopolateV2(false);
							triplettaErrori.PopolateV3(false);
							throw new Exception("impossibile trovare il prodotto specificato.");
						}
					}
				}

				if (float.IsNaN(quantitaDaEliminare) || float.IsInfinity(quantitaDaEliminare))
				{
					throw new Exception("Valore non valido per la quantità da eliminare.");
				}

				//using (var command = new OracleCommand("UPDATE_PRODOTTO", conn))
				//{
				//	command.CommandType = CommandType.StoredProcedure;
				//	command.Parameters.Add(new OracleParameter("nomeProdotto_param", OracleDbType.Varchar2)).Value = nomeProdotto;
				//	command.Parameters.Add(new OracleParameter("qtaDaEliminare_param", OracleDbType.Decimal)).Value = quantitaDaEliminare;
				//	//command.Parameters.Add(new OracleParameter("qtaDaEliminare2", OracleDbType.Decimal) { Value = (decimal)quantitaDaEliminare });

				//	var righeAggiornateParam = new OracleParameter("righeAggiornate", OracleDbType.Int32)
				//	{
				//		Direction = ParameterDirection.Output
				//	};
				//	command.Parameters.Add(righeAggiornateParam);

				//	// Log dei valori dei parametri
				//	//Console.WriteLine($"nomeProdotto: {nomeProdotto}");
				//	//Console.WriteLine($"qtaDaEliminare1: {quantitaDaEliminare}, Tipo: {quantitaDaEliminare.GetType()}, Valore: {quantitaDaEliminare}");
				//	//Console.WriteLine($"qtaDaEliminare2: {quantitaDaEliminare}, Tipo: {quantitaDaEliminare.GetType()}, Valore: {quantitaDaEliminare}");

				//	command.ExecuteNonQuery();

				//	// Recupera il valore del parametro di output
				//	int righeAggiornate = Convert.ToInt32(righeAggiornateParam.Value);

				//	if (righeAggiornate > 0)
				//	{
				//		Console.WriteLine($"Procedura eseguita con successo. Numero di righe aggiornate: {righeAggiornate}");
				//	}
				//	else
				//	{
				//		Console.WriteLine("Nessuna riga modificata. Verifica i parametri della procedura.");
				//	}

				//	Console.WriteLine("prodotto correttamente modificato.");
				//}



				// aggiorno lo spazio presente nel magazzino
				bool esito = AggiornaCapienzaMagazzino(conn, dimensioniTotProd, nomeMagazzino, '+');

				if (esito)
				{
					transaction.Commit();
					Console.WriteLine("---------------------------------------------------");
					Console.WriteLine("Quantità del prodotto e capienza del magazzino aggiornate con successo.");
					Console.WriteLine("---------------------------------------------------");
					triplettaErrori.PopolateV1(false);
					triplettaErrori.PopolateV2(false);
					triplettaErrori.PopolateV3(false);
					return triplettaErrori;
				}
				else
				{
					triplettaErrori.PopolateV1(false);
					triplettaErrori.PopolateV2(false);
					triplettaErrori.PopolateV3(true);
					throw new Exception("impossibile aggiornare la capienza del magazzino.");
				}
			}
			catch (Exception ex)
			{
				transaction?.Rollback();
				Console.WriteLine("---------------------------------------------------");
				Console.WriteLine($"Errore durante la modifica di un prodotto dal magazzino. Errore: {ex.Message}");
				Console.WriteLine($"RollBack delle modifiche.");
				Console.WriteLine("---------------------------------------------------");
				return triplettaErrori;
			}
			finally
			{
				ClosingConnection(conn);
			}
		}


	}
}
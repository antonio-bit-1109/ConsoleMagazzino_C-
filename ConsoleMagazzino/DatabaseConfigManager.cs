using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;


namespace ConsoleMagazzino
{
	class DatabaseConfigManager : IDatabaseConfigManager_Interface
	{
		//proprietà della classe
		private readonly string _connectionString;
		private Timer _timer;

		// costruttore
		public DatabaseConfigManager()
		{
			_connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            _timer = new Timer(NotificaCApienzaCallBack, null, 0, 10000);
        }

		private void NotificaCApienzaCallBack (object state)
		{
			NotificaCapienza();
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

			var transaction = conn.BeginTransaction();


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
						//var commandText = $" INSERT INTO PRODOTTI (NOME_PRODOTTO , QTA , DIMENSIONI_MQ ,DIMENSIONI_MQ_TOT ) " +
						//$"VALUES (:nomeProdotto , :quantita , :dimensione_singoloProd , :dimensione_totProd)";

						// check se il magazzino non sia gia pieno o che il prodotto che si vuole inserire superi la capacita del magazzino.
						using (var command = new OracleCommand("CHECK_CAPIENZA_MAGAZZINO", conn))
						{
							command.CommandType = CommandType.StoredProcedure;
                            //parametri input
							command.Parameters.Add(new OracleParameter("nomeMagazzino", nomeMagazzino));

                            //parametro output
							var capienzaResidua_Param = new OracleParameter("capienzaResidua", OracleDbType.Decimal)
                            {
                                Direction = ParameterDirection.Output
                            };
							command.Parameters.Add(capienzaResidua_Param);
                            command.ExecuteNonQuery();

							if (capienzaResidua_Param.Value != DBNull.Value)
							{
								var oracleValue = (OracleDecimal)capienzaResidua_Param.Value;
                                float capienzaResidua = Convert.ToSingle(oracleValue.Value);

								if (capienzaResidua < dimensioniTotProdotto)
								{
									throw new Exception("impossibile inserire il prodotto. Capienza magazzino insufficiente.");
                                }
                            }
                        }

                        using (var command = new OracleCommand("INSERT_INTO_DB_PRODOTTO", conn))
						{
							command.CommandType = CommandType.StoredProcedure;	

							//parametri di input 
                            command.Parameters.Add(new OracleParameter("nomeProdotto", prodotto.Key));
							command.Parameters.Add(new OracleParameter("quantita", prodotto.Value));
							command.Parameters.Add(new OracleParameter("dimensione", dimensioniProdotto));
							command.Parameters.Add(new OracleParameter("dimensione_totProd", dimensioniTotProdotto));

							//parametro di output della store procedure 
							var righeAggiornate_Param = new OracleParameter("righeAggiornate", OracleDbType.Int32)
                            {
                                Direction = ParameterDirection.Output
                            };
							command.Parameters.Add(righeAggiornate_Param);
                            command.ExecuteNonQuery();


							if (righeAggiornate_Param.Value != DBNull.Value)
							{
								var oracleValue = (OracleDecimal)righeAggiornate_Param.Value;
                                int righeAggiornate = Convert.ToInt32(oracleValue.Value);

                                if ( righeAggiornate != 1 ) 
								{
									throw new Exception("Errore durante l'insert del prodotto nel database.");
                                }
                              
                            } 

							

                        }
					}
					AggiornaCapienzaMagazzino(conn, dimensioniTotProdotto, nomeMagazzino, '-');
                    Console.WriteLine("--------------------------------------------");
                    Console.WriteLine("Prodotto aggiunto con successo al magazzino.");
                    Console.WriteLine("--------------------------------------------");

                    transaction.Commit();
                    return true;
				}
				else
				{
					throw new Exception("impossibile eseguire l'insert. IL dictionary è vuoto.");
				}
			}
			catch (Exception ex)
			{
                transaction?.Rollback();
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

					//var commandText = $" INSERT INTO MAGAZZINO (NOME_MAGAZZINO , CAPIENZA ) " +
					//				$"VALUES (:nomeMagazzino , :capienza )";
					using (var command = new OracleCommand("INIZIALIZZAZIONE_MAGAZZINO", conn))
					{
						// PARAMETRI INPUT 
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new OracleParameter("nomeMagazzino", nomeMagazzino));
						command.Parameters.Add(new OracleParameter("capienza", capienzaIniziale));

                        //parametro di output
                        var rigaInserita_Param = new OracleParameter("rigaInserita", OracleDbType.Int32)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(rigaInserita_Param);
						
                        command.ExecuteNonQuery();

						if (rigaInserita_Param.Value != DBNull.Value)
                        {
                            var oracleValue = (OracleDecimal)rigaInserita_Param.Value;
                            int rigaInserita = Convert.ToInt32(oracleValue.Value);

                            if (rigaInserita != 1)
                            {
                                throw new Exception("Errore durante l'inserimento del record nel database.");
                            }
                        }

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

			//float qta = 0;
			//float dimensioniSingProd = 0;
			//float dimensioniTotProd = 0;

			try
			{
				transaction = conn.BeginTransaction();

				if (float.IsNaN(quantitaDaEliminare) || float.IsInfinity(quantitaDaEliminare))
				{
					throw new Exception("Valore non valido per la quantità da eliminare.");
				}

				int righeAggiornate = 0;
				int differenzaMetriQuadri = 0;

				using (var command = new OracleCommand("UPDATE_PRODOTTO", conn))
				{
					command.CommandType = CommandType.StoredProcedure;

					// aggiungi parametri di input
					command.Parameters.Add(new OracleParameter("nomeProdotto_param", OracleDbType.Varchar2)).Value = nomeProdotto;
					command.Parameters.Add(new OracleParameter("qtaDaEliminare_param", OracleDbType.Decimal)).Value = quantitaDaEliminare;

					// definisci i parametri di output
					var righeAggiornate_Param = new OracleParameter("righeAggiornate", OracleDbType.Int32)
					{
						Direction = ParameterDirection.Output
					};

					var differenzaMetriQuadri_Param = new OracleParameter("differenzaMetriQuadri", OracleDbType.Int32)
					{
						Direction = ParameterDirection.Output
					};
					command.Parameters.Add(righeAggiornate_Param);
					command.Parameters.Add(differenzaMetriQuadri_Param);

					// esegui la store procedure
					command.ExecuteNonQuery();

					// Recupera i valori dei parametri di output
					//int righeAggiornate = 0;
					//int differenzaMetriQuadri = 0;

					if (righeAggiornate_Param.Value != DBNull.Value && differenzaMetriQuadri_Param.Value != DBNull.Value)
					{
						var oracleDecimalValue = (OracleDecimal)righeAggiornate_Param.Value;
						righeAggiornate = Convert.ToInt32(oracleDecimalValue.Value);

						var oracleDecimalValue2 = (OracleDecimal)differenzaMetriQuadri_Param.Value;
						differenzaMetriQuadri = Convert.ToInt32(oracleDecimalValue2.Value);
					}


					if (righeAggiornate == 1)
					{
						Console.WriteLine($"Procedura eseguita con successo. Numero di righe aggiornate: {righeAggiornate}");
					}
					else if (righeAggiornate == 2)
					{
						Console.WriteLine("Procedura eseguita con successo.");
						Console.WriteLine("hai rimosso interamente il prodotto dal magazzino.");
					}
					else
					{
						Console.WriteLine("Nessuna riga modificata. Verifica i parametri della procedura.");
					}

					Console.WriteLine("Prodotto correttamente modificato.");
				}


				if (differenzaMetriQuadri == 0)
				{
					throw new Exception("errore nel calcolo dei metri quadrati da restituire al magazzino.");
				};


				float spazioDaRestituire = (float)differenzaMetriQuadri;

				// aggiorno lo spazio presente nel magazzino
				bool esito = AggiornaCapienzaMagazzino(conn, spazioDaRestituire, nomeMagazzino, '+');

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

        public async void NotificaCapienza()
        {
            var (result, conn) = OpenConnectionDb();
            var transaction = conn.BeginTransaction();
            try
            {
                using (var command = new OracleCommand("NOTIFICA_CAPIENZA", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    //parametri di output
                    var capienzaMagazzino_param = new OracleParameter("capienzaMagazzino", OracleDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(capienzaMagazzino_param);

                    await command.ExecuteNonQueryAsync();

                    if (capienzaMagazzino_param.Value != DBNull.Value)
                    {
                        var oracleCapienzaValue = (OracleDecimal)capienzaMagazzino_param.Value;
                        float capienzaResiduaMagazzino = Convert.ToSingle(oracleCapienzaValue.Value);
						Console.WriteLine("                                                         "); 
						Console.WriteLine("--------------MESSAGGIO AUTOMATICO ------------");
						Console.WriteLine("---------------------------------------------------");
                        Console.WriteLine($"Capienza residua del magazzino: {capienzaResiduaMagazzino} mq");
                        Console.WriteLine("---------------------------------------------------");
                      
                    }
                }

                transaction.Commit();
                
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Console.WriteLine($"Errore durante il controllo capienza Magazzino. Errore: {ex.Message}");
                
            }
            finally
            {
                ClosingConnection(conn);
            }
        }
	}
}
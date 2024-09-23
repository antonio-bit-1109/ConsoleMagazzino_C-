using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleMagazzino
{
    internal interface IDatabaseConfigManager_Interface
    {

        (bool, OracleConnection) OpenConnectionDb();
        bool ClosingConnection(OracleConnection connection);
        bool InsertRecordintoDb(Dictionary<string, float> ProdottoDictionary, float dimensioniProdotto, float dimensioniTotProdotto, string nomeMagazzino);
        bool InizializzaMagazzinoDb(string nomeMagazzino, float capienzaIniziale);
        bool IsRecord_Magazzino_delle_favole_AlreadyPresent(OracleConnection conn, string nomeMagazzino);
        bool AggiornaCapienzaMagazzino(OracleConnection conn, float nuovaCapienza, string nomeMagazzino, char segno);
        (List<Prodotto>, bool) GetTuttiProdottiDalMagazzino();
        void ControllaStatoCapienzaMagazzino(string nomeMagazzino_to_find);
        Triplet RimuoviQuantitaProdottoDalDb(string nomeProdotto, float quantitaDaEliminare, string nomeMagazzino);
        void NotificaCapienza();

    }
}

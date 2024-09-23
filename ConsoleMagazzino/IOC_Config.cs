using Microsoft.Extensions.DependencyInjection;

namespace ConsoleMagazzino
{
	// classe per Inversion of Control
	internal static class IOC_Config
	{
		// classe per implementare l'inversion of control per la dependency injection,
		// ovvero quella classe che si occupa di creare e distruggere in automatico un istanza di una classe, 
		// senza doverla istanziare a mano. 

		public static ServiceProvider Configure()
		{
			var serviceProvider = new ServiceCollection();
			//aggiunge un servizio al contenitore IoC,
			//specificando che ogni volta che viene richiesta un'istanza di IDatabaseConfigManager_Interface,
			//il contenitore deve fornire un'istanza di DatabaseConfigManager.

			//Quando utilizzi AddSingleton,
			//stai specificando che il contenitore IoC deve creare una sola istanza della classe e riutilizzarla per tutte le richieste future di quella dipendenza.
			//In altre parole, verrà creata una singola istanza di DatabaseConfigManager
			//e questa stessa istanza verrà utilizzata ogni volta che viene richiesta un'istanza di IDatabaseConfigManager_Interface.
			serviceProvider.AddSingleton<IDatabaseConfigManager_Interface, DatabaseConfigManager>();
            serviceProvider.AddSingleton<IEmail_Interface, InvioEmail>();
            return serviceProvider.BuildServiceProvider();
		}
	}
}

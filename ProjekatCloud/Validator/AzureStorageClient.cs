using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public class AzureStorageClient
{
    private readonly CloudStorageAccount storageAccount;
    private readonly CloudTableClient tableClient;

    public AzureStorageClient(string connectionString)
    {
        this.storageAccount = CloudStorageAccount.Parse(connectionString);
        this.tableClient = storageAccount.CreateCloudTableClient();
    }

    public CloudTable GetTableReference(string tableName)
    {
        return tableClient.GetTableReference(tableName);
    }

    public async Task DodajProizvodAsync(Proizvod proizvod)
    {
        CloudTable proizvodiTable = GetTableReference("Proizvodi");

        // Kreiraj tabelu ako ne postoji
        await proizvodiTable.CreateIfNotExistsAsync();

        ProizvodEntity proizvodEntity = new ProizvodEntity
        {
            Id = proizvod.Id,
            KategorijaProizvoda = proizvod.KategorijaProizvoda,
            NazivProizvoda = proizvod.NazivProizvoda,
            OpisProizvoda = proizvod.OpisProizvoda,
            CijenaProizvoda = proizvod.CijenaProizvoda,
            KolicinaProizvoda = proizvod.KolicinaProizvoda
        };

        TableOperation insertOperation = TableOperation.Insert(proizvodEntity);

        await proizvodiTable.ExecuteAsync(insertOperation);
    }
}

using System.Text.Json;
using Azure.Storage.Queues;
using CurrencyLotManagementLibrary.Enums;
using CurrencyLotManagementLibrary.Models.Entities;
using CurrencyLotManagementSystem.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace CurrencyLotManagementSystem.Services;

public class AzureQueueLotManager : ILotManager
{
    private readonly QueueClient queueClient;

    public AzureQueueLotManager(QueueServiceClient queueServiceClient, IOptions<Configuration.Azure> azureOptions)
    {
        var queueName = azureOptions.Value.QueueName;
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(azureOptions));
        
        queueClient = queueServiceClient.GetQueueClient(queueName);
        queueClient.CreateIfNotExists();
    }

    public Task AddAsync(Lot lot)
    {
        return queueClient.SendMessageAsync(messageText: JsonSerializer.Serialize(lot), visibilityTimeout: TimeSpan.FromSeconds(1), timeToLive: lot.ExpirationTime);
    }


    public async Task<IEnumerable<Lot>> GetByCurrencyAsync(Currency currency)
    {
        var serializedLots = (await queueClient.ReceiveMessagesAsync(maxMessages: 10, visibilityTimeout: TimeSpan.FromSeconds(10))).Value;
        if (serializedLots?.Any() is not true)
        {
            return new List<Lot>();
        }

        var mappedLots =
            serializedLots.Select(serializedLot =>
            {
                var mappedLot = JsonSerializer.Deserialize<Lot>(serializedLot.MessageText)!;
                mappedLot.Id = serializedLot.MessageId;
                mappedLot.PopReceipt = serializedLot.PopReceipt;

                return mappedLot;
            });

        var filteredLots = mappedLots.Where(lot => lot.Currency == currency);

        return filteredLots;
    }

    public Task DeleteAsync(string id, string popReceipt)
    {
        return queueClient.DeleteMessageAsync(id, popReceipt);
    }
}
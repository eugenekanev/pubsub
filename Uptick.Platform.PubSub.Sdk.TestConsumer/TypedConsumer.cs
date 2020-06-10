using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.TestConsumer
{
    public class TypedConsumer : IConsumer<BookingCreated>
    {

        public async Task ConsumeAsync(IntegrationEvent<BookingCreated> integrationEvent)
        {
            Console.WriteLine("New Message BookingCreated has been recieved");
            Console.WriteLine(integrationEvent.ToString());
            Console.WriteLine($"BookingName is {integrationEvent.Content.BookingName}");
            await Task.Delay(10000);
        }
    }
}

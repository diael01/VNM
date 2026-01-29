using MassTransit;
using EventBusCore.Events;
using System;
using System.Threading.Tasks;

namespace Dashboard.Consumers
{
    public class DashboardConsumer : IConsumer<DashboardStatusEvent>
    {
        public Task Consume(ConsumeContext<DashboardStatusEvent> context)
        {
            // Handle the event, e.g., log it or update a database
            Console.WriteLine($"[DashboardConsumer] Received event: {context.Message.Message} at {context.Message.Timestamp}");

            return Task.CompletedTask;
        }
    }
}
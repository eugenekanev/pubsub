using EasyNetQ;
using Serilog;
using System;
using System.Collections.Concurrent;
using EasyNetQ.Events;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    /// <summary>
    /// Connection bus
    /// </summary>
    public class EasyNetQBroker : IBroker
    {
        /// <summary>actions that run on bus disconnect</summary>
        /// <remarks>second parameter is not used</remarks>
        private readonly ConcurrentDictionary<Action, bool> _disconnectedActions;

        /// <summary>custom logger</summary>
        private readonly ILogger _logger = Log.ForContext<EasyNetQSubscriber>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyNetQBroker"/> class.
        /// </summary>
        /// <param name="createBus">create bus method</param>
        /// <param name="connectionString">rabbitmq connection string</param>
        /// <param name="exchangeName">exchange name</param>
        /// <param name="tombQueue">queue name</param>
        /// <param name="environmentNamingConventionController">environment naming convention controller</param>
        public EasyNetQBroker(
            Func<string, EventHandler, EventHandler, Action<IServiceRegister>, IAdvancedBus> createBus, 
            string connectionString,string exchangeName, string tombQueue, 
            IEnvironmentNamingConventionController environmentNamingConventionController)
        {
            _disconnectedActions = new ConcurrentDictionary<Action, bool>();

            Bus = createBus(
                connectionString,
                (s, e) =>
                    {
                        _logger.Information("Rabbitmq bus connected");
                    },
                (s, e) =>
                    {
                        _logger.Information("Rabbitmq bus disconnected");

                        foreach (var record in _disconnectedActions.ToArray())
                        {
                            try
                            {
                                record.Key?.Invoke();
                            }
                            catch (Exception exp)
                            {
                                _logger.Error(exp, $"Failed to execute disconnect callback {exp.Message}");
                            }
                            finally
                            {
                                // call action only one time
                                _disconnectedActions.Remove(record.Key);
                            }
                        }
                    },
                register => register.Register<IEventBus>(provider =>
                    {
                        var eventBus = new EventBus();
                        eventBus.Subscribe<StartConsumingFailedEvent>(
                            x =>
                            {
                                _logger.Error($"Failed to connect to queue {x.Queue.Name}");
                            });
                        return eventBus;
                    }));

            var conventions = Bus.Container.Resolve<IConventions>();
            if (conventions != null)
            {
                conventions.ErrorQueueNamingConvention = () => environmentNamingConventionController.GetQueueName(tombQueue);
                conventions.ErrorExchangeNamingConvention = messageReceivedInfo => exchangeName + "_error";
            }
        }

        /// <summary>Gets or sets connection bus</summary>
        public IAdvancedBus Bus { get; set; }

        /// <summary>
        /// The register disconnected action.
        /// </summary>
        /// <param name="action">add action on disconnect</param>
        public void RegisterDisconnectedAction(Action action)
        {
            _disconnectedActions.Add(action, false);
        }
    }
}

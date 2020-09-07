using System;
using System.IO;
using Confluent.Kafka;
using System.Threading.Tasks;
using Shared;

namespace Producer
{
     public class Program
    {
        public static async Task Main(string[] args)
        {
            string brokerList = Common.GetConfigValue("bootstrap.servers");
            string topicName = Common.GetConfigValue("topic");

            var config = new ProducerConfig
            {
                BootstrapServers = brokerList,
                Partitioner = Partitioner.Consistent
                
            };

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine($"Producer {producer.Name} producing on topic {topicName}.");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine("Ctrl-C to quit.\n");

                var cancelled = false;
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cancelled = true;
                };

                while (!cancelled)
                {
                    Console.Write(DateTime.Now);

                    var key = DateTime.Now.Date.ToLongDateString();
                    var val = DateTime.Now.ToLongTimeString();

                        try
                    {
                        // Note: Awaiting the asynchronous produce request below prevents flow of execution
                        // from proceeding until the acknowledgement from the broker is received (at the 
                        // expense of low throughput).
                        var deliveryReport = await producer.ProduceAsync(
                            topicName, new Message<string, string> { Key = key, Value = val });

                        Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset}");
                    }
                    catch (ProduceException<string, string> e)
                    {
                        Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(3));
                }

                // Since we are producing synchronously, at this point there will be no messages
                // in-flight and no delivery reports waiting to be acknowledged, so there is no
                // need to call producer.Flush before disposing the producer.
            }
        }
    }
}

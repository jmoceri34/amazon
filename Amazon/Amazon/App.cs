using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Amazon
{
    public class App
    {
        private readonly ISqs sqs;
        private System.Timers.Timer timer;

        public App(ISqs sqs)
        {
            this.sqs = sqs;
        }

        public async Task Run()
        {
            Console.Read();

            // use our test fifo queue url
            var queueUrl = "https://sqs.region.amazonaws.com/your-test-queue/TestQueue.fifo";

            // start sending messages every 5 seconds
            SendMessages(queueUrl, 5000);

            // start polling for messages, checking for messages every second
            await PollForMessages(queueUrl, 30, 1);

            // wait and review output
            Console.ReadLine();
        }

        public void SendMessages(string queueUrl, int interval)
        {
            // create a new timer with the interval, and on every event trigger send a message with random data
            timer = new System.Timers.Timer();
            timer.Interval = interval;
            timer.Elapsed += async (sender, e) =>
            {
                await sqs.SendMessage(queueUrl, JsonConvert.SerializeObject(new
                {
                    Test = "Test string" + new Random().Next(),
                    Test2 = new Random().Next()
                }), true);
            };

            // start the timer
            timer.Start();
        }

        public async Task PollForMessages(string queueUrl, int seconds, int waitTimeSeconds)
        {
            // get right now
            var now = DateTime.UtcNow;

            // compare it with the current time, and if it's more than the specified seconds quit processing messages
            while((DateTime.UtcNow - now).TotalSeconds < seconds)
            {
                // get the message
                var message = await sqs.GetMessage(queueUrl, waitTimeSeconds);

                // if there is one, process it
                if (message != null)
                {
                    // process the message
                    Console.WriteLine($"Processing message {message.MessageId}: {JObject.FromObject(JsonConvert.DeserializeObject(message.Body))["Test"]}");
                    await sqs.DeleteMessage(queueUrl, message.ReceiptHandle);
                }
            }

            // stop the timer
            timer?.Stop();
            timer?.Dispose();
        }
    }
}

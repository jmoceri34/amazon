using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Amazon
{
    public interface ISqs
    {
        Task SendMessage(string queueUrl, string jsonBody, bool deduplicate);
        Task<Message> GetMessage(string queueUrl, int waitTimeSeconds);
        Task DeleteMessage(string queueUrl, string receiptHandle);
    }

    public class Sqs : ISqs
    {
        private readonly IAmazonSQS sqsClient;

        public Sqs(IAmazonSQS sqsClient)
        {
            this.sqsClient = sqsClient;
        }

        public async Task SendMessage(string queueUrl, string jsonBody, bool deduplicate)
        {
            Console.WriteLine("Sending message...");
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = jsonBody,
                // FIFO needs a message group id. It sends and receives messages by group ids in the right order, but don't expect 
                // different message groups messages to be in order, more details can be found here https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/FIFO-queues-understanding-logic.html
                MessageGroupId = "TestQueueGroupId2",
            };

            // unless content-based deduplication is enabled, we need to generate a unique message deduplication id
            // we use a sha512 hash combined with the current unix time which is more than enough
            if (deduplicate)
            {
                var unixTimestamp = DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

                var stringToHash = jsonBody + unixTimestamp;
                var bytes = Encoding.UTF8.GetBytes(stringToHash);

                using (var sha512 = SHA512.Create())
                {
                    var hashBytes = sha512.ComputeHash(bytes);
                    var result = Convert.ToBase64String(hashBytes);
                    sendMessageRequest.MessageDeduplicationId = result;
                }
            }

            // send the message
            var response = await sqsClient.SendMessageAsync(sendMessageRequest);

            // throw an error if it's not 200
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Error sending request");
            }
        }

        public async Task<Message> GetMessage(string queueUrl, int waitTimeSeconds)
        {
            Console.WriteLine("Checking for a message...");
            var request = new ReceiveMessageRequest
            {
                WaitTimeSeconds = waitTimeSeconds, // this determines how long it should wait for a message, if a message arrives before this time, it will return immediately
                MaxNumberOfMessages = 1, // we only want one message for this demo
                QueueUrl = queueUrl,
                VisibilityTimeout = 20 // this is the time to process the message, including deleting it, it's receipt handle will expire after this time
            };

            // get the message(s)
            var messages = (await sqsClient.ReceiveMessageAsync(request)).Messages;
            return messages?.FirstOrDefault(); // null or message
        }

        public async Task DeleteMessage(string queueUrl, string receiptHandle)
        {
            var request = new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle // the VisibilityTimeout for the message will cause this to expire, the message must be processed before that
            };

            // delete the message
            var response = await sqsClient.DeleteMessageAsync(request);

            // throw an error if it's not 200
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"({receiptHandle})-Error processing delete message request");
            }
        }
    }
}

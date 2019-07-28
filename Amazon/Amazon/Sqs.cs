using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Amazon
{
    public interface ISqs
    {
        void SendMessage(string jsonBody, bool deduplicate = false);
        Message GetMessage();
        void DeleteMessage(string receiptHandle);
    }
    public class Sqs : ISqs
    {
        private readonly AmazonSQSClient sqsClient;
        private readonly string queueUrl;

        public Sqs(string queueUrl, AmazonSQSClient sqsClient)
        {
            if (string.IsNullOrWhiteSpace(queueUrl))
            {
                throw new ArgumentException("Queue url cannot be null", nameof(queueUrl));
            }

            this.sqsClient = sqsClient ?? throw new ArgumentException("AmazonSQSClient cannot be null", nameof(sqsClient));
            this.queueUrl = queueUrl;
        }

        public void SendMessage(string jsonBody, bool deduplicate = false)
        {
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = jsonBody,
                MessageGroupId = "{MessageGroupId}"
            };

            if (deduplicate)
            {
                var unixTimestamp = DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

                var stringToHash = jsonBody + unixTimestamp;
                var bytes = Encoding.UTF8.GetBytes(stringToHash);
                using (var sha512 = new SHA512Managed())
                {
                    var hashBytes = sha512.ComputeHash(bytes);
                    var result = Convert.ToBase64String(hashBytes);
                    sendMessageRequest.MessageDeduplicationId = result;
                }
            }

            var response = sqsClient.SendMessage(sendMessageRequest);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Error sending request");
            }
        }

        public Message GetMessage()
        {
            Console.WriteLine("Checking for a message...");
            var request = new ReceiveMessageRequest
            {
                WaitTimeSeconds = 1,
                MaxNumberOfMessages = 1,
                QueueUrl = queueUrl,
                VisibilityTimeout = 300 // wait five minutes if there's an issue processing the message
            };

            var messages = sqsClient.ReceiveMessage(request).Messages;
            return messages?.FirstOrDefault(); // null or message
        }

        public void DeleteMessage(string receiptHandle)
        {
            var request = new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle
            };

            var response = sqsClient.DeleteMessage(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"({receiptHandle})-Error processing delete message request");
            }
        }
    }
}

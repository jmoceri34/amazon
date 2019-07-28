using Amazon.SQS;
using System.Configuration;

namespace Amazon
{
    class Program
    {
        static void Main(string[] args)
        {
            var accessKey = ConfigurationManager.AppSettings["AmazonAccessKey"];
            var accessSecretKey = ConfigurationManager.AppSettings["AmazonSecretKey"];
            var sqsClient = new AmazonSQSClient(accessKey, accessSecretKey, RegionEndpoint.USEast1);

            var queueUrl = ConfigurationManager.AppSettings["AmazonQueueUrl"];
            ISqs sqs = new Sqs(queueUrl, sqsClient);
        }
    }
}

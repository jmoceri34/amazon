using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace Amazon
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var app = serviceProvider.GetService<App>();
            
            await app.Run();
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // add options
            var awsOptions = Configuration.GetAWSOptions();

            // get the key and secret from appsettings.json or environment
            var key = Configuration["Settings:AmazonAccessKey"];
            var secret = Configuration["Settings:AmazonSecretKey"];

            // create aws credentials
            awsOptions.Credentials = new BasicAWSCredentials(key, secret);
            
            // you can use the service url or the region
            awsOptions.DefaultClientConfig.ServiceURL = "https://sqs-fips.us-east-1.amazonaws.com";
            //awsOptions.Region = RegionEndpoint.USEast1;

            services.AddOptions();
            services.Configure<ApplicationOptions>(Configuration.GetSection("Settings"));

            // add app and services
            services.AddTransient<App>();

            services.AddDefaultAWSOptions(awsOptions);
            // use the extension from AWSSDK.Extensions.NETCore.Setup with IAmazonSQS
            services.AddAWSService<IAmazonSQS>();
            services.AddTransient<ISqs, Sqs>();
        }
    }
}

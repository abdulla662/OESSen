using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.ILM;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Text;

namespace OES.Services.Services
{
    public class MinioPayloadStorageService : IPayloadStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly MinioSettings _minioSettings;

        public MinioPayloadStorageService(IConfiguration configuration)
        {
            _minioSettings = configuration.GetSection(MiscConstants.MinioSection).Get<MinioSettings>();

            _bucketName = _minioSettings.BucketName;

            _minioClient = new MinioClient()
                .WithEndpoint(_minioSettings.Endpoint)
                .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
                .WithSSL(false)
                .Build();

            try
            {
                EnsureBucketExistsAsync().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw new Exception(Resource.CannotConnectToServer)!;
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);

            bool found = await _minioClient.BucketExistsAsync(beArgs);

            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
            }

            if (_minioSettings.AutoPurge.Enabled)
            {
                await ApplyLifecyclePolicyAsync();
            }
        }

        private async Task ApplyLifecyclePolicyAsync()
        {
            var rule = new LifecycleRule
            {
                ID = MiscConstants.MinioAutoPurgeRuleId,
                Status = _minioSettings.AutoPurge.Enabled ? MiscConstants.MinioLifecycleEnabledStatus : MiscConstants.MinioLifecycleDisabledStatus,
                Filter = new RuleFilter
                {
                    Prefix = _minioSettings.AutoPurge.Prefix
                },
                Expiration = new Expiration
                {
                    Days = _minioSettings.AutoPurge.ExpireAfterDays
                }
            };

            var config = new LifecycleConfiguration
            {
                Rules = [rule]
            };

            await _minioClient.SetBucketLifecycleAsync(
                new SetBucketLifecycleArgs()
                    .WithBucket(_bucketName)
                    .WithLifecycleConfiguration(config)
            );
        }

        public async Task<string> WritePayloadAsync(string fileName, string jsonContent)
        {
            var contentBytes = Encoding.UTF8.GetBytes(jsonContent);

            await using var stream = new MemoryStream(contentBytes);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(MiscConstants.ApplicationJsonContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            return fileName;
        }

        public async Task<string> ReadPayloadAsync(string fileName)
        {
            await using var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithCallbackStream(async (stream, cancellationToken) => await stream.CopyToAsync(memoryStream, cancellationToken));

            await _minioClient.GetObjectAsync(getObjectArgs);

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        public async Task DeletePayloadAsync(string fileName)
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
        }

        public async Task<Stream> GetPayloadStreamAsync(string fileName)
        {
            var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
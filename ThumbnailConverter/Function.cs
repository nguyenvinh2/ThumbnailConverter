using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
// https://www.grapecity.com/blogs/create-a-thumbnail-image-using-documents-for-imaging
using GrapeCity.Documents.Imaging;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ThumbnailConverter
{
  public class Function
  {
    IAmazonS3 S3Client { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
      S3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
      this.S3Client = s3Client;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
      foreach (var message in evnt.Records)
      {
        await ProcessMessageAsync(message, context);
      }
    }
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
      context.Logger.LogLine($"Processed message {message.Body}");
      await ImageHandler(message.Body);
    }

    public async Task<string> ImageHandler(string key)
    {
      var response = await this.S3Client.GetObjectMetadataAsync("taskmaster-storage", key);
      try
      {
        using (GetObjectResponse responseObject = await S3Client.GetObjectAsync(
            "taskmaster-storage", key))
        {
          using (Stream responseStream = responseObject.ResponseStream)
          {
            using (StreamReader reader = new StreamReader(responseStream))
            {
              using (var memoryStream = new MemoryStream())
              {
                var buffer = new byte[512];
                var bytesRead = default(int);
                while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                  memoryStream.Write(buffer, 0, bytesRead);

                var thumbnail = Thumbnail.ConvertToThumbnail(memoryStream.ToArray());

                using (var outputStream = new MemoryStream())
                {
                  thumbnail.SaveAsJpeg(outputStream);
                  PutObjectRequest putRequest = new PutObjectRequest()
                  {
                    BucketName = "taskmaster-thumbnail",
                    Key = $"thumbnail-{key}",
                    ContentType = response.Headers.ContentType,
                    InputStream = outputStream
                  };

                  await S3Client.PutObjectAsync(putRequest);
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        throw;
      }
       return response.Headers.ContentType;
    }
  }

  public class Thumbnail
  {
    public static GcBitmap ConvertToThumbnail(byte[] imageStream)
    {
      using (var image = new GcBitmap())
      {
        image.Load(imageStream);

        var resizeImage = image.Resize(50, 50, InterpolationMode.Linear);
        return resizeImage;
      }
    }
  }
}

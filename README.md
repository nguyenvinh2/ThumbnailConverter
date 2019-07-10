# Lambda Function

Front-End Application:

http://taskmaster-interface.s3-website-us-west-2.amazonaws.com/


## Introduction:

This is a .NET Core 2.1 Lambda function thats gets trigger upon an image upload
to one S3 bucket and creates a resized image to another bucket.

## Requirements

- Created Using Visual Studios 2017 AWS Lambda function
- Deployed within Visual Studios with AWS plugin
- Uses GrapeCity imaging library

## Instructions
-  Clone repo locally
-  Within Visual Studios, right click on project and click deploy AWS Lambda (this requires AWS Toolkit)
-  select the appropriate role with lambda access and deploy
-  Create an event in the targeted S3 bucket to trigger the lambda function

## Notes

Grape City Library not working properly, uploading to target bucket with String

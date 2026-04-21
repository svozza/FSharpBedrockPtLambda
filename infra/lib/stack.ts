import * as cdk from "aws-cdk-lib";
import * as lambda from "aws-cdk-lib/aws-lambda";
import * as apigateway from "aws-cdk-lib/aws-apigateway";
import * as dynamodb from "aws-cdk-lib/aws-dynamodb";
import { Construct } from "constructs";
import { execSync } from "child_process";
import * as path from "path";

const lambdaDir = path.join(__dirname, "../../src/Lambda");

export class FSharpBedrockPtLambdaStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const table = new dynamodb.Table(this, "ItemsTable", {
      partitionKey: { name: "id", type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
    });

    const fn = new lambda.Function(this, "FSharpLambda", {
      runtime: new lambda.Runtime("dotnet10", lambda.RuntimeFamily.DOTNET_CORE),
      architecture: lambda.Architecture.ARM_64,
      handler:
        "FSharpBedrockPtLambda::FSharpBedrockPtLambda.Handler::FunctionHandler",
      code: lambda.Code.fromAsset(lambdaDir, {
        bundling: {
          image: lambda.Runtime.DOTNET_8.bundlingImage, // unused, local bundling takes precedence
          local: {
            tryBundle(outputDir: string) {
              execSync(
                `dotnet publish -c Release -r linux-arm64 --self-contained false -o "${outputDir}"`,
                { cwd: lambdaDir, stdio: "inherit" },
              );
              return true;
            },
          },
        },
      }),
      memorySize: 512,
      timeout: cdk.Duration.seconds(30),
      tracing: lambda.Tracing.ACTIVE,
      environment: {
        TABLE_NAME: table.tableName,
        POWERTOOLS_SERVICE_NAME: "FSharpBedrockPtLambda",
        POWERTOOLS_LOG_LEVEL: "Info",
        POWERTOOLS_METRICS_NAMESPACE: "FSharpBedrockPtLambda",
      },
    });

    table.grantReadWriteData(fn);

    const api = new apigateway.RestApi(this, "ItemsApi", {
      restApiName: "FSharpBedrockPtLambda",
    });

    const integration = new apigateway.LambdaIntegration(fn);

    const items = api.root.addResource("items");
    items.addMethod("GET", integration);
    items.addMethod("POST", integration);

    const singleItem = items.addResource("{id}");
    singleItem.addMethod("GET", integration);

    new cdk.CfnOutput(this, "ApiUrl", { value: api.url });
    new cdk.CfnOutput(this, "TableName", { value: table.tableName });
  }
}

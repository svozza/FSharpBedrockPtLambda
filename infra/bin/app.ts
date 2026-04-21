#!/usr/bin/env npx ts-node
import * as cdk from "aws-cdk-lib";
import { FSharpBedrockPtLambdaStack } from "../lib/stack";

const app = new cdk.App();
new FSharpBedrockPtLambdaStack(app, "FSharpBedrockPtLambdaStack", {
  env: {
    account: process.env.CDK_DEFAULT_ACCOUNT,
    region: "eu-west-1",
  },
});

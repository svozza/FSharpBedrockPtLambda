namespace FSharpBedrockPtLambda

open System.Text.Json.Serialization
open Amazon.DynamoDBv2.DataModel

[<DynamoDBTable("placeholder")>]
[<CLIMutable>]
type Item =
    { [<DynamoDBHashKey("id")>]
      [<JsonPropertyName("id")>]
      Id: string
      [<DynamoDBProperty("name")>]
      [<JsonPropertyName("name")>]
      Name: string
      [<DynamoDBProperty("description")>]
      [<JsonPropertyName("description")>]
      Description: string }

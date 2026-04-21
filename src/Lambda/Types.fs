namespace FSharpBedrockPtLambda

open System.Text.Json.Serialization

[<CLIMutable>]
type Item =
    { [<JsonPropertyName("id")>]
      Id: string
      [<JsonPropertyName("name")>]
      Name: string
      [<JsonPropertyName("description")>]
      Description: string }

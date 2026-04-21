namespace FSharpBedrockPtLambda

open System
open System.Text.Json
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DataModel
open Amazon.Lambda.Core
open Amazon.Lambda.APIGatewayEvents
open AWS.Lambda.Powertools.Logging
open AWS.Lambda.Powertools.Metrics

[<assembly: LambdaSerializer(typeof<Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer>)>]
do ()

type Handler() =
    let client = new AmazonDynamoDBClient()
    let tableName = Environment.GetEnvironmentVariable("TABLE_NAME")
    let context = DynamoDBContextBuilder().WithDynamoDBClient(fun () -> client).Build()
    let jsonOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    let response statusCode body =
        APIGatewayProxyResponse(
            StatusCode = statusCode,
            Headers = dict [ "Content-Type", "application/json" ],
            Body = body
        )

    let jsonResponse statusCode msg =
        response statusCode (JsonSerializer.Serialize({| message = msg |}, jsonOptions))

    let handleGet (request: APIGatewayProxyRequest) =
        async {
            match request.PathParameters
                  |> Option.ofObj
                  |> Option.bind (fun p -> if p.ContainsKey("id") then Some p["id"] else None)
            with
            | Some id ->
                let! item = DynamoDb.getItem context tableName id
                match item with
                | Some i -> return response 200 (JsonSerializer.Serialize(i, jsonOptions))
                | None -> return jsonResponse 404 "Item not found"
            | None ->
                let! items = DynamoDb.getAllItems context tableName
                return response 200 (JsonSerializer.Serialize(items, jsonOptions))
        }

    let handlePost (request: APIGatewayProxyRequest) =
        async {
            let item = JsonSerializer.Deserialize<Item>(request.Body, jsonOptions)

            let itemWithId =
                if String.IsNullOrEmpty(item.Id) then
                    { item with Id = Guid.NewGuid().ToString() }
                else
                    item

            do! DynamoDb.putItem context tableName itemWithId
            return response 201 (JsonSerializer.Serialize(itemWithId, jsonOptions))
        }

    let route (request: APIGatewayProxyRequest) =
        match request.HttpMethod.ToUpperInvariant() with
        | "GET" -> handleGet request
        | "POST" -> handlePost request
        | m -> async { return jsonResponse 405 $"Method {m} not allowed" }

    member _.FunctionHandler(request: APIGatewayProxyRequest, _context: ILambdaContext) : APIGatewayProxyResponse =
        try
            try
                Logger.LogInformation($"Received {request.HttpMethod} {request.Path}")
                Metrics.AddMetric("Invocations", 1.0, MetricUnit.Count)
                Powertools.withPowertools request.HttpMethod route request
                |> Async.RunSynchronously
            with ex ->
                Logger.LogError($"Unhandled exception: {ex.Message}")
                jsonResponse 500 "Internal server error"
        finally
            Powertools.flushMetrics ()

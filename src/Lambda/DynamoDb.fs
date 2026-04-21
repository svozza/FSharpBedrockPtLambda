module FSharpBedrockPtLambda.DynamoDb

open Amazon.DynamoDBv2.DataModel

let putItem (context: IDynamoDBContext) (tableName: string) (item: Item) =
    context.SaveAsync(item, SaveConfig(OverrideTableName = tableName)) |> Async.AwaitTask

let getItem (context: IDynamoDBContext) (tableName: string) (id: string) =
    async {
        let! item = context.LoadAsync<Item>(id, LoadConfig(OverrideTableName = tableName)) |> Async.AwaitTask
        return Option.ofObj item
    }

let getAllItems (context: IDynamoDBContext) (tableName: string) =
    async {
        let! items = context.ScanAsync<Item>([], ScanConfig(OverrideTableName = tableName)).GetRemainingAsync() |> Async.AwaitTask
        return Seq.toList items
    }

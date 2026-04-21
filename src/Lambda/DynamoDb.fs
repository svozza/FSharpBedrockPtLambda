#nowarn "3536"

module FSharpBedrockPtLambda.DynamoDb

open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.Model
open System.Collections.Generic

let private toAttributeMap (item: Item) : Dictionary<string, AttributeValue> =
    let m = Dictionary<string, AttributeValue>()
    m["id"] <- AttributeValue(S = item.Id)
    m["name"] <- AttributeValue(S = item.Name)
    m["description"] <- AttributeValue(S = item.Description)
    m

let private fromAttributeMap (m: Dictionary<string, AttributeValue>) : Item =
    { Id = m["id"].S
      Name = m["name"].S
      Description = m["description"].S }

let putItem (client: IAmazonDynamoDB) (tableName: string) (item: Item) =
    async {
        let req = PutItemRequest(TableName = tableName, Item = toAttributeMap item)
        let! _ = client.PutItemAsync(req) |> Async.AwaitTask
        return ()
    }

let getItem (client: IAmazonDynamoDB) (tableName: string) (id: string) =
    async {
        let key = Dictionary<string, AttributeValue>()
        key["id"] <- AttributeValue(S = id)
        let req = GetItemRequest(TableName = tableName, Key = key)
        let! resp = client.GetItemAsync(req) |> Async.AwaitTask
        if resp.IsItemSet then return Some(fromAttributeMap resp.Item)
        else return None
    }

let getAllItems (client: IAmazonDynamoDB) (tableName: string) =
    async {
        let req = ScanRequest(TableName = tableName)
        let! resp = client.ScanAsync(req) |> Async.AwaitTask
        return resp.Items |> Seq.map fromAttributeMap |> Seq.toList
    }
